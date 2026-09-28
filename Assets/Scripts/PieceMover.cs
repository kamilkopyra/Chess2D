using System.Collections.Generic;
using ChessEngine;
using UnityEngine;

// Kontroler partii: trzyma stan gry (silnik), zamienia kliknięcia na ruchy
// i przekazuje wynik do widoku planszy, podświetleń i UI.
// Każdy ruch przechodzi przez ApplyMove, więc później tak samo podłączy się bot albo gra przez sieć.
public class PieceMover : MonoBehaviour
{
    public Position Position { get; private set; }

    private BoardCreator boardView;
    private LightAvailableMoves lightManager;

    private List<Move> legalMoves = new List<Move>();
    private int selectedSquare = Square.None;
    private bool isWaitingForPromotion;
    private bool isGameOver;
    private readonly Side botSide = Side.Black; 

    private bool IsBotTurn => Position.SideToMove == botSide && !isWaitingForPromotion && !isGameOver;

    private Bot_v0 bot;

    public bool IsWhiteTurn => Position.SideToMove == Side.White;
    public bool IsGameOver => isGameOver;

    void Awake()
    {
        Position = Position.StartPosition();
    }

    void Start()
    {
        bot = new Bot_v0();
        boardView = FindFirstObjectByType<BoardCreator>();
        lightManager = FindFirstObjectByType<LightAvailableMoves>();
        ResetGame();
    }

    // Nowa partia (start, Rewanż, Nowa gra)
    public void ResetGame()
    {
        CancelInvoke(nameof(MakeBotMove));
        Position = Position.StartPosition();
        legalMoves = Position.GetLegalMoves();
        selectedSquare = Square.None;
        isWaitingForPromotion = false;
        isGameOver = false;

        boardView.ShowPosition(Position);
        lightManager.ResetAll();
        UIManager.Instance?.UpdateTurn(true, false);
    }

    void Update()
    {
        if (isWaitingForPromotion || isGameOver || !Input.GetMouseButtonDown(0) || IsBotTurn) return;

        // Kliknięcia w menu i przyciski nie mogą przechodzić na planszę
        if (UIManager.Instance != null && UIManager.Instance.BlocksBoardInput(Input.mousePosition)) return;

        Vector2 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        OnSquareClicked(BoardCreator.WorldToSquare(world));
    }

    void OnSquareClicked(int square)
    {
        if (square == Square.None)
        {
            Deselect();
            return;
        }

        Piece clicked = Position[square];
        bool clickedOwnPiece = !clicked.IsEmpty && clicked.Color == Position.SideToMove;

        // Wybór (albo zmiana wyboru) własnej figury
        if (clickedOwnPiece && square != selectedSquare)
        {
            Select(square);
            return;
        }

        if (selectedSquare == Square.None || square == selectedSquare)
        {
            Deselect();
            return;
        }

        // Próba ruchu z wybranego pola na kliknięte
        List<Move> candidates = legalMoves.FindAll(m => m.From == selectedSquare && m.To == square);
        Deselect();

        if (candidates.Count == 0) return;

        if (candidates[0].IsPromotion)
        {
            // Kilka ruchów różniących się tylko figurą promocji: pytamy gracza
            isWaitingForPromotion = true;
            UIManager.Instance.ShowPromotion(Position.SideToMove == Side.White, pieceType =>
            {
                isWaitingForPromotion = false;
                ApplyMove(candidates.Find(m => m.Promotion == pieceType));
            });
        }
        else
        {
            ApplyMove(candidates[0]);
        }
    }

    void Select(int square)
    {
        selectedSquare = square;
        lightManager.ShowSelection(square, legalMoves.FindAll(m => m.From == square));
    }

    void Deselect()
    {
        selectedSquare = Square.None;
        lightManager.ClearHighlights();
    }

    // Wykonuje legalny ruch i aktualizuje wszystko, co go pokazuje
    public void ApplyMove(Move move)
    {
        Position.MakeMove(move);
        legalMoves = Position.GetLegalMoves();

        boardView.ShowMove(move, Position);
        lightManager.ShowLastMove(move.From, move.To);

        bool inCheck = Position.InCheck;
        lightManager.ShowCheck(inCheck ? Position.KingSquare(Position.SideToMove) : Square.None);
        UIManager.Instance?.UpdateTurn(Position.SideToMove == Side.White, inCheck);

        GameStatus status = Position.GetStatus();
        if (status != GameStatus.Ongoing)
        {
            isGameOver = true;
            Debug.Log($"Koniec gry: {status}");
            EndGame.Show(status, winner: Position.SideToMove.Opponent());
        }
        if (!isGameOver && IsBotTurn)
        {
            Invoke(nameof(MakeBotMove), 0.5f); 
        }
    }

    void MakeBotMove()
    {
        if(isGameOver || isWaitingForPromotion) return;
        Move move = bot.ChooseMove(Position);
        ApplyMove(move);
    }
}
