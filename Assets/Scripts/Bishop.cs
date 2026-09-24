//using UnityEngine;

//public class Bishop : ChessPiece
//{
//    public override bool[,] GetPossibleMoves()
//    {

//        Debug.Log($"GetPossibleMoves dla: {name} ({currentX},{currentY})");
//        bool[,] moves = new bool[8, 8];

//        oardCreator board = FindFirstObjectByType<BoardCreator>();
//        if (board == null) return moves;


//        for (int i = 1; i < 8; i++)
//        {
//            if (currentX + i < 8 && currentY + i < 8)
//                moves[currentX + i, currentY + i] = true;


//            if (currentX - i >= 0 && currentY + i < 8)
//                moves[currentX - i, currentY + i] = true;


//            if (currentX + i < 8 && currentY - i >= 0)
//                moves[currentX + i, currentY - i] = true;


//            if (currentX - i >= 0 && currentY - i >= 0)
//                moves[currentX - i, currentY - i] = true;
//        }

//        moves[currentX, currentY] = false;

//        return moves;
//    }



//    bool CheckMove(BoardCreator board, int x, int y, bool[,] moves)
//    {
//        ChessPiece target = board.board[x, y];

//        if (target == null)
//        {
//            moves[x, y] = true;
//            return true;
//        }
//        else
//        {
//            if (target.isWhite != this.isWhite)
//            {
//                moves[x, y] = true; // przeciwnik — mo¿na zbiæ
//            }
//            return false; // napotkaliœmy przeszkodê - nie mo¿na przeskoczyæ
//        }
//    }
//}









using UnityEngine;

public class Bishop : ChessPiece
{
    public override bool[,] GetPossibleMoves()
    {

        
        bool[,] moves = new bool[8, 8];

        BoardCreator board = FindFirstObjectByType<BoardCreator>();
        if (board == null) return moves;

        int i, j;

        for (i = currentX + 1, j = currentY + 1; i < 8 && j < 8; i++, j++)
    {
            if (!CheckMove(board, i, j, moves)) break;
        }
        for (i = currentX - 1,j = currentY - 1; i >= 0 && j >= 0; i--, j-- )
        {
            if (!CheckMove(board, i, j, moves)) break;
        }
        for (i = currentX - 1, j = currentY + 1; i >= 0 && j < 8; i--, j++)
        {
            if (!CheckMove(board, i, j, moves)) break;
        }
        for (i = currentX + 1, j = currentY - 1; i < 8 && j >= 0; i++, j-- )
        {
            if (!CheckMove(board, i, j, moves))  break;
        }

        moves[currentX, currentY] = false;

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
                moves[x, y] = true; // przeciwnik — mo¿na zbiæ
            }
            return false; // napotkaliœmy przeszkodê - nie mo¿na przeskoczyæ
        }
    }
}

// Doda³em logikê ¿eby goñce nie mog³y przeskakiwaæ figur







