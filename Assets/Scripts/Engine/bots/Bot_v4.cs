using System;
using System.Collections.Generic;

namespace ChessEngine
{
    // Bot v4: negamax with alpha-beta pruning and additional sorting of moves to improve speed
    
    public class Bot_v4 : BotBase
    {
        private readonly int depth;

        public Bot_v4(int depth = 6)
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
            SortMoves(position, moves);

            int bestScore = -Infinity;
            var bestMoves = new List<Move>();

            foreach (var move in moves)
            {
                position.MakeMove(move);
                int score = -Negamax(position, depth - 1, -Infinity, -(bestScore - 1));
                position.UnmakeMove();

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                }
                else if (score == bestScore)
                {
                    bestMoves.Add(move);
                }
            }

            return bestMoves[rand.Next(bestMoves.Count)];
        }

        // Score of the position searched `depth` plies ahead, from the point of view of the side to move.
        // alpha: score the side to move is already guaranteed elsewhere (anything lower doesn't matter).
        // beta: score above which the opponent will avoid this position (so searching further is pointless).
        private int Negamax(Position position, int depth, int alpha, int beta)
        {
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
            SortMoves(position, moves);

            foreach (var move in moves)
            {
                position.MakeMove(move);
                // The window flips for the opponent: their alpha is our -beta and vice versa
                int score = -Negamax(position, depth - 1, -beta, -alpha);
                position.UnmakeMove();

                if (score >= beta)
                {
                    return beta; // cutoff: the opponent won't allow this position
                }
                if (score > alpha)
                {
                    alpha = score;
                }
            }
            return alpha;
        }

        private int ScoreMove(Position position, Move move)
        {
            int score = 0;
            if (move.IsCapture)
            {
                // Captures are scored by the value of the captured piece
                if (position[move.To] == Piece.None) // en passant capture
                {
                    score += PieceGrades[(int)PieceType.Pawn] * 9; // prioritize en passant captures
                }
                else
                {
                score += PieceGrades[(int)position[move.To].Type] * 10 - PieceGrades[(int)position[move.From].Type]; // prioritize captures with higher value difference;
                }
            }

            if (move.IsPromotion)
            {
                score += PieceGrades[(int)move.Promotion]*5; // prioritize promotions
            }

            // Moving a piece to a square attacked by an enemy pawn usually just loses it, so try such moves last.
            // A pawn of our colour standing on move.To would attack exactly the squares an enemy pawn
            // needs to stand on to attack move.To.
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

        // Best-looking moves first: alpha-beta cuts off more when good moves are searched early.
        // Each move is scored once; sorting with ScoreMove inside the comparison would re-score
        // the same move many times.
        private void SortMoves(Position position, List<Move> moves)
        {
            var scores = new int[moves.Count];
            var order = new int[moves.Count];
            for (int i = 0; i < moves.Count; i++)
            {
                scores[i] = ScoreMove(position, moves[i]);
                order[i] = i;
            }

            // Sort move indices by their stored scores, highest first
            Array.Sort(order, (a, b) => scores[b].CompareTo(scores[a]));

            var sorted = new Move[moves.Count];
            for (int i = 0; i < order.Length; i++)
            {
                sorted[i] = moves[order[i]];
            }
            moves.Clear();
            moves.AddRange(sorted);
        }
    }
}
