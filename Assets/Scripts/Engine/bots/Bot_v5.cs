using System;
using System.Collections.Generic;

namespace ChessEngine
{
    // Bot v5: v4 + better move ordering.
    // - Iterative deepening: searches depth 1, 2, ..., depth. Each iteration remembers the principal
    //   variation (the best line found) and the next iteration tries that line first at every ply.
    // - Killer moves: quiet moves that caused a beta cutoff at the same ply elsewhere in the tree
    //   are tried right after good captures.
    // Plays exactly like v4 at the same depth, only faster.
    public class Bot_v5 : BotBase
    {
        private const int MaxPly = 64;

        // Ordering bonuses (only the relative order matters)
        private const int PvMoveBonus = 1_000_000;
        private const int FirstKillerBonus = 800;   // below equal/winning captures (PxP = 900), above quiet moves
        private const int SecondKillerBonus = 700;

        private readonly int depth;

        // Two killer moves per ply
        private readonly Move[,] killers = new Move[MaxPly, 2];

        // Triangular principal variation table: pv[ply, ...] is the best line found from `ply`
        private readonly Move[,] pv = new Move[MaxPly, MaxPly];
        private readonly int[] pvLength = new int[MaxPly];

        // Principal variation of the previous iteration, tried first in the next one
        private readonly Move[] previousPv = new Move[MaxPly];
        private int previousPvLength;

        public Bot_v5(int depth = 6)
        {
            this.depth = depth;
        }

        public override Move ChooseMove(Position position)
        {
            var moves = position.GetLegalMoves();
            if (moves.Count == 0)
            {
                throw new InvalidOperationException("No legal moves available for the bot.");
            }

            Array.Clear(killers, 0, killers.Length);
            previousPvLength = 0;
            List<Move> bestMoves = null;

            for (int currentDepth = 1; currentDepth <= depth; currentDepth++)
            {
                bestMoves = SearchRoot(position, moves, currentDepth);

                // Remember the best line for ordering in the next iteration
                previousPvLength = pvLength[0];
                for (int i = 0; i < previousPvLength; i++)
                {
                    previousPv[i] = pv[0, i];
                }
            }

            return bestMoves[rand.Next(bestMoves.Count)];
        }

        // Searches all root moves at the given depth and returns the moves with the best score
        private List<Move> SearchRoot(Position position, List<Move> moves, int currentDepth)
        {
            Move pvMove = previousPvLength > 0 ? previousPv[0] : default;
            SortMoves(position, moves, 0, pvMove);

            int bestScore = -Infinity;
            var bestMoves = new List<Move>();
            pvLength[0] = 0;

            foreach (var move in moves)
            {
                position.MakeMove(move);
                // Window one point below the best score: moves that tie with the best get an exact score
                int score = -Negamax(position, currentDepth - 1, 1, -Infinity, -(bestScore - 1), move == pvMove);
                position.UnmakeMove();

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

            if (depth == 0)
            {
                return EvaluatePosition(position);
            }

            var moves = position.GetLegalMoves();
            if (moves.Count == 0)
            {
                // No legal moves: in check means checkmate (side to move lost), otherwise stalemate (draw)
                return position.InCheck ? -MateScore : 0;
            }

            Move pvMove = onPv && ply < previousPvLength ? previousPv[ply] : default;
            SortMoves(position, moves, ply, pvMove);

            foreach (var move in moves)
            {
                position.MakeMove(move);
                // The window flips for the opponent: their alpha is our -beta and vice versa
                int score = -Negamax(position, depth - 1, ply + 1, -beta, -alpha, onPv && move == pvMove);
                position.UnmakeMove();

                if (score >= beta)
                {
                    StoreKiller(ply, move);
                    return beta; // cutoff: the opponent won't allow this position
                }
                if (score > alpha)
                {
                    alpha = score;
                    UpdatePv(ply, move);
                }
            }
            return alpha;
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
        private void SortMoves(Position position, List<Move> moves, int ply, Move pvMove)
        {
            var scores = new int[moves.Count];
            var order = new int[moves.Count];
            for (int i = 0; i < moves.Count; i++)
            {
                scores[i] = ScoreMove(position, moves[i], ply, pvMove);
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

        private int ScoreMove(Position position, Move move, int ply, Move pvMove)
        {
            if (move == pvMove) return PvMoveBonus;

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

            // Moving a piece to a square attacked by an enemy pawn usually just loses it
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
