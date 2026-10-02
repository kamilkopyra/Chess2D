using System;

namespace ChessEngine
{
    // Static exchange evaluation (SEE): the material outcome of the exchange sequence on one square,
    // started by a given move. After the first move both sides recapture on the target square with their
    // least valuable attacker, and each side may stop recapturing when continuing would lose material.
    //
    // Values are in the same units as BotBase.PieceGrades: pawn 100, knight 300, bishop 300, rook 500, queen 900.
    // The result is from the point of view of the side making the move (positive = it wins material).
    //
    // Simplifications (as in most engines): pins and checks are ignored (except that a king never captures
    // onto a square the opponent still attacks), and every pawn recapturing onto the last rank becomes a queen.
    // Uses only the public Position API and the Attacks tables; the pieces already used up in the exchange are
    // tracked in a 64-bit mask, so sliders behind them (x-rays) are found automatically.
    public static class See
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

        // Whether the exchange started by the move gains at least `threshold`
        public static bool IsAtLeast(Position position, Move move, int threshold)
        {
            return Evaluate(position, move) >= threshold;
        }

        private static int PieceValue(PieceType type)
        {
            return type == PieceType.King ? KingValue : Values[(int)type];
        }

        // Square of the least valuable piece of `side` attacking `target`, ignoring the pieces in `removed`
        // (Square.None if there is none)
        private static int LeastValuableAttacker(Position position, int target, Side side, ulong removed, out PieceType type)
        {
            // A pawn of `side` attacks target from the squares an opponent pawn on target would attack
            foreach (int sq in Attacks.Pawn[(int)side.Opponent()][target])
            {
                if ((removed & (1UL << sq)) == 0 && position[sq].Is(PieceType.Pawn, side))
                {
                    type = PieceType.Pawn;
                    return sq;
                }
            }

            foreach (int sq in Attacks.Knight[target])
            {
                if ((removed & (1UL << sq)) == 0 && position[sq].Is(PieceType.Knight, side))
                {
                    type = PieceType.Knight;
                    return sq;
                }
            }

            // Sliders: the first piece still on the board along each ray
            int[][] rays = Attacks.Rays[target];
            int rook = Square.None, queen = Square.None;
            for (int dir = 0; dir < 8; dir++)
            {
                bool diagonal = dir >= Attacks.FirstDiagonal;
                foreach (int sq in rays[dir])
                {
                    if ((removed & (1UL << sq)) != 0) continue;
                    Piece p = position[sq];
                    if (p.IsEmpty) continue;
                    if (p.Color == side)
                    {
                        if (diagonal && p.Type == PieceType.Bishop)
                        {
                            type = PieceType.Bishop;
                            return sq;
                        }
                        if (!diagonal && p.Type == PieceType.Rook) rook = sq;
                        else if (p.Type == PieceType.Queen) queen = sq;
                    }
                    break;
                }
            }

            if (rook != Square.None)
            {
                type = PieceType.Rook;
                return rook;
            }
            if (queen != Square.None)
            {
                type = PieceType.Queen;
                return queen;
            }

            foreach (int sq in Attacks.King[target])
            {
                if ((removed & (1UL << sq)) == 0 && position[sq].Is(PieceType.King, side))
                {
                    type = PieceType.King;
                    return sq;
                }
            }

            type = PieceType.None;
            return Square.None;
        }
    }
}
