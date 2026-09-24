using UnityEngine;

public class BoardCreator : MonoBehaviour
{
    public GameObject tilePrefab;
    public Color whiteColor;
    public Color blackColor;

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

    void Start()
    {
        CreateChessBoard();
        SetUpInitialBoardPosition();
    }

    void CreateChessBoard()
    {

        Debug.Log("Creating chess board");
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                bool isBlack = (i + j) % 2 == 0;
                

                Vector3 position = new Vector3(-3.5f + j, -3.5f + i, 0);
                GameObject square = Instantiate(tilePrefab, position, Quaternion.identity);

                SpriteRenderer sr = square.GetComponent<SpriteRenderer>();
                sr.color = isBlack ? blackColor : whiteColor;
            }
        }
    }

    ChessPiece CreatePiece(GameObject piecePrefab, int x, int y)
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
        pieceObject.GetComponent<SpriteRenderer>().sortingOrder = 1;

        // Nadanie typu
        if (prefabName.StartsWith("king")) piece.type = ChessPiece.PieceType.King;
        else if (prefabName.StartsWith("queen")) piece.type = ChessPiece.PieceType.Queen;
        else if (prefabName.StartsWith("rook")) piece.type = ChessPiece.PieceType.Rook;
        else if (prefabName.StartsWith("bishop")) piece.type = ChessPiece.PieceType.Bishop;
        else if (prefabName.StartsWith("knight")) piece.type = ChessPiece.PieceType.Knight;
        else if (prefabName.StartsWith("pawn")) piece.type = ChessPiece.PieceType.Pawn;
        
        pieceObject.name = $"{(piece.isWhite ? "White" : "Black")}_{piece.type}_{x}{y}";

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


