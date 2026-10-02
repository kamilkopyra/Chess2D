using System;
using System.Collections.Generic;

namespace ChessEngine
{
    // The evaluation of Bot_v22 (Evaluation.EvaluateWithActivity) with all its numbers in one weight array,
    // so they can be tuned automatically (Texel tuning, Tools/Chess2D.Tune).
    //
    // The evaluation is a sum of "terms": material, piece-square tables, passed pawns, ..., mobility.
    // Each term has a coefficient taken from the position (e.g. "2 white rooks on open files minus 1 black one" = 1)
    // and two weights, one for the middlegame and one for the endgame (Weights[2 * term] and Weights[2 * term + 1]).
    // Like Evaluation, the middlegame and endgame sums are blended by the game phase.
    //
    // Not tuned: the king attack (it grows non-linearly) and the mop-up bonus for mating a lone king.
    // With Default weights the score is exactly Evaluation.EvaluateWithActivity (checked by a test).
    public static class TunableEvaluation
    {
        // ===== Terms (index into the weight array = 2 * term for the middlegame, 2 * term + 1 for the endgame) =====

        public const int Material = 0;                  // + (PieceType - 1): pawn .. queen
        public const int PieceSquare = 5;               // + (PieceType - 1) * 64 + table index (as Evaluation.TableIndex)
        public const int Passed = PieceSquare + 6 * 64; // + rank counted from the pawn's side
        public const int Doubled = Passed + 8;
        public const int Isolated = Doubled + 1;
        public const int BishopPair = Isolated + 1;
        public const int RookOpenFile = BishopPair + 1;
        public const int RookSemiOpenFile = RookOpenFile + 1;
        public const int RookSeventh = RookSemiOpenFile + 1;
        public const int ShieldNear = RookSeventh + 1;
        public const int ShieldFar = ShieldNear + 1;
        public const int ShieldAdvanced = ShieldFar + 1;
        public const int ShieldNoOwnPawn = ShieldAdvanced + 1;
        public const int ShieldOpenFile = ShieldNoOwnPawn + 1;
        // The terms from here on belong to the activity part, which is blended and rounded separately
        // (as in Evaluation.EvaluateWithActivity) and left out against a lone king
        public const int Mobility = ShieldOpenFile + 1; // + (PieceType - Knight): knight .. queen
        public const int PawnThreat = Mobility + 4;
        public const int MinorThreat = PawnThreat + 1;
        public const int Hanging = MinorThreat + 1;
        public const int Outpost = Hanging + 1;
        public const int TermCount = Outpost + 1;

        public const int FirstActivityTerm = Mobility;
        public const int MaxPhase = 24;

        // Mobility is counted relative to a typical number of safe squares (not tuned)
        private static readonly int[] MobilityBase = { 0, 0, 4, 7, 7, 14, 0 };
        private static readonly int[] PhaseWeights = { 0, 0, 1, 1, 2, 4, 0 };
        private static readonly int[] KingAttackWeight = { 0, 0, 2, 2, 3, 5, 0 };
        private const int KingAttackMaxPenalty = 500;

        // The weights of Evaluation (v22). Tables use the same layout as there: rank 8 first.
        public static readonly int[] Default = BuildDefault();

        // Human-readable name of a term, for the tuner's output
        public static string TermName(int term)
        {
            string[] pieces = { "Pawn", "Knight", "Bishop", "Rook", "Queen", "King" };
            if (term < PieceSquare) return "Material " + pieces[term - Material];
            if (term < Passed)
            {
                int index = term - PieceSquare;
                return $"PieceSquare {pieces[index / 64]} {index % 64}";
            }
            if (term < Doubled) return "Passed rank " + (term - Passed);
            if (term >= Mobility && term < PawnThreat) return "Mobility " + pieces[term - Mobility + 1];
            switch (term)
            {
                case Doubled: return "Doubled";
                case Isolated: return "Isolated";
                case BishopPair: return "BishopPair";
                case RookOpenFile: return "RookOpenFile";
                case RookSemiOpenFile: return "RookSemiOpenFile";
                case RookSeventh: return "RookSeventh";
                case ShieldNear: return "ShieldNear";
                case ShieldFar: return "ShieldFar";
                case ShieldAdvanced: return "ShieldAdvanced";
                case ShieldNoOwnPawn: return "ShieldNoOwnPawn";
                case ShieldOpenFile: return "ShieldOpenFile";
                case PawnThreat: return "PawnThreat";
                case MinorThreat: return "MinorThreat";
                case Hanging: return "Hanging";
                case Outpost: return "Outpost";
                default: return "Term " + term;
            }
        }

