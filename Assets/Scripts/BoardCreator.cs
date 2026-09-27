using ChessEngine;
using TMPro;
using UnityEngine;

// Widok planszy: pola, współrzędne, motyw kolorystyczny i figury.
// Nie zna zasad gry, tylko pokazuje to, co jest w silniku (ChessEngine.Position).
public class BoardCreator : MonoBehaviour
{
    // Kolejność rysowania (sortingOrder): pola 0, podświetlenia 1, figury 2, podpowiedzi ruchów 3
    public const int TileOrder = 0;
    public const int HighlightOrder = 1;
    public const int PieceOrder = 2;
    public const int HintOrder = 3;

    public GameObject tilePrefab;

    private readonly PieceView[] pieces = new PieceView[64];
    private readonly SpriteRenderer[,] tiles = new SpriteRenderer[8, 8];
    private readonly TextMeshPro[] fileLabels = new TextMeshPro[8]; // a–h w dolnym rzędzie
    private readonly TextMeshPro[] rankLabels = new TextMeshPro[8]; // 1–8 w lewej kolumnie

    // Plansza tworzona w Awake, żeby PieceMover mógł w Start od razu ustawić figury
    void Awake()
    {
        // Ciemne tło i trochę miejsca nad planszą na pasek z informacją o ruchu
        Camera cam = Camera.main;
        cam.orthographicSize = 5.6f;
        cam.backgroundColor = new Color32(0x16, 0x17, 0x1A, 255);

        CreateChessBoard();
        ApplyTheme();
    }

    void OnEnable() => GameSettings.Changed += OnSettingsChanged;
    void OnDisable() => GameSettings.Changed -= OnSettingsChanged;

    void OnSettingsChanged()
    {
        ApplyTheme();
        foreach (PieceView piece in pieces)
            if (piece != null) piece.RefreshSprite();
    }

    // ===== Współrzędne =====

    public static Vector3 SquareToWorld(int square) =>
        new Vector3(Square.File(square) - 3.5f, Square.Rank(square) - 3.5f, 0);

    // Pole pod punktem na ekranie w świecie gry, albo Square.None poza planszą
    public static int WorldToSquare(Vector2 world)
    {
        int file = Mathf.RoundToInt(world.x + 3.5f);
        int rank = Mathf.RoundToInt(world.y + 3.5f);
        return Square.IsValid(file, rank) ? Square.Make(file, rank) : Square.None;
    }

    public static bool IsDarkSquare(int x, int y) => (x + y) % 2 == 0;

    // ===== Figury =====

    // Ustawia figury dokładnie według pozycji, bez animacji (start i nowa partia)
    public void ShowPosition(Position position)
    {
        for (int sq = 0; sq < 64; sq++)
        {
            if (pieces[sq] != null) Destroy(pieces[sq].gameObject);
            pieces[sq] = null;

            Piece piece = position[sq];
            if (!piece.IsEmpty) pieces[sq] = PieceView.Create(transform, piece, sq);
        }
    }

    // Animuje ruch, który właśnie wykonał silnik. `position` to stan PO ruchu.
    public void ShowMove(Move move, Position position)
    {
        int from = move.From, to = move.To;

        // Zbita figura (przy en passant stoi obok pola docelowego)
        int captureSquare = move.IsEnPassant ? Square.Make(Square.File(to), Square.Rank(from)) : to;
        if (move.IsCapture) RemoveView(captureSquare);

        MoveView(from, to);

        if ((move.Flags & MoveFlags.CastleKingside) != 0) MoveView(from + 3, from + 1);
        else if ((move.Flags & MoveFlags.CastleQueenside) != 0) MoveView(from - 4, from - 1);

        if (move.IsPromotion && pieces[to] != null)
            pieces[to].SetPiece(move.Promotion, pieces[to].Color);

        // Zabezpieczenie: jeśli widok z jakiegoś powodu rozjechał się z silnikiem, ustaw figury od nowa
        if (!MatchesPosition(position))
        {
            Debug.LogWarning("Widok planszy nie zgadzał się z silnikiem, odświeżam figury.");
            ShowPosition(position);
        }
    }

