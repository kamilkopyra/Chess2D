using System;

namespace ChessEngine
{
    // Position evaluation: material + piece-square tables, with a tapered score that blends a
    // middlegame and an endgame evaluation depending on how much material is left.
    // Tables and piece values are the "Simplified Evaluation Function" by Tomasz Michniewski
    // (https://www.chessprogramming.org/Simplified_Evaluation_Function); the endgame pawn table
    // is an extra one that rewards passed-pawn-like advancement more strongly.
    public static class Evaluation
    {
        // Indexed by PieceType: None, Pawn, Knight, Bishop, Rook, Queen, King
        public static readonly int[] PieceValues = { 0, 100, 320, 330, 500, 900, 0 };

        // Game phase: 24 with all minor and major pieces on the board, 0 when only kings and pawns are left
        private static readonly int[] PhaseWeights = { 0, 0, 1, 1, 2, 4, 0 };
        private const int MaxPhase = 24;

        // All tables are written from White's point of view with rank 8 in the first row,
        // exactly like a board diagram. TableIndex() converts a square to an index.

        private static readonly int[] PawnMiddlegame =
        {
              0,   0,   0,   0,   0,   0,   0,   0,
             50,  50,  50,  50,  50,  50,  50,  50,
             10,  10,  20,  30,  30,  20,  10,  10,
              5,   5,  10,  25,  25,  10,   5,   5,
              0,   0,   0,  20,  20,   0,   0,   0,
              5,  -5, -10,   0,   0, -10,  -5,   5,
              5,  10,  10, -20, -20,  10,  10,   5,
              0,   0,   0,   0,   0,   0,   0,   0,
        };

        // In the endgame pawns are worth more the closer they get to promotion
        private static readonly int[] PawnEndgame =
        {
              0,   0,   0,   0,   0,   0,   0,   0,
            100, 100, 100, 100, 100, 100, 100, 100,
             60,  60,  60,  60,  60,  60,  60,  60,
             35,  35,  35,  35,  35,  35,  35,  35,
             20,  20,  20,  20,  20,  20,  20,  20,
             10,  10,  10,  10,  10,  10,  10,  10,
              0,   0,   0,   0,   0,   0,   0,   0,
              0,   0,   0,   0,   0,   0,   0,   0,
        };

        private static readonly int[] Knight =
        {
            -50, -40, -30, -30, -30, -30, -40, -50,
            -40, -20,   0,   0,   0,   0, -20, -40,
            -30,   0,  10,  15,  15,  10,   0, -30,
            -30,   5,  15,  20,  20,  15,   5, -30,
            -30,   0,  15,  20,  20,  15,   0, -30,
            -30,   5,  10,  15,  15,  10,   5, -30,
            -40, -20,   0,   5,   5,   0, -20, -40,
            -50, -40, -30, -30, -30, -30, -40, -50,
        };

        private static readonly int[] Bishop =
        {
            -20, -10, -10, -10, -10, -10, -10, -20,
            -10,   0,   0,   0,   0,   0,   0, -10,
            -10,   0,   5,  10,  10,   5,   0, -10,
            -10,   5,   5,  10,  10,   5,   5, -10,
            -10,   0,  10,  10,  10,  10,   0, -10,
            -10,  10,  10,  10,  10,  10,  10, -10,
            -10,   5,   0,   0,   0,   0,   5, -10,
            -20, -10, -10, -10, -10, -10, -10, -20,
        };

        private static readonly int[] Rook =
        {
              0,   0,   0,   0,   0,   0,   0,   0,
              5,  10,  10,  10,  10,  10,  10,   5,
             -5,   0,   0,   0,   0,   0,   0,  -5,
             -5,   0,   0,   0,   0,   0,   0,  -5,
             -5,   0,   0,   0,   0,   0,   0,  -5,
             -5,   0,   0,   0,   0,   0,   0,  -5,
             -5,   0,   0,   0,   0,   0,   0,  -5,
              0,   0,   0,   5,   5,   0,   0,   0,
        };

        private static readonly int[] Queen =
        {
            -20, -10, -10,  -5,  -5, -10, -10, -20,
            -10,   0,   0,   0,   0,   0,   0, -10,
            -10,   0,   5,   5,   5,   5,   0, -10,
             -5,   0,   5,   5,   5,   5,   0,  -5,
              0,   0,   5,   5,   5,   5,   0,  -5,
            -10,   5,   5,   5,   5,   5,   0, -10,
            -10,   0,   5,   0,   0,   0,   0, -10,
            -20, -10, -10,  -5,  -5, -10, -10, -20,
        };

