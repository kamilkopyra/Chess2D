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

        // Tables by PieceType (index 0 unused); knights, bishops, rooks and queens use one table for both phases
        private static readonly int[][] MiddlegameTables = { null, PawnMiddlegame, Knight, Bishop, Rook, Queen, KingMiddlegame };
        private static readonly int[][] EndgameTables = { null, PawnEndgame, Knight, Bishop, Rook, Queen, KingEndgame };

        // Copies of the piece-square tables (for TunableEvaluation)
        public static int[] MiddlegameTable(PieceType type) => (int[])MiddlegameTables[(int)type].Clone();
        public static int[] EndgameTable(PieceType type) => (int[])EndgameTables[(int)type].Clone();

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

            // Material and piece-square tables, piece type by piece type from the bitboards.
            // Only sums of integers, so the order doesn't change the result.
            for (int side = 0; side < 2; side++)
            {
                int material = 0, mg = 0, eg = 0;
                for (int type = (int)PieceType.Pawn; type <= (int)PieceType.King; type++)
                {
                    ulong bits = position.Pieces((PieceType)type, (Side)side);
                    if (bits == 0) continue;

                    int count = Bits.PopCount(bits);
                    material += count * PieceValues[type];
                    phase += count * PhaseWeights[type];

                    int[] mgTable = MiddlegameTables[type], egTable = EndgameTables[type];
                    while (bits != 0)
                    {
                        int index = TableIndex(Bits.PopLowest(ref bits), (Side)side);
                        mg += mgTable[index];
                        eg += egTable[index];
                    }
                }

                int sign = side == (int)Side.White ? 1 : -1;
                if (sign > 0) whiteMaterial = material; else blackMaterial = material;
                middlegame += sign * (material + mg);
                endgame += sign * (material + eg);
            }

            ulong whitePawns = position.Pieces(PieceType.Pawn, Side.White);
            ulong blackPawns = position.Pieces(PieceType.Pawn, Side.Black);

            if (withStructure)
            {
                AddStructure(position, Side.White, whitePawns, blackPawns, ref middlegame, ref endgame);
                AddStructure(position, Side.Black, blackPawns, whitePawns, ref middlegame, ref endgame);
            }

            if (withKingSafety)
            {
                // Only in the middlegame part: in the endgame the king should leave its shelter and be active
                middlegame += KingShelter(position, Side.White, whitePawns, blackPawns)
                            - KingShelter(position, Side.Black, blackPawns, whitePawns);
            }

            // Blend: full middlegame score with all pieces on the board, full endgame score with none left
            phase = Math.Min(phase, MaxPhase);
            int score = (middlegame * phase + endgame * (MaxPhase - phase)) / MaxPhase;

            if (withMopUp)
            {
                // Same rule as MopUp(): at least a rook more against a side without pawns
                if (whiteMaterial - blackMaterial >= 400 && blackPawns == 0)
                    score += MopUpBonus(position.KingSquare(Side.White), position.KingSquare(Side.Black));
                else if (blackMaterial - whiteMaterial >= 400 && whitePawns == 0)
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

        // Passed, doubled and isolated pawns, the bishop pair and rook placement of one side (added for White,
        // subtracted for Black), added to the middlegame and endgame scores before they are blended
        private static void AddStructure(Position position, Side color, ulong ownPawns, ulong enemyPawns,
                                         ref int middlegame, ref int endgame)
        {
            int us = (int)color;
            int sign = color == Side.White ? 1 : -1;

            ulong pawns = ownPawns;
            while (pawns != 0)
            {
                int square = Bits.PopLowest(ref pawns);
                int file = Square.File(square);
                int relativeRank = color == Side.White ? Square.Rank(square) : 7 - Square.Rank(square);

                // Passed: no enemy pawn in front of it on its own file or the neighbouring ones.
                // Of doubled pawns only the front one counts, the rear one is blocked by its own pawn.
                if ((ownPawns & Bitboards.ForwardFile[us][square]) == 0
                    && (enemyPawns & Bitboards.PassedPawnMask[us][square]) == 0)
                {
                    middlegame += sign * PassedMiddlegame[relativeRank];
                    endgame += sign * PassedEndgame[relativeRank];
                }

                if ((ownPawns & Bitboards.AdjacentFiles[file]) == 0)
                {
                    middlegame += sign * IsolatedMiddlegame;
                    endgame += sign * IsolatedEndgame;
                }
            }

            ulong rooks = position.Pieces(PieceType.Rook, color);
            while (rooks != 0)
            {
                int square = Bits.PopLowest(ref rooks);
                ulong file = Bitboards.Files[Square.File(square)];
                int relativeRank = color == Side.White ? Square.Rank(square) : 7 - Square.Rank(square);

                if ((ownPawns & file) == 0)
                {
                    bool open = (enemyPawns & file) == 0;
                    middlegame += sign * (open ? RookOpenFileMiddlegame : RookSemiOpenFileMiddlegame);
                    endgame += sign * (open ? RookOpenFileEndgame : RookSemiOpenFileEndgame);
                }
                if (relativeRank == 6)
                {
                    middlegame += sign * RookSeventhMiddlegame;
                    endgame += sign * RookSeventhEndgame;
                }
            }

            for (int file = 0; file < 8; file++)
            {
                int count = Bits.PopCount(ownPawns & Bitboards.Files[file]);
                if (count > 1)
                {
                    middlegame += sign * DoubledMiddlegame * (count - 1);
                    endgame += sign * DoubledEndgame * (count - 1);
                }
            }

            if (Bits.PopCount(position.Pieces(PieceType.Bishop, color)) >= 2)
            {
                middlegame += sign * BishopPairMiddlegame;
                endgame += sign * BishopPairEndgame;
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
        private static int KingShelter(Position position, Side color, ulong ownPawns, ulong enemyPawns)
        {
            int king = position.KingSquare(color);
            int kingFile = Square.File(king);
            int relativeRank = color == Side.White ? Square.Rank(king) : 7 - Square.Rank(king);
            if (relativeRank > 1) return 0;

            int forward = color == Side.White ? 1 : -1;
            int kingRank = Square.Rank(king);
            int score = 0;

            for (int file = Math.Max(0, kingFile - 1); file <= Math.Min(7, kingFile + 1); file++)
            {
                if ((ownPawns & (1UL << Square.Make(file, kingRank + forward))) != 0)
                    score += ShieldPawnNear;
                else if ((ownPawns & (1UL << Square.Make(file, kingRank + 2 * forward))) != 0)
                    score += ShieldPawnFar;
                else if ((ownPawns & Bitboards.Files[file]) != 0)
                    score += ShieldPawnAdvanced;
                else
                {
                    score += ShieldFileNoOwnPawn;
                    if ((enemyPawns & Bitboards.Files[file]) == 0) score += ShieldFileOpen;
                }
            }
            return score;
        }

        // ===== Piece activity (used by Bot_v22 and newer, on top of EvaluateFull) =====

        // Mobility: per safe square (not ours, not attacked by an enemy pawn) above/below a typical count.
        // Index = PieceType; pawns and kings have no mobility term.
        private static readonly int[] MobilityBase = { 0, 0, 4, 7, 7, 14, 0 };
        private static readonly int[] MobilityMiddlegame = { 0, 0, 4, 5, 2, 1, 0 };
        private static readonly int[] MobilityEndgame = { 0, 0, 4, 5, 4, 2, 0 };

        // King attack: "attack units" per square of the enemy king zone a piece attacks
        private static readonly int[] KingAttackWeight = { 0, 0, 2, 2, 3, 5, 0 };
        private const int KingAttackMaxPenalty = 500;

        // Threats: our pawn attacks an enemy piece, our minor piece attacks an enemy rook or queen,
        // an enemy piece is attacked and not defended at all
        private const int PawnThreatMiddlegame = 40, PawnThreatEndgame = 30;
        private const int MinorThreatMiddlegame = 25, MinorThreatEndgame = 20;
        private const int HangingMiddlegame = 15, HangingEndgame = 15;

        // Outpost: a knight on the enemy half, defended by our pawn, that no enemy pawn can ever attack
        private const int OutpostMiddlegame = 20, OutpostEndgame = 10;

        // EvaluateFull + piece activity: mobility, attacks on the enemy king zone, threats and knight outposts.
        // When one side has only its king left, the activity terms are left out: mating it needs the mop-up plan
        // (box the king in), and mobility would pull the other way (free squares for our pieces).
        public static int EvaluateWithActivity(Position position)
        {
            ulong kings = position.Pieces(PieceType.King);
            bool loneKing = (position.Pieces(Side.White) & ~kings) == 0 || (position.Pieces(Side.Black) & ~kings) == 0;
            return loneKing ? EvaluateFull(position) : EvaluateFull(position) + Activity(position);
        }

        // Only the activity terms, from the point of view of the side to move, tapered like Evaluate
        public static int Activity(Position position)
        {
            ulong occupied = position.Occupied;

            // Squares attacked by each side's pawns, and by all pieces of each side
            Span<ulong> pawnAttacks = stackalloc ulong[2];
            Span<ulong> allAttacks = stackalloc ulong[2];
            for (int side = 0; side < 2; side++)
            {
                ulong pawns = position.Pieces(PieceType.Pawn, (Side)side);
                ulong attacks = 0;
                while (pawns != 0) attacks |= Bitboards.PawnAttacks[side][Bits.PopLowest(ref pawns)];
                pawnAttacks[side] = attacks;
                allAttacks[side] = attacks | Bitboards.KingAttacks[position.KingSquare((Side)side)];
            }

            // First pass: attacks of the pieces (they are needed for "defended" before the threats are scored)
            int middlegame = 0, endgame = 0, phase = 0;
            Span<int> kingUnits = stackalloc int[2];
            Span<int> kingAttackers = stackalloc int[2];
            for (int side = 0; side < 2; side++)
            {
                int them = 1 - side;
                int sign = side == (int)Side.White ? 1 : -1;
                ulong own = position.Pieces((Side)side);
                ulong safe = ~own & ~pawnAttacks[them];
                ulong enemyKingZone = Bitboards.KingAttacks[position.KingSquare((Side)them)];

                for (int type = (int)PieceType.Knight; type <= (int)PieceType.Queen; type++)
                {
                    ulong pieces = position.Pieces((PieceType)type, (Side)side);
                    phase += Bits.PopCount(pieces) * PhaseWeights[type];
                    while (pieces != 0)
                    {
                        int square = Bits.PopLowest(ref pieces);
                        ulong attacks = PieceAttacks((PieceType)type, square, occupied);
                        allAttacks[side] |= attacks;

                        int mobility = Bits.PopCount(attacks & safe) - MobilityBase[type];
                        middlegame += sign * mobility * MobilityMiddlegame[type];
                        endgame += sign * mobility * MobilityEndgame[type];

                        ulong zone = attacks & enemyKingZone;
                        if (zone != 0)
                        {
                            kingAttackers[side]++;
                            kingUnits[side] += KingAttackWeight[type] * Bits.PopCount(zone);
                        }
                    }
                }
            }

            for (int side = 0; side < 2; side++)
            {
                int them = 1 - side;
                int sign = side == (int)Side.White ? 1 : -1;

                // King attack: only with at least two attackers, growing faster than linearly; middlegame only
                if (kingAttackers[side] >= 2)
                {
                    int units = kingUnits[side];
                    middlegame += sign * Math.Min(KingAttackMaxPenalty, units * units / 4);
                }

                // Threats against the enemy pieces (pawns and the king are not counted)
                ulong enemyPieces = position.Pieces((Side)them) & ~position.Pieces(PieceType.Pawn) & ~position.Pieces(PieceType.King);
                int pawnThreats = Bits.PopCount(pawnAttacks[side] & enemyPieces);
                middlegame += sign * pawnThreats * PawnThreatMiddlegame;
                endgame += sign * pawnThreats * PawnThreatEndgame;

                ulong minorAttacks = 0;
                ulong minors = position.Pieces(PieceType.Knight, (Side)side) | position.Pieces(PieceType.Bishop, (Side)side);
                while (minors != 0)
                {
                    int square = Bits.PopLowest(ref minors);
                    minorAttacks |= PieceAttacks(position[square].Type, square, occupied);
                }
                ulong heavy = (position.Pieces(PieceType.Rook) | position.Pieces(PieceType.Queen)) & position.Pieces((Side)them);
                int minorThreats = Bits.PopCount(minorAttacks & heavy);
                middlegame += sign * minorThreats * MinorThreatMiddlegame;
                endgame += sign * minorThreats * MinorThreatEndgame;

                int hanging = Bits.PopCount(enemyPieces & allAttacks[side] & ~allAttacks[them]);
                middlegame += sign * hanging * HangingMiddlegame;
                endgame += sign * hanging * HangingEndgame;

                // Knight outposts on the enemy half (ranks 4-6 from our side)
                ulong knights = position.Pieces(PieceType.Knight, (Side)side);
                ulong enemyPawns = position.Pieces(PieceType.Pawn, (Side)them);
                while (knights != 0)
                {
                    int square = Bits.PopLowest(ref knights);
                    int relativeRank = side == (int)Side.White ? Square.Rank(square) : 7 - Square.Rank(square);
                    if (relativeRank < 3 || relativeRank > 5) continue;
                    if ((pawnAttacks[side] & (1UL << square)) == 0) continue;
                    // No enemy pawn on the neighbouring files in front of the knight can ever attack it
                    if ((enemyPawns & Bitboards.PassedPawnMask[side][square] & Bitboards.AdjacentFiles[Square.File(square)]) != 0) continue;
                    middlegame += sign * OutpostMiddlegame;
                    endgame += sign * OutpostEndgame;
                }
            }

            phase = Math.Min(phase, MaxPhase);
            int score = (middlegame * phase + endgame * (MaxPhase - phase)) / MaxPhase;
            return position.SideToMove == Side.White ? score : -score;
        }

        private static ulong PieceAttacks(PieceType type, int square, ulong occupied)
        {
            switch (type)
            {
                case PieceType.Knight: return Bitboards.KnightAttacks[square];
                case PieceType.Bishop: return Bitboards.BishopAttacks(square, occupied);
                case PieceType.Rook: return Bitboards.RookAttacks(square, occupied);
                case PieceType.Queen: return Bitboards.QueenAttacks(square, occupied);
                default: return 0;
            }
        }

        // Tables are drawn with rank 8 first. For White, a1 (square 0) is the last row: index = square ^ 56.
        // Black sees the board upside down, so its pieces use the square itself (a8 for Black is like a1 for White).
        private static int TableIndex(int square, Side color)
        {
            return color == Side.White ? square ^ 56 : square;
        }
    }
}
