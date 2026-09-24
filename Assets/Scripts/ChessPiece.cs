using UnityEngine;


// Klasa przypisuj¹ca ka¿demu pionowi typ, kolor i aktualn¹ pozycjê

public abstract class ChessPiece : MonoBehaviour
{
    public int currentX;
    public int currentY;


    public bool isWhite;
    public bool hasBeenMoved = false;
    public bool isChecked = false;
    public bool justMadeFirstMove = false;


    public PieceType type;



    public enum PieceType
    {
        Pawn, Rook, Knight, Bishop, Queen, King
    }

    public string sourcePrefabName;
    public void SetPosition(int x, int y)
    {
        currentX = x;
        currentY = y;
        transform.position = new Vector3(x - 3.5f, y - 3.5f, 0);       
    }

    public virtual bool[,] GetPossibleMoves()
    {
        return new bool[8, 8];
    }

    void OnMouseDown()
    {
        Debug.Log($"Klikniêto: {type} (Prefab: {sourcePrefabName})\n" +
                 $"Kolor: {(isWhite ? "Bia³y" : "Czarny")}\n" +
                 $"Pozycja: [{currentX},{currentY}]");

    }

    public bool[,] GetLegalMoves()
    {
        BoardCreator board = FindFirstObjectByType<BoardCreator>();
        bool[,] possibleMoves = GetPossibleMoves();
        bool[,] legalMoves = new bool[8, 8];

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (!possibleMoves[x, y])
                    continue;

                ChessPiece target = board.board[x, y];

                // Nie mo¿esz zbiæ w³asnej figury
                if (target != null && target.isWhite == this.isWhite)
                    continue;
                // Legalny ruch
                legalMoves[x, y] = true;
            }
        }

        return legalMoves;
    }
}
