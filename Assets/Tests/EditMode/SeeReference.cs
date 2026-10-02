using System;

namespace ChessEngine.Tests
{
    // The first SEE implementation (walking the rays square by square on the mailbox board), kept only as an
    // independent reference for the bitboard version in See.cs: both must give the same result for every move.
    public static class SeeReference
    {
        public static readonly int[] Values = { 0, 100, 300, 300, 500, 900, 0 };

        // The king is never captured in a legal exchange; a big value makes a pseudo-legal king capture
        // into a defended square look like a huge loss
        private const int KingValue = 20_000;

        private const int MaxExchange = 40;

        public static int Evaluate(Position position, Move move)
        {
            int to = move.To;
            int from = move.From;
            Piece mover = position[from];
            Side side = mover.Color;

            Span<int> gain = stackalloc int[MaxExchange];
            ulong removed = 1UL << from;

            // Value of the first captured piece (en passant: the pawn is not on `to`, take it off the board)
            int captured;
            if (move.IsEnPassant)
            {
                captured = Values[(int)PieceType.Pawn];
                removed |= 1UL << Square.Make(Square.File(to), Square.Rank(from));
            }
            else
            {
                captured = PieceValue(position[to].Type);
            }

            // The piece standing on `to` after the first move (a promoted pawn counts as the new piece)
            int onSquare;
            if (move.IsPromotion)
            {
                captured += Values[(int)move.Promotion] - Values[(int)PieceType.Pawn];
                onSquare = Values[(int)move.Promotion];
            }
            else
            {
                onSquare = PieceValue(mover.Type);
            }

            gain[0] = captured;
            int d = 0;
            side = side.Opponent();
            bool promotionRank = Square.Rank(to) == 0 || Square.Rank(to) == 7;

            while (d + 1 < MaxExchange)
            {
                int attacker = LeastValuableAttacker(position, to, side, removed, out PieceType type);
                if (attacker == Square.None) break;

                if (type == PieceType.King)
                {
                    // The king may only capture when the opponent can't recapture
                    if (LeastValuableAttacker(position, to, side.Opponent(), removed | (1UL << attacker), out _) != Square.None)
                    {
                        break;
                    }
                }

                d++;
                // Gain of this capture, if the other side then stops: the piece on the square minus what was lost so far
                gain[d] = onSquare - gain[d - 1];
                if (type == PieceType.Pawn && promotionRank)
                {
                    gain[d] += Values[(int)PieceType.Queen] - Values[(int)PieceType.Pawn];
                    onSquare = Values[(int)PieceType.Queen];
                }
                else
                {
                    onSquare = PieceValue(type);
                }

                removed |= 1UL << attacker;
                side = side.Opponent();
            }

            // Back up the sequence: each side either takes or stops, whichever is better for it
            while (d > 0)
            {
                gain[d - 1] = -Math.Max(-gain[d - 1], gain[d]);
                d--;
            }
            return gain[0];
        }

        private static int PieceValue(PieceType type)
        {
            return type == PieceType.King ? KingValue : Values[(int)type];
        }

        // Square of the least valuable piece of `side` attacking `target`, ignoring the pieces in `removed`
        // (Square.None if there is none). Among pieces of the same type the lowest square is taken - the same
        // rule as in See.cs (which of two equal pieces captures first can change the result through x-rays).
        private static int LeastValuableAttacker(Position position, int target, Side side, ulong removed, out PieceType type)
        {
            // Lowest attacking square per piece type (index = PieceType)
            Span<int> lowest = stackalloc int[7];
            lowest.Fill(int.MaxValue);

            // A pawn of `side` attacks target from the squares an opponent pawn on target would attack
            foreach (int sq in Attacks.Pawn[(int)side.Opponent()][target])
                if ((removed & (1UL << sq)) == 0 && position[sq].Is(PieceType.Pawn, side))
                    lowest[(int)PieceType.Pawn] = Math.Min(lowest[(int)PieceType.Pawn], sq);

            foreach (int sq in Attacks.Knight[target])
                if ((removed & (1UL << sq)) == 0 && position[sq].Is(PieceType.Knight, side))
                    lowest[(int)PieceType.Knight] = Math.Min(lowest[(int)PieceType.Knight], sq);

            // Sliders: the first piece still on the board along each ray
            int[][] rays = Attacks.Rays[target];
            for (int dir = 0; dir < 8; dir++)
            {
                bool diagonal = dir >= Attacks.FirstDiagonal;
                foreach (int sq in rays[dir])
                {
                    if ((removed & (1UL << sq)) != 0) continue;
                    Piece p = position[sq];
                    if (p.IsEmpty) continue;
                    if (p.Color == side && (p.Type == PieceType.Queen || p.Type == (diagonal ? PieceType.Bishop : PieceType.Rook)))
                        lowest[(int)p.Type] = Math.Min(lowest[(int)p.Type], sq);
                    break;
                }
            }

            foreach (int sq in Attacks.King[target])
                if ((removed & (1UL << sq)) == 0 && position[sq].Is(PieceType.King, side))
                    lowest[(int)PieceType.King] = sq;

            for (int t = (int)PieceType.Pawn; t <= (int)PieceType.King; t++)
            {
                if (lowest[t] != int.MaxValue)
                {
                    type = (PieceType)t;
                    return lowest[t];
                }
            }

            type = PieceType.None;
            return Square.None;
        }
    }
}
