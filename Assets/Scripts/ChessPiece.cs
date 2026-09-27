using UnityEngine;


// Klasa przypisująca każdemu pionowi typ, kolor i aktualną pozycję

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

    // Pola, które figura atakuje (do sprawdzania szacha).
    // Domyślnie to samo co ruchy, ale król (bez roszady) i pion (tylko skosy) to nadpisują.
    public virtual bool[,] GetAttackedSquares()
    {
        return GetPossibleMoves();
    }

    void OnMouseDown()
    {
        Debug.Log($"Kliknięto: {type} (Prefab: {sourcePrefabName})\n" +
                 $"Kolor: {(isWhite ? "Biały" : "Czarny")}\n" +
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

                // Nie możesz zbić własnej figury
                if (target != null && target.isWhite == this.isWhite)
                    continue;
                // Legalny ruch
                legalMoves[x, y] = true;
            }
        }

        return legalMoves;
    }
}
