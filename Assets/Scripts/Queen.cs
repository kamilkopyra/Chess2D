using UnityEngine;

public class Queen : ChessPiece
{
    public override bool[,] GetPossibleMoves()
    {

        

        bool[,] moves = new bool[8, 8];


        BoardCreator board = FindFirstObjectByType<BoardCreator>();
        if (board == null) return moves;


        // Ruchy Goñca
        int i, j;

        for (i = currentX + 1, j = currentY + 1; i < 8 && j < 8; i++, j++)
        {
            if (!CheckMoveB(board, i, j, moves)) break;
        }
        for (i = currentX - 1, j = currentY - 1; i >= 0 && j >= 0; i--, j--)
        {
            if (!CheckMoveB(board, i, j, moves)) break;
        }
        for (i = currentX - 1, j = currentY + 1; i >= 0 && j < 8; i--, j++)
        {
            if (!CheckMoveB(board, i, j, moves)) break;
        }
        for (i = currentX + 1, j = currentY - 1; i < 8 && j >= 0; i++, j--)
        {
            if (!CheckMoveB(board, i, j, moves)) break;
        }

        moves[currentX, currentY] = false;

        // Ruchy wie¿¹

        // Prawo
        for (int k = currentX + 1; k < 8; k++)
        {
            if (!CheckMoveR(board, k, currentY, moves)) break;
        }

        // Lewo
        for (int k = currentX - 1; k >= 0; k--)
        {
            if (!CheckMoveR(board, k, currentY, moves)) break;
        }

        // Góra
        for (int l = currentY + 1; l < 8; l++)
        {
            if (!CheckMoveR(board, currentX, l, moves)) break;
        }

        // Dó³
        for (int l = currentY - 1; l >= 0; l--)
        {
            if (!CheckMoveR(board, currentX, l, moves)) break;
        }

        return moves;
    }

    
    bool CheckMoveB(BoardCreator board, int x, int y, bool[,] moves)
    {
        ChessPiece target = board.board[x, y];

        if (target == null)
        {
            moves[x, y] = true;
            return true;
        }
        else
        {
            if (target.isWhite != this.isWhite)
            {
                moves[x, y] = true; // przeciwnik — mo¿na zbiæ
            }
            return false; // napotkaliœmy przeszkodê - nie mo¿na przeskoczyæ
        }
    }


    bool CheckMoveR(BoardCreator board, int x, int y, bool[,] moves)
    {
        ChessPiece target = board.board[x, y];

        if (target == null)
        {
            moves[x, y] = true;
            return true;
        }
        else
        {
            if (target.isWhite != this.isWhite)
            {
                moves[x, y] = true; // przeciwnik — mo¿na zbiæ
            }
            return false; // napotkaliœmy przeszkodê - nie mo¿na przeskoczyæ
        }
    }
}

