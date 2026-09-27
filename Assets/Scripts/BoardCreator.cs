using TMPro;
using UnityEngine;

public class BoardCreator : MonoBehaviour
{
    // Kolejność rysowania (sortingOrder): pola 0, podświetlenia 1, figury 2, podpowiedzi ruchów 3
    public const int TileOrder = 0;
    public const int HighlightOrder = 1;
    public const int PieceOrder = 2;
    public const int HintOrder = 3;

    public GameObject tilePrefab;

    public GameObject KingWhite;
    public GameObject QueenWhite;
    public GameObject KnightWhite;
    public GameObject BishopWhite;
    public GameObject RookWhite;
    public GameObject PawnWhite;
    public GameObject KingBlack;
    public GameObject QueenBlack;
    public GameObject KnightBlack;
    public GameObject BishopBlack;
    public GameObject RookBlack;
    public GameObject PawnBlack;

    public ChessPiece[,] board = new ChessPiece[8, 8];

    private readonly SpriteRenderer[,] tiles = new SpriteRenderer[8, 8];
    private readonly TextMeshPro[] fileLabels = new TextMeshPro[8]; // a–h w dolnym rzędzie
    private readonly TextMeshPro[] rankLabels = new TextMeshPro[8]; // 1–8 w lewej kolumnie

    void Start()
    {
        // Ciemne tło i trochę miejsca nad planszą na pasek z informacją o ruchu
        Camera cam = Camera.main;
        cam.orthographicSize = 5.6f;
        cam.backgroundColor = new Color32(0x16, 0x17, 0x1A, 255);

        CreateChessBoard();
        SetUpInitialBoardPosition();
        ApplyTheme();
    }

    void OnEnable() => GameSettings.Changed += OnSettingsChanged;
    void OnDisable() => GameSettings.Changed -= OnSettingsChanged;

    void OnSettingsChanged()
    {
        ApplyTheme();
        ApplyPieceSkins();
    }

    void CreateChessBoard()
    {
        // Delikatna poświata w tle, cień i ramka pod planszą
        CreateBackdrop("BackgroundGlow", SpriteFactory.Glow, 22f, Vector3.zero, new Color(1f, 1f, 1f, 0.06f), -3);
        CreateBackdrop("BoardShadow", SpriteFactory.SoftSquare(0.06f), 9.1f, new Vector3(0, -0.12f, 0), new Color(0, 0, 0, 0.45f), -2);
        CreateBackdrop("BoardFrame", SpriteFactory.RoundedSquare(0.02f), 8.3f, Vector3.zero, new Color32(0x2A, 0x2B, 0x30, 255), -1);

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Vector3 position = new Vector3(-3.5f + j, -3.5f + i, 0);
                GameObject square = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                square.name = $"Tile_{(char)('a' + j)}{i + 1}";

                SpriteRenderer sr = square.GetComponent<SpriteRenderer>();
                sr.sortingOrder = TileOrder;
                tiles[j, i] = sr;
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
        go.transform.localPosition = offset;
        go.transform.localScale = new Vector3(size, size, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
    }

    TextMeshPro CreateLabel(string text, int x, int y, TextAlignmentOptions alignment)
    {
        var go = new GameObject("Coord_" + text);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(x - 3.5f, y - 3.5f, 0);

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

    public static bool IsDarkSquare(int x, int y) => (x + y) % 2 == 0;

    public void ApplyPieceSkins()
    {
        foreach (ChessPiece piece in board)
            if (piece != null) ApplySkin(piece);
    }

    // Grafika z wybranego zestawu. Sprite ma dokładnie 1 jednostkę, collider pokrywa całe pole.
    void ApplySkin(ChessPiece piece)
    {
        var sr = piece.GetComponent<SpriteRenderer>();
        sr.sprite = GameSettings.GetPieceSprite(piece.type, piece.isWhite);
        sr.sortingOrder = PieceOrder;
        piece.transform.localScale = Vector3.one;

        var col = piece.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.offset = Vector2.zero;
            col.size = Vector2.one;
        }
    }

    // Nowa partia: usuwa wszystkie figury i ustawia je od nowa
    public void ResetBoard()
    {
        foreach (ChessPiece piece in GetComponentsInChildren<ChessPiece>())
            Destroy(piece.gameObject);

        // Destroy działa dopiero na końcu klatki, więc tablicę czyścimy od razu
        board = new ChessPiece[8, 8];
        SetUpInitialBoardPosition();
    }

    public ChessPiece CreatePiece(GameObject piecePrefab, int x, int y)
    {
        GameObject pieceObject = Instantiate(
            piecePrefab,
            new Vector3(x - 3.5f, y - 3.5f, 0),
            Quaternion.identity,
            transform
        );

        ChessPiece piece = pieceObject.GetComponent<ChessPiece>();
        piece.SetPosition(x, y);

        string prefabName = piecePrefab.name;
        piece.isWhite = prefabName.EndsWith("-w_0"); // "-w_0" = białe, "-b_0" = czarne

        // Nadanie typu
        if (prefabName.StartsWith("king")) piece.type = ChessPiece.PieceType.King;
        else if (prefabName.StartsWith("queen")) piece.type = ChessPiece.PieceType.Queen;
        else if (prefabName.StartsWith("rook")) piece.type = ChessPiece.PieceType.Rook;
        else if (prefabName.StartsWith("bishop")) piece.type = ChessPiece.PieceType.Bishop;
        else if (prefabName.StartsWith("knight")) piece.type = ChessPiece.PieceType.Knight;
        else if (prefabName.StartsWith("pawn")) piece.type = ChessPiece.PieceType.Pawn;
        
        pieceObject.name = $"{(piece.isWhite ? "White" : "Black")}_{piece.type}_{x}{y}";
        ApplySkin(piece);

        return piece;
    }

    void SetUpInitialBoardPosition()
    {

        //white first row

        board[0, 0] = CreatePiece(RookWhite, 0, 0);
        board[1, 0] = CreatePiece(KnightWhite, 1, 0);
        board[2, 0] = CreatePiece(BishopWhite, 2, 0);
        board[3, 0] = CreatePiece(QueenWhite, 3, 0);
        board[4, 0] = CreatePiece(KingWhite, 4, 0);
        board[5, 0] = CreatePiece(BishopWhite, 5, 0);
        board[6, 0] = CreatePiece(KnightWhite, 6, 0);
        board[7, 0] = CreatePiece(RookWhite, 7, 0);

        // white second row

        for (int i = 0; i< 8; i++)
        {
            board[i,1] = CreatePiece(PawnWhite, i, 1);
        }

        // black first row

        board[0, 7] = CreatePiece(RookBlack, 0, 7);
        board[1, 7] = CreatePiece(KnightBlack, 1, 7);
        board[2, 7] = CreatePiece(BishopBlack, 2, 7);
        board[3, 7] = CreatePiece(QueenBlack, 3, 7);
        board[4, 7] = CreatePiece(KingBlack, 4, 7);
        board[5, 7] = CreatePiece(BishopBlack, 5, 7);
        board[6, 7] = CreatePiece(KnightBlack, 6, 7);
        board[7, 7] = CreatePiece(RookBlack, 7, 7);

        // black second row

        for (int i = 0; i<8; i++)
        {
            board[i, 6] = CreatePiece(PawnBlack, i, 6);
        }

    }


    public ChessPiece GetPieceAtPosition(int x, int y, ChessPiece.PieceType pieceType)
    {
        ChessPiece piece = board[x, y];
        if (piece != null && piece.type == pieceType)
            return piece;
        return null;
    }
}


