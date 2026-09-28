using System;

namespace ChessEngine
{
    public abstract class BotBase
    {
        protected static readonly int[] PieceGrades = { 0, 100, 300, 300, 500, 900, 0 };
        protected static readonly Random rand = new Random();

        protected const int MateScore = 1_000_000;
        protected const int Infinity = 10_000_000;

        public abstract Move ChooseMove(Position position);

        // Material from the point of view of the side to move: its pieces count as plus, the opponent's as minus.
        protected int EvaluatePosition(Position position)
        {
            int score = 0;

            for (int square = 0; square < 64; square++)
            {
                Piece piece = position[square];
                if (piece != Piece.None)
                {
                    int pieceValue = PieceGrades[(int)piece.Type];
                    score += piece.Color == position.SideToMove ? pieceValue : -pieceValue;
                }
            }

            return score;
        }
    }
}