        // Everything the evaluation needs from a position, apart from the weights. Coefficients are White minus Black.
        public sealed class Trace
        {
            public readonly List<int> Terms = new List<int>(64);
            public readonly List<int> Coefficients = new List<int>(64);
            public int Phase;              // 0 (only kings and pawns) .. 24
            public int KingAttack;         // fixed middlegame part of the activity, White minus Black
            public bool LoneKing;          // one side has only its king: the activity part is left out
            public int MopUp;              // fixed score (White's view), already blended

            public void Clear()
            {
                Terms.Clear();
                Coefficients.Clear();
                Phase = KingAttack = MopUp = 0;
                LoneKing = false;
            }

            // Score from White's point of view for the given weights (same rounding as Evaluation)
            public int Evaluate(int[] weights)
            {
                int mg0 = 0, eg0 = 0, mg1 = KingAttack, eg1 = 0;
                for (int i = 0; i < Terms.Count; i++)
                {
                    int term = Terms[i], coefficient = Coefficients[i];
                    if (term < FirstActivityTerm)
                    {
                        mg0 += coefficient * weights[2 * term];
                        eg0 += coefficient * weights[2 * term + 1];
                    }
                    else
                    {
                        mg1 += coefficient * weights[2 * term];
                        eg1 += coefficient * weights[2 * term + 1];
                    }
                }
                int score = (mg0 * Phase + eg0 * (MaxPhase - Phase)) / MaxPhase + MopUp;
                if (!LoneKing) score += (mg1 * Phase + eg1 * (MaxPhase - Phase)) / MaxPhase;
                return score;
            }
        }

        // Receives the coefficient of each term as the position is scanned. Implemented by structs, so the JIT
        // compiles a separate, inlined copy of the scan for scoring and for tracing.
        private interface ITermSink
        {
            void Add(int term, int coefficient);
        }

        // Multiplies the coefficients by the weights right away (used by the bots)
        private struct ScoreSink : ITermSink
        {
            public int[] Weights;
            public int Mg0, Eg0, Mg1, Eg1;

            public void Add(int term, int coefficient)
            {
                if (term < FirstActivityTerm)
                {
                    Mg0 += coefficient * Weights[2 * term];
                    Eg0 += coefficient * Weights[2 * term + 1];
                }
                else
                {
                    Mg1 += coefficient * Weights[2 * term];
                    Eg1 += coefficient * Weights[2 * term + 1];
                }
            }
        }

        // Sums the coefficients per term (used to build a Trace for the tuner)
        private struct CoefficientSink : ITermSink
        {
            public int[] Coefficients;

            public void Add(int term, int coefficient) => Coefficients[term] += coefficient;
        }

        // Score from the point of view of the side to move, like Evaluation.EvaluateWithActivity
        public static int Evaluate(Position position, int[] weights)
        {
            var sink = new ScoreSink { Weights = weights };
            Scan(position, ref sink, out int phase, out int kingAttack, out bool loneKing, out int mopUp);
            int score = ((sink.Mg0 * phase + sink.Eg0 * (MaxPhase - phase)) / MaxPhase) + mopUp;
            if (!loneKing) score += ((sink.Mg1 + kingAttack) * phase + sink.Eg1 * (MaxPhase - phase)) / MaxPhase;
            return position.SideToMove == Side.White ? score : -score;
        }

        [ThreadStatic] private static int[] threadCoefficients;

