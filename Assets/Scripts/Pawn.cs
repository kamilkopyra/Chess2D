using UnityEngine;

public class Pawn : ChessPiece
{
    public override bool[,] GetPossibleMoves()
    {

        BoardCreator boardCreator = FindFirstObjectByType<BoardCreator>();
        
        bool[,] moves = new bool[8, 8];


        CheckEnPassant(boardCreator, moves);
        if (isWhite)
        {
            if (hasBeenMoved == false)
            {
                CheckMoveForward(boardCreator, currentX, currentY + 1, moves);
                DontJumpOver(boardCreator, currentX, currentY + 2, moves);


            }
            else CheckMoveForward(boardCreator, currentX, currentY + 1, moves);

        }

        else
        {
            if (hasBeenMoved == false)
            {
                CheckMoveForward(boardCreator, currentX, currentY - 1, moves);
                DontJumpOver(boardCreator, currentX, currentY - 2, moves);


            }
            else CheckMoveForward(boardCreator, currentX, currentY - 1, moves);


        }


       
        if (boardCreator != null)
        {
            if (isWhite)
            {
                // Sprawdzam czy pion nie jest na granicy planszy 
                CheckTake(boardCreator,currentX + 1, currentY + 1, moves);
                CheckTake(boardCreator,currentX - 1, currentY + 1, moves);
            }
            else
            {
                CheckTake(boardCreator,currentX + 1, currentY - 1, moves);
                CheckTake(boardCreator,currentX - 1, currentY - 1, moves);
            }

        }

        return moves;
    }

    // Pion atakuje oba pola na ukos przed sobą, niezależnie od tego, czy coś tam stoi
    public override bool[,] GetAttackedSquares()
    {
        bool[,] attacks = new bool[8, 8];
        int y = currentY + (isWhite ? 1 : -1);

        if (y >= 0 && y < 8)
        {
            if (currentX + 1 < 8) attacks[currentX + 1, y] = true;
            if (currentX - 1 >= 0) attacks[currentX - 1, y] = true;
        }

        return attacks;
    }


        void CheckTake(BoardCreator boardCreator,int x, int y,bool[,] moves)
        {
            if (x >= 0 && x < 8 && y >= 0 && y < 8) // Zabezpieczenie przed wyjściem poza planszę
            {   

                // Dodaję logikę bicia na ukos pionami (jeśli znajduje się tam figura, to mogę zbić) 
                if (boardCreator.board[x, y] != null)
                {
                    moves[x, y] = true;
                }

            }

        }

        void CheckMoveForward(BoardCreator boardCreator, int x, int y, bool[,] moves) 
        {
        if (x >= 0 && x < 8 && y >= 0 && y < 8)
        {
            if (boardCreator.board[x, y] == null)
                moves[x, y] = true;
        }
        }

        void DontJumpOver(BoardCreator boardCreator, int x, int y, bool[,] moves)
        {
            if (isWhite)
            {
                if (boardCreator.board[x, y] == null && boardCreator.board[x, y - 1] == null)
                { moves[x, y] = true; }
            }
            else
            {
                if (boardCreator.board[x, y] == null && boardCreator.board[x, y + 1] == null)
                { moves[x, y] = true; }

            }
        }

        public void CheckEnPassant(BoardCreator board, bool[,] moves)
        {
            int x = currentX;
            int y = currentY;

            int direction = isWhite ? 1 : -1;

            // Tylko jeśli pion stoi na 5. (biały) lub 4. (czarny) rzędzie
            if ((isWhite && y == 4) || (!isWhite && y == 3))
            {
                // Lewo
                if (x > 0)
                {
                    ChessPiece left = board.board[x - 1, y];
                    if (left is Pawn && left.isWhite != isWhite && left.justMadeFirstMove)
                    {
                        moves[x - 1, y + direction] = true;
                    }
                }

                // Prawo
                if (x < 7)
                {
                    ChessPiece right = board.board[x + 1, y];
                    if (right is Pawn && right.isWhite != isWhite && right.justMadeFirstMove)
                    {
                        moves[x + 1, y + direction] = true;
                    }
                }
            }
        }


}

// Piony biją tak jak powinny i poruszają się prawidłowo. Muszę jeszcze dodać:
// - en passant
// - promocję
// ale to kiedy indziej