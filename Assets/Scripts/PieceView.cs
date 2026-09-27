using ChessEngine;
using UnityEngine;

// Figura na scenie: tylko grafika i animacja ruchu. Zasady gry są w silniku (ChessEngine.Position).
[RequireComponent(typeof(SpriteRenderer))]
public class PieceView : MonoBehaviour
{
    const float MoveDuration = 0.14f;

    public PieceType Type { get; private set; }
    public Side Color { get; private set; }

    private SpriteRenderer spriteRenderer;
    private Vector3 moveFrom, moveTo;
    private float moveTime = -1f;

    public static PieceView Create(Transform parent, Piece piece, int square)
    {
        var go = new GameObject();
        go.transform.SetParent(parent, false);

        var view = go.AddComponent<PieceView>();
        view.spriteRenderer = go.GetComponent<SpriteRenderer>();
        view.spriteRenderer.sortingOrder = BoardCreator.PieceOrder;
        view.SetPiece(piece.Type, piece.Color);
        view.SetSquare(square, animate: false);
        return view;
    }

    public void SetPiece(PieceType type, Side color)
    {
        Type = type;
        Color = color;
        name = $"{color}_{type}";
        RefreshSprite();
    }

    public void RefreshSprite()
    {
        spriteRenderer.sprite = GameSettings.GetPieceSprite(Type, Color == Side.White);
    }

    // animate = true: płynne przesunięcie (zwykły ruch); false: od razu (ustawienie planszy)
    public void SetSquare(int square, bool animate)
    {
        Vector3 target = BoardCreator.SquareToWorld(square);

        if (animate)
        {
            moveFrom = transform.position;
            moveTo = target;
            moveTime = 0f;
            spriteRenderer.sortingOrder = BoardCreator.HintOrder + 1; // nad innymi figurami w trakcie ruchu
        }
        else
        {
            moveTime = -1f;
            transform.position = target;
            spriteRenderer.sortingOrder = BoardCreator.PieceOrder;
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
            spriteRenderer.sortingOrder = BoardCreator.PieceOrder;
        }
    }
}
