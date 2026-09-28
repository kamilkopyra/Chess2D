using System;
using System.Collections.Generic;

namespace ChessEngine
{
    public class Bot_v2 : BotBase
    {
        private int depth;

        public Bot_v2(int depth = 2)
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

            int bestScore = -Infinity;
            var bestMoves = new List<Move>();

            foreach (var move in moves)
            {
                position.MakeMove(move);
                int score = -Negamax(position, depth - 1);
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

        // Score of the position searched `depth` plies ahead, from the point of view of the side to move
        private int Negamax(Position position, int depth)
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

            int best = -Infinity;
            foreach (var move in moves)
            {
                position.MakeMove(move);
                int score = -Negamax(position, depth - 1);
                position.UnmakeMove();

                best = Math.Max(best, score);
            }
            return best;
        }
    }
}
