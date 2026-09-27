using UnityEngine;

public class KIng : ChessPiece
{

    
    
    public override bool[,] GetPossibleMoves()
    {
        bool[,] moves = GetAttackedSquares();
        BoardCreator boardCreator = FindFirstObjectByType<BoardCreator>();
        bool longC = true;
        bool shortC = false;

        if (!hasBeenMoved)
        {
            int row = isWhite ? 0 : 7;
            bool longCastle = CanKingCastle(boardCreator.GetPieceAtPosition(0, row, PieceType.Rook) as Rook, boardCreator, true);
            bool shortCastle = CanKingCastle(boardCreator.GetPieceAtPosition(7, row, PieceType.Rook) as Rook, boardCreator, false);
            makeCastleLegal(longCastle, longC, moves);
            makeCastleLegal(shortCastle, shortC, moves);
        }

        return moves;
    }

    // Tylko pola wokół króla, bez roszady. Dzięki temu sprawdzanie szacha
    // nie wywołuje sprawdzania roszady przeciwnika (wcześniej była tu nieskończona rekurencja).
    public override bool[,] GetAttackedSquares()
    {
        bool[,] moves = new bool[8, 8];
        int range = 1;

        CheckMove(currentX + range, currentY + range, moves);
        CheckMove(currentX - range, currentY - range, moves);
        CheckMove(currentX + range, currentY - range, moves);
        CheckMove(currentX - range, currentY + range, moves);
        CheckMove(currentX, currentY + range, moves);
        CheckMove(currentX + range, currentY, moves);
        CheckMove(currentX, currentY - range, moves);
        CheckMove(currentX - range, currentY, moves);

        return moves;
    }


    private void CheckMove(int x, int y, bool[,] moves)
    {
        if (x >= 0 && x < 8 && y >= 0 && y < 8) // Zabezpieczenie przed wyjściem poza planszę
        {
            moves[x, y] = true;

        }

    }

    private bool CanKingCastle(Rook rook, BoardCreator board, bool type) 
    {
        PieceMover mover = FindFirstObjectByType<PieceMover>();
        
        if (rook == null) return false;
        if (rook.isWhite != this.isWhite) return false;
        if (rook.hasBeenMoved || this.hasBeenMoved) return false;

        // Nie można roszować z szacha (liczone na bieżąco, bo flaga isChecked bywa nieaktualna)
        if (mover.IsKingInCheck(this.isWhite)) return false;

        int row = isWhite ? 0 : 7;

        if (type == true)
        {
            // Długa: b, c, d muszą być puste, ale atakowane nie mogą być tylko c i d
            // (pole b król nie przechodzi, więc może być atakowane)
            if (board.board[1, row] == null && board.board[2, row] == null && board.board[3, row] == null)
            {
                if (mover.WillKingBeSafe(this, 3, row) && mover.WillKingBeSafe(this, 2, row)) return true;
            }
        }
        else
        {
            // Krótka: f i g muszą być puste i nieatakowane
            if (board.board[5, row] == null && board.board[6, row] == null)
            {
                if (mover.WillKingBeSafe(this, 5, row) && mover.WillKingBeSafe(this, 6, row)) return true;
            }
        }
        return false;
    }

    private void makeCastleLegal(bool CanKingCastle, bool type, bool[,] moves) 
    {
        if (CanKingCastle)  
        {
            if (this.isWhite)
            {
                if (type == true)
                {
                    moves[2, 0] = true;
                }
                else { moves[6, 0] = true; }
            }
            else
            {
                if (type == true)
                {
                    moves[2, 7] = true;
                }
                else { moves[6, 7] = true; }
            }
        }
    
    }

}


