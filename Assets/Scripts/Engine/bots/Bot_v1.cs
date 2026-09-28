using System;
using System.Collections.Generic;


namespace ChessEngine
{
    public class Bot_v1 : BotBase
    {

        public override Move ChooseMove(Position position)
        {
            var moves = position.GetLegalMoves();
            if (moves.Count == 0)
            {
                throw new InvalidOperationException("No legal moves available for the bot.");
            }

            Move bestMove = moves[0];
            int bestScore = int.MinValue;
            var bestMoves = new List<Move>();

            foreach (var move in moves)
            {

                position.MakeMove(move);
                // EvaluatePosition scores for the side to move (the opponent after MakeMove), hence the minus
                int score = position.GetStatus() == GameStatus.Checkmate ? MateScore : -EvaluatePosition(position);
                position.UnmakeMove();

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                    if (score == MateScore) {return bestMove;}
                    bestMoves.Clear();
                    bestMoves.Add(bestMove);
                }
                else if (score == bestScore)
                {
                    bestMoves.Add(move);
                }
            }

            if (  bestMoves.Count != 0)
            {
                int chosen = BotBase.rand.Next(bestMoves.Count);
                bestMove = bestMoves[chosen];
            }

            return bestMove;

        }
    }
}

   