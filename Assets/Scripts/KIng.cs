using UnityEngine;

public class KIng : ChessPiece
{

    
    
    public override bool[,] GetPossibleMoves()
    {
        bool[,] moves = new bool[8, 8];
        int range = 1;
        BoardCreator boardCreator = FindFirstObjectByType<BoardCreator>();
        bool longCastleWhite = true;
        bool shortCastleBlack = true;
        bool shortCastleWhite = true;
        bool longCastleBlack = true;
        bool longC = true;
        bool shortC = false;



        CheckMove(currentX + range, currentY + range, moves);
        CheckMove(currentX - range, currentY - range, moves);
        CheckMove(currentX + range, currentY - range, moves);
        CheckMove(currentX - range, currentY + range, moves);
        CheckMove(currentX, currentY + range, moves);
        CheckMove(currentX + range, currentY, moves);
        CheckMove(currentX, currentY - range, moves);
        CheckMove(currentX - range, currentY, moves);
        if (!hasBeenMoved)
        {
            if (this.isWhite)
            {
                longCastleWhite = CanKingCastle(boardCreator.GetPieceAtPosition(0, 0, PieceType.Rook) as Rook, boardCreator, true);
                shortCastleWhite = CanKingCastle(boardCreator.GetPieceAtPosition(7, 0, PieceType.Rook) as Rook, boardCreator, false);
                makeCastleLegal(longCastleWhite, longC, moves);
                makeCastleLegal(shortCastleWhite, shortC, moves);

            }
            else if (!this.isWhite)
            {
                longCastleBlack = CanKingCastle(boardCreator.GetPieceAtPosition(0, 7, PieceType.Rook) as Rook, boardCreator, true);
                shortCastleBlack = CanKingCastle(boardCreator.GetPieceAtPosition(7, 7, PieceType.Rook) as Rook, boardCreator, false);
                makeCastleLegal(longCastleBlack,longC, moves);
                makeCastleLegal(shortCastleBlack, shortC, moves);
            }
        }
        Debug.Log($"Long Castle White: {longCastleWhite}");
        Debug.Log($"Short Castle White: {shortCastleWhite}");
        Debug.Log($"Long Castle Black: {longCastleBlack}");
        Debug.Log($"Short Castle Black: {shortCastleBlack}");



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



        bool color = this.isWhite;
        if (rook.isWhite == color && !this.isChecked)
        {
            if (rook.hasBeenMoved == true || this.hasBeenMoved) { return false; }

            if (this.isWhite)
            {
                if (type == true)
                {
                    if (board.board[1, 0] == null && board.board[2, 0] == null && board.board[3, 0] == null)
                    {
                        if (mover.WillKingBeSafe(this, 1, 0) && mover.WillKingBeSafe(this, 2, 0) && mover.WillKingBeSafe(this, 3, 0)) return true;


                    }
                }
                else if (board.board[5, 0] == null && board.board[6, 0] == null) 
                {
                    if (mover.WillKingBeSafe(this, 5, 0) && mover.WillKingBeSafe(this, 6, 0)) return true;
                }
            }
            else
            {
                if (type == true)
                {
                    if (board.board[1, 7] == null && board.board[2, 7] == null && board.board[3, 7] == null)
                    {
                        if (mover.WillKingBeSafe(this, 1, 7) && mover.WillKingBeSafe(this, 2, 7) && mover.WillKingBeSafe(this, 3, 7)) return true;
                    }
                }
                else if (board.board[5, 7] == null && board.board[6, 7] == null)
                {
                    if (mover.WillKingBeSafe(this, 5, 7) && mover.WillKingBeSafe(this, 6, 7)) return true;
                }

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


// Trzeba dodać roszadę


