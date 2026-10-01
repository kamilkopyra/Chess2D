using System;
using System.Collections.Generic;

namespace ChessEngine
{
    // Bot v12: v11 + mating fixes.
    // - Mate distance: a mate in N plies scores MateScore - N, so a faster mate is always preferred.
    //   Before, every mate scored the same, the bot picked randomly among "winning" moves and often
    //   just postponed the mate until the 50-move rule drew the game.
    // - Mop-up evaluation (Evaluation.MopUp): with a big material advantage against a king without pawns,
    //   push that king to the edge and bring our king closer - the plan needed to mate with K+Q or K+R.
    public class Bot_v12 : BotBase, ITimedBot
    {
        private const int MaxPly = 64;

        // Ordering bonuses (only the relative order matters)
        private const int PvMoveBonus = 1_000_000;
        private const int TableMoveBonus = 999_999;
        private const int FirstKillerBonus = 800;   // below equal/winning captures (PxP = 900), above quiet moves
        private const int SecondKillerBonus = 700;

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

        // depth: maximum search depth; normally the time budget stops the search earlier
        public Bot_v12(int depth = 64)
        {
            this.depth = Math.Min(depth, MaxPly - 1);
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
            if (OpeningBook.Default != null && OpeningBook.Default.TryGetMove(position, rand, out Move bookMove))
            {
                return bookMove;
            }

            Array.Clear(killers, 0, killers.Length);
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
                int score = -Negamax(position, currentDepth - 1, 1, -Infinity, -(bestScore - 1), move == pvMove);
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
        private int Negamax(Position position, int depth, int ply, int alpha, int beta, bool onPv)
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

            var moves = position.GetLegalMoves();
            if (moves.Count == 0)
            {
                // No legal moves: in check means checkmate (side to move lost), otherwise stalemate (draw).
                // The closer to the root the mate is, the bigger the score: faster mates are preferred.
                return position.InCheck ? -(MateScore - ply) : 0;
            }

            Move pvMove = onPv && ply < previousPvLength ? previousPv[ply] : default;
            SortMoves(position, moves, ply, pvMove, tableMove);

            int originalAlpha = alpha;
            Move bestMove = tableMove;

            foreach (var move in moves)
            {
                position.MakeMove(move);
                // The window flips for the opponent: their alpha is our -beta and vice versa
                int score = -Negamax(position, depth - 1, ply + 1, -beta, -alpha, onPv && move == pvMove);
                position.UnmakeMove();

                // Don't let an interrupted search write wrong scores into the transposition table
                if (timeUp) return 0;

                if (score >= beta)
                {
                    StoreKiller(ply, move);
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

            if (position.InCheck)
            {
                // In check there is no "doing nothing": every legal move (evasion) has to be considered,
                // and having none means checkmate
                moves = position.GetLegalMoves();
                if (moves.Count == 0)
                {
                    return -(MateScore - ply);
                }
            }
            else
            {
                // Stand pat: the side to move doesn't have to capture. If the static score is already
                // good enough, the captures can only make it better for us - or we just won't make them.
                int standPat = Evaluate(position);
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
                moves = new List<Move>();
                position.GeneratePseudoLegalMoves(moves);
                moves.RemoveAll(m => !m.IsCapture && !m.IsPromotion);
            }

            SortMoves(position, moves, ply, default, default);

            Side us = position.SideToMove;
            foreach (var move in moves)
            {
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
            return Evaluation.Evaluate(position) + Evaluation.MopUp(position);
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

        // Each move is scored once, then the list is sorted by the stored scores (highest first)
        private void SortMoves(Position position, List<Move> moves, int ply, Move pvMove, Move tableMove)
        {
            var scores = new int[moves.Count];
            var order = new int[moves.Count];
            for (int i = 0; i < moves.Count; i++)
            {
                scores[i] = ScoreMove(position, moves[i], ply, pvMove, tableMove);
                order[i] = i;
            }

            Array.Sort(order, (a, b) => scores[b].CompareTo(scores[a]));

            var sorted = new Move[moves.Count];
            for (int i = 0; i < order.Length; i++)
            {
                sorted[i] = moves[order[i]];
            }
            moves.Clear();
            moves.AddRange(sorted);
        }

        private int ScoreMove(Position position, Move move, int ply, Move pvMove, Move tableMove)
        {
            if (move == pvMove) return PvMoveBonus;
            if (move == tableMove) return TableMoveBonus;

            int score = 0;
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
