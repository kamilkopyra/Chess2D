using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Całe UI gry (UI Toolkit): menu, ustawienia, pasek u góry, promocja, ekran końca gry.
// Tworzy się samo po załadowaniu sceny, nie trzeba go dodawać w edytorze.
// Style są w Resources/UI/Chess.uss.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null || FindFirstObjectByType<PieceMover>() == null) return;
        new GameObject("UIManager").AddComponent<UIManager>();
    }

    private PieceMover mover;
    private VisualElement root;
    private VisualElement overlay;
    private readonly List<VisualElement> cards = new List<VisualElement>();
    private VisualElement menuCard, settingsCard, promotionCard, gameOverCard;
    private VisualElement currentCard;
    private bool overlayOpen;
    private bool gameStarted;

    // HUD
    private VisualElement turnPill, turnDot;
    private Label turnLabel;

    // Menu
    private VisualElement continueButton;

    // Ustawienia
    private readonly List<VisualElement> themeOptions = new List<VisualElement>();
    private readonly List<VisualElement> setOptions = new List<VisualElement>();
    private VisualElement coordsSwitch, hintsSwitch;

    // Koniec gry
    private VisualElement gameOverHero;
    private Label gameOverTitle, gameOverSubtitle;

    // Obrazki figur, które zmieniają się razem z wybranym zestawem
    private readonly List<(VisualElement element, ChessPiece.PieceType type, bool isWhite)> livePieceImages =
        new List<(VisualElement, ChessPiece.PieceType, bool)>();

    void Awake()
    {
        Instance = this;
        mover = FindFirstObjectByType<PieceMover>();

        var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
        panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/ChessTheme");
        panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panelSettings.referenceResolution = new Vector2Int(1920, 1080);
        panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panelSettings.match = 1f; // skalowanie według wysokości ekranu
        panelSettings.sortingOrder = 100;

        var document = gameObject.AddComponent<UIDocument>();
        document.panelSettings = panelSettings;

        root = document.rootVisualElement;
        root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Chess"));
        root.AddToClassList("chess-root");
        root.pickingMode = PickingMode.Ignore;

        BuildHud();

        overlay = new VisualElement { name = "Overlay" };
        overlay.AddToClassList("overlay");
        root.Add(overlay);

        BuildMenu();
        BuildSettings();
        BuildPromotion();
        BuildGameOver();

        GameSettings.Changed += RefreshFromSettings;
        RefreshFromSettings();
        UpdateTurn(true, false);
        ShowMenu();
    }

    void OnDestroy()
    {
        GameSettings.Changed -= RefreshFromSettings;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (overlayOpen && currentCard == settingsCard) ShowMenu();
        else if (overlayOpen && currentCard == menuCard && gameStarted) CloseOverlay();
        else if (!overlayOpen && gameStarted) ShowMenu();
    }

    // ===== API dla gry =====

    // Czy kliknięcie w tym miejscu ekranu trafia w UI (wtedy plansza go ignoruje)
    public bool BlocksBoardInput(Vector3 mouseScreenPosition)
    {
        if (overlayOpen) return true;

        IPanel panel = root.panel;
        if (panel == null) return false;

        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel,
            new Vector2(mouseScreenPosition.x, Screen.height - mouseScreenPosition.y));
        VisualElement picked = panel.Pick(panelPos);
        return picked != null && picked != root && root.Contains(picked);
    }

    public void UpdateTurn(bool whiteToMove, bool inCheck)
    {
        turnPill.RemoveFromClassList("pill--result");
        turnPill.EnableInClassList("pill--check", inCheck);
        turnDot.style.display = DisplayStyle.Flex;
        turnDot.EnableInClassList("pill__dot--white", whiteToMove);
        turnDot.EnableInClassList("pill__dot--black", !whiteToMove);

        string side = whiteToMove ? "White to move" : "Black to move";
        turnLabel.text = inCheck ? $"Check!  {side}" : side;
    }

    public void ShowPromotion(bool isWhite, Action<string> onChosen)
    {
        promotionCard.Clear();

        var title = new Label("Pawn promotion");
        title.AddToClassList("title");
        promotionCard.Add(title);

        var subtitle = new Label("Choose a piece");
        subtitle.AddToClassList("subtitle");
        promotionCard.Add(subtitle);

        var row = new VisualElement();
        row.AddToClassList("promo-row");
        promotionCard.Add(row);

        var choices = new[]
        {
            ("Queen", ChessPiece.PieceType.Queen),
            ("Rook", ChessPiece.PieceType.Rook),
            ("Bishop", ChessPiece.PieceType.Bishop),
            ("Knight", ChessPiece.PieceType.Knight),
        };

        for (int i = 0; i < choices.Length; i++)
        {
            (string pieceName, ChessPiece.PieceType type) = choices[i];

            var choice = new VisualElement();
            choice.AddToClassList("promo-choice");
            var theme = GameSettings.CurrentTheme;
            choice.style.backgroundColor = i % 2 == 0 ? theme.Light : theme.Dark;
            choice.Add(PieceImage(GameSettings.GetPieceSprite(type, isWhite), 100));
            choice.AddManipulator(new Clickable(() =>
            {
                CloseOverlay();
                onChosen(pieceName);
            }));
            row.Add(choice);
        }

        OpenCard(promotionCard);
    }

    // winnerIsWhite: null = remis
    public void ShowGameOver(string title, string subtitle, bool? winnerIsWhite)
    {
        gameOverTitle.text = title;
        gameOverSubtitle.text = subtitle;

        gameOverHero.Clear();
        if (winnerIsWhite.HasValue)
        {
            gameOverHero.Add(PieceImage(GameSettings.GetPieceSprite(ChessPiece.PieceType.King, winnerIsWhite.Value), 128));
        }
        else
        {
            gameOverHero.Add(PieceImage(GameSettings.GetPieceSprite(ChessPiece.PieceType.King, true), 112));
            gameOverHero.Add(PieceImage(GameSettings.GetPieceSprite(ChessPiece.PieceType.King, false), 112));
        }

        // Wynik zostaje też w pasku u góry, gdy gracz wybierze "View board"
        turnPill.RemoveFromClassList("pill--check");
        turnPill.AddToClassList("pill--result");
        turnDot.style.display = DisplayStyle.None;
        turnLabel.text = $"{title}  {subtitle}";

        // Chwila opóźnienia, żeby było widać ostatni ruch
        root.schedule.Execute(() =>
        {
            if (mover.IsGameOver && !overlayOpen) OpenCard(gameOverCard);
        }).StartingIn(550);
    }

    // ===== Budowa ekranów =====

    void BuildHud()
    {
        var hud = new VisualElement { name = "Hud", pickingMode = PickingMode.Ignore };
        hud.AddToClassList("hud");
        root.Add(hud);

        turnPill = new VisualElement();
        turnPill.AddToClassList("pill");
        turnDot = new VisualElement { pickingMode = PickingMode.Ignore };
        turnDot.AddToClassList("pill__dot");
        turnLabel = new Label { pickingMode = PickingMode.Ignore };
        turnLabel.AddToClassList("pill__label");
        turnLabel.AddToClassList("semibold");
        turnPill.Add(turnDot);
        turnPill.Add(turnLabel);
        hud.Add(turnPill);

        var right = new VisualElement { pickingMode = PickingMode.Ignore };
        right.AddToClassList("hud__group");
        right.Add(MakeButton("New game", "small", StartNewGame));
        right.Add(MakeButton("Menu", "small", ShowMenu));
        hud.Add(right);
    }

    void BuildMenu()
    {
        menuCard = NewCard("card--menu");

        var hero = new VisualElement();
        hero.AddToClassList("hero");
        hero.Add(LivePieceImage(ChessPiece.PieceType.Knight, false, 84));
        var king = LivePieceImage(ChessPiece.PieceType.King, true, 112);
        king.AddToClassList("hero__main");
        hero.Add(king);
        hero.Add(LivePieceImage(ChessPiece.PieceType.Knight, true, 84));
        menuCard.Add(hero);

        var title = new Label("Chess");
        title.AddToClassList("title");
        title.AddToClassList("title--xl");
        menuCard.Add(title);

        var subtitle = new Label("Local two-player game");
        subtitle.AddToClassList("subtitle");
        menuCard.Add(subtitle);

        continueButton = MakeButton("Continue", "primary", CloseOverlay);
        menuCard.Add(continueButton);
        menuCard.Add(MakeButton("New game", "primary", StartNewGame));
        menuCard.Add(MakeButton("Settings", "secondary", ShowSettings));
        menuCard.Add(MakeButton("Quit", "ghost", QuitGame));
    }

    void BuildSettings()
    {
        settingsCard = NewCard("card--settings");

        var title = new Label("Settings");
        title.AddToClassList("title");
        settingsCard.Add(title);

        // Kolor planszy
        settingsCard.Add(SectionTitle("BOARD"));
        var themeGrid = new VisualElement();
        themeGrid.AddToClassList("option-grid");
        settingsCard.Add(themeGrid);

        for (int i = 0; i < GameSettings.BoardThemes.Length; i++)
        {
            int index = i;
            var theme = GameSettings.BoardThemes[i];

            var option = NewOption("option--theme", () => GameSettings.BoardThemeIndex = index);
            var mini = new VisualElement { pickingMode = PickingMode.Ignore };
            mini.AddToClassList("mini-board");
            for (int s = 0; s < 4; s++)
            {
                var square = new VisualElement { pickingMode = PickingMode.Ignore };
                square.AddToClassList("mini-board__square");
                square.style.backgroundColor = (s == 0 || s == 3) ? theme.Light : theme.Dark;
                mini.Add(square);
            }
            option.Add(mini);
            option.Add(OptionLabel(theme.Name));
            themeGrid.Add(option);
            themeOptions.Add(option);
        }

        // Zestaw figur
        settingsCard.Add(SectionTitle("PIECES"));
        var setGrid = new VisualElement();
        setGrid.AddToClassList("option-grid");
        settingsCard.Add(setGrid);

        for (int i = 0; i < GameSettings.PieceSets.Length; i++)
        {
            int index = i;
            var set = GameSettings.PieceSets[i];

            var option = NewOption("option--set", () => GameSettings.PieceSetIndex = index);
            var preview = new VisualElement { pickingMode = PickingMode.Ignore };
            preview.AddToClassList("set-preview");
            preview.Add(PieceImage(GameSettings.GetPieceSprite(ChessPiece.PieceType.Knight, true, set), 60));
            preview.Add(PieceImage(GameSettings.GetPieceSprite(ChessPiece.PieceType.Queen, false, set), 60));
            option.Add(preview);
            option.Add(OptionLabel(set.Name));
            setGrid.Add(option);
            setOptions.Add(option);
        }

        // Opcje
        settingsCard.Add(SectionTitle("OPTIONS"));
        settingsCard.Add(SwitchRow("Show coordinates", out coordsSwitch,
            () => GameSettings.ShowCoordinates = !GameSettings.ShowCoordinates));
        settingsCard.Add(SwitchRow("Show legal moves", out hintsSwitch,
            () => GameSettings.ShowHints = !GameSettings.ShowHints));

        var footer = new VisualElement();
        footer.AddToClassList("settings-footer");
        footer.Add(MakeButton("Done", "primary", ShowMenu));
        settingsCard.Add(footer);
    }

    void BuildPromotion()
    {
        promotionCard = NewCard("card--promotion");
    }

    void BuildGameOver()
    {
        gameOverCard = NewCard("card--gameover");

        gameOverHero = new VisualElement();
        gameOverHero.AddToClassList("hero");
        gameOverCard.Add(gameOverHero);

        gameOverTitle = new Label();
        gameOverTitle.AddToClassList("title");
        gameOverTitle.AddToClassList("title--xl");
        gameOverCard.Add(gameOverTitle);

        gameOverSubtitle = new Label();
        gameOverSubtitle.AddToClassList("subtitle");
        gameOverCard.Add(gameOverSubtitle);

        gameOverCard.Add(MakeButton("Rematch", "primary", StartNewGame));
        gameOverCard.Add(MakeButton("View board", "secondary", CloseOverlay));
        gameOverCard.Add(MakeButton("Menu", "ghost", ShowMenu));
    }

    // ===== Nawigacja =====

    void ShowMenu()
    {
        continueButton.style.display = gameStarted ? DisplayStyle.Flex : DisplayStyle.None;
        OpenCard(menuCard);
    }

    void ShowSettings() => OpenCard(settingsCard);

    void StartNewGame()
    {
        gameStarted = true;
        mover.ResetGame();
        CloseOverlay();
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OpenCard(VisualElement card)
    {
        overlayOpen = true;
        currentCard = card;

        foreach (VisualElement c in cards)
        {
            c.style.display = c == card ? DisplayStyle.Flex : DisplayStyle.None;
            c.RemoveFromClassList("card--shown");
        }

        overlay.style.display = DisplayStyle.Flex;

        // Klasy dodane w następnej klatce, żeby zadziałały animacje (transition)
        root.schedule.Execute(() =>
        {
            if (currentCard != card) return;
            overlay.AddToClassList("overlay--visible");
            card.AddToClassList("card--shown");
        }).StartingIn(20);
    }

    void CloseOverlay()
    {
        overlayOpen = false;
        currentCard = null;
        overlay.RemoveFromClassList("overlay--visible");
        foreach (VisualElement c in cards) c.RemoveFromClassList("card--shown");

        root.schedule.Execute(() =>
        {
            if (!overlayOpen) overlay.style.display = DisplayStyle.None;
        }).StartingIn(220);
    }

    void RefreshFromSettings()
    {
        for (int i = 0; i < themeOptions.Count; i++)
            themeOptions[i].EnableInClassList("option--selected", i == GameSettings.BoardThemeIndex);

        for (int i = 0; i < setOptions.Count; i++)
            setOptions[i].EnableInClassList("option--selected", i == GameSettings.PieceSetIndex);

        coordsSwitch.EnableInClassList("switch--on", GameSettings.ShowCoordinates);
        hintsSwitch.EnableInClassList("switch--on", GameSettings.ShowHints);

        foreach (var (element, type, isWhite) in livePieceImages)
            element.style.backgroundImage = new StyleBackground(GameSettings.GetPieceSprite(type, isWhite));
    }

    // ===== Pomocnicze =====

    VisualElement NewCard(string modifier)
    {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.AddToClassList(modifier);
        card.style.display = DisplayStyle.None;
        overlay.Add(card);
        cards.Add(card);
        return card;
    }

    VisualElement MakeButton(string text, string variant, Action onClick)
    {
        var button = new VisualElement();
        button.AddToClassList("btn");
        button.AddToClassList("btn--" + variant);

        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.AddToClassList("btn__label");
        button.Add(label);

        button.AddManipulator(new Clickable(onClick));
        return button;
    }

    VisualElement NewOption(string modifier, Action onClick)
    {
        var option = new VisualElement();
        option.AddToClassList("option");
        option.AddToClassList(modifier);
        option.AddManipulator(new Clickable(onClick));
        return option;
    }

    Label OptionLabel(string text)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.AddToClassList("option__label");
        return label;
    }

    Label SectionTitle(string text)
    {
        var label = new Label(text);
        label.AddToClassList("section-title");
        return label;
    }

    VisualElement SwitchRow(string text, out VisualElement switchElement, Action onClick)
    {
        var row = new VisualElement();
        row.AddToClassList("switch-row");

        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        row.Add(label);

        switchElement = new VisualElement { pickingMode = PickingMode.Ignore };
        switchElement.AddToClassList("switch");
        var knob = new VisualElement { pickingMode = PickingMode.Ignore };
        knob.AddToClassList("switch__knob");
        switchElement.Add(knob);
        row.Add(switchElement);

        row.AddManipulator(new Clickable(onClick));
        return row;
    }

    VisualElement PieceImage(Sprite sprite, float size)
    {
        var image = new VisualElement { pickingMode = PickingMode.Ignore };
        image.AddToClassList("piece-img");
        image.style.width = size;
        image.style.height = size;
        image.style.backgroundImage = new StyleBackground(sprite);
        return image;
    }

    VisualElement LivePieceImage(ChessPiece.PieceType type, bool isWhite, float size)
    {
        var image = PieceImage(GameSettings.GetPieceSprite(type, isWhite), size);
        livePieceImages.Add((image, type, isWhite));
        return image;
    }
}
