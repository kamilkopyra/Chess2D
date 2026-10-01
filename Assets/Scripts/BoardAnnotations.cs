using System.Collections.Generic;
using ChessEngine;
using UnityEngine;

// Analysis marks drawn with the right mouse button, like on chess.com:
// right click on a square toggles a highlight, right drag from one square to another toggles an arrow.
// Knight jumps get an L-shaped arrow. Purely visual, the game logic never looks at them.
public class BoardAnnotations : MonoBehaviour
{
    static readonly Color MarkColor = new Color(0.92f, 0.28f, 0.22f, 0.65f);
    static readonly Color ArrowColor = new Color(1f, 0.66f, 0.05f, 0.8f);

    const float ShaftWidth = 0.2f;
    const float HeadLength = 0.42f;
    const float HeadWidth = 0.5f;
    const int ArrowOrder = BoardCreator.HintOrder + 2; // above pieces, also above a piece that is moving

    private readonly Dictionary<int, GameObject> marks = new Dictionary<int, GameObject>();
    private readonly Dictionary<(int, int), GameObject> arrows = new Dictionary<(int, int), GameObject>();

    public void ToggleMark(int square)
    {
        if (marks.TryGetValue(square, out GameObject existing))
        {
            Destroy(existing);
            marks.Remove(square);
            return;
        }

        var mark = Part("Mark", SpriteFactory.Square, BoardCreator.HighlightOrder, MarkColor);
        mark.transform.position = BoardCreator.SquareToWorld(square);
        marks[square] = mark;
    }

    public void ToggleArrow(int from, int to)
    {
        if (arrows.TryGetValue((from, to), out GameObject existing))
        {
            Destroy(existing);
            arrows.Remove((from, to));
            return;
        }
        arrows[(from, to)] = CreateArrow(from, to);
    }

    public void Clear()
    {
        foreach (GameObject go in marks.Values) Destroy(go);
        foreach (GameObject go in arrows.Values) Destroy(go);
        marks.Clear();
        arrows.Clear();
    }

    GameObject CreateArrow(int from, int to)
    {
        var arrow = new GameObject($"Arrow_{Square.ToName(from)}{Square.ToName(to)}");
        arrow.transform.SetParent(transform, false);

        Vector2 start = BoardCreator.SquareToWorld(from);
        Vector2 end = BoardCreator.SquareToWorld(to);
        int df = Square.File(to) - Square.File(from), dr = Square.Rank(to) - Square.Rank(from);
        bool knightJump = (Mathf.Abs(df) == 1 && Mathf.Abs(dr) == 2) || (Mathf.Abs(df) == 2 && Mathf.Abs(dr) == 1);

        if (knightJump)
        {
            // L shape: first along the long leg (covering the corner), then turn and finish with the head.
            // The second leg starts where the first one ends, so the semi-transparent parts don't overlap.
            Vector2 corner = Mathf.Abs(dr) == 2 ? new Vector2(start.x, end.y) : new Vector2(end.x, start.y);
            AddShaft(arrow, start, corner, extendEnd: ShaftWidth / 2);
            AddSegmentWithHead(arrow, corner + (end - corner).normalized * (ShaftWidth / 2), end);
        }
        else
        {
            AddSegmentWithHead(arrow, start, end);
        }
        return arrow;
    }

    // Shaft from `a` up to the head, then the head ending a bit before the centre of the target square
    void AddSegmentWithHead(GameObject arrow, Vector2 a, Vector2 b)
    {
        Vector2 direction = (b - a).normalized;
        Vector2 tip = b - direction * 0.12f;
        Vector2 headBase = tip - direction * HeadLength;

        AddShaft(arrow, a, headBase, extendEnd: 0f);

        var head = Part("Head", SpriteFactory.Triangle, ArrowOrder, ArrowColor, arrow.transform);
        head.transform.position = (Vector3)(headBase + tip) / 2f;
        head.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        head.transform.localScale = new Vector3(HeadLength, HeadWidth, 1);
    }

    void AddShaft(GameObject arrow, Vector2 a, Vector2 b, float extendEnd)
    {
        Vector2 direction = (b - a).normalized;
        Vector2 end = b + direction * extendEnd;   // lets two legs of an L overlap at the corner
        float length = Vector2.Distance(a, end);
        if (length <= 0.001f) return;

        var shaft = Part("Shaft", SpriteFactory.Square, ArrowOrder, ArrowColor, arrow.transform);
        shaft.transform.position = (Vector3)(a + end) / 2f;
        shaft.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        shaft.transform.localScale = new Vector3(length, ShaftWidth, 1);
    }

    GameObject Part(string name, Sprite sprite, int order, Color color, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }
}
