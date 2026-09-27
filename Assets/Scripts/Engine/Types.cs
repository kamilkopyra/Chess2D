using System;

namespace ChessEngine
{
    public enum PieceType : byte
    {
        None = 0,
        Pawn = 1,
        Knight = 2,
        Bishop = 3,
        Rook = 4,
        Queen = 5,
        King = 6,
    }

    public enum Side : byte
    {
        White = 0,
        Black = 1,
    }

    public static class SideExtensions
    {
        public static Side Opponent(this Side side) => side == Side.White ? Side.Black : Side.White;
    }

    // Figura na polu zapisana w jednym bajcie: bity 0-2 = typ, bit 3 = czarna. 0 = puste pole.
    public readonly struct Piece : IEquatable<Piece>
    {
        private readonly byte value;

        public static readonly Piece None = default;

        public Piece(PieceType type, Side color)
        {
            value = type == PieceType.None ? (byte)0 : (byte)((byte)type | (color == Side.Black ? 8 : 0));
        }

        public PieceType Type => (PieceType)(value & 7);
        public Side Color => (value & 8) != 0 ? Side.Black : Side.White;
        public bool IsEmpty => value == 0;
        public int Index => value; // 0..14, do tablic Zobrista

        public bool Is(PieceType type, Side color) => Type == type && !IsEmpty && Color == color;

        public bool Equals(Piece other) => value == other.value;
        public override bool Equals(object obj) => obj is Piece other && Equals(other);
        public override int GetHashCode() => value;
        public static bool operator ==(Piece a, Piece b) => a.value == b.value;
        public static bool operator !=(Piece a, Piece b) => a.value != b.value;

        // Litera jak w notacji FEN: wielka = białe, mała = czarne
        public char ToFenChar()
        {
            if (IsEmpty) return '.';
            char c = "?pnbrqk"[(int)Type];
            return Color == Side.White ? char.ToUpperInvariant(c) : c;
        }

        public static Piece FromFenChar(char c)
        {
            Side color = char.IsUpper(c) ? Side.White : Side.Black;
            switch (char.ToLowerInvariant(c))
            {
                case 'p': return new Piece(PieceType.Pawn, color);
                case 'n': return new Piece(PieceType.Knight, color);
                case 'b': return new Piece(PieceType.Bishop, color);
                case 'r': return new Piece(PieceType.Rook, color);
                case 'q': return new Piece(PieceType.Queen, color);
                case 'k': return new Piece(PieceType.King, color);
                default: throw new FormatException($"Nieznana figura w FEN: '{c}'");
            }
        }

        public override string ToString() => ToFenChar().ToString();
    }

    // Pola numerowane 0..63: a1 = 0, b1 = 1, ..., h8 = 63 (index = rank * 8 + file).
    // file i rank odpowiadają x i y na planszy w Unity.
    public static class Square
    {
        public const int None = -1;

        public static int File(int square) => square & 7;
        public static int Rank(int square) => square >> 3;
        public static int Make(int file, int rank) => rank * 8 + file;
        public static bool IsValid(int file, int rank) => file >= 0 && file < 8 && rank >= 0 && rank < 8;

        public static string ToName(int square) =>
            square == None ? "-" : $"{(char)('a' + File(square))}{(char)('1' + Rank(square))}";

        public static int Parse(string name)
        {
            if (name == "-") return None;
            if (name.Length != 2) throw new FormatException($"Złe pole: '{name}'");
            int file = name[0] - 'a', rank = name[1] - '1';
            if (!IsValid(file, rank)) throw new FormatException($"Złe pole: '{name}'");
            return Make(file, rank);
        }
    }

    [Flags]
    public enum CastlingRights : byte
    {
        None = 0,
        WhiteKingside = 1,
        WhiteQueenside = 2,
        BlackKingside = 4,
        BlackQueenside = 8,
        All = 15,
    }

    [Flags]
    public enum MoveFlags : byte
    {
        None = 0,
        Capture = 1,
        DoublePawnPush = 2,
        EnPassant = 4,
        CastleKingside = 8,
        CastleQueenside = 16,
    }

    public readonly struct Move : IEquatable<Move>
    {
        public readonly byte From;
        public readonly byte To;
        public readonly PieceType Promotion; // None, jeśli to nie promocja
        public readonly MoveFlags Flags;

        public Move(int from, int to, MoveFlags flags = MoveFlags.None, PieceType promotion = PieceType.None)
        {
            From = (byte)from;
            To = (byte)to;
            Flags = flags;
            Promotion = promotion;
        }

        public bool IsCapture => (Flags & MoveFlags.Capture) != 0;
        public bool IsEnPassant => (Flags & MoveFlags.EnPassant) != 0;
        public bool IsCastle => (Flags & (MoveFlags.CastleKingside | MoveFlags.CastleQueenside)) != 0;
        public bool IsPromotion => Promotion != PieceType.None;

        public bool Equals(Move other) =>
            From == other.From && To == other.To && Promotion == other.Promotion && Flags == other.Flags;
        public override bool Equals(object obj) => obj is Move other && Equals(other);
        public override int GetHashCode() => From | (To << 6) | ((int)Promotion << 12) | ((int)Flags << 16);
        public static bool operator ==(Move a, Move b) => a.Equals(b);
        public static bool operator !=(Move a, Move b) => !a.Equals(b);

        // Notacja UCI, np. "e2e4", "e7e8q"
        public override string ToString()
        {
            string s = Square.ToName(From) + Square.ToName(To);
            if (IsPromotion) s += "?pnbrqk"[(int)Promotion];
            return s;
        }
    }

    public enum GameStatus
    {
        Ongoing,
        Checkmate,
        Stalemate,
        FiftyMoveRule,
        ThreefoldRepetition,
        InsufficientMaterial,
    }
}
