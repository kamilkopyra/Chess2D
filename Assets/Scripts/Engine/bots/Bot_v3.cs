using System;
using System.Collections.Generic;

namespace ChessEngine
{
    // Bot v3: negamax with alpha-beta pruning.
    // Plays exactly like v2 at the same depth, but skips branches that can't change the result.
    public class Bot_v3 : BotBase
    {
        private readonly int depth;

        public Bot_v3(int depth = 4)
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
    }
}
