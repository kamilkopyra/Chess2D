using System;

namespace ChessEngine.Tests
{
    // The evaluation exactly as it was before it was rewritten on bitboards (Bot_v18 era), kept unchanged
    // as the reference: the bitboard Evaluation must give the same score for every position.
    //
    // Position evaluation: material + piece-square tables, with a tapered score that blends a
    // middlegame and an endgame evaluation depending on how much material is left.
    // Tables and piece values are the "Simplified Evaluation Function" by Tomasz Michniewski
    // (https://www.chessprogramming.org/Simplified_Evaluation_Function); the endgame pawn table
    // is an extra one that rewards passed-pawn-like advancement more strongly.
    internal static class EvaluationReference
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
        public static int Evaluate(Position position) => Evaluate(position, false);

        // Evaluate + pawn structure and piece placement terms (used by Bot_v15 and newer)
        public static int EvaluateWithStructure(Position position) => Evaluate(position, true);

        // Evaluate + structure + king safety (used by Bot_v16 and newer)
        public static int EvaluateWithKingSafety(Position position) => Evaluate(position, true, true);

        // EvaluateWithKingSafety + MopUp computed in one pass over the board (used by Bot_v17 and newer).
        // Gives exactly the same score as EvaluateWithKingSafety(position) + MopUp(position).
        public static int EvaluateFull(Position position) => Evaluate(position, true, true, true);

        // Only the structure terms, for tests
        public static int Structure(Position position) => Evaluate(position, true) - Evaluate(position, false);

        // Only the king safety terms, for tests
        public static int KingSafety(Position position) => Evaluate(position, true, true) - Evaluate(position, true);