        // Fills the trace with the coefficients of every term for this position
        public static void Collect(Position position, Trace trace)
        {
            trace.Clear();
            int[] coefficients = threadCoefficients ?? (threadCoefficients = new int[TermCount]);
            Array.Clear(coefficients, 0, TermCount);
            var sink = new CoefficientSink { Coefficients = coefficients };
            Scan(position, ref sink, out trace.Phase, out trace.KingAttack, out trace.LoneKing, out trace.MopUp);

            for (int term = 0; term < TermCount; term++)
            {
                if (coefficients[term] == 0) continue;
                trace.Terms.Add(term);
                trace.Coefficients.Add(coefficients[term]);
            }
        }

        // Goes through the position once and reports every term's coefficient (White minus Black) to the sink
        private static void Scan<TSink>(Position position, ref TSink sink, out int phase, out int kingAttack,
                                        out bool loneKing, out int mopUp) where TSink : struct, ITermSink
        {
            ulong occupied = position.Occupied;
            ulong whitePawns = position.Pieces(PieceType.Pawn, Side.White);
            ulong blackPawns = position.Pieces(PieceType.Pawn, Side.Black);
            int whiteMaterial = 0, blackMaterial = 0;
            phase = 0;

            for (int side = 0; side < 2; side++)
            {
                int sign = side == (int)Side.White ? 1 : -1;
                for (int type = (int)PieceType.Pawn; type <= (int)PieceType.King; type++)
                {
                    ulong bits = position.Pieces((PieceType)type, (Side)side);
                    if (bits == 0) continue;
                    int count = Bits.PopCount(bits);
                    phase += count * PhaseWeights[type];
                    if (type != (int)PieceType.King)
                    {
                        sink.Add(Material + type - 1, sign * count);
                        int value = count * Evaluation.PieceValues[type];
                        if (sign > 0) whiteMaterial += value; else blackMaterial += value;
                    }
                    while (bits != 0)
                    {
                        int square = Bits.PopLowest(ref bits);
                        int index = side == (int)Side.White ? square ^ 56 : square;
                        sink.Add(PieceSquare + (type - 1) * 64 + index, sign);
                    }
                }
            }
            phase = Math.Min(phase, MaxPhase);

            CollectStructure(position, Side.White, whitePawns, blackPawns, ref sink);
            CollectStructure(position, Side.Black, blackPawns, whitePawns, ref sink);
            CollectShelter(position, Side.White, whitePawns, blackPawns, ref sink);
            CollectShelter(position, Side.Black, blackPawns, whitePawns, ref sink);

            ulong kings = position.Pieces(PieceType.King);
            loneKing = (position.Pieces(Side.White) & ~kings) == 0 || (position.Pieces(Side.Black) & ~kings) == 0;
            CollectActivity(position, occupied, ref sink, out kingAttack);

            // Mop-up (not tuned), as in Evaluation: at least a rook more against a side without pawns
            mopUp = 0;
            if (whiteMaterial - blackMaterial >= 400 && blackPawns == 0)
                mopUp = MopUpBonus(position.KingSquare(Side.White), position.KingSquare(Side.Black));
            else if (blackMaterial - whiteMaterial >= 400 && whitePawns == 0)
                mopUp = -MopUpBonus(position.KingSquare(Side.Black), position.KingSquare(Side.White));
        }

        private static void CollectStructure<TSink>(Position position, Side color, ulong ownPawns, ulong enemyPawns, ref TSink sink)
            where TSink : struct, ITermSink
        {
            int us = (int)color;
            int sign = color == Side.White ? 1 : -1;

            ulong pawns = ownPawns;
            while (pawns != 0)
            {
                int square = Bits.PopLowest(ref pawns);
                int relativeRank = color == Side.White ? Square.Rank(square) : 7 - Square.Rank(square);
                if ((ownPawns & Bitboards.ForwardFile[us][square]) == 0 && (enemyPawns & Bitboards.PassedPawnMask[us][square]) == 0)
                    sink.Add(Passed + relativeRank, sign);
                if ((ownPawns & Bitboards.AdjacentFiles[Square.File(square)]) == 0)
                    sink.Add(Isolated, sign);
            }

            ulong rooks = position.Pieces(PieceType.Rook, color);
            while (rooks != 0)
            {
                int square = Bits.PopLowest(ref rooks);
                ulong file = Bitboards.Files[Square.File(square)];
                int relativeRank = color == Side.White ? Square.Rank(square) : 7 - Square.Rank(square);
                if ((ownPawns & file) == 0)
                    sink.Add((enemyPawns & file) == 0 ? RookOpenFile : RookSemiOpenFile, sign);
                if (relativeRank == 6)
                    sink.Add(RookSeventh, sign);
            }

            for (int file = 0; file < 8; file++)
            {
                int count = Bits.PopCount(ownPawns & Bitboards.Files[file]);
                if (count > 1) sink.Add(Doubled, sign * (count - 1));
            }

            if (Bits.PopCount(position.Pieces(PieceType.Bishop, color)) >= 2)
                sink.Add(BishopPair, sign);
        }

