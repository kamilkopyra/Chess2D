namespace ChessEngine
{
    // Losowe klucze do haszowania pozycji (Zobrist hashing). Hash pozycji to XOR kluczy
    // wszystkich figur na ich polach, strony na ruchu, praw do roszady i pola en passant.
    // Służy do wykrywania powtórzeń, a później do tablicy transpozycji w bocie.
    public static class Zobrist
    {
        public static readonly ulong[,] PieceSquare = new ulong[16, 64]; // [Piece.Index, pole]
        public static readonly ulong[] Castling = new ulong[16];
        public static readonly ulong[] EnPassantFile = new ulong[8];
        public static readonly ulong BlackToMove;

        static Zobrist()
        {
            // Stałe ziarno: te same klucze przy każdym uruchomieniu
            ulong state = 0x9E3779B97F4A7C15UL;

            for (int p = 0; p < 16; p++)
                for (int sq = 0; sq < 64; sq++)
                    PieceSquare[p, sq] = Next(ref state);

            for (int i = 0; i < 16; i++) Castling[i] = Next(ref state);
            for (int i = 0; i < 8; i++) EnPassantFile[i] = Next(ref state);
            BlackToMove = Next(ref state);
        }

        // SplitMix64
        static ulong Next(ref ulong state)
        {
            ulong z = state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