        // withMopUp needs withStructure (it uses the pawn counts collected for it)
        private static int Evaluate(Position position, bool withStructure, bool withKingSafety = false, bool withMopUp = false)
        {
            int middlegame = 0, endgame = 0, phase = 0;
            int whiteMaterial = 0, blackMaterial = 0;

            // Collected for the structure terms during the same pass over the board.
            // Per side and file (index side * 8 + file): number of pawns and the lowest/highest rank of a pawn on it.
            // stackalloc: no garbage for the collector, this runs millions of times per search.
            // Without structure terms nothing is allocated, so the older bots run exactly as before.
            Span<int> pawnCount = withStructure ? stackalloc int[16] : default;
            Span<int> minRank = withStructure ? stackalloc int[16] : default;
            Span<int> maxRank = withStructure ? stackalloc int[16] : default;
            Span<int> bishops = withStructure ? stackalloc int[2] : default;
            Span<int> pieceSquares = withStructure ? stackalloc int[64] : default;   // squares of pawns and rooks
            int pieceCount = 0;
            if (withStructure)
            {
                minRank.Fill(8);
                maxRank.Fill(-1);
            }

            for (int square = 0; square < 64; square++)
            {
                Piece piece = position[square];
                if (piece.IsEmpty) continue;

                if (withStructure)
                {
                    int side = (int)piece.Color;
                    if (piece.Type == PieceType.Pawn)
                    {
                        int file = Square.File(square), rank = Square.Rank(square);
                        pawnCount[side * 8 + file]++;
                        minRank[side * 8 + file] = Math.Min(minRank[side * 8 + file], rank);
                        maxRank[side * 8 + file] = Math.Max(maxRank[side * 8 + file], rank);
                        pieceSquares[pieceCount++] = square;
                    }
                    else if (piece.Type == PieceType.Rook)
                    {
                        pieceSquares[pieceCount++] = square;
                    }
                    else if (piece.Type == PieceType.Bishop)
                    {
                        bishops[side]++;
                    }
                }

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
                if (sign > 0) whiteMaterial += material; else blackMaterial += material;
                middlegame += sign * (material + mg);
                endgame += sign * (material + eg);
                phase += PhaseWeights[(int)piece.Type];
            }

            if (withStructure)
            {
                AddStructure(position, pawnCount, minRank, maxRank, bishops, pieceSquares.Slice(0, pieceCount),
                             ref middlegame, ref endgame);
            }

            if (withKingSafety)
            {
                // Only in the middlegame part: in the endgame the king should leave its shelter and be active
                middlegame += KingShelter(position, Side.White, pawnCount) - KingShelter(position, Side.Black, pawnCount);
            }

            // Blend: full middlegame score with all pieces on the board, full endgame score with none left
            phase = Math.Min(phase, MaxPhase);
            int score = (middlegame * phase + endgame * (MaxPhase - phase)) / MaxPhase;

            if (withMopUp)
            {
                // Same rule as MopUp(): at least a rook more against a side without pawns
                bool whitePawns = false, blackPawns = false;
                for (int file = 0; file < 8; file++)
                {
                    whitePawns |= pawnCount[file] > 0;
                    blackPawns |= pawnCount[8 + file] > 0;
                }
                if (whiteMaterial - blackMaterial >= 400 && !blackPawns)
                    score += MopUpBonus(position.KingSquare(Side.White), position.KingSquare(Side.Black));
                else if (blackMaterial - whiteMaterial >= 400 && !whitePawns)
                    score -= MopUpBonus(position.KingSquare(Side.Black), position.KingSquare(Side.White));
            }

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

        // ===== Pawn structure and piece placement (used by Bot_v15 and newer, on top of Evaluate) =====

        // Passed pawn bonus by rank counted from the pawn's own side (index 1 = starting rank, 6 = one step from promotion)
        private static readonly int[] PassedMiddlegame = { 0, 0, 5, 10, 20, 35, 60, 0 };
        private static readonly int[] PassedEndgame = { 0, 5, 10, 20, 40, 70, 110, 0 };

        private const int DoubledMiddlegame = -10, DoubledEndgame = -20;     // per extra pawn on the same file
        private const int IsolatedMiddlegame = -10, IsolatedEndgame = -15;   // per pawn without friendly pawns on neighbouring files
        private const int BishopPairMiddlegame = 30, BishopPairEndgame = 50;
        private const int RookOpenFileMiddlegame = 25, RookOpenFileEndgame = 10;          // no pawns at all on the file
        private const int RookSemiOpenFileMiddlegame = 12, RookSemiOpenFileEndgame = 5;   // only enemy pawns on the file
        private const int RookSeventhMiddlegame = 20, RookSeventhEndgame = 30;            // rook on the enemy's second rank

        // Passed, doubled and isolated pawns, the bishop pair and rook placement (White minus Black),
        // added to the middlegame and endgame scores before they are blended
        private static void AddStructure(Position position, Span<int> pawnCount, Span<int> minRank, Span<int> maxRank,
                                         Span<int> bishops, Span<int> pieceSquares, ref int middlegame, ref int endgame)
        {

            for (int i = 0; i < pieceSquares.Length; i++)
            {
                int square = pieceSquares[i];
                Piece piece = position[square];

                Side color = piece.Color;
                int us = (int)color, them = 1 - us;
                int sign = color == Side.White ? 1 : -1;
                int file = Square.File(square), rank = Square.Rank(square);
                int relativeRank = color == Side.White ? rank : 7 - rank;

                if (piece.Type == PieceType.Pawn)
                {
                    // Passed: no enemy pawn in front of it on its own file or the neighbouring ones.
                    // Of doubled pawns only the front one counts, the rear one is blocked by its own pawn.
                    bool passed = color == Side.White ? maxRank[us * 8 + file] == rank : minRank[us * 8 + file] == rank;
                    for (int f = Math.Max(0, file - 1); f <= Math.Min(7, file + 1) && passed; f++)
                    {
                        passed = color == Side.White ? maxRank[them * 8 + f] <= rank : minRank[them * 8 + f] >= rank;
                    }
                    if (passed)
                    {
                        middlegame += sign * PassedMiddlegame[relativeRank];
                        endgame += sign * PassedEndgame[relativeRank];
                    }

                    bool isolated = (file == 0 || pawnCount[us * 8 + file - 1] == 0) && (file == 7 || pawnCount[us * 8 + file + 1] == 0);
                    if (isolated)
                    {
                        middlegame += sign * IsolatedMiddlegame;
                        endgame += sign * IsolatedEndgame;
                    }
                }
                else // Rook
                {
                    if (pawnCount[us * 8 + file] == 0)
                    {
                        bool open = pawnCount[them * 8 + file] == 0;
                        middlegame += sign * (open ? RookOpenFileMiddlegame : RookSemiOpenFileMiddlegame);
                        endgame += sign * (open ? RookOpenFileEndgame : RookSemiOpenFileEndgame);
                    }
                    if (relativeRank == 6)
                    {
                        middlegame += sign * RookSeventhMiddlegame;
                        endgame += sign * RookSeventhEndgame;
                    }
                }
            }

            for (int side = 0; side < 2; side++)
            {
                int sign = side == (int)Side.White ? 1 : -1;
                for (int file = 0; file < 8; file++)
                {
                    if (pawnCount[side * 8 + file] > 1)
                    {
                        middlegame += sign * DoubledMiddlegame * (pawnCount[side * 8 + file] - 1);
                        endgame += sign * DoubledEndgame * (pawnCount[side * 8 + file] - 1);
                    }
                }
                if (bishops[side] >= 2)
                {
                    middlegame += sign * BishopPairMiddlegame;
                    endgame += sign * BishopPairEndgame;
                }
            }

        }

        // ===== King safety (used by Bot_v16 and newer) =====

        private const int ShieldPawnNear = 12;     // own pawn right in front of the king (or diagonally in front)
        private const int ShieldPawnFar = 6;       // own pawn two squares in front
        private const int ShieldPawnAdvanced = -8; // own pawn on the file, but advanced further away
        private const int ShieldFileNoOwnPawn = -15;   // no own pawn on a file next to the king
        private const int ShieldFileOpen = -10;        // ...and no enemy pawn either: a fully open file towards the king

        // Pawn shelter of a king still on its first two ranks: pawns on its own file and the two neighbouring ones.
        // A king that has walked up the board gets nothing here (the king tables already punish that).
        private static int KingShelter(Position position, Side color, Span<int> pawnCount)
        {
            int king = position.KingSquare(color);
            int kingFile = Square.File(king);
            int relativeRank = color == Side.White ? Square.Rank(king) : 7 - Square.Rank(king);
            if (relativeRank > 1) return 0;

            int us = (int)color, them = 1 - us;
            int forward = color == Side.White ? 1 : -1;
            int kingRank = Square.Rank(king);
            int score = 0;

            for (int file = Math.Max(0, kingFile - 1); file <= Math.Min(7, kingFile + 1); file++)
            {
                if (position[Square.Make(file, kingRank + forward)].Is(PieceType.Pawn, color))
                    score += ShieldPawnNear;
                else if (position[Square.Make(file, kingRank + 2 * forward)].Is(PieceType.Pawn, color))
                    score += ShieldPawnFar;
                else if (pawnCount[us * 8 + file] > 0)
                    score += ShieldPawnAdvanced;
                else
                {
                    score += ShieldFileNoOwnPawn;
                    if (pawnCount[them * 8 + file] == 0) score += ShieldFileOpen;
                }
            }
            return score;
        }

        // Tables are drawn with rank 8 first. For White, a1 (square 0) is the last row: index = square ^ 56.
        // Black sees the board upside down, so its pieces use the square itself (a8 for Black is like a1 for White).
        private static int TableIndex(int square, Side color)
        {
            return color == Side.White ? square ^ 56 : square;
        }
    }
}