        private static void CollectShelter<TSink>(Position position, Side color, ulong ownPawns, ulong enemyPawns, ref TSink sink)
            where TSink : struct, ITermSink
        {
            int king = position.KingSquare(color);
            int kingFile = Square.File(king), kingRank = Square.Rank(king);
            int relativeRank = color == Side.White ? kingRank : 7 - kingRank;
            if (relativeRank > 1) return;

            int sign = color == Side.White ? 1 : -1;
            int forward = color == Side.White ? 1 : -1;
            for (int file = Math.Max(0, kingFile - 1); file <= Math.Min(7, kingFile + 1); file++)
            {
                if ((ownPawns & (1UL << Square.Make(file, kingRank + forward))) != 0)
                    sink.Add(ShieldNear, sign);
                else if ((ownPawns & (1UL << Square.Make(file, kingRank + 2 * forward))) != 0)
                    sink.Add(ShieldFar, sign);
                else if ((ownPawns & Bitboards.Files[file]) != 0)
                    sink.Add(ShieldAdvanced, sign);
                else
                {
                    sink.Add(ShieldNoOwnPawn, sign);
                    if ((enemyPawns & Bitboards.Files[file]) == 0) sink.Add(ShieldOpenFile, sign);
                }
            }
        }

        // Mobility, threats and outposts as coefficients; the king attack as a fixed score (White minus Black)
        private static void CollectActivity<TSink>(Position position, ulong occupied, ref TSink sink, out int kingAttack)
            where TSink : struct, ITermSink
        {
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

            Span<int> kingUnits = stackalloc int[2];
            Span<int> kingAttackers = stackalloc int[2];
            for (int side = 0; side < 2; side++)
            {
                int them = 1 - side;
                int sign = side == (int)Side.White ? 1 : -1;
                ulong safe = ~position.Pieces((Side)side) & ~pawnAttacks[them];
                ulong enemyKingZone = Bitboards.KingAttacks[position.KingSquare((Side)them)];

                for (int type = (int)PieceType.Knight; type <= (int)PieceType.Queen; type++)
                {
                    ulong pieces = position.Pieces((PieceType)type, (Side)side);
                    while (pieces != 0)
                    {
                        int square = Bits.PopLowest(ref pieces);
                        ulong attacks = PieceAttacks((PieceType)type, square, occupied);
                        allAttacks[side] |= attacks;
                        sink.Add(Mobility + type - (int)PieceType.Knight, sign * (Bits.PopCount(attacks & safe) - MobilityBase[type]));

                        ulong zone = attacks & enemyKingZone;
                        if (zone != 0)
                        {
                            kingAttackers[side]++;
                            kingUnits[side] += KingAttackWeight[type] * Bits.PopCount(zone);
                        }
                    }
                }
            }

            kingAttack = 0;
            for (int side = 0; side < 2; side++)
            {
                int them = 1 - side;
                int sign = side == (int)Side.White ? 1 : -1;

                if (kingAttackers[side] >= 2)
                    kingAttack += sign * Math.Min(KingAttackMaxPenalty, kingUnits[side] * kingUnits[side] / 4);

                ulong enemyPieces = position.Pieces((Side)them) & ~position.Pieces(PieceType.Pawn) & ~position.Pieces(PieceType.King);
                sink.Add(PawnThreat, sign * Bits.PopCount(pawnAttacks[side] & enemyPieces));

                ulong minorAttacks = 0;
                ulong minors = position.Pieces(PieceType.Knight, (Side)side) | position.Pieces(PieceType.Bishop, (Side)side);
                while (minors != 0)
                {
                    int square = Bits.PopLowest(ref minors);
                    minorAttacks |= PieceAttacks(position[square].Type, square, occupied);
                }
                ulong heavy = (position.Pieces(PieceType.Rook) | position.Pieces(PieceType.Queen)) & position.Pieces((Side)them);
                sink.Add(MinorThreat, sign * Bits.PopCount(minorAttacks & heavy));
                sink.Add(Hanging, sign * Bits.PopCount(enemyPieces & allAttacks[side] & ~allAttacks[them]));

                ulong knights = position.Pieces(PieceType.Knight, (Side)side);
                ulong enemyPawns = position.Pieces(PieceType.Pawn, (Side)them);
                while (knights != 0)
                {
                    int square = Bits.PopLowest(ref knights);
                    int relativeRank = side == (int)Side.White ? Square.Rank(square) : 7 - Square.Rank(square);
                    if (relativeRank < 3 || relativeRank > 5) continue;
                    if ((pawnAttacks[side] & (1UL << square)) == 0) continue;
                    if ((enemyPawns & Bitboards.PassedPawnMask[side][square] & Bitboards.AdjacentFiles[Square.File(square)]) != 0) continue;
                    sink.Add(Outpost, sign);
                }
            }
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

        private static int MopUpBonus(int strongKing, int weakKing)
        {
            int weakFile = Square.File(weakKing), weakRank = Square.Rank(weakKing);
            int centreDistance = Math.Max(3 - weakFile, weakFile - 4) + Math.Max(3 - weakRank, weakRank - 4);
            int kingsDistance = Math.Abs(Square.File(strongKing) - weakFile) + Math.Abs(Square.Rank(strongKing) - weakRank);
            return 10 * centreDistance + 4 * (14 - kingsDistance);
        }

        private static int[] BuildDefault()
        {
            var w = new int[2 * TermCount];
            void Set(int term, int mg, int eg) { w[2 * term] = mg; w[2 * term + 1] = eg; }

            for (int type = (int)PieceType.Pawn; type <= (int)PieceType.Queen; type++)
                Set(Material + type - 1, Evaluation.PieceValues[type], Evaluation.PieceValues[type]);

            for (int type = (int)PieceType.Pawn; type <= (int)PieceType.King; type++)
            {
                int[] mg = Evaluation.MiddlegameTable((PieceType)type), eg = Evaluation.EndgameTable((PieceType)type);
                for (int i = 0; i < 64; i++) Set(PieceSquare + (type - 1) * 64 + i, mg[i], eg[i]);
            }

            int[] passedMg = { 0, 0, 5, 10, 20, 35, 60, 0 }, passedEg = { 0, 5, 10, 20, 40, 70, 110, 0 };
            for (int rank = 0; rank < 8; rank++) Set(Passed + rank, passedMg[rank], passedEg[rank]);

            Set(Doubled, -10, -20);
            Set(Isolated, -10, -15);
            Set(BishopPair, 30, 50);
            Set(RookOpenFile, 25, 10);
            Set(RookSemiOpenFile, 12, 5);
            Set(RookSeventh, 20, 30);
            Set(ShieldNear, 12, 0);
            Set(ShieldFar, 6, 0);
            Set(ShieldAdvanced, -8, 0);
            Set(ShieldNoOwnPawn, -15, 0);
            Set(ShieldOpenFile, -10, 0);
            Set(Mobility + 0, 4, 4);   // knight
            Set(Mobility + 1, 5, 5);   // bishop
            Set(Mobility + 2, 2, 4);   // rook
            Set(Mobility + 3, 1, 2);   // queen
            Set(PawnThreat, 40, 30);
            Set(MinorThreat, 25, 20);
            Set(Hanging, 15, 15);
            Set(Outpost, 20, 10);
            return w;
        }
    }
}
