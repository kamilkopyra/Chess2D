namespace ChessEngine
{
    // Precomputed bitboard tables (bit index = square index, a1 = 0, h8 = 63):
    // attacks of knight, king and pawns, rays, squares between two squares, and magic bitboards
    // for rook and bishop attacks. Built once on startup from the square lists in Attacks.
    public static class Bitboards
    {
        public const ulong FileA = 0x0101010101010101UL;
        public const ulong Rank1 = 0xFFUL;

        public static readonly ulong[] Files = new ulong[8];
        public static readonly ulong[] Ranks = new ulong[8];

        public static readonly ulong[] KnightAttacks = new ulong[64];
        public static readonly ulong[] KingAttacks = new ulong[64];

        // PawnAttacks[(int)side][square] = squares attacked by a pawn of that side standing on square
        public static readonly ulong[][] PawnAttacks = { new ulong[64], new ulong[64] };

        // RayMask[square * 8 + direction] = all squares of Attacks.Rays[square][direction]
        public static readonly ulong[] RayMask = new ulong[64 * 8];

        // Between[a * 64 + b] = squares strictly between a and b if they share a line, else 0
        public static readonly ulong[] Between = new ulong[64 * 64];

        // Line[a * 64 + b] = the whole line (edge to edge) through a and b if they share one, else 0
        public static readonly ulong[] Line = new ulong[64 * 64];

        // Magic bitboards: for a slider on `square`, the relevant blockers (occupied & Mask) multiplied
        // by Magic and shifted right by Shift give a unique index into that square's part of the table.
        // The magic numbers were found once with a random search for sparse numbers that give no
        // harmful collisions; finding them on every startup was too slow (Unity editor runs unoptimized code).
        private static readonly ulong[] RookMask = new ulong[64];
        private static readonly ulong[] RookMagic =
        {
            0x1080004008801020UL, 0x0840092002C03000UL, 0x1900200010400900UL, 0x0880100008000480UL,
            0x4200100420080200UL, 0x8100020100080400UL, 0x0200040110886200UL, 0x0200008040220411UL,
            0x0404800084400220UL, 0x0000401000402000UL, 0x0086001081220440UL, 0x0408800800100280UL,
            0x000A001201040820UL, 0x8848800200840080UL, 0x4001000100040200UL, 0x0442000102105084UL,
            0x9080010020804100UL, 0x0040404000201009UL, 0x0000808010002009UL, 0x2200090021D00100UL,
            0x0008008008040080UL, 0x0004004002010040UL, 0x0011040008015042UL, 0x00000A0001768104UL,
            0x0000800080204009UL, 0x2010004140002001UL, 0x9800200280100080UL, 0x1000100080080080UL,
            0x0442000A00049020UL, 0x2100040080020080UL, 0x0800120400900148UL, 0x0010040A00128541UL,
            0x2800804000800030UL, 0x1010002000400041UL, 0x4000200011004100UL, 0x0610008410800800UL,
            0x0400802402800800UL, 0xC100020080800400UL, 0x0002000802000401UL, 0x0182085882000401UL,
            0x0220204000808000UL, 0x2860100040024022UL, 0x0001002004110040UL, 0x99101042000A0020UL,
            0x0004080004008080UL, 0x0010040002008080UL, 0x2012004881020004UL, 0x8300842444820011UL,
            0x0088403882010200UL, 0x0820400080210100UL, 0x0110910040A00300UL, 0x0801100280080480UL,
            0x0242009008200600UL, 0x1002000489500200UL, 0x0040800200010080UL, 0x0091800041000080UL,
            0x0000209300488001UL, 0x04C1002414824001UL, 0x020020000B001041UL, 0x7000100004200901UL,
            0x8002002004100802UL, 0x30010002084C0007UL, 0x0888221800813004UL, 0x4000002840840112UL
        };
        private static readonly int[] RookShift = new int[64];
        private static readonly int[] RookOffset = new int[64];
        private static readonly ulong[] RookTable;

        private static readonly ulong[] BishopMask = new ulong[64];
        private static readonly ulong[] BishopMagic =
        {
            0x0020428400408200UL, 0x2008010104210004UL, 0x02D0009200480190UL, 0x0018158B00010100UL,
            0x02C4042132048008UL, 0x020082202000C221UL, 0x4000421050080009UL, 0x0210140202022020UL,
            0x00C0101410042248UL, 0x0405204800D48080UL, 0x3800C89200420002UL, 0x180844124A020440UL,
            0x04403410A8002221UL, 0x4040209004200400UL, 0x084004020202A204UL, 0x3010002104022000UL,
            0x00200240A9110900UL, 0x2302800404080210UL, 0x0204188800240010UL, 0x8048000C01401200UL,
            0x120C001A11040900UL, 0x0000401200500440UL, 0x00004040840420A0UL, 0x0020930822880804UL,
            0x4044401090900161UL, 0x0034100015210804UL, 0x8004100009010120UL, 0x48C8080000820500UL,
            0x0080848004002000UL, 0x0801004012005044UL, 0x000080902C040400UL, 0x0004009005004100UL,
            0x0B103010048A0200UL, 0x8004100203181A00UL, 0x0800140200100080UL, 0x8401010800910040UL,
            0x0840010011290040UL, 0x40100214202E1000UL, 0x0842040040010840UL, 0x0028010040010860UL,
            0x00080202A2051000UL, 0x4200841008084204UL, 0x0021120110000D02UL, 0x48C1004208000084UL,
            0x0010088100414400UL, 0x0021101000420580UL, 0x0010040558401410UL, 0x200C0C82A1050205UL,
            0x0011108820088000UL, 0x0001011910120402UL, 0x1580008608091248UL, 0x8010018020880C02UL,
            0x20A1101032088480UL, 0x0080100408082800UL, 0x28100401140401C0UL, 0x8002102200930012UL,
            0x4001040082080200UL, 0x082200A498081808UL, 0x000508610080D003UL, 0x0052020044842402UL,
            0x4800A00140C84840UL, 0x5000000848080820UL, 0x0101086004240040UL, 0x0028280808005014UL
        };
        private static readonly int[] BishopShift = new int[64];
        private static readonly int[] BishopOffset = new int[64];
        private static readonly ulong[] BishopTable;

        static Bitboards()
        {
            for (int i = 0; i < 8; i++)
            {
                Files[i] = FileA << i;
                Ranks[i] = Rank1 << (8 * i);
            }

            for (int sq = 0; sq < 64; sq++)
            {
                KnightAttacks[sq] = ToBitboard(Attacks.Knight[sq]);
                KingAttacks[sq] = ToBitboard(Attacks.King[sq]);
                PawnAttacks[(int)Side.White][sq] = ToBitboard(Attacks.Pawn[(int)Side.White][sq]);
                PawnAttacks[(int)Side.Black][sq] = ToBitboard(Attacks.Pawn[(int)Side.Black][sq]);
                for (int d = 0; d < 8; d++)
                    RayMask[sq * 8 + d] = ToBitboard(Attacks.Rays[sq][d]);
            }

            for (int a = 0; a < 64; a++)
            {
                for (int d = 0; d < 8; d++)
                {
                    int[] ray = Attacks.Rays[a][d];
                    // The opposite direction: 0<->1, 2<->3, 4<->7, 5<->6
                    int opposite = d < 4 ? d ^ 1 : 11 - d;
                    ulong line = RayMask[a * 8 + d] | RayMask[a * 8 + opposite] | (1UL << a);
                    ulong between = 0;
                    foreach (int b in ray)
                    {
                        Between[a * 64 + b] = between;
                        Line[a * 64 + b] = line;
                        between |= 1UL << b;
                    }
                }
            }

            RookTable = BuildMagicTable(false, RookMask, RookMagic, RookShift, RookOffset);
            BishopTable = BuildMagicTable(true, BishopMask, BishopMagic, BishopShift, BishopOffset);
        }

        public static ulong RookAttacks(int square, ulong occupied)
        {
            return RookTable[RookOffset[square] + (int)(((occupied & RookMask[square]) * RookMagic[square]) >> RookShift[square])];
        }

        public static ulong BishopAttacks(int square, ulong occupied)
        {
            return BishopTable[BishopOffset[square] + (int)(((occupied & BishopMask[square]) * BishopMagic[square]) >> BishopShift[square])];
        }

        public static ulong QueenAttacks(int square, ulong occupied) => RookAttacks(square, occupied) | BishopAttacks(square, occupied);

        // Slider attacks by walking the rays square by square (slow; used to build the tables and in tests).
        // Each ray goes up to and including the first occupied square.
        public static ulong SlidingAttacksSlow(int square, ulong occupied, bool diagonal)
        {
            ulong attacks = 0;
            int first = diagonal ? Attacks.FirstDiagonal : Attacks.FirstOrthogonal;
            for (int d = first; d < first + 4; d++)
            {
                foreach (int sq in Attacks.Rays[square][d])
                {
                    attacks |= 1UL << sq;
                    if ((occupied & (1UL << sq)) != 0) break;
                }
            }
            return attacks;
        }

        private static ulong ToBitboard(int[] squares)
        {
            ulong bits = 0;
            foreach (int sq in squares) bits |= 1UL << sq;
            return bits;
        }

        // Computes the masks, shifts and offsets and fills the attack table for the given magic numbers
        private static ulong[] BuildMagicTable(bool diagonal, ulong[] masks, ulong[] magics, int[] shifts, int[] offsets)
        {
            int first = diagonal ? Attacks.FirstDiagonal : Attacks.FirstOrthogonal;
            int totalSize = 0;
            for (int sq = 0; sq < 64; sq++)
            {
                // A blocker on the last square of a ray doesn't change the attacks: leave the edges out
                ulong mask = 0;
                for (int d = first; d < first + 4; d++)
                {
                    int[] ray = Attacks.Rays[sq][d];
                    for (int i = 0; i < ray.Length - 1; i++) mask |= 1UL << ray[i];
                }
                masks[sq] = mask;
                int bits = Bits.PopCount(mask);
                shifts[sq] = 64 - bits;
                offsets[sq] = totalSize;
                totalSize += 1 << bits;
            }

            var table = new ulong[totalSize];
            var filled = new bool[totalSize];
            for (int sq = 0; sq < 64; sq++)
            {
                ulong mask = masks[sq];

                // Every subset of the mask (Carry-Rippler trick)
                ulong subset = 0;
                do
                {
                    int index = offsets[sq] + (int)((subset * magics[sq]) >> shifts[sq]);
                    ulong attacks = SlidingAttacksSlow(sq, subset, diagonal);
                    // Two blocker sets may share a slot only if they give the same attacks
                    if (filled[index] && table[index] != attacks)
                        throw new System.InvalidOperationException($"Bad magic number for square {sq}");
                    table[index] = attacks;
                    filled[index] = true;
                    subset = (subset - mask) & mask;
                } while (subset != 0);
            }
            return table;
        }
    }
}
