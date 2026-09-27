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

    const float MoveDuration = 0.14f;
    private Vector3 moveFrom, moveTo;
    private float moveTime = -1f;

    // animate = true: figura płynnie przesuwa się na nowe pole (zwykły ruch).
    // Przy ustawianiu planszy i promocji pozycja zmienia się od razu.
    public void SetPosition(int x, int y, bool animate = false)
    {
        currentX = x;
        currentY = y;
        Vector3 target = new Vector3(x - 3.5f, y - 3.5f, 0);

        if (animate && Application.isPlaying)
        {
            moveFrom = transform.position;
            moveTo = target;
            moveTime = 0f;
            GetComponent<SpriteRenderer>().sortingOrder = BoardCreator.HintOrder + 1; // nad innymi figurami w trakcie ruchu
        }
        else
        {
            moveTime = -1f;
            transform.position = target;
        }
    }

    void Update()
    {
        if (moveTime < 0f) return;

        moveTime += Time.deltaTime;
        float t = Mathf.Clamp01(moveTime / MoveDuration);
        t = 1f - (1f - t) * (1f - t); // ease-out
        transform.position = Vector3.Lerp(moveFrom, moveTo, t);

        if (t >= 1f)
        {
            moveTime = -1f;
            GetComponent<SpriteRenderer>().sortingOrder = BoardCreator.PieceOrder;
        }
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
