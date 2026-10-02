using System;
using System.Collections.Generic;

namespace ChessEngine
{
    // Bot v23: v22 with Texel-tuned evaluation weights.
    // The evaluation is the same as in v22 (material, piece-square tables, pawn structure, king shelter,
    // mobility, threats, outposts; king attack and mop-up unchanged), but all its numbers were fitted to
    // predict the results of ~1.5 million positions from real games (Tools/Chess2D.Tune -> TunedWeights.cs).
    // The search is the same as in v22.
    public class Bot_v23 : BotBase, ITimedBot, ISearchInfo
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

        // Late move pruning: remaining depth up to this. At depth d, once LmpBase + d * d quiet moves have
        // been searched (4, 7 and 12 at depths 1-3), the remaining quiet moves that don't give check are skipped.
        // Typical engines use similar numbers; quadratic growth keeps LMP gentle at depth 3, where it would
        // otherwise cut away most of the tree under a node.
        private const int LmpMaxDepth = 3;
        private const int LmpBase = 3;

        // Delta pruning: safety margin on top of the captured piece's value
        private const int DeltaMargin = 200;

        // Ordering scores (only the relative order matters):
        // PV move > table move > good captures (SEE >= 0) and promotions > killers > quiet moves by history
        // > losing captures (SEE < 0)
        private const int PvMoveBonus = 100_000_000;
        private const int TableMoveBonus = 99_999_999;
        private const int CaptureBase = 10_000_000;              // + MVV-LVA (PxP = 900)
        private const int GoodCaptureBonus = 1_000;              // lifts every good capture (QxP = 100) above the killers
        private const int FirstKillerBonus = CaptureBase + 800;  // below good captures, above quiet moves
        private const int SecondKillerBonus = CaptureBase + 700;
        private const int BadCaptureBase = -CaptureBase;         // + SEE (negative): below every quiet move

        // History values stay far below CaptureBase: when one gets this big, all of them are halved
        private const int HistoryLimit = 1_000_000;

        // history[side, from, to]: how useful the quiet move has been in this search (cutoffs, weighted by depth)
        private readonly int[,,] history = new int[2, 64, 64];

        // What a stored score means with alpha-beta:
        // Exact - the real score; LowerBound - at least this (search stopped on a cutoff);
        // UpperBound - at most this (no move beat alpha)
        private enum Bound : byte { Exact, LowerBound, UpperBound }

        private struct TableEntry
        {
            public ulong Key;      // full hash, to detect two positions sharing the same slot
            public int Score;
            public short Depth;    // how many plies deep the score was searched
            public Bound Bound;
            public Move BestMove;
        }

        // 2^20 entries (~24 MB). The index is the lowest bits of the hash.
        private const int TableSizeBits = 20;
        private const ulong TableMask = (1UL << TableSizeBits) - 1;
        private readonly TableEntry[] table = new TableEntry[1 << TableSizeBits];

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
        public int LastScore { get; private set; }
        public long LastNodes { get; private set; }

        // Best score of the last completed root search (SearchRoot)
        private int rootScore;

        // Reused for every node at a given ply: generated moves and their ordering scores
        private readonly List<Move>[] moveLists = new List<Move>[MaxPly];
        private readonly int[][] moveScores = new int[MaxPly][];
        private readonly List<Move> scratchMoves = new List<Move>(256);

        // depth: maximum search depth; normally the time budget stops the search earlier
        public Bot_v23(int depth = 64)
        {
            this.depth = Math.Min(depth, MaxPly - 1);
            for (int ply = 0; ply < MaxPly; ply++)
            {
                moveLists[ply] = new List<Move>(256);
                moveScores[ply] = new int[256];   // more than the maximum number of moves in a position (218)
            }
        }

        // The table is kept between moves: positions from the previous search often come back
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
            LastScore = 0;
            LastNodes = 0;
            if (OpeningBook.Default != null && OpeningBook.Default.TryGetMove(position, rand, out Move bookMove))
            {
                return bookMove;
            }

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
                LastScore = rootScore;

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

            LastNodes = nodes;
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

