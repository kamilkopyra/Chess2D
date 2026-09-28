using System;

namespace ChessEngine
{
    public class Bot_v0 : BotBase
    {

        public override Move ChooseMove(Position position)
        {
            var moves = position.GetLegalMoves();
            if (moves.Count == 0)
            {
                throw new InvalidOperationException("No legal moves available for the bot.");
            }
            int index = rand.Next(moves.Count);
            return moves[index];
        }

    }



}