    void MoveView(int from, int to)
    {
        PieceView view = pieces[from];
        if (view == null) return;

        RemoveView(to);
        pieces[from] = null;
        pieces[to] = view;
        view.SetSquare(to, animate: true);
    }

    void RemoveView(int square)
    {
        if (pieces[square] == null) return;
        Destroy(pieces[square].gameObject);
        pieces[square] = null;
    }

    bool MatchesPosition(Position position)
    {
        for (int sq = 0; sq < 64; sq++)
        {
            Piece piece = position[sq];
            PieceView view = pieces[sq];

            if (piece.IsEmpty != (view == null)) return false;
            if (view != null && (view.Type != piece.Type || view.Color != piece.Color)) return false;
        }
        return true;
    }

    // ===== Plansza =====

    void CreateChessBoard()
    {
        // Delikatna poświata w tle, cień i ramka pod planszą
        CreateBackdrop("BackgroundGlow", SpriteFactory.Glow, 22f, Vector3.zero, new Color(1f, 1f, 1f, 0.06f), -3);
        CreateBackdrop("BoardShadow", SpriteFactory.SoftSquare(0.06f), 9.1f, new Vector3(0, -0.12f, 0), new Color(0, 0, 0, 0.45f), -2);
        CreateBackdrop("BoardFrame", SpriteFactory.RoundedSquare(0.02f), 8.3f, Vector3.zero, new Color32(0x2A, 0x2B, 0x30, 255), -1);

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int sq = Square.Make(file, rank);
                GameObject tile = Instantiate(tilePrefab, SquareToWorld(sq), Quaternion.identity, transform);
                tile.name = "Tile_" + Square.ToName(sq);

                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                sr.sortingOrder = TileOrder;
                tiles[file, rank] = sr;
            }
        }

        for (int k = 0; k < 8; k++)
        {
            fileLabels[k] = CreateLabel(((char)('a' + k)).ToString(), k, 0, TextAlignmentOptions.BottomRight);
            rankLabels[k] = CreateLabel((k + 1).ToString(), 0, k, TextAlignmentOptions.TopLeft);
        }
    }

    void CreateBackdrop(string name, Sprite sprite, float size, Vector3 offset, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = offset; // pozycja w świecie: obiekt Board w scenie nie stoi w (0, 0)
        go.transform.localScale = new Vector3(size, size, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
    }

    TextMeshPro CreateLabel(string text, int file, int rank, TextAlignmentOptions alignment)
    {
        var go = new GameObject("Coord_" + text);
        go.transform.SetParent(transform, false);
        go.transform.position = SquareToWorld(Square.Make(file, rank));

        var label = go.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = 2.2f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = alignment;
        label.rectTransform.sizeDelta = new Vector2(0.9f, 0.88f);
        label.sortingOrder = HighlightOrder;
        return label;
    }

    // Kolory pól i współrzędnych według wybranego motywu
    public void ApplyTheme()
    {
        var theme = GameSettings.CurrentTheme;

        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                if (tiles[x, y] != null)
                    tiles[x, y].color = IsDarkSquare(x, y) ? theme.Dark : theme.Light;

        bool show = GameSettings.ShowCoordinates;
        for (int k = 0; k < 8; k++)
        {
            if (fileLabels[k] == null) continue;
            // Napis w kolorze przeciwnym do koloru pola
            fileLabels[k].color = IsDarkSquare(k, 0) ? theme.Light : theme.Dark;
            rankLabels[k].color = IsDarkSquare(0, k) ? theme.Light : theme.Dark;
            fileLabels[k].gameObject.SetActive(show);
            rankLabels[k].gameObject.SetActive(show);
        }
    }
}
