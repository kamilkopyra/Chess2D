using UnityEngine;

public class Rook : ChessPiece
{
    public override bool[,] GetPossibleMoves()
    {
        bool[,] moves = new bool[8, 8];
        BoardCreator board = FindFirstObjectByType<BoardCreator>();
        if (board == null) return moves;

        // Prawo
        for (int i = currentX + 1; i < 8; i++)
        {
            if (!CheckMove(board, i, currentY, moves)) break;
        }

        // Lewo
        for (int i = currentX - 1; i >= 0; i--)
        {
            if (!CheckMove(board, i, currentY, moves)) break;
        }

        // Góra
        for (int j = currentY + 1; j < 8; j++)
        {
            if (!CheckMove(board, currentX, j, moves)) break;
        }

        // Dół
        for (int j = currentY - 1; j >= 0; j--)
        {
            if (!CheckMove(board, currentX, j, moves)) break;
        }

        return moves;
    }

    bool CheckMove(BoardCreator board, int x, int y, bool[,] moves)
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
                moves[x, y] = true; // przeciwnik — można zbić
            }
            return false; // napotkaliśmy przeszkodę - nie można przeskoczyć
        }
    }
}

