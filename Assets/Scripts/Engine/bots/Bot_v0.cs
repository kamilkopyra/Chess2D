using System;

namespace ChessEngine
{
    public class Bot_v0 : IBot
    {

        static readonly Random rand = new Random();

        public Move ChooseMove(Position position)
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