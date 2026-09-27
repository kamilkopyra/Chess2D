using System.Collections.Generic;
using UnityEngine;

// Podświetlenia na planszy: wybrana figura, możliwe ruchy (kropki / obwódki bicia),
// ostatni ruch i król w szachu. Sprite'y generuje SpriteFactory, więc nie trzeba prefabów.
public class LightAvailableMoves : MonoBehaviour
{
    static readonly Color SelectedColor = new Color(1f, 0.93f, 0.25f, 0.5f);
    static readonly Color LastMoveColor = new Color(1f, 0.93f, 0.25f, 0.32f);
    static readonly Color HintColor = new Color(0.08f, 0.08f, 0.08f, 0.22f);
    static readonly Color CheckColor = new Color(1f, 0.12f, 0.12f, 0.9f);

    private readonly List<GameObject> moveHints = new List<GameObject>(); // wybór + kropki, czyszczone po każdym kliknięciu
    private readonly List<GameObject> lastMove = new List<GameObject>();
    private GameObject checkGlow;

    public void LightSquares(ChessPiece piece)
    {
        ClearHighlights();

        var board = FindFirstObjectByType<BoardCreator>();
        var mover = FindFirstObjectByType<PieceMover>();

        moveHints.Add(Spawn("Selected", SpriteFactory.Square, piece.currentX, piece.currentY, 1f, SelectedColor, BoardCreator.HighlightOrder));

        if (!GameSettings.ShowHints) return;

        bool[,] allMoves = piece.GetLegalMoves();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (!allMoves[x, y] || !mover.WillKingBeSafe(piece, x, y)) continue;

                // Bicie (także w przelocie, gdzie pole docelowe jest puste) = obwódka, zwykły ruch = kropka
                bool isCapture = board.board[x, y] != null || (piece is Pawn && x != piece.currentX);

                moveHints.Add(isCapture
                    ? Spawn("CaptureHint", SpriteFactory.Ring, x, y, 1f, HintColor, BoardCreator.HintOrder)
                    : Spawn("MoveHint", SpriteFactory.Circle, x, y, 0.32f, HintColor, BoardCreator.HintOrder));
            }
        }
    }

    public void ClearHighlights()
    {
        DestroyAll(moveHints);
    }

    public void ShowLastMove(int fromX, int fromY, int toX, int toY)
    {
        DestroyAll(lastMove);
        lastMove.Add(Spawn("LastMoveFrom", SpriteFactory.Square, fromX, fromY, 1f, LastMoveColor, BoardCreator.HighlightOrder));
        lastMove.Add(Spawn("LastMoveTo", SpriteFactory.Square, toX, toY, 1f, LastMoveColor, BoardCreator.HighlightOrder));
    }

    // Czerwona poświata pod królem w szachu; null = brak szacha
    public void ShowCheck(ChessPiece king)
    {
        if (checkGlow != null) Destroy(checkGlow);
        checkGlow = king != null
            ? Spawn("CheckGlow", SpriteFactory.Glow, king.currentX, king.currentY, 1.05f, CheckColor, BoardCreator.HighlightOrder)
            : null;
    }

    public void ResetAll()
    {
        ClearHighlights();
        DestroyAll(lastMove);
        ShowCheck(null);
    }

    GameObject Spawn(string name, Sprite sprite, int x, int y, float scale, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(x - 3.5f, y - 3.5f, 0);
        go.transform.localScale = new Vector3(scale, scale, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    static void DestroyAll(List<GameObject> list)
    {
        foreach (GameObject go in list)
            if (go != null) Destroy(go);
        list.Clear();
    }
}
