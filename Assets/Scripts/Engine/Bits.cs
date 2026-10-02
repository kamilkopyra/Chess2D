namespace ChessEngine
{
    // Bit tricks on 64-bit bitboards (bit index = square index, a1 = 0, h8 = 63).
    // On .NET Core 3.0+ these use hardware instructions through System.Numerics.BitOperations;
    // Unity (.NET Standard 2.1) doesn't have that class, so it uses the portable versions below.
    // The portable versions are always compiled, so tests can check them on any runtime.
    public static class Bits
    {
        private const ulong DeBruijn = 0x03F79D71B4CB0A89UL;

        // Index of a single set bit: (bit * DeBruijn) >> 58 is different for each of the 64 bits
        private static readonly int[] DeBruijnIndex = BuildDeBruijnIndex();

        public static int PopCount(ulong bits)
        {
#if NETCOREAPP3_0_OR_GREATER
            return System.Numerics.BitOperations.PopCount(bits);
#else
            return PopCountPortable(bits);
#endif
        }

        // Square of the lowest set bit. bits must not be 0.
        public static int LowestSquare(ulong bits)
        {
#if NETCOREAPP3_0_OR_GREATER
            return System.Numerics.BitOperations.TrailingZeroCount(bits);
#else
            return LowestSquarePortable(bits);
#endif
        }

        // Square of the highest set bit. bits must not be 0.
        public static int HighestSquare(ulong bits)
        {
#if NETCOREAPP3_0_OR_GREATER
            return 63 - System.Numerics.BitOperations.LeadingZeroCount(bits);
#else
            return HighestSquarePortable(bits);
#endif
        }

        // Returns the square of the lowest set bit and clears it. bits must not be 0.
        public static int PopLowest(ref ulong bits)
        {
            int square = LowestSquare(bits);
            bits &= bits - 1;
            return square;
        }

        public static ulong SquareBit(int square) => 1UL << square;

        public static bool Contains(ulong bits, int square) => (bits & (1UL << square)) != 0;

        // SWAR: count bits in pairs, then nibbles, then bytes, and add the bytes with one multiplication
        public static int PopCountPortable(ulong bits)
        {
            bits -= (bits >> 1) & 0x5555555555555555UL;
            bits = (bits & 0x3333333333333333UL) + ((bits >> 2) & 0x3333333333333333UL);
            bits = (bits + (bits >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
            return (int)((bits * 0x0101010101010101UL) >> 56);
        }

        public static int LowestSquarePortable(ulong bits)
        {
            return DeBruijnIndex[(int)(((bits & (0 - bits)) * DeBruijn) >> 58)];
        }

        public static int HighestSquarePortable(ulong bits)
        {
            // Fill everything below the highest bit, then keep only the highest bit
            bits |= bits >> 1;
            bits |= bits >> 2;
            bits |= bits >> 4;
            bits |= bits >> 8;
            bits |= bits >> 16;
            bits |= bits >> 32;
            return DeBruijnIndex[(int)(((bits ^ (bits >> 1)) * DeBruijn) >> 58)];
        }

        private static int[] BuildDeBruijnIndex()
        {
            var index = new int[64];
            for (int i = 0; i < 64; i++)
                index[(int)(((1UL << i) * DeBruijn) >> 58)] = i;
            return index;
        }
    }
}
