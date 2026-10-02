using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ChessEngine
{
    // Bot v21: v18 with a better transposition table. The search itself is unchanged.
    // - Buckets: the hash selects a bucket of 4 entries (64 bytes, the size of a cache line); a probe checks all 4.
    // - Compact 16-byte entries: 32 bits of the hash for verification (the bucket index comes from the other 32),
    //   16-bit score, 16-bit static evaluation, 16-bit packed move, 8-bit depth, 8 bits of bound + generation.
    // - Generation (age): incremented at the start of every search. Replacement in a full bucket evicts the entry
    //   with the lowest depth - 8 * age, so old entries from earlier moves go first, then shallow ones.
    // - The static evaluation is stored and reused instead of evaluating the position again.
    // - Quiescence uses the table too: it probes (cutoffs, move ordering) and stores its results with depth 0.
    // - Size configurable through HashMb (UCI option "Hash"), 32 MB by default.
    public class Bot_v21 : BotBase, ITimedBot, IHashSizeBot
    {
        private const int MaxPly = 64;

        // Null move: depth reduction and the minimum remaining depth where it's tried
        private const int NullMoveReduction = 2;
        private const int NullMoveMinDepth = 3;

        // LMR: moves from this index on (0-based) at this remaining depth or more can be reduced
        private const int LmrMinMoveIndex = 3;
        private const int LmrMinDepth = 3;

        // Reverse futility pruning: remaining depth up to this, margin per ply of depth
        private const int ReverseFutilityMaxDepth = 3;
        private const int ReverseFutilityMargin = 120;

        // Futility pruning: margin by remaining depth (index 1 and 2)
        private static readonly int[] FutilityMargin = { 0, 150, 300 };

        // Delta pruning: safety margin on top of the captured piece's value
        private const int DeltaMargin = 200;

        // Ordering scores (only the relative order matters):
        // PV move > table move > captures/promotions and killers > quiet moves by history
        private const int PvMoveBonus = 100_000_000;
        private const int TableMoveBonus = 99_999_999;
        private const int CaptureBase = 10_000_000;              // + MVV-LVA (PxP = 900)
        private const int FirstKillerBonus = CaptureBase + 800;  // below equal/winning captures, above quiet moves
        private const int SecondKillerBonus = CaptureBase + 700;

        // History values stay far below CaptureBase: when one gets this big, all of them are halved
        private const int HistoryLimit = 1_000_000;

        // history[side, from, to]: how useful the quiet move has been in this search (cutoffs, weighted by depth)
        private readonly int[,,] history = new int[2, 64, 64];

        // What a stored score means with alpha-beta:
        // Exact - the real score; LowerBound - at least this (search stopped on a cutoff);
        // UpperBound - at most this (no move beat alpha). None marks an empty entry (a cleared table is all None).
        private enum Bound : byte { None, Exact, LowerBound, UpperBound }

        // 16 bytes, so a bucket of 4 is 64 bytes. (.NET doesn't guarantee that the array starts on a cache line
        // boundary, so a bucket may still straddle two lines - but never more.)
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        private struct TableEntry
        {
            public uint Key;          // upper 32 bits of the hash (the lower 32 bits chose the bucket)
            public short Score;       // PackScore of the table score (mate scores relative to this node)
            public short StaticEval;  // Evaluate() of the position, NoEval if unknown (in check)
            public ushort Move;       // PackMove of the best move, 0 = none
            public byte Depth;        // how many plies deep the score was searched (0 = quiescence)
            public byte BoundAndAge;  // bits 0-1: Bound, bits 2-7: generation of the search that wrote it
        }

        private const int BucketSize = 4;
        private const int BucketBytes = 64;
        private const int BoundMask = 3;
        private const int GenerationShift = 2;
        private const int GenerationMask = 63;   // generations wrap around after 64 searches

        // Same key but the new result is shallower: only overwrite it if it's not much shallower
        private const int SameKeyDepthMargin = 2;

        // Replacement: an entry this many generations old is worth one ply of depth less... times 8
        private const int AgeWeight = 8;

        public const int DefaultHashMb = 32;
        public const int MaxHashMb = 1024;

        private TableEntry[] table;
        private int hashMb;
        private byte generation;

        private readonly int depth;

        // Two killer moves per ply
        private readonly Move[,] killers = new Move[MaxPly, 2];

        // Triangular principal variation table: pv[ply, ...] is the best line found from `ply`
        private readonly Move[,] pv = new Move[MaxPly, MaxPly];
        private readonly int[] pvLength = new int[MaxPly];

        // Principal variation of the previous iteration, tried first in the next one
        private readonly Move[] previousPv = new Move[MaxPly];
        private int previousPvLength;

        // Number of moves already played in the game when the current search started
        private int searchStart;

        // Time management
        private readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private int currentDepth;
        private long nodes;
        private bool timeUp;

        public int MoveTimeMs { get; set; } = 1000;
        public int LastDepth { get; private set; }

        // Size of the transposition table in megabytes. Changing it allocates a new (empty) table.
        public int HashMb
        {
            get => hashMb;
            set
            {
                int mb = Math.Max(1, Math.Min(MaxHashMb, value));
                if (mb == hashMb && table != null) return;
                hashMb = mb;
                long buckets = (long)mb * 1024 * 1024 / BucketBytes;
                table = new TableEntry[buckets * BucketSize];
            }
        }

        // Reused for every node at a given ply: generated moves and their ordering scores
        private readonly List<Move>[] moveLists = new List<Move>[MaxPly];
        private readonly int[][] moveScores = new int[MaxPly][];
        private readonly List<Move> scratchMoves = new List<Move>(256);

        // depth: maximum search depth; normally the time budget stops the search earlier
        public Bot_v21(int depth = 64)
        {
            this.depth = Math.Min(depth, MaxPly - 1);
            HashMb = DefaultHashMb;
            for (int ply = 0; ply < MaxPly; ply++)
            {
                moveLists[ply] = new List<Move>(256);
                moveScores[ply] = new int[256];   // more than the maximum number of moves in a position (218)
            }
        }

        // The table is kept between moves: positions from the previous search often come back.
        // The UCI tool calls this on "ucinewgame".
        public void ClearTable()
        {
            Array.Clear(table, 0, table.Length);
        }

        public override Move ChooseMove(Position position)
        {
            var moves = position.GetLegalMoves();
            if (moves.Count == 0)
            {
                throw new InvalidOperationException("No legal moves available for the bot.");
            }

            LastDepth = 0;
            if (OpeningBook.Default != null && OpeningBook.Default.TryGetMove(position, rand, out Move bookMove))
            {
                return bookMove;
            }

            // New search: entries written from now on are "young", older ones are the first to be replaced
            generation = (byte)((generation + 1) & GenerationMask);

            Array.Clear(killers, 0, killers.Length);
            // Keep what was learned in the previous move's search, but let the new one outweigh it
            AgeHistory();
            searchStart = position.MovesPlayed;
            previousPvLength = 0;
            List<Move> bestMoves = null;

            watch.Restart();
            timeUp = false;
            nodes = 0;

            for (currentDepth = 1; currentDepth <= depth; currentDepth++)
            {
                List<Move> result = SearchRoot(position, moves, currentDepth);

                // Time ran out in the middle of this depth: its result is incomplete, keep the previous one
                if (timeUp) break;

                bestMoves = result;
                LastDepth = currentDepth;

                // Remember the best line for ordering in the next iteration
                previousPvLength = pvLength[0];
                for (int i = 0; i < previousPvLength; i++)
                {
                    previousPv[i] = pv[0, i];
                }

                // The next depth usually takes several times longer than this one: don't start it
                // if more than half of the budget is already gone, it wouldn't finish anyway
                if (watch.ElapsedMilliseconds * 2 > MoveTimeMs) break;
            }

            return bestMoves[rand.Next(bestMoves.Count)];
        }

        // Called in every node; checks the clock every 2048 nodes (reading it is relatively slow).
        // Depth 1 is never interrupted, so there is always a move to play.
        private bool OutOfTime()
        {
            if (!timeUp && currentDepth > 1 && (++nodes & 2047) == 0 && watch.ElapsedMilliseconds >= MoveTimeMs)
            {
                timeUp = true;
            }
            return timeUp;
        }

        // Searches all root moves at the given depth and returns the moves with the best score
        private List<Move> SearchRoot(Position position, List<Move> moves, int currentDepth)
        {
            Move pvMove = previousPvLength > 0 ? previousPv[0] : default;
            SortMoves(position, moves, 0, pvMove, default);

            int bestScore = -Infinity;
            var bestMoves = new List<Move>();
            pvLength[0] = 0;

            foreach (var move in moves)
            {
                position.MakeMove(move);
                // Window one point below the best score: moves that tie with the best get an exact score
                int score = -Negamax(position, currentDepth - 1, 1, -Infinity, -(bestScore - 1), move == pvMove, true);
                position.UnmakeMove();

                if (timeUp) break;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                    UpdatePv(0, move);
                }
                else if (score == bestScore)
                {
                    bestMoves.Add(move);
                }
            }

            return bestMoves;
        }

        // Score of the position searched `depth` plies ahead, from the point of view of the side to move.
        // ply: distance from the root. onPv: whether this node lies on the previous iteration's best line.
        // allowNull: false right after a null move (two passes in a row would prove nothing).
        private int Negamax(Position position, int depth, int ply, int alpha, int beta, bool onPv, bool allowNull)
        {
            pvLength[ply] = ply;

            // Out of time: the value doesn't matter, the whole iteration will be thrown away
            if (OutOfTime()) return 0;

            // Draw by repetition or by the 50-move rule. Checked before the transposition table:
            // a stored score doesn't know the path that led here, so it can't see repetitions.
            if (position.IsRepetition(searchStart) || position.HalfmoveClock >= 100)
            {
                return Math.Max(alpha, Math.Min(beta, 0));
            }

            // Extensions can make a line longer than the arrays indexed by ply
            if (ply >= MaxPly - 1)
            {
                return Math.Max(alpha, Math.Min(beta, Evaluate(position)));
            }

            // Check extension: in check, search one ply deeper (also keeps us out of quiescence while in check)
            bool inCheck = position.InCheck;
            if (inCheck) depth++;

            if (depth == 0)
            {
                // Don't stop in the middle of an exchange: play out the captures first
                return Quiescence(position, ply, alpha, beta);
            }

            // Look the position up in the transposition table
            TableEntry[] table = this.table;
            ulong key = position.Hash;
            Move tableMove = default;
            int tableEval = NoEval;
            int slot = Probe(table, key);
            if (slot >= 0)
            {
                ref TableEntry entry = ref table[slot];
                tableMove = UnpackMove(entry.Move);
                tableEval = entry.StaticEval;

                // Only reuse the score if it was searched at least as deep as we need now
                if (entry.Depth >= depth)
                {
                    int stored = ScoreFromTable(UnpackScore(entry.Score), ply);
                    Bound bound = (Bound)(entry.BoundAndAge & BoundMask);
                    if (bound == Bound.Exact) return Math.Max(alpha, Math.Min(beta, stored));
                    if (bound == Bound.LowerBound && stored >= beta) return beta;
                    if (bound == Bound.UpperBound && stored <= alpha) return alpha;
                }
            }

            // Static score, used by the pruning below (not meaningful in check). Taken from the table if stored there.
            int staticEval = inCheck ? -Infinity : tableEval != NoEval ? tableEval : Evaluate(position);
            int evalToStore = inCheck ? NoEval : staticEval;
            bool nearMate = alpha <= -MateThreshold || beta >= MateThreshold;

            // Reverse futility pruning: we are so far above beta that even a bad move won't drop us below it
            if (!onPv && !inCheck && !nearMate && depth <= ReverseFutilityMaxDepth
                && staticEval - ReverseFutilityMargin * depth >= beta)
            {
                return beta;
            }

            // Null move pruning. Skipped near mate scores: "passing" says nothing about forced mates.
            if (allowNull && !onPv && !inCheck && depth >= NullMoveMinDepth && beta < MateThreshold
                && HasPieces(position, position.SideToMove) && staticEval >= beta)
            {
                position.MakeNullMove();
                int nullScore = -Negamax(position, depth - 1 - NullMoveReduction, ply + 1, -beta, -beta + 1, false, false);
                position.UnmakeNullMove();

                if (timeUp) return 0;
                if (nullScore >= beta) return beta;
            }

            var moves = moveLists[ply];
            position.GenerateLegalMovesFast(moves, scratchMoves);
            if (moves.Count == 0)
            {
                // No legal moves: in check means checkmate (side to move lost), otherwise stalemate (draw).
                // The closer to the root the mate is, the bigger the score: faster mates are preferred.
                return inCheck ? -(MateScore - ply) : 0;
            }

            Move pvMove = onPv && ply < previousPvLength ? previousPv[ply] : default;
            SortMoves(position, moves, ply, pvMove, tableMove);

            int originalAlpha = alpha;
            Move bestMove = tableMove;

            // Futility pruning: so far below alpha that a quiet move can't help (decided once for the node)
            bool futile = !onPv && !inCheck && !nearMate && depth < FutilityMargin.Length
                          && staticEval + FutilityMargin[depth] <= alpha;

            for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
            {
                Move move = moves[moveIndex];
                bool quiet = !move.IsCapture && !move.IsPromotion;
                bool killer = move == killers[ply, 0] || move == killers[ply, 1];

                position.MakeMove(move);
                bool givesCheck = position.InCheck;

                // Futility: skip quiet moves that don't give check (never the first move, so something is searched)
                if (futile && quiet && !givesCheck && moveIndex > 0)
                {
                    position.UnmakeMove();
                    continue;
                }

                int score;
                if (moveIndex == 0)
                {
                    // First (expected best) move: full window. The window flips for the opponent.
                    score = -Negamax(position, depth - 1, ply + 1, -beta, -alpha, onPv && move == pvMove, true);
                }
                else
                {
                    // Late quiet move: also searched shallower (LMR). The later, the bigger the reduction.
                    int reduction = 0;
                    if (quiet && !killer && !inCheck && !givesCheck && moveIndex >= LmrMinMoveIndex && depth >= LmrMinDepth)
                    {
                        reduction = moveIndex >= 8 && depth >= 6 ? 2 : 1;
                    }

                    // Null window: we only ask whether the move is better than alpha
                    score = -Negamax(position, depth - 1 - reduction, ply + 1, -alpha - 1, -alpha, false, true);

                    // Better than alpha after a reduced search: verify at full depth, still with the null window
                    if (score > alpha && reduction > 0 && !timeUp)
                    {
                        score = -Negamax(position, depth - 1, ply + 1, -alpha - 1, -alpha, false, true);
                    }

                    // Really better, and the window was wider than a null window: get its exact score
                    if (score > alpha && score < beta && !timeUp)
                    {
                        score = -Negamax(position, depth - 1, ply + 1, -beta, -alpha, false, true);
                    }
                }
                position.UnmakeMove();

                // Don't let an interrupted search write wrong scores into the transposition table
                if (timeUp) return 0;

                if (score >= beta)
                {
                    StoreKiller(ply, move);
                    if (quiet) AddHistory(position.SideToMove, move, depth);
                    Store(table, key, depth, ScoreToTable(beta, ply), Bound.LowerBound, move, evalToStore);
                    return beta; // cutoff: the opponent won't allow this position
                }
                if (score > alpha)
                {
                    alpha = score;
                    bestMove = move;
                    UpdatePv(ply, move);
                }
            }

            Store(table, key, depth, ScoreToTable(alpha, ply), alpha > originalAlpha ? Bound.Exact : Bound.UpperBound,
                  bestMove, evalToStore);
            return alpha;
        }

        // Searches only captures and promotions until the position is quiet, then evaluates it.
        // Score from the point of view of the side to move, same alpha-beta rules as Negamax.
        private int Quiescence(Position position, int ply, int alpha, int beta)
        {
            if (OutOfTime()) return 0;

            if (ply >= MaxPly - 1)
            {
                return Evaluate(position);
            }

            // Transposition table: any stored entry was searched at least as deep as quiescence (depth 0)
            TableEntry[] table = this.table;
            ulong key = position.Hash;
            Move tableMove = default;
            int tableEval = NoEval;
            int slot = Probe(table, key);
            if (slot >= 0)
            {
                ref TableEntry entry = ref table[slot];
                tableMove = UnpackMove(entry.Move);
                tableEval = entry.StaticEval;

                int stored = ScoreFromTable(UnpackScore(entry.Score), ply);
                Bound bound = (Bound)(entry.BoundAndAge & BoundMask);
                if (bound == Bound.Exact) return Math.Max(alpha, Math.Min(beta, stored));
                if (bound == Bound.LowerBound && stored >= beta) return beta;
                if (bound == Bound.UpperBound && stored <= alpha) return alpha;
            }

            List<Move> moves;
            bool inCheck = position.InCheck;
            int standPat = 0;
            int originalAlpha = alpha;

            if (inCheck)
            {
                // In check there is no "doing nothing": every legal move (evasion) has to be considered,
                // and having none means checkmate
                moves = moveLists[ply];
                position.GenerateLegalMovesFast(moves, scratchMoves);
                if (moves.Count == 0)
                {
                    Store(table, key, 0, ScoreToTable(-(MateScore - ply), ply), Bound.Exact, default, NoEval);
                    return -(MateScore - ply);
                }
            }
            else
            {
                // Stand pat: the side to move doesn't have to capture. If the static score is already
                // good enough, the captures can only make it better for us - or we just won't make them.
                standPat = tableEval != NoEval ? tableEval : Evaluate(position);
                if (standPat >= beta)
                {
                    Store(table, key, 0, ScoreToTable(beta, ply), Bound.LowerBound, default, standPat);
                    return beta;
                }
                if (standPat > alpha)
                {
                    alpha = standPat;
                }

                // Only captures and promotions. Generating pseudo-legal moves and checking legality
                // just for those is much cheaper than full legal move generation.
                moves = moveLists[ply];
                moves.Clear();
                position.GeneratePseudoLegalCaptures(moves);
            }

            SortMoves(position, moves, ply, default, tableMove);

            int evalToStore = inCheck ? NoEval : standPat;
            Move bestMove = default;
            Side us = position.SideToMove;
            foreach (var move in moves)
            {
                // Delta pruning: even winning the captured piece for free (plus a margin) wouldn't reach alpha
                if (!inCheck && !move.IsPromotion)
                {
                    int gain = move.IsEnPassant ? PieceGrades[(int)PieceType.Pawn] : PieceGrades[(int)position[move.To].Type];
                    if (standPat + gain + DeltaMargin <= alpha) continue;
                }

                position.MakeMove(move);
                // Pseudo-legal move that leaves our own king in check: illegal, skip it
                if (position.IsSquareAttacked(position.KingSquare(us), us.Opponent()))
                {
                    position.UnmakeMove();
                    continue;
                }
                int score = -Quiescence(position, ply + 1, -beta, -alpha);
                position.UnmakeMove();

                if (timeUp) return 0;

                if (score >= beta)
                {
                    Store(table, key, 0, ScoreToTable(beta, ply), Bound.LowerBound, move, evalToStore);
                    return beta;
                }
                if (score > alpha)
                {
                    alpha = score;
                    bestMove = move;
                }
            }

            // alpha above the original alpha: the exact quiescence score (stand pat or the best capture)
            Store(table, key, 0, ScoreToTable(alpha, ply), alpha > originalAlpha ? Bound.Exact : Bound.UpperBound,
                  bestMove, evalToStore);
            return alpha;
        }

        private static int Evaluate(Position position)
        {
            return Evaluation.EvaluateFull(position);
        }

        // Whether the side has anything besides the king and pawns. With only pawns, zugzwang is common
        // (every move makes things worse), so the null move assumption "a move never hurts" fails.
        private static bool HasPieces(Position position, Side side)
        {
            for (int square = 0; square < 64; square++)
            {
                Piece piece = position[square];
                if (!piece.IsEmpty && piece.Color == side && piece.Type != PieceType.Pawn && piece.Type != PieceType.King)
                {
                    return true;
                }
            }
            return false;
        }

        // Mate scores depend on the distance from the root ("mate in N from here"), but a table entry can be
        // reached again at a different ply. So they are stored as distance from this node and converted back.
        private const int MateThreshold = MateScore - 1000;

        public static int ScoreToTable(int score, int ply)
        {
            if (score > MateThreshold) return score + ply;
            if (score < -MateThreshold) return score - ply;
            return score;
        }

        public static int ScoreFromTable(int score, int ply)
        {
            if (score > MateThreshold) return score - ply;
            if (score < -MateThreshold) return score + ply;
            return score;
        }

        // Packed scores (16 bits). Table scores are ScoreToTable results: a mate is MateScore - distance from the
        // node (distance < 1000), stored as PackedMate - distance. Other scores are clamped to +/-PackedMaxScore,
        // far beyond anything the evaluation returns. Scores beyond +/-MateScore (only +/-Infinity window bounds)
        // are clamped to +/-MateScore - still a correct lower / upper bound.
        private const int PackedMate = 32000;
        private const int PackedMateThreshold = PackedMate - (MateScore - MateThreshold);
        private const int PackedMaxScore = 30000;

        // StaticEval of an entry without a static evaluation (the position was in check)
        private const int NoEval = short.MinValue;

        public static short PackScore(int score)
        {
            if (score > MateThreshold) return (short)(PackedMate - (MateScore - Math.Min(score, MateScore)));
            if (score < -MateThreshold) return (short)-(PackedMate - (MateScore - Math.Min(-score, MateScore)));
            return (short)Math.Max(-PackedMaxScore, Math.Min(PackedMaxScore, score));
        }

        public static int UnpackScore(short packed)
        {
            if (packed > PackedMateThreshold) return MateScore - (PackedMate - packed);
            if (packed < -PackedMateThreshold) return -(MateScore - (PackedMate + packed));
            return packed;
        }

        // Packed moves (16 bits): bits 0-5 from, bits 6-11 to, bits 12-15 a code for the flags and promotion:
        // 0 quiet, 1 double pawn push, 2 kingside castle, 3 queenside castle, 4 capture, 5 en passant,
        // 8-11 promotion to knight/bishop/rook/queen, 12-15 the same with a capture. These are exactly the
        // flag combinations the move generator produces, so UnpackMove(PackMove(m)) == m for every generated
        // move. A move from the table is never played directly: it's only compared with generated moves for
        // ordering, so a wrong move (after a hash collision) costs nothing. 0 (a1a1) means no move.
        public static ushort PackMove(Move move)
        {
            int code;
            if (move.IsPromotion) code = 8 | (move.IsCapture ? 4 : 0) | ((int)move.Promotion - (int)PieceType.Knight);
            else if (move.IsEnPassant) code = 5;
            else if (move.IsCapture) code = 4;
            else if ((move.Flags & MoveFlags.CastleKingside) != 0) code = 2;
            else if ((move.Flags & MoveFlags.CastleQueenside) != 0) code = 3;
            else if ((move.Flags & MoveFlags.DoublePawnPush) != 0) code = 1;
            else code = 0;
            return (ushort)(move.From | (move.To << 6) | (code << 12));
        }

        public static Move UnpackMove(ushort packed)
        {
            int from = packed & 63;
            int to = (packed >> 6) & 63;
            int code = packed >> 12;
            if ((code & 8) != 0)
            {
                MoveFlags flags = (code & 4) != 0 ? MoveFlags.Capture : MoveFlags.None;
                return new Move(from, to, flags, (PieceType)((code & 3) + (int)PieceType.Knight));
            }
            switch (code)
            {
                case 1: return new Move(from, to, MoveFlags.DoublePawnPush);
                case 2: return new Move(from, to, MoveFlags.CastleKingside);
                case 3: return new Move(from, to, MoveFlags.CastleQueenside);
                case 4: return new Move(from, to, MoveFlags.Capture);
                case 5: return new Move(from, to, MoveFlags.Capture | MoveFlags.EnPassant);
                default: return new Move(from, to);
            }
        }

        // Index of the first entry of the key's bucket. The lower 32 bits of the hash are scaled to the number
        // of buckets (multiply-shift instead of a mask, so the table size doesn't have to be a power of two).
        private static int BucketStart(TableEntry[] table, ulong key)
        {
            ulong buckets = (ulong)(table.Length / BucketSize);
            return (int)(((key & 0xFFFFFFFFUL) * buckets) >> 32) * BucketSize;
        }

        // Index of the entry holding the position, or -1. A hit marks the entry as used by the current search,
        // so it isn't replaced as an old one.
        private int Probe(TableEntry[] table, ulong key)
        {
            int start = BucketStart(table, key);
            uint check = (uint)(key >> 32);
            for (int i = start; i < start + BucketSize; i++)
            {
                ref TableEntry entry = ref table[i];
                if (entry.Key == check && (entry.BoundAndAge & BoundMask) != (int)Bound.None)
                {
                    entry.BoundAndAge = (byte)((entry.BoundAndAge & BoundMask) | (generation << GenerationShift));
                    return i;
                }
            }
            return -1;
        }

        // Replacement scheme, in this order:
        // 1. An entry with the same key is updated - unless the new result is much shallower than the stored one
        //    from this search (and not exact): then only a missing best move is filled in.
        // 2. Otherwise an empty entry is used.
        // 3. Otherwise the entry with the lowest depth - AgeWeight * age is replaced, where age is the number of
        //    searches since it was written (or last hit). Old entries from earlier moves go first, then shallow ones.
        // score is a table score (ScoreToTable).
        private void Store(TableEntry[] table, ulong key, int depth, int score, Bound bound, Move bestMove, int staticEval)
        {
            int start = BucketStart(table, key);
            uint check = (uint)(key >> 32);
            int victim = start;
            int victimValue = int.MaxValue;
            for (int i = start; i < start + BucketSize; i++)
            {
                ref TableEntry entry = ref table[i];
                int entryBound = entry.BoundAndAge & BoundMask;
                if (entryBound == (int)Bound.None)
                {
                    if (victimValue > int.MinValue)
                    {
                        victim = i;
                        victimValue = int.MinValue;
                    }
                    continue;
                }

                int age = (generation - (entry.BoundAndAge >> GenerationShift)) & GenerationMask;
                if (entry.Key == check)
                {
                    if (bound != Bound.Exact && age == 0 && depth + SameKeyDepthMargin <= entry.Depth)
                    {
                        if (entry.Move == 0) entry.Move = PackMove(bestMove);
                        return;
                    }
                    victim = i;
                    break;
                }

                int value = entry.Depth - AgeWeight * age;
                if (value < victimValue)
                {
                    victim = i;
                    victimValue = value;
                }
            }

            ref TableEntry target = ref table[victim];
            ushort packedMove = PackMove(bestMove);
            // Same position without a new best move: keep the old one for ordering
            if (packedMove == 0 && target.Key == check && (target.BoundAndAge & BoundMask) != (int)Bound.None)
            {
                packedMove = target.Move;
            }
            target.Key = check;
            target.Score = PackScore(score);
            target.StaticEval = (short)(staticEval == NoEval ? NoEval : Math.Max(-PackedMaxScore, Math.Min(PackedMaxScore, staticEval)));
            target.Move = packedMove;
            target.Depth = (byte)Math.Min(depth, byte.MaxValue);
            target.BoundAndAge = (byte)((int)bound | (generation << GenerationShift));
        }

        // New best move at `ply`: the line from here is this move followed by the best line of the child
        private void UpdatePv(int ply, Move move)
        {
            pv[ply, ply] = move;
            int childLength = ply + 1 < MaxPly ? pvLength[ply + 1] : ply + 1;
            for (int i = ply + 1; i < childLength; i++)
            {
                pv[ply, i] = pv[ply + 1, i];
            }
            pvLength[ply] = Math.Max(childLength, ply + 1);
        }

        // Only quiet moves are stored: captures are already ordered well by MVV-LVA
        private void StoreKiller(int ply, Move move)
        {
            if (move.IsCapture || move.IsPromotion || killers[ply, 0] == move) return;
            killers[ply, 1] = killers[ply, 0];
            killers[ply, 0] = move;
        }

        // Deeper cutoffs save more work, so they count more (depth^2)
        private void AddHistory(Side side, Move move, int depth)
        {
            ref int value = ref history[(int)side, move.From, move.To];
            value += depth * depth;
            if (value > HistoryLimit) AgeHistory();
        }

        // Halves all history values: keeps their order, makes room for new information
        private void AgeHistory()
        {
            for (int s = 0; s < 2; s++)
                for (int from = 0; from < 64; from++)
                    for (int to = 0; to < 64; to++)
                        history[s, from, to] /= 2;
        }

        // Each move is scored once, then the list is sorted in place by the scores (highest first).
        // Insertion sort: with ~40 moves it's fast and needs no extra memory.
        private void SortMoves(Position position, List<Move> moves, int ply, Move pvMove, Move tableMove)
        {
            int[] scores = moveScores[ply];
            int count = moves.Count;
            for (int i = 0; i < count; i++)
            {
                scores[i] = ScoreMove(position, moves[i], ply, pvMove, tableMove);
            }

            for (int i = 1; i < count; i++)
            {
                Move move = moves[i];
                int score = scores[i];
                int j = i - 1;
                while (j >= 0 && scores[j] < score)
                {
                    scores[j + 1] = scores[j];
                    moves[j + 1] = moves[j];
                    j--;
                }
                scores[j + 1] = score;
                moves[j + 1] = move;
            }
        }

        private int ScoreMove(Position position, Move move, int ply, Move pvMove, Move tableMove)
        {
            if (move == pvMove) return PvMoveBonus;
            if (move == tableMove) return TableMoveBonus;

            int score = 0;
            if (move.IsCapture || move.IsPromotion)
            {
                score += CaptureBase;
            }

            if (move.IsCapture)
            {
                // Captures: most valuable victim first, least valuable attacker first (MVV-LVA)
                if (move.IsEnPassant)
                {
                    score += PieceGrades[(int)PieceType.Pawn] * 9;
                }
                else
                {
                    score += PieceGrades[(int)position[move.To].Type] * 10 - PieceGrades[(int)position[move.From].Type];
                }
            }
            else if (killers[ply, 0] == move)
            {
                score += FirstKillerBonus;
            }
            else if (killers[ply, 1] == move)
            {
                score += SecondKillerBonus;
            }
            else if (!move.IsPromotion)
            {
                // Other quiet moves: by how often they caused cutoffs so far
                score += history[(int)position.SideToMove, move.From, move.To];
            }

            if (move.IsPromotion)
            {
                score += PieceGrades[(int)move.Promotion] * 5;
            }

            // Moving a piece to a square attacked by an enemy pawn usually just loses it, so try such moves last
            Side us = position.SideToMove;
            Side them = us.Opponent();
            foreach (int square in Attacks.Pawn[(int)us][move.To])
            {
                if (position[square].Is(PieceType.Pawn, them))
                {
                    score -= PieceGrades[(int)position[move.From].Type];
                    break;
                }
            }

            return score;
        }
    }
}