            rootScore = bestScore;
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
            ulong key = position.Hash;
            long index = (long)(key & TableMask);
            Move tableMove = default;
            TableEntry entry = table[index];
            if (entry.Key == key)
            {
                tableMove = entry.BestMove;

                // Only reuse the score if it was searched at least as deep as we need now
                if (entry.Depth >= depth)
                {
                    int stored = ScoreFromTable(entry.Score, ply);
                    if (entry.Bound == Bound.Exact) return Math.Max(alpha, Math.Min(beta, stored));
                    if (entry.Bound == Bound.LowerBound && stored >= beta) return beta;
                    if (entry.Bound == Bound.UpperBound && stored <= alpha) return alpha;
                }
            }

            // Static score, used by the pruning below (not meaningful in check)
            int staticEval = inCheck ? -Infinity : Evaluate(position);
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

            // Late move pruning: only near the leaves, off the PV, not in check and not near mate scores.
            // quietSearched counts quiet moves (killers and checking moves included) that were actually
            // searched in this node; moves skipped by futility or LMP don't count.
            bool lmpNode = !onPv && !inCheck && !nearMate && depth <= LmpMaxDepth;
            int lmpLimit = LmpBase + depth * depth;
            int quietSearched = 0;

            for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
            {
                Move move = moves[moveIndex];
                bool quiet = !move.IsCapture && !move.IsPromotion;
                bool killer = move == killers[ply, 0] || move == killers[ply, 1];

                // LMP: decided before making the move, GivesCheck answers without changing the position.
                // The first move is never pruned (the limit is at least 4 searched quiet moves anyway).
                // Skipped moves are simply not searched: if nothing else beats alpha, the node still
                // returns alpha as an upper bound, same as with futility pruning.
                if (lmpNode && quiet && !killer && moveIndex > 0 && quietSearched >= lmpLimit
                    && !GivesCheck(position, move))
                {
                    continue;
                }

                position.MakeMove(move);
                bool givesCheck = position.InCheck;

                // Futility: skip quiet moves that don't give check (never the first move, so something is searched)
                if (futile && quiet && !givesCheck && moveIndex > 0)
                {
                    position.UnmakeMove();
                    continue;
                }

                if (quiet) quietSearched++;

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
                    Store(index, key, depth, ScoreToTable(beta, ply), Bound.LowerBound, move);
                    return beta; // cutoff: the opponent won't allow this position
                }
                if (score > alpha)
                {
                    alpha = score;
                    bestMove = move;
                    UpdatePv(ply, move);
                }
            }

            Store(index, key, depth, ScoreToTable(alpha, ply), alpha > originalAlpha ? Bound.Exact : Bound.UpperBound, bestMove);
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

            List<Move> moves;
            bool inCheck = position.InCheck;
            int standPat = 0;

            if (inCheck)
            {
                // In check there is no "doing nothing": every legal move (evasion) has to be considered,
                // and having none means checkmate
                moves = moveLists[ply];
                position.GenerateLegalMovesFast(moves, scratchMoves);
                if (moves.Count == 0)
                {
                    return -(MateScore - ply);
                }
            }
            else
            {
                // Stand pat: the side to move doesn't have to capture. If the static score is already
                // good enough, the captures can only make it better for us - or we just won't make them.
                standPat = Evaluate(position);
                if (standPat >= beta)
                {
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

            SortMoves(position, moves, ply, default, default);
            int[] scores = moveScores[ply];

            Side us = position.SideToMove;
            for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
            {
                Move move = moves[moveIndex];

                // Losing captures (SEE < 0) are sorted last: once the first one is reached, the rest lose too
                if (!inCheck && scores[moveIndex] < 0) break;

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
                    return beta;
                }
                if (score > alpha)
                {
                    alpha = score;
                }
            }
            return alpha;
        }

        private static int Evaluate(Position position)
        {
            return TunableEvaluation.Evaluate(position, TunedWeights.Weights);
        }

        // Whether the move, made by the side to move, would give check - without making it.
        // On bitboards: the occupied squares as they will be after the move (`from` empty, `to` occupied),
        // then the enemy king is checked directly by the moved piece or by one of our sliders uncovered
        // by leaving `from` (a discovered check). Castling, en passant and promotions change more squares
        // or the piece type: for those (rare) moves it just makes the move and looks.
        public static bool GivesCheck(Position position, Move move)
        {
            if (move.IsCastle || move.IsEnPassant || move.IsPromotion)
            {
                position.MakeMove(move);
                bool check = position.InCheck;
                position.UnmakeMove();
                return check;
            }

            Side us = position.SideToMove;
            int king = position.KingSquare(us.Opponent());
            ulong kingBit = 1UL << king;
            ulong fromBit = 1UL << move.From, toBit = 1UL << move.To;
            PieceType moving = position[move.From].Type;

            // Direct check by a knight or pawn on its new square (a discovered check is still possible otherwise)
            if (moving == PieceType.Knight && (Bitboards.KnightAttacks[move.To] & kingBit) != 0) return true;
            if (moving == PieceType.Pawn && (Bitboards.PawnAttacks[(int)us][move.To] & kingBit) != 0) return true;

            // Our sliders after the move: the moving piece counts on `to`, nothing stays on `from`
            ulong occupied = (position.Occupied & ~fromBit) | toBit;
            ulong queens = position.Pieces(PieceType.Queen, us);
            ulong rooks = (position.Pieces(PieceType.Rook, us) | queens) & ~fromBit;
            ulong bishops = (position.Pieces(PieceType.Bishop, us) | queens) & ~fromBit;
            if (moving == PieceType.Rook || moving == PieceType.Queen) rooks |= toBit;
            if (moving == PieceType.Bishop || moving == PieceType.Queen) bishops |= toBit;

            return (Bitboards.RookAttacks(king, occupied) & rooks) != 0
                || (Bitboards.BishopAttacks(king, occupied) & bishops) != 0;
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

        private static int ScoreToTable(int score, int ply)
        {
            if (score > MateThreshold) return score + ply;
            if (score < -MateThreshold) return score - ply;
            return score;
        }

        private static int ScoreFromTable(int score, int ply)
        {
            if (score > MateThreshold) return score - ply;
            if (score < -MateThreshold) return score + ply;
            return score;
        }

        // Always-replace scheme: the newest result overwrites whatever was in the slot
        private void Store(long index, ulong key, int depth, int score, Bound bound, Move bestMove)
        {
            table[index] = new TableEntry
            {
                Key = key,
                Score = score,
                Depth = (short)depth,
                Bound = bound,
                BestMove = bestMove,
            };
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

            if (move.IsCapture)
            {
                // Captures: most valuable victim first, least valuable attacker first (MVV-LVA)
                int attacker = PieceGrades[(int)position[move.From].Type];
                int victim = move.IsEnPassant ? PieceGrades[(int)PieceType.Pawn] : PieceGrades[(int)position[move.To].Type];
                int mvvLva = victim * 10 - attacker;
                int promotion = move.IsPromotion ? PieceGrades[(int)move.Promotion] * 5 : 0;

                // Taking a piece worth at least the attacker can't lose material: no need for SEE.
                // Promotions always stay in the good band (as in v18).
                if (move.IsPromotion || (victim >= attacker && position[move.From].Type != PieceType.King))
                {
                    return CaptureBase + GoodCaptureBonus + mvvLva + promotion;
                }

                int see = See.Evaluate(position, move);
                if (see >= 0)
                {
                    return CaptureBase + GoodCaptureBonus + mvvLva;
                }
                return BadCaptureBase + see;
            }

            int score = 0;
            if (move.IsPromotion)
            {
                score += CaptureBase + PieceGrades[(int)move.Promotion] * 5;
            }
            else if (killers[ply, 0] == move)
            {
                score += FirstKillerBonus;
            }
            else if (killers[ply, 1] == move)
            {
                score += SecondKillerBonus;
            }
            else
            {
                // Other quiet moves: by how often they caused cutoffs so far
                score += history[(int)position.SideToMove, move.From, move.To];
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
