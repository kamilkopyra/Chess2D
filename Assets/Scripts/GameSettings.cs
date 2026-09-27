using System;
using System.Collections.Generic;
using UnityEngine;

// Ustawienia gry (kolor planszy, zestaw figur, opcje) zapisywane w PlayerPrefs.
// Każda zmiana wywołuje zdarzenie Changed, więc plansza i UI odświeżają się od razu.
public static class GameSettings
{
    public class BoardTheme
    {
        public string Name;
        public Color Light;
        public Color Dark;

        public BoardTheme(string name, string light, string dark)
        {
            Name = name;
            Light = Hex(light);
            Dark = Hex(dark);
        }
    }

    public class PieceSet
    {
        public string Folder;
        public string Name;

        public PieceSet(string folder, string name)
        {
            Folder = folder;
            Name = name;
        }
    }

    public static readonly BoardTheme[] BoardThemes =
    {
        new BoardTheme("Classic",   "#F0D9B5", "#B58863"),
        new BoardTheme("Green",     "#EEEED2", "#769656"),
        new BoardTheme("Blue",      "#DEE3E6", "#8CA2AD"),
        new BoardTheme("Purple",    "#E6DEF2", "#8B74B3"),
        new BoardTheme("Pink","#F4E1E1", "#C98A94"),
        new BoardTheme("Night",     "#A7B0BD", "#5A6576"),
    };

    public static readonly PieceSet[] PieceSets =
    {
        new PieceSet("cburnett", "Classic"),
        new PieceSet("staunty",  "Staunty"),
        new PieceSet("merida",   "Merida"),
        new PieceSet("chessnut", "Chessnut"),
        new PieceSet("pixel",    "Pixel"),
    };

    public static event Action Changed;

    const string ThemeKey = "chess.boardTheme";
    const string PiecesKey = "chess.pieceSet";
    const string CoordsKey = "chess.showCoordinates";
    const string HintsKey = "chess.showHints";

    public static int BoardThemeIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(ThemeKey, 0), 0, BoardThemes.Length - 1);
        set { PlayerPrefs.SetInt(ThemeKey, value); Save(); }
    }

    public static int PieceSetIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(PiecesKey, 0), 0, PieceSets.Length - 1);
        set { PlayerPrefs.SetInt(PiecesKey, value); Save(); }
    }

    public static bool ShowCoordinates
    {
        get => PlayerPrefs.GetInt(CoordsKey, 1) == 1;
        set { PlayerPrefs.SetInt(CoordsKey, value ? 1 : 0); Save(); }
    }

    public static bool ShowHints
    {
        get => PlayerPrefs.GetInt(HintsKey, 1) == 1;
        set { PlayerPrefs.SetInt(HintsKey, value ? 1 : 0); Save(); }
    }

    public static BoardTheme CurrentTheme => BoardThemes[BoardThemeIndex];
    public static PieceSet CurrentPieceSet => PieceSets[PieceSetIndex];

    static void Save()
    {
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    // Sprite figury z Resources/Pieces/<zestaw>/<w|b><K|Q|R|B|N|P>.png
    static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    public static Sprite GetPieceSprite(ChessEngine.PieceType type, bool isWhite, PieceSet set = null)
    {
        set ??= CurrentPieceSet;
        string path = $"Pieces/{set.Folder}/{(isWhite ? 'w' : 'b')}{PieceLetter(type)}";

        if (!spriteCache.TryGetValue(path, out Sprite sprite) || sprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogError($"Brak grafiki figury: Resources/{path}");
                return null;
            }

            // Sprite tworzony w kodzie: zawsze dokładnie 1 jednostka, niezależnie od ustawień importu
            if (set.Folder == "pixel") tex.filterMode = FilterMode.Point;
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            sprite.name = path;
            spriteCache[path] = sprite;
        }
        return sprite;
    }

    static char PieceLetter(ChessEngine.PieceType type)
    {
        switch (type)
        {
            case ChessEngine.PieceType.King: return 'K';
            case ChessEngine.PieceType.Queen: return 'Q';
            case ChessEngine.PieceType.Rook: return 'R';
            case ChessEngine.PieceType.Bishop: return 'B';
            case ChessEngine.PieceType.Knight: return 'N';
            default: return 'P';
        }
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
