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
    // The attackers come from bitboards (Position.AttackersTo). The pieces already used up in the exchange are
    // tracked in a 64-bit mask and taken off the occupied squares, so sliders behind them (x-rays) are found
    // automatically. The first, mailbox version of this class is kept in the tests as SeeReference.
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
        // (Square.None if there is none). Among pieces of the same type the lowest square is taken.
        private static int LeastValuableAttacker(Position position, int target, Side side, ulong removed, out PieceType type)
        {
            ulong occupied = position.Occupied & ~removed;
            ulong attackers = position.AttackersTo(target, occupied) & position.Pieces(side) & ~removed;
            if (attackers != 0)
            {
                for (int t = (int)PieceType.Pawn; t <= (int)PieceType.King; t++)
                {
                    ulong ofType = attackers & position.Pieces((PieceType)t);
                    if (ofType != 0)
                    {
                        type = (PieceType)t;
                        return Bits.LowestSquare(ofType);
                    }
                }
            }

            type = PieceType.None;
            return Square.None;
        }
    }
}
