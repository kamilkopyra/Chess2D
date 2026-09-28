using System;
using System.Collections.Generic;


namespace ChessEngine
{
    public class Bot_v1 : IBot
    {

        // indexes of pieces :              None, Pawn, Knight, Bishop, Rook, Queen, King
        static readonly int[] PieceGrades = { 0,   100,   300,    300,   500,  900,   0 }; 
        // static readonly int minVal = - (8* PieceGrades[1] + 2*(PieceGrades[2] + PieceGrades[3]
        //                                 + PieceGrades[4])+ PieceGrades[5]);

        static readonly Random rand = new Random();
        public Move ChooseMove(Position position)
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
                int score = EvaluatePosition(position);
                position.UnmakeMove();

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                    if (score == int.MaxValue) {return bestMove;}
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
                int chosen = rand.Next(bestMoves.Count);
                bestMove = bestMoves[chosen];
            }

            return bestMove;

        }

         int EvaluatePosition(Position position)
        {
            int score = 0;

            if (position.GetStatus() == GameStatus.Checkmate)
            {
                score = int.MaxValue;
                return score;
            }

            for (int square = 0; square < 64; square++)
            {
                Piece piece = position[square];
                if (piece != Piece.None)
                {
                    int pieceValue = PieceGrades[(int)piece.Type];
                    score = score + (piece.Color == position.SideToMove ? -pieceValue : pieceValue);
                }

            }

            return score;
        }
    
    }
}

   