using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ChessEngine;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class PieceMover : MonoBehaviour
{
    // The bot's move never appears sooner than this after it started thinking,
    // so it doesn't land in the middle of the player's move animation
    const float MinBotMoveDelay = 0.3f;

    public Position Position { get; private set; }

    private BoardCreator boardView;
    private LightAvailableMoves lightManager;
    private BoardAnnotations annotations;

    private List<Move> legalMoves = new List<Move>();
    private int selectedSquare = Square.None;
    private bool isWaitingForPromotion;
    private bool isGameOver;

    // Opponent: null means two players on one computer
    private BotBase bot;
    private Side botSide;

    // The bot searches on a copy of the position in a background thread, so the game keeps running
    private Task<(Move Move, double Seconds)> botTask;
    private readonly Stopwatch botWatch = new Stopwatch();

    // Premove: a move entered while the bot is thinking, played right after the bot's move
    private int premoveSelected = Square.None;
    private int premoveFrom = Square.None, premoveTo = Square.None;

    // Right mouse button: start square of an arrow being drawn
    private int rightDragStart = Square.None;

    private bool IsBotTurn => bot != null && Position.SideToMove == botSide && !isGameOver;

    public bool IsWhiteTurn => Position.SideToMove == Side.White;
    public bool IsGameOver => isGameOver;

    void Awake()
    {
        Position = Position.StartPosition();
    }

    void Start()
    {
        // Opening book for bots that use it (Bot_v9 and newer)
        var bookText = Resources.Load<TextAsset>("Books/elite_book");
        if (bookText != null) OpeningBook.Default = OpeningBook.Parse(bookText.text);

        boardView = FindFirstObjectByType<BoardCreator>();
        lightManager = FindFirstObjectByType<LightAvailableMoves>();
        annotations = gameObject.AddComponent<BoardAnnotations>();
        ResetGame();
    }

    // Nowa partia (start, Rewanż, Nowa gra)
    public void ResetGame()
    {
        // A search from the previous game may still be running: forget it. It works on its own bot
        // and position copy, so it can't disturb the new game.
        botTask = null;
        CancelPremove();
        annotations.Clear();

        Position = Position.StartPosition();
        legalMoves = Position.GetLegalMoves();
        selectedSquare = Square.None;
        isWaitingForPromotion = false;
        isGameOver = false;

        CreateOpponent();

        boardView.ShowPosition(Position);
        lightManager.ResetAll();
        UIManager.Instance?.UpdateTurn(true, false);
        UIManager.Instance?.HideBotThinkTime();

        if (IsBotTurn) StartBotSearch();
    }

    // Opponent and colours from the settings. A new bot every game, so no state leaks between games.
    void CreateOpponent()
    {
        string opponent = GameSettings.Opponent;
        if (opponent == GameSettings.HumanOpponent || !BotFactory.Exists(opponent))
        {
            bot = null;
            return;
        }

        // For time-managed bots the depth is only an upper limit, the time setting decides
        int depth = BotFactory.IsTimed(opponent) ? 64 : GameSettings.BotDepth;
        bot = BotFactory.Create(opponent, depth);
        if (bot is ITimedBot timedBot) timedBot.MoveTimeMs = GameSettings.BotMoveTimeMs;

        Side humanSide;
        switch (GameSettings.HumanColor)
        {
            case GameSettings.PlayerColor.White: humanSide = Side.White; break;
            case GameSettings.PlayerColor.Black: humanSide = Side.Black; break;
            default: humanSide = Random.value < 0.5f ? Side.White : Side.Black; break;
        }
        botSide = humanSide.Opponent();
    }

    void Update()
    {
        CheckBotSearch();

        // Kliknięcia w menu i przyciski nie mogą przechodzić na planszę
        bool blockedByUi = UIManager.Instance != null && UIManager.Instance.BlocksBoardInput(Input.mousePosition);

        HandleRightMouse(blockedByUi);

        if (!Input.GetMouseButtonDown(0) || blockedByUi) return;

        // Any left click clears the analysis marks, like on chess.com
        annotations.Clear();

        if (isWaitingForPromotion || isGameOver) return;

        int square = MouseSquare();
        if (IsBotTurn) OnPremoveClick(square);
        else OnSquareClicked(square);
    }

    static int MouseSquare()
    {
        Vector2 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return BoardCreator.WorldToSquare(world);
    }

    // Right click on a square: highlight it. Right drag from one square to another: arrow.
    // Pressing the right button also cancels a premove.
    void HandleRightMouse(bool blockedByUi)
    {
        if (Input.GetMouseButtonDown(1))
        {
            rightDragStart = blockedByUi ? Square.None : MouseSquare();
            if (!blockedByUi) CancelPremove();
        }

        if (Input.GetMouseButtonUp(1) && rightDragStart != Square.None)
        {
            int end = MouseSquare();
            if (end == rightDragStart) annotations.ToggleMark(end);
            else if (end != Square.None) annotations.ToggleArrow(rightDragStart, end);
            rightDragStart = Square.None;
        }
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
            CancelPremove();
            Debug.Log($"Koniec gry: {status}");
            EndGame.Show(status, winner: Position.SideToMove.Opponent());
        }

        if (IsBotTurn) StartBotSearch();
    }

    // ===== Bot =====

    void StartBotSearch()
    {
        // The bot works on its own copy: it makes and unmakes moves while searching,
        // and the game shows and reads the real position at the same time
        Position searchPosition = Position.Clone();
        BotBase searchingBot = bot;

        botWatch.Restart();
        botTask = Task.Run(() =>
        {
            var watch = Stopwatch.StartNew();
            Move move = searchingBot.ChooseMove(searchPosition);
            return (move, watch.Elapsed.TotalSeconds);
        });
    }

    // Called every frame: plays the bot's move once its background search has finished
    void CheckBotSearch()
    {
        if (botTask == null || !botTask.IsCompleted) return;
        if (botWatch.Elapsed.TotalSeconds < MinBotMoveDelay) return;

        var task = botTask;
        botTask = null;

        if (task.IsFaulted)
        {
            // Shouldn't happen; if a bot ever crashes, play a random legal move so the game doesn't get stuck
            Debug.LogException(task.Exception);
            ApplyMove(legalMoves[Random.Range(0, legalMoves.Count)]);
            return;
        }

        var (move, seconds) = task.Result;

        // Time-managed bots also report how deep they got (0 = move from the opening book)
        string botLabel = bot is ITimedBot timed
            ? $"{bot.GetType().Name} (depth {(timed.LastDepth > 0 ? timed.LastDepth.ToString() : "book")})"
            : bot.GetType().Name;
        UIManager.Instance?.ShowBotThinkTime(botLabel, seconds);

        ApplyMove(move);
        PlayPremove();
    }

    // ===== Premove =====

    // Clicks while the bot is thinking: pick one of your pieces, then the target square
    void OnPremoveClick(int square)
    {
        Side human = botSide.Opponent();
        if (square == Square.None)
        {
            CancelPremove();
            return;
        }

        Piece clicked = Position[square];
        bool ownPiece = !clicked.IsEmpty && clicked.Color == human;

        if (premoveSelected == Square.None || square == premoveSelected)
        {
            // Clicking the already selected piece again just deselects it
            bool sameSquare = square == premoveSelected;
            CancelPremove();
            if (ownPiece && !sameSquare)
            {
                premoveSelected = square;
                lightManager.ShowPremoveSelection(square);
            }
            return;
        }

        if (ownPiece)
        {
            // Another of your pieces: change the selection
            premoveSelected = square;
            lightManager.ShowPremoveSelection(square);
            return;
        }

        premoveFrom = premoveSelected;
        premoveTo = square;
        premoveSelected = Square.None;
        lightManager.ShowPremove(premoveFrom, premoveTo);
    }

    // After the bot's move: play the queued premove if it is legal now, otherwise drop it
    void PlayPremove()
    {
        int selected = premoveSelected;
        int from = premoveFrom, to = premoveTo;
        CancelPremove();

        if (isGameOver || IsBotTurn) return;

        if (from == Square.None)
        {
            // Only a piece was picked: keep it as a normal selection
            if (selected != Square.None && !Position[selected].IsEmpty && Position[selected].Color == Position.SideToMove)
            {
                Select(selected);
            }
            return;
        }

        List<Move> candidates = legalMoves.FindAll(m => m.From == from && m.To == to);
        if (candidates.Count == 0) return;

        // Premoved promotions always become a queen
        Move move = candidates.Find(m => !m.IsPromotion || m.Promotion == PieceType.Queen);
        ApplyMove(move);
    }

    void CancelPremove()
    {
        premoveSelected = Square.None;
        premoveFrom = Square.None;
        premoveTo = Square.None;
        if (lightManager != null) lightManager.ClearPremove();
    }
}
