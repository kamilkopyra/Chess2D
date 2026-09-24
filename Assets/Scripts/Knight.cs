using UnityEngine;

public class Knight : ChessPiece
{
    public override bool[,] GetPossibleMoves()
    {
        bool[,] moves = new bool[8, 8];


        int[] xOffsets = { 1, 2, 2, 1, -1, -2, -2, -1 };
        int[] yOffsets = { 2, 1, -1, -2, -2, -1, 1, 2 };

        for (int i = 0; i < 8; i++)
        {
            int x = currentX + xOffsets[i];
            int y = currentY + yOffsets[i];

            // SprawdŸ czy ruch jest w granicach planszy
            if (x >= 0 && x < 8 && y >= 0 && y < 8)
            {
                moves[x, y] = true;
            }

           
        }

        return moves;
    }
}