        // Middlegame: the king hides behind its pawns after castling
        private static readonly int[] KingMiddlegame =
        {
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -20, -30, -30, -40, -40, -30, -30, -20,
            -10, -20, -20, -20, -20, -20, -20, -10,
             20,  20,   0,   0,   0,   0,  20,  20,
             20,  30,  10,   0,   0,  10,  30,  20,
        };

        // Endgame: the king becomes an active piece and belongs in the centre
        private static readonly int[] KingEndgame =
        {
            -50, -40, -30, -20, -20, -30, -40, -50,
            -30, -20, -10,   0,   0, -10, -20, -30,
            -30, -10,  20,  30,  30,  20, -10, -30,
            -30, -10,  30,  40,  40,  30, -10, -30,
            -30, -10,  30,  40,  40,  30, -10, -30,
            -30, -10,  20,  30,  30,  20, -10, -30,
            -30, -30,   0,   0,   0,   0, -30, -30,
            -50, -30, -30, -30, -30, -30, -30, -50,
        };

        // Score from the point of view of the side to move (positive = good for the side to move)
        public static int Evaluate(Position position)
        {
            int middlegame = 0, endgame = 0, phase = 0;

            for (int square = 0; square < 64; square++)
            {
                Piece piece = position[square];
                if (piece.IsEmpty) continue;

                int index = TableIndex(square, piece.Color);
                int material = PieceValues[(int)piece.Type];
                int mg, eg;

                switch (piece.Type)
                {
                    case PieceType.Pawn:
                        mg = PawnMiddlegame[index];
                        eg = PawnEndgame[index];
                        break;
                    case PieceType.Knight:
                        mg = eg = Knight[index];
                        break;
                    case PieceType.Bishop:
                        mg = eg = Bishop[index];
                        break;
                    case PieceType.Rook:
                        mg = eg = Rook[index];
                        break;
                    case PieceType.Queen:
                        mg = eg = Queen[index];
                        break;
                    default: // King
                        mg = KingMiddlegame[index];
                        eg = KingEndgame[index];
                        break;
                }

                int sign = piece.Color == Side.White ? 1 : -1;
                middlegame += sign * (material + mg);
                endgame += sign * (material + eg);
                phase += PhaseWeights[(int)piece.Type];
            }

            // Blend: full middlegame score with all pieces on the board, full endgame score with none left
            phase = Math.Min(phase, MaxPhase);
            int score = (middlegame * phase + endgame * (MaxPhase - phase)) / MaxPhase;

            return position.SideToMove == Side.White ? score : -score;
        }

        // "Mop-up" evaluation for won endgames without enemy pawns (e.g. K+Q vs K, K+R vs K):
        // mating a lone king needs it pushed to the edge and our own king brought close to it.
        // Returns a bonus from the point of view of the side to move; 0 when it doesn't apply.
        public static int MopUp(Position position)
        {
            int whiteMaterial = 0, blackMaterial = 0;
            bool whitePawns = false, blackPawns = false;
            for (int square = 0; square < 64; square++)
            {
                Piece piece = position[square];
                if (piece.IsEmpty) continue;
                if (piece.Color == Side.White) whiteMaterial += PieceValues[(int)piece.Type];
                else blackMaterial += PieceValues[(int)piece.Type];
                if (piece.Type == PieceType.Pawn)
                {
                    if (piece.Color == Side.White) whitePawns = true;
                    else blackPawns = true;
                }
            }

            // Only with a clear advantage (at least a rook more) against a side without pawns
            int score = 0;
            if (whiteMaterial - blackMaterial >= 400 && !blackPawns)
                score = MopUpBonus(position.KingSquare(Side.White), position.KingSquare(Side.Black));
            else if (blackMaterial - whiteMaterial >= 400 && !whitePawns)
                score = -MopUpBonus(position.KingSquare(Side.Black), position.KingSquare(Side.White));

            return position.SideToMove == Side.White ? score : -score;
        }

        // Bonus for the stronger side: losing king far from the centre, kings close to each other
        private static int MopUpBonus(int strongKing, int weakKing)
        {
            int weakFile = Square.File(weakKing), weakRank = Square.Rank(weakKing);
            int centreDistance = Math.Max(3 - weakFile, weakFile - 4) + Math.Max(3 - weakRank, weakRank - 4);
            int kingsDistance = Math.Abs(Square.File(strongKing) - weakFile) + Math.Abs(Square.Rank(strongKing) - weakRank);
            return 10 * centreDistance + 4 * (14 - kingsDistance);
        }

        // Tables are drawn with rank 8 first. For White, a1 (square 0) is the last row: index = square ^ 56.
        // Black sees the board upside down, so its pieces use the square itself (a8 for Black is like a1 for White).
        private static int TableIndex(int square, Side color)
        {
            return color == Side.White ? square ^ 56 : square;
        }
    }
}
