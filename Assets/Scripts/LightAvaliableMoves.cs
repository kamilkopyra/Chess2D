using System.Collections.Generic;
using ChessEngine;
using UnityEngine;

// Podświetlenia na planszy: wybrana figura, możliwe ruchy (kropki / obwódki bicia),
// ostatni ruch i król w szachu. Sprite'y generuje SpriteFactory, więc nie trzeba prefabów.
public class LightAvailableMoves : MonoBehaviour
{
    static readonly Color SelectedColor = new Color(1f, 0.93f, 0.25f, 0.5f);
    static readonly Color LastMoveColor = new Color(1f, 0.93f, 0.25f, 0.32f);
    static readonly Color HintColor = new Color(0.08f, 0.08f, 0.08f, 0.22f);
    static readonly Color CheckColor = new Color(1f, 0.12f, 0.12f, 0.9f);
    static readonly Color PremoveColor = new Color(0.86f, 0.26f, 0.26f, 0.55f);

    private readonly List<GameObject> moveHints = new List<GameObject>(); // wybór + kropki, czyszczone po każdym kliknięciu
    private readonly List<GameObject> lastMove = new List<GameObject>();
    private readonly List<GameObject> premove = new List<GameObject>();
    private GameObject checkGlow;

    // Premove: a piece picked while the bot is thinking...
    public void ShowPremoveSelection(int square)
    {
        DestroyAll(premove);
        premove.Add(Spawn("PremoveSelected", SpriteFactory.Square, square, 1f, PremoveColor, BoardCreator.HighlightOrder));
    }

    // ...and the queued move, played as soon as the bot has moved
    public void ShowPremove(int from, int to)
    {
        DestroyAll(premove);
        premove.Add(Spawn("PremoveFrom", SpriteFactory.Square, from, 1f, PremoveColor, BoardCreator.HighlightOrder));
        premove.Add(Spawn("PremoveTo", SpriteFactory.Square, to, 1f, PremoveColor, BoardCreator.HighlightOrder));
    }

    public void ClearPremove()
    {
        DestroyAll(premove);
    }

    // Zaznacza wybraną figurę i jej legalne ruchy (moves = ruchy z tego pola)
    public void ShowSelection(int square, List<Move> moves)
    {
        ClearHighlights();
        moveHints.Add(Spawn("Selected", SpriteFactory.Square, square, 1f, SelectedColor, BoardCreator.HighlightOrder));

        if (!GameSettings.ShowHints) return;

        var shown = new HashSet<int>(); // promocja daje 4 ruchy na to samo pole
        foreach (Move move in moves)
        {
            if (!shown.Add(move.To)) continue;

            // Bicie (także w przelocie) = obwódka, zwykły ruch = kropka
            moveHints.Add(move.IsCapture
                ? Spawn("CaptureHint", SpriteFactory.Ring, move.To, 1f, HintColor, BoardCreator.HintOrder)
                : Spawn("MoveHint", SpriteFactory.Circle, move.To, 0.32f, HintColor, BoardCreator.HintOrder));
        }
    }

    public void ClearHighlights()
    {
        DestroyAll(moveHints);
    }

    public void ShowLastMove(int from, int to)
    {
        DestroyAll(lastMove);
        lastMove.Add(Spawn("LastMoveFrom", SpriteFactory.Square, from, 1f, LastMoveColor, BoardCreator.HighlightOrder));
        lastMove.Add(Spawn("LastMoveTo", SpriteFactory.Square, to, 1f, LastMoveColor, BoardCreator.HighlightOrder));
    }

    // Czerwona poświata pod królem w szachu; Square.None = brak szacha
    public void ShowCheck(int kingSquare)
    {
        if (checkGlow != null) Destroy(checkGlow);
        checkGlow = kingSquare != Square.None
            ? Spawn("CheckGlow", SpriteFactory.Glow, kingSquare, 1.05f, CheckColor, BoardCreator.HighlightOrder)
            : null;
    }

    public void ResetAll()
    {
        ClearHighlights();
        DestroyAll(lastMove);
        ClearPremove();
        ShowCheck(Square.None);
    }

    GameObject Spawn(string name, Sprite sprite, int square, float scale, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = BoardCreator.SquareToWorld(square);
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
