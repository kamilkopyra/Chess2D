namespace ChessEngine
{
    // Tablice liczone raz na starcie: pola atakowane przez skoczka, króla, piona
    // i promienie w 8 kierunkach dla gońca, wieży i hetmana.
    public static class Attacks
    {
        public static readonly int[][] Knight = new int[64][];
        public static readonly int[][] King = new int[64][];

        // Pawn[(int)side][square] = pola, które atakuje pion tej strony stojący na square
        public static readonly int[][][] Pawn = { new int[64][], new int[64][] };

        // Rays[square][direction] = kolejne pola w danym kierunku aż do krawędzi planszy.
        // Kierunki 0-3 są proste (wieża), 4-7 ukośne (goniec).
        public static readonly int[][][] Rays = new int[64][][];

        public const int FirstOrthogonal = 0;
        public const int FirstDiagonal = 4;

        static readonly (int df, int dr)[] KnightSteps =
            { (1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2) };

        static readonly (int df, int dr)[] KingSteps =
            { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };

        static readonly (int df, int dr)[] Directions =
            { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };

        static Attacks()
        {
            for (int sq = 0; sq < 64; sq++)
            {
                int file = Square.File(sq), rank = Square.Rank(sq);

                Knight[sq] = Steps(file, rank, KnightSteps);
                King[sq] = Steps(file, rank, KingSteps);
                Pawn[(int)Side.White][sq] = Steps(file, rank, new[] { (-1, 1), (1, 1) });
                Pawn[(int)Side.Black][sq] = Steps(file, rank, new[] { (-1, -1), (1, -1) });

                Rays[sq] = new int[8][];
                for (int d = 0; d < 8; d++)
                {
                    var ray = new System.Collections.Generic.List<int>();
                    int f = file + Directions[d].df, r = rank + Directions[d].dr;
                    while (Square.IsValid(f, r))
                    {
                        ray.Add(Square.Make(f, r));
                        f += Directions[d].df;
                        r += Directions[d].dr;
                    }
                    Rays[sq][d] = ray.ToArray();
                }
            }
        }

        static int[] Steps(int file, int rank, (int df, int dr)[] steps)
        {
            var result = new System.Collections.Generic.List<int>();
            foreach (var (df, dr) in steps)
                if (Square.IsValid(file + df, rank + dr))
                    result.Add(Square.Make(file + df, rank + dr));
            return result.ToArray();
        }
    }
}
