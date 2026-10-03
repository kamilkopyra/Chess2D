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
    // Two feature sets:
    // - basic (extended = false): exactly the v22 evaluation. With Default weights the score is exactly
    //   Evaluation.EvaluateWithActivity (checked by a test). The king attack is a fixed, untuned formula.
    // - extended (extended = true): adds tempo, mobility per number of squares (instead of a linear bonus),
    //   a tunable king danger table (instead of the fixed formula), safe checks, pawn storms, weak squares
    //   around the king, backward / phalanx / supported pawns, passed pawn details (blocked, free path,
    //   king distances, rook behind), bad and trapped bishops, trapped rooks, minor pieces behind pawns,
    //   space, and a fixed scale-down of drawish endgames. With DefaultExtended weights it scores like the
    //   basic set (new terms start at 0, the tables reproduce the old formulas) except in the scaled endgames.
    //
    // - full (FeatureSet.Full): the extended set with the king danger rebuilt so that all its numbers are tuned:
    //   danger = sum of weight * input (king zone squares hit by each piece type, safe checks, weak squares,
    //   number of attackers, a constant), penalty = min(500, danger^2 / 1024). The tuner handles this one
    //   non-linear block with the chain rule. Plus threats by piece type against each kind of enemy piece,
    //   material interactions (knights and rooks by the number of own pawns, the rook pair), king distance to
    //   all pawns, and one more drawish endgame (bishop of the wrong colour with rook pawns).
    //   With DefaultFull weights it scores like the extended set (except in the newly scaled endgames).
    //
    // Never tuned: the mop-up bonus for mating a lone king.
    public enum FeatureSet { Basic, Extended, Full }

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
        public const int BasicTermCount = Outpost + 1;

        // ----- Extended feature set -----
        public const int Tempo = BasicTermCount;                       // +1 when White is to move, -1 for Black
        public const int MobilityTable = Tempo + 1;                    // + MobilityTableStart[type] + safe squares
        public const int KingDanger = MobilityTable + 66;              // + attack units (0..99), with 2+ attackers
        public const int KingDangerSize = 100;
        public const int SafeCheck = KingDanger + KingDangerSize;      // + (PieceType - Knight): safe checking squares
        public const int PawnStorm = SafeCheck + 4;                    // + rank of our pawn near the enemy king
        public const int WeakKingSquare = PawnStorm + 8;               // enemy king zone squares we attack, defended only by its king
        public const int Backward = WeakKingSquare + 1;
        public const int Phalanx = Backward + 1;                       // + rank: own pawn beside it
        public const int Supported = Phalanx + 8;                      // + rank: defended by an own pawn
        public const int PassedBlocked = Supported + 8;                // + rank: an enemy piece stands in front
        public const int PassedFree = PassedBlocked + 8;               // + rank: nothing at all in front
        public const int PassedOwnKingDistance = PassedFree + 8;       // + rank: coefficient = distance to the stop square
        public const int PassedEnemyKingDistance = PassedOwnKingDistance + 8;
        public const int RookBehindPassed = PassedEnemyKingDistance + 8;
        public const int BadBishopPawns = RookBehindPassed + 1;        // coefficient = own pawns on the bishop's colour
        public const int TrappedBishop = BadBishopPawns + 1;
        public const int TrappedRook = TrappedBishop + 1;
        public const int MinorBehindPawn = TrappedRook + 1;
        public const int Space = MinorBehindPawn + 1;

        // ----- Full feature set -----
        public const int ThreatByMinor = Space + 1;                    // + (victim PieceType - 1): pawn .. queen
        public const int ThreatByRook = ThreatByMinor + 5;
        public const int ThreatByQueen = ThreatByRook + 5;
        public const int ThreatByKing = ThreatByQueen + 5;
        public const int KnightPawns = ThreatByKing + 1;               // coefficient = knights * (own pawns - 5)
        public const int RookPawns = KnightPawns + 1;                  // coefficient = rooks * (own pawns - 5)
        public const int RookPair = RookPawns + 1;
        public const int KingOwnPawnDistance = RookPair + 1;           // coefficient = sum of distances king - own pawns
        public const int KingEnemyPawnDistance = KingOwnPawnDistance + 1;
        // King danger inputs (non-linear block, middlegame weight only): danger = sum of weight * input
        public const int DangerZoneAttack = KingEnemyPawnDistance + 1; // + (PieceType - Knight): king zone squares hit
        public const int DangerSafeCheck = DangerZoneAttack + 4;       // + (PieceType - Knight)
        public const int DangerWeakSquare = DangerSafeCheck + 4;
        public const int DangerAttackers = DangerWeakSquare + 1;
        public const int DangerBias = DangerAttackers + 1;
        public const int TermCount = DangerBias + 1;

        public const int FirstDangerTerm = DangerZoneAttack;
        public const int DangerCap = 500;

        public const int FirstActivityTerm = Mobility;
        public const int MaxPhase = 24;
        public const int FullScale = 64;   // endgame scale factor of a normal position

        // Mobility table layout: knight 0..8, bishop 0..13, rook 0..14, queen 0..27 safe squares
        private static readonly int[] MobilityTableStart = { 0, 0, 0, 9, 23, 38, 0 };
        private static readonly int[] MobilityTableSize = { 0, 0, 9, 14, 15, 28, 0 };

        // Mobility is counted relative to a typical number of safe squares (not tuned)
        private static readonly int[] MobilityBase = { 0, 0, 4, 7, 7, 14, 0 };
        private static readonly int[] PhaseWeights = { 0, 0, 1, 1, 2, 4, 0 };
        private static readonly int[] KingAttackWeight = { 0, 0, 2, 2, 3, 5, 0 };
        private const int KingAttackMaxPenalty = 500;
        private const ulong DarkSquares = 0xAA55AA55AA55AA55UL;   // a1 is dark

        // The weights of Evaluation (v22). Tables use the same layout as there: rank 8 first.
        public static readonly int[] Default = BuildDefault();

        // Starting weights for the extended set: the basic ones, the new tables reproducing the old formulas
        public static readonly int[] DefaultExtended = BuildDefaultExtended(Default);

        // Starting weights for the full set: the extended ones, the king danger inputs reproducing the old formula
        public static readonly int[] DefaultFull = BuildDefaultFull(DefaultExtended);

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
            if (term >= MobilityTable && term < KingDanger)
            {
                int index = term - MobilityTable;
                for (int type = (int)PieceType.Queen; type >= (int)PieceType.Knight; type--)
                    if (index >= MobilityTableStart[type]) return $"MobilityTable {pieces[type - 1]} {index - MobilityTableStart[type]}";
            }
            if (term >= KingDanger && term < SafeCheck) return "KingDanger " + (term - KingDanger);
            if (term >= SafeCheck && term < PawnStorm) return "SafeCheck " + pieces[term - SafeCheck + 1];
            if (term >= PawnStorm && term < WeakKingSquare) return "PawnStorm rank " + (term - PawnStorm);
            if (term >= Phalanx && term < Supported) return "Phalanx rank " + (term - Phalanx);
            if (term >= Supported && term < PassedBlocked) return "Supported rank " + (term - Supported);
            if (term >= PassedBlocked && term < PassedFree) return "PassedBlocked rank " + (term - PassedBlocked);
            if (term >= PassedFree && term < PassedOwnKingDistance) return "PassedFree rank " + (term - PassedFree);
            if (term >= PassedOwnKingDistance && term < PassedEnemyKingDistance) return "PassedOwnKingDistance rank " + (term - PassedOwnKingDistance);
            if (term >= PassedEnemyKingDistance && term < RookBehindPassed) return "PassedEnemyKingDistance rank " + (term - PassedEnemyKingDistance);
            if (term >= ThreatByMinor && term < ThreatByRook) return "ThreatByMinor on " + pieces[term - ThreatByMinor];
            if (term >= ThreatByRook && term < ThreatByQueen) return "ThreatByRook on " + pieces[term - ThreatByRook];
            if (term >= ThreatByQueen && term < ThreatByKing) return "ThreatByQueen on " + pieces[term - ThreatByQueen];
            if (term >= DangerZoneAttack && term < DangerSafeCheck) return "DangerZoneAttack " + pieces[term - DangerZoneAttack + 1];
            if (term >= DangerSafeCheck && term < DangerWeakSquare) return "DangerSafeCheck " + pieces[term - DangerSafeCheck + 1];
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
                case Tempo: return "Tempo";
                case WeakKingSquare: return "WeakKingSquare";
                case Backward: return "Backward";
                case RookBehindPassed: return "RookBehindPassed";
                case BadBishopPawns: return "BadBishopPawns";
                case TrappedBishop: return "TrappedBishop";
                case TrappedRook: return "TrappedRook";
                case MinorBehindPawn: return "MinorBehindPawn";
                case Space: return "Space";
                case ThreatByKing: return "ThreatByKing";
                case KnightPawns: return "KnightPawns";
                case RookPawns: return "RookPawns";
                case RookPair: return "RookPair";
                case KingOwnPawnDistance: return "KingOwnPawnDistance";
                case KingEnemyPawnDistance: return "KingEnemyPawnDistance";
                case DangerWeakSquare: return "DangerWeakSquare";
                case DangerAttackers: return "DangerAttackers";
                case DangerBias: return "DangerBias";
                default: return "Term " + term;
            }
        }

        // Whether a term is a single value (not part of a table), for the tuner's summary
        public static bool IsScalarTerm(int term)
        {
            return !(term >= PieceSquare && term < Passed)
                && !(term >= MobilityTable && term < SafeCheck);
        }

        // Everything the evaluation needs from a position, apart from the weights. Coefficients are White minus Black.
        public sealed class Trace
        {
            public readonly List<int> Terms = new List<int>(64);
            public readonly List<int> Coefficients = new List<int>(64);
            public int Phase;              // 0 (only kings and pawns) .. 24
            public int KingAttack;         // fixed middlegame part of the activity, White minus Black (basic set)
            public bool LoneKing;          // one side has only its king: the activity part is left out
            public int MopUp;              // fixed score (White's view), already blended
            public int EgScale;            // endgame part multiplied by EgScale / 64 (extended set: drawish endgames)
            // King danger inputs per attacking side (0 = White attacks the black king), full set only
            public readonly List<int>[] DangerTerms = { new List<int>(12), new List<int>(12) };
            public readonly List<int>[] DangerCoefficients = { new List<int>(12), new List<int>(12) };

            public void Clear()
            {
                Terms.Clear();
                Coefficients.Clear();
                for (int side = 0; side < 2; side++)
                {
                    DangerTerms[side].Clear();
                    DangerCoefficients[side].Clear();
                }
                Phase = KingAttack = MopUp = 0;
                EgScale = FullScale;
                LoneKing = false;
            }

            // Score from White's point of view for the given weights (same rounding as Evaluate)
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
                mg1 += DangerPenalty(Danger(0, weights)) - DangerPenalty(Danger(1, weights));
                return Blend(mg0, eg0, mg1, eg1, Phase, EgScale, LoneKing) + MopUp;
            }

            // Danger score of one attacking side for the given weights
            public int Danger(int side, int[] weights)
            {
                int danger = 0;
                for (int i = 0; i < DangerTerms[side].Count; i++)
                    danger += DangerCoefficients[side][i] * weights[2 * DangerTerms[side][i]];
                return danger;
            }
        }

        // Middlegame penalty for the king under attack: grows with the square of the danger, capped
        public static int DangerPenalty(int danger)
        {
            if (danger <= 0) return 0;
            return (int)Math.Min(DangerCap, (long)danger * danger / 1024);
        }

        // Receives the coefficient of each term as the position is scanned. Implemented by structs, so the JIT
        // compiles a separate, inlined copy of the scan for scoring and for tracing.
        private interface ITermSink
        {
            void Add(int term, int coefficient);
            void AddDanger(int side, int term, int coefficient);   // input of the king danger of `side`'s attack
        }

        // Multiplies the coefficients by the weights right away (used by the bots)
        private struct ScoreSink : ITermSink
        {
            public int[] Weights;
            public int Mg0, Eg0, Mg1, Eg1;
            public int DangerWhite, DangerBlack;

            public void AddDanger(int side, int term, int coefficient)
            {
                if (side == 0) DangerWhite += coefficient * Weights[2 * term];
                else DangerBlack += coefficient * Weights[2 * term];
            }

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
            public Trace Trace;

            public void Add(int term, int coefficient) => Coefficients[term] += coefficient;

            public void AddDanger(int side, int term, int coefficient)
            {
                if (coefficient == 0) return;
                Trace.DangerTerms[side].Add(term);
                Trace.DangerCoefficients[side].Add(coefficient);
            }
        }

        // The two parts (structure and activity) are blended and rounded separately, as in Evaluation
        private static int Blend(int mg0, int eg0, int mg1, int eg1, int phase, int egScale, bool loneKing)
        {
            int score = (mg0 * phase + eg0 * egScale / FullScale * (MaxPhase - phase)) / MaxPhase;
            if (!loneKing) score += (mg1 * phase + eg1 * egScale / FullScale * (MaxPhase - phase)) / MaxPhase;
            return score;
        }

        // Score from the point of view of the side to move. Basic set: like Evaluation.EvaluateWithActivity.
        public static int Evaluate(Position position, int[] weights, bool extended = false) =>
            Evaluate(position, weights, extended ? FeatureSet.Extended : FeatureSet.Basic);

        public static int Evaluate(Position position, int[] weights, FeatureSet features) =>
            Evaluate(position, weights, features, null);

        // The same score; the pawn-only terms are taken from (and stored in) the pawn cache when one is given.
        // The cache belongs to one weight array and feature set (one bot).
        public static int Evaluate(Position position, int[] weights, FeatureSet features, PawnCache pawnCache)
        {
            var sink = new ScoreSink { Weights = weights };
            int level = (int)features;
            if (pawnCache != null)
            {
                ulong whitePawns = position.Pieces(PieceType.Pawn, Side.White);
                ulong blackPawns = position.Pieces(PieceType.Pawn, Side.Black);
                int slot = pawnCache.Slot(whitePawns, blackPawns);
                if (pawnCache.White[slot] != whitePawns || pawnCache.Black[slot] != blackPawns || !pawnCache.Filled[slot])
                {
                    var pawnSink = new ScoreSink { Weights = weights };
                    CollectPawns(whitePawns, blackPawns, level, ref pawnSink);
                    pawnCache.Store(slot, whitePawns, blackPawns, pawnSink.Mg0, pawnSink.Eg0, pawnSink.Mg1, pawnSink.Eg1);
                }
                else
                {
                    pawnCache.Hits++;
                }
                sink.Mg0 = pawnCache.Mg0[slot];
                sink.Eg0 = pawnCache.Eg0[slot];
                sink.Mg1 = pawnCache.Mg1[slot];
                sink.Eg1 = pawnCache.Eg1[slot];
            }
            Scan(position, ref sink, level, pawnCache == null, out int phase, out int kingAttack, out bool loneKing, out int mopUp, out int egScale);
            int mg1 = sink.Mg1 + kingAttack + DangerPenalty(sink.DangerWhite) - DangerPenalty(sink.DangerBlack);
            int score = Blend(sink.Mg0, sink.Eg0, mg1, sink.Eg1, phase, egScale, loneKing) + mopUp;
            return position.SideToMove == Side.White ? score : -score;
        }

        [ThreadStatic] private static int[] threadCoefficients;

        // Fills the trace with the coefficients of every term for this position
        public static void Collect(Position position, Trace trace, bool extended = false) =>
            Collect(position, trace, extended ? FeatureSet.Extended : FeatureSet.Basic);

        public static void Collect(Position position, Trace trace, FeatureSet features)
        {
            trace.Clear();
            int[] coefficients = threadCoefficients ?? (threadCoefficients = new int[TermCount]);
            Array.Clear(coefficients, 0, TermCount);
            var sink = new CoefficientSink { Coefficients = coefficients, Trace = trace };
            Scan(position, ref sink, (int)features, true, out trace.Phase, out trace.KingAttack, out trace.LoneKing, out trace.MopUp, out trace.EgScale);

            for (int term = 0; term < TermCount; term++)
            {
                if (coefficients[term] == 0) continue;
                trace.Terms.Add(term);
                trace.Coefficients.Add(coefficients[term]);
            }
        }

        // Goes through the position once and reports every term's coefficient (White minus Black) to the sink
        // level: 0 = basic, 1 = extended, 2 = full feature set
        // withPawns: false when the pawn-only terms (CollectPawns) were already added from a pawn cache
        private static void Scan<TSink>(Position position, ref TSink sink, int level, bool withPawns, out int phase, out int kingAttack,
                                        out bool loneKing, out int mopUp, out int egScale) where TSink : struct, ITermSink
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

            if (withPawns) CollectPawns(whitePawns, blackPawns, level, ref sink);
            CollectStructure(position, Side.White, whitePawns, blackPawns, level, ref sink);
            CollectStructure(position, Side.Black, blackPawns, whitePawns, level, ref sink);
            CollectShelter(position, Side.White, whitePawns, blackPawns, ref sink);
            CollectShelter(position, Side.Black, blackPawns, whitePawns, ref sink);

            ulong kings = position.Pieces(PieceType.King);
            loneKing = (position.Pieces(Side.White) & ~kings) == 0 || (position.Pieces(Side.Black) & ~kings) == 0;
            CollectActivity(position, occupied, level, ref sink, out kingAttack);

            // Mop-up (not tuned), as in Evaluation: at least a rook more against a side without pawns
            mopUp = 0;
            if (whiteMaterial - blackMaterial >= 400 && blackPawns == 0)
                mopUp = MopUpBonus(position.KingSquare(Side.White), position.KingSquare(Side.Black));
            else if (blackMaterial - whiteMaterial >= 400 && whitePawns == 0)
                mopUp = -MopUpBonus(position.KingSquare(Side.Black), position.KingSquare(Side.White));

            egScale = FullScale;
            if (level >= 1)
            {
                sink.Add(Tempo, position.SideToMove == Side.White ? 1 : -1);
                egScale = EndgameScale(position, whiteMaterial, blackMaterial, whitePawns, blackPawns, level);
            }

            if (level >= 2)
            {
                for (int side = 0; side < 2; side++)
                {
                    int sign = side == (int)Side.White ? 1 : -1;
                    ulong ownPawns = side == (int)Side.White ? whitePawns : blackPawns;
                    ulong enemyPawns = side == (int)Side.White ? blackPawns : whitePawns;
                    int pawnCount = Bits.PopCount(ownPawns);

                    // Knights get better with many pawns on the board (closed positions), rooks with few (open files)
                    sink.Add(KnightPawns, sign * Bits.PopCount(position.Pieces(PieceType.Knight, (Side)side)) * (pawnCount - 5));
                    int rooks = Bits.PopCount(position.Pieces(PieceType.Rook, (Side)side));
                    sink.Add(RookPawns, sign * rooks * (pawnCount - 5));
                    if (rooks >= 2) sink.Add(RookPair, sign);

                    // King distance to the pawns: in the endgame the king belongs near them
                    int king = position.KingSquare((Side)side);
                    int own = 0, enemy = 0;
                    ulong bits = ownPawns;
                    while (bits != 0) own += Distance(king, Bits.PopLowest(ref bits));
                    bits = enemyPawns;
                    while (bits != 0) enemy += Distance(king, Bits.PopLowest(ref bits));
                    sink.Add(KingOwnPawnDistance, sign * own);
                    sink.Add(KingEnemyPawnDistance, sign * enemy);
                }
            }
        }

        // Fixed (not tuned) scale-down of endgames that tend to be drawn, applied to the endgame part
        private static int EndgameScale(Position position, int whiteMaterial, int blackMaterial, ulong whitePawns, ulong blackPawns, int level)
        {
            int scale = FullScale;

            // Opposite-coloured bishops: one bishop each, on squares of different colours
            ulong whiteBishops = position.Pieces(PieceType.Bishop, Side.White);
            ulong blackBishops = position.Pieces(PieceType.Bishop, Side.Black);
            if (Bits.PopCount(whiteBishops) == 1 && Bits.PopCount(blackBishops) == 1
                && ((whiteBishops & DarkSquares) != 0) != ((blackBishops & DarkSquares) != 0))
            {
                ulong otherPieces = position.Pieces(PieceType.Knight) | position.Pieces(PieceType.Rook) | position.Pieces(PieceType.Queen);
                scale = Math.Min(scale, otherPieces == 0 ? 32 : 48);
            }

            // The stronger side has no pawns and less than a rook more: often a draw
            bool whiteStronger = whiteMaterial >= blackMaterial;
            int difference = Math.Abs(whiteMaterial - blackMaterial);
            ulong strongPawns = whiteStronger ? whitePawns : blackPawns;
            if (strongPawns == 0 && difference < Evaluation.PieceValues[(int)PieceType.Rook])
                scale = Math.Min(scale, 16);

            // Full set: bishop and rook pawns only, the bishop doesn't control the promotion corner and the
            // defending king sits next to it - a known draw
            if (level >= 2 && strongPawns != 0)
            {
                Side strong = whiteStronger ? Side.White : Side.Black;
                ulong strongPieces = position.Pieces(strong) & ~position.Pieces(PieceType.Pawn) & ~position.Pieces(PieceType.King);
                ulong strongBishop = position.Pieces(PieceType.Bishop, strong);
                bool onlyAFile = (strongPawns & ~Bitboards.Files[0]) == 0, onlyHFile = (strongPawns & ~Bitboards.Files[7]) == 0;
                if (strongPieces == strongBishop && Bits.PopCount(strongBishop) == 1 && (onlyAFile || onlyHFile))
                {
                    int corner = Square.Make(onlyAFile ? 0 : 7, strong == Side.White ? 7 : 0);
                    bool bishopDark = (strongBishop & DarkSquares) != 0;
                    bool cornerDark = (DarkSquares & (1UL << corner)) != 0;
                    if (bishopDark != cornerDark && Distance(position.KingSquare(strong == Side.White ? Side.Black : Side.White), corner) <= 1)
                        scale = Math.Min(scale, 4);
                }
            }

            return scale;
        }

        private static void CollectStructure<TSink>(Position position, Side color, ulong ownPawns, ulong enemyPawns, int level,
                                                    ref TSink sink) where TSink : struct, ITermSink
        {
            int us = (int)color, them = 1 - us;
            int sign = color == Side.White ? 1 : -1;
            int forward = color == Side.White ? 8 : -8;

            ulong pawns = ownPawns;
            while (pawns != 0)
            {
                int square = Bits.PopLowest(ref pawns);
                int file = Square.File(square), rank = Square.Rank(square);
                int relativeRank = color == Side.White ? rank : 7 - rank;
                // (Passed, isolated, phalanx, supported and backward pawns depend on pawns only: CollectPawns)
                if (level < 1) continue;
                bool passed = (ownPawns & Bitboards.ForwardFile[us][square]) == 0 && (enemyPawns & Bitboards.PassedPawnMask[us][square]) == 0;
                int stop = square + forward;

                // Passed pawn details that depend on the pieces and kings
                if (passed)
                {
                    if ((position.Pieces((Side)them) & (1UL << stop)) != 0)
                        sink.Add(PassedBlocked + relativeRank, sign);
                    if ((Bitboards.ForwardFile[us][square] & position.Occupied) == 0)
                        sink.Add(PassedFree + relativeRank, sign);
                    sink.Add(PassedOwnKingDistance + relativeRank, sign * Distance(position.KingSquare(color), stop));
                    sink.Add(PassedEnemyKingDistance + relativeRank, sign * Distance(position.KingSquare((Side)them), stop));
                    if ((position.Pieces(PieceType.Rook, color) & Bitboards.ForwardFile[them][square]) != 0)
                        sink.Add(RookBehindPassed, sign);
                }
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

            ulong bishops = position.Pieces(PieceType.Bishop, color);
            if (Bits.PopCount(bishops) >= 2)
                sink.Add(BishopPair, sign);

            if (level < 1) return;

            // Bad bishop: own pawns on the squares of its colour block it
            while (bishops != 0)
            {
                int square = Bits.PopLowest(ref bishops);
                ulong colour = (DarkSquares & (1UL << square)) != 0 ? DarkSquares : ~DarkSquares;
                sink.Add(BadBishopPawns, sign * Bits.PopCount(ownPawns & colour));

                // Trapped on a7/h7 (a2/h2 for Black) by an enemy pawn on b6/g6 (b3/g3)
                int relative = color == Side.White ? square : square ^ 56;
                if ((relative == 48 && (enemyPawns & (1UL << (color == Side.White ? 41 : 41 ^ 56))) != 0)
                    || (relative == 55 && (enemyPawns & (1UL << (color == Side.White ? 46 : 46 ^ 56))) != 0))
                    sink.Add(TrappedBishop, sign);
            }

            // Knights and bishops right behind an own pawn (sheltered from frontal attacks)
            ulong minors = position.Pieces(PieceType.Knight, color) | position.Pieces(PieceType.Bishop, color);
            while (minors != 0)
            {
                int square = Bits.PopLowest(ref minors);
                int front = square + forward;
                if (front >= 0 && front < 64 && (ownPawns & (1UL << front)) != 0)
                    sink.Add(MinorBehindPawn, sign);
            }

            // Pawn storm: our pawns advanced on the files around the enemy king
            int enemyKingFile = Square.File(position.KingSquare((Side)them));
            ulong storm = ownPawns & (Bitboards.Files[enemyKingFile] | Bitboards.AdjacentFiles[enemyKingFile]);
            while (storm != 0)
            {
                int square = Bits.PopLowest(ref storm);
                int relativeRank = color == Side.White ? Square.Rank(square) : 7 - Square.Rank(square);
                if (relativeRank >= 3) sink.Add(PawnStorm + relativeRank, sign);
            }
        }

        // Every term that depends on the pawns only (so it can be cached by the pawn structure): passed, isolated,
        // doubled pawns, and in the extended sets phalanx, supported and backward pawns and space
        private static void CollectPawns<TSink>(ulong whitePawns, ulong blackPawns, int level, ref TSink sink)
            where TSink : struct, ITermSink
        {
            Span<ulong> pawnAttacks = stackalloc ulong[2];
            for (int side = 0; side < 2; side++)
            {
                ulong pawns = side == (int)Side.White ? whitePawns : blackPawns;
                ulong attacks = 0;
                while (pawns != 0) attacks |= Bitboards.PawnAttacks[side][Bits.PopLowest(ref pawns)];
                pawnAttacks[side] = attacks;
            }

            for (int side = 0; side < 2; side++)
            {
                Side color = (Side)side;
                int us = side, them = 1 - side;
                int sign = color == Side.White ? 1 : -1;
                int forward = color == Side.White ? 8 : -8;
                ulong ownPawns = color == Side.White ? whitePawns : blackPawns;
                ulong enemyPawns = color == Side.White ? blackPawns : whitePawns;

                ulong pawns = ownPawns;
                while (pawns != 0)
                {
                    int square = Bits.PopLowest(ref pawns);
                    int file = Square.File(square), rank = Square.Rank(square);
                    int relativeRank = color == Side.White ? rank : 7 - rank;
                    if ((ownPawns & Bitboards.ForwardFile[us][square]) == 0 && (enemyPawns & Bitboards.PassedPawnMask[us][square]) == 0)
                        sink.Add(Passed + relativeRank, sign);
                    if ((ownPawns & Bitboards.AdjacentFiles[file]) == 0)
                        sink.Add(Isolated, sign);

                    if (level < 1) continue;

                    // Phalanx: an own pawn right beside it; supported: defended by an own pawn
                    if ((ownPawns & Bitboards.AdjacentFiles[file] & Bitboards.Ranks[rank]) != 0)
                        sink.Add(Phalanx + relativeRank, sign);
                    if ((Bitboards.PawnAttacks[them][square] & ownPawns) != 0)
                        sink.Add(Supported + relativeRank, sign);

                    // Backward: has neighbours, but all of them are further up the board, and the square in front
                    // is controlled by an enemy pawn - it can't safely advance and no pawn can come to defend it
                    int stop = square + forward;
                    ulong neighbours = ownPawns & Bitboards.AdjacentFiles[file];
                    ulong behindOrLevel = color == Side.White ? (2UL << (8 * rank + 7)) - 1 : ~((1UL << (8 * rank)) - 1);
                    if (neighbours != 0 && (neighbours & behindOrLevel) == 0
                        && (Bitboards.PawnAttacks[us][stop] & enemyPawns) != 0)
                        sink.Add(Backward, sign);
                }

                for (int file = 0; file < 8; file++)
                {
                    int count = Bits.PopCount(ownPawns & Bitboards.Files[file]);
                    if (count > 1) sink.Add(Doubled, sign * (count - 1));
                }

                if (level < 1) continue;

                // Space: central squares on our side behind our own pawns that enemy pawns don't attack
                                ulong behind = ownPawns;
                if (side == (int)Side.White) { behind |= behind >> 8; behind |= behind >> 16; behind |= behind >> 32; }
                else { behind |= behind << 8; behind |= behind << 16; behind |= behind << 32; }
                ulong ourRanks = side == (int)Side.White
                    ? Bitboards.Ranks[1] | Bitboards.Ranks[2] | Bitboards.Ranks[3]
                    : Bitboards.Ranks[6] | Bitboards.Ranks[5] | Bitboards.Ranks[4];
                ulong centre = Bitboards.Files[2] | Bitboards.Files[3] | Bitboards.Files[4] | Bitboards.Files[5];
                sink.Add(Space, sign * Bits.PopCount(centre & ourRanks & behind & ~ownPawns & ~pawnAttacks[them]));
            }
        }

        // Pawn-only part of the score per pawn structure (both sides' pawn bitboards), for one weight array.
        // Pawns move rarely, so the same structure is evaluated again and again during a search.
        public sealed class PawnCache
        {
            private const int SizeBits = 16;
            public readonly ulong[] White = new ulong[1 << SizeBits];
            public readonly ulong[] Black = new ulong[1 << SizeBits];
            public readonly bool[] Filled = new bool[1 << SizeBits];
            public readonly int[] Mg0 = new int[1 << SizeBits], Eg0 = new int[1 << SizeBits], Mg1 = new int[1 << SizeBits], Eg1 = new int[1 << SizeBits];
            public long Hits;

            public int Slot(ulong white, ulong black) =>
                (int)(((white * 0x9E3779B97F4A7C15UL) ^ (black * 0xC2B2AE3D27D4EB4FUL)) >> (64 - SizeBits));

            public void Store(int slot, ulong white, ulong black, int mg0, int eg0, int mg1, int eg1)
            {
                White[slot] = white;
                Black[slot] = black;
                Filled[slot] = true;
                Mg0[slot] = mg0; Eg0[slot] = eg0; Mg1[slot] = mg1; Eg1[slot] = eg1;
            }
        }

        private static int Distance(int a, int b)
        {
            return Math.Max(Math.Abs(Square.File(a) - Square.File(b)), Math.Abs(Square.Rank(a) - Square.Rank(b)));
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

        // Mobility, threats and outposts (and in the extended set the king safety terms and space).
        // In the basic set the king attack is a fixed score (White minus Black).
        private static void CollectActivity<TSink>(Position position, ulong occupied, int level, ref TSink sink, out int kingAttack)
            where TSink : struct, ITermSink
        {
            Span<ulong> pawnAttacks = stackalloc ulong[2];
            Span<ulong> allAttacks = stackalloc ulong[2];       // pawns, pieces and king
            Span<ulong> pieceAttacks = stackalloc ulong[2];     // knights, bishops, rooks, queens
            Span<ulong> typeAttacks = stackalloc ulong[2 * 7];  // per side and piece type
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
            Span<int> zoneSquares = stackalloc int[2 * 7];      // per side and piece type: enemy king zone squares hit
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
                        pieceAttacks[side] |= attacks;
                        typeAttacks[side * 7 + type] |= attacks;

                        int mobility = Bits.PopCount(attacks & safe);
                        if (level >= 1)
                        {
                            sink.Add(MobilityTable + MobilityTableStart[type] + Math.Min(mobility, MobilityTableSize[type] - 1), sign);
                            if (type == (int)PieceType.Rook && mobility <= 3 && IsTrappedRook(position, (Side)side, square))
                                sink.Add(TrappedRook, sign);
                        }
                        else
                        {
                            sink.Add(Mobility + type - (int)PieceType.Knight, sign * (mobility - MobilityBase[type]));
                        }

                        ulong zone = attacks & enemyKingZone;
                        if (zone != 0)
                        {
                            kingAttackers[side]++;
                            kingUnits[side] += KingAttackWeight[type] * Bits.PopCount(zone);
                            zoneSquares[side * 7 + type] += Bits.PopCount(zone);
                        }
                    }
                }
            }

            Span<int> safeChecks = stackalloc int[4];
            kingAttack = 0;
            for (int side = 0; side < 2; side++)
            {
                int them = 1 - side;
                int sign = side == (int)Side.White ? 1 : -1;

                // Full set: the king danger is built below from tunable inputs
                if (kingAttackers[side] >= 2 && level < 2)
                {
                    if (level == 1) sink.Add(KingDanger + Math.Min(kingUnits[side], KingDangerSize - 1), sign);
                    else kingAttack += sign * Math.Min(KingAttackMaxPenalty, kingUnits[side] * kingUnits[side] / 4);
                }

                ulong enemyPieces = position.Pieces((Side)them) & ~position.Pieces(PieceType.Pawn) & ~position.Pieces(PieceType.King);
                sink.Add(PawnThreat, sign * Bits.PopCount(pawnAttacks[side] & enemyPieces));

                ulong minorAttacks = typeAttacks[side * 7 + (int)PieceType.Knight] | typeAttacks[side * 7 + (int)PieceType.Bishop];
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

                if (level < 1) continue;

                // Safe checks: squares from which our pieces could check the enemy king and which the enemy doesn't attack
                int enemyKing = position.KingSquare((Side)them);
                ulong safeForChecks = ~position.Pieces((Side)side) & ~allAttacks[them];
                ulong rookLines = Bitboards.RookAttacks(enemyKing, occupied), bishopLines = Bitboards.BishopAttacks(enemyKing, occupied);
                safeChecks[0] = Bits.PopCount(Bitboards.KnightAttacks[enemyKing] & typeAttacks[side * 7 + (int)PieceType.Knight] & safeForChecks);
                safeChecks[1] = Bits.PopCount(bishopLines & typeAttacks[side * 7 + (int)PieceType.Bishop] & safeForChecks);
                safeChecks[2] = Bits.PopCount(rookLines & typeAttacks[side * 7 + (int)PieceType.Rook] & safeForChecks);
                safeChecks[3] = Bits.PopCount((rookLines | bishopLines) & typeAttacks[side * 7 + (int)PieceType.Queen] & safeForChecks);
                for (int i = 0; i < 4; i++) sink.Add(SafeCheck + i, sign * safeChecks[i]);

                // Weak squares next to the enemy king: we attack them, only its king defends them
                ulong enemyZone = Bitboards.KingAttacks[enemyKing];
                int weakSquares = Bits.PopCount(enemyZone & allAttacks[side] & ~(pawnAttacks[them] | pieceAttacks[them]));
                sink.Add(WeakKingSquare, sign * weakSquares);
                if (level >= 2)
                {
                    // King danger from tunable inputs, counted with two or more attackers (as before)
                    if (kingAttackers[side] >= 2)
                    {
                        for (int type = (int)PieceType.Knight; type <= (int)PieceType.Queen; type++)
                            sink.AddDanger(side, DangerZoneAttack + type - (int)PieceType.Knight, zoneSquares[side * 7 + type]);
                        for (int i = 0; i < 4; i++) sink.AddDanger(side, DangerSafeCheck + i, safeChecks[i]);
                        sink.AddDanger(side, DangerWeakSquare, weakSquares);
                        sink.AddDanger(side, DangerAttackers, kingAttackers[side]);
                        sink.AddDanger(side, DangerBias, 1);
                    }

                    // Threats by piece type against each kind of enemy piece. A minor's or rook's target counts when
                    // it isn't defended by a pawn (or is worth more than the attacker); a queen's or the king's when
                    // it isn't defended at all.
                    ulong undefended = ~allAttacks[them];
                    ulong notPawnDefended = ~pawnAttacks[them];
                    ulong minorHits = typeAttacks[side * 7 + (int)PieceType.Knight] | typeAttacks[side * 7 + (int)PieceType.Bishop];
                    ulong rookHits = typeAttacks[side * 7 + (int)PieceType.Rook];
                    ulong queenHits = typeAttacks[side * 7 + (int)PieceType.Queen];
                    for (int victim = (int)PieceType.Pawn; victim <= (int)PieceType.Queen; victim++)
                    {
                        ulong targets = position.Pieces((PieceType)victim, (Side)them);
                        if (targets == 0) continue;
                        ulong minorTargets = victim >= (int)PieceType.Rook ? targets : targets & notPawnDefended;
                        ulong rookTargets = victim == (int)PieceType.Queen ? targets : targets & undefended;
                        sink.Add(ThreatByMinor + victim - 1, sign * Bits.PopCount(minorHits & minorTargets));
                        sink.Add(ThreatByRook + victim - 1, sign * Bits.PopCount(rookHits & rookTargets));
                        sink.Add(ThreatByQueen + victim - 1, sign * Bits.PopCount(queenHits & targets & undefended));
                    }
                    ulong kingTargets = position.Pieces((Side)them) & ~position.Pieces(PieceType.King) & undefended;
                    sink.Add(ThreatByKing, sign * Bits.PopCount(Bitboards.KingAttacks[position.KingSquare((Side)side)] & kingTargets));
                }

            }
        }

        // A rook shut in by its own uncastled king on the back rank (e.g. Kf1/Kg1 with the rook on g1/h1)
        private static bool IsTrappedRook(Position position, Side color, int rook)
        {
            int king = position.KingSquare(color);
            int backRank = color == Side.White ? 0 : 7;
            if (Square.Rank(king) != backRank || Square.Rank(rook) != backRank) return false;
            int kingFile = Square.File(king), rookFile = Square.File(rook);
            return (kingFile >= 5 && kingFile <= 6 && rookFile > kingFile) || (kingFile >= 1 && kingFile <= 2 && rookFile < kingFile);
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

        // Extended starting weights from basic ones: the mobility tables give the same bonus as the linear
        // mobility, the king danger table the same as the fixed formula, all the new terms start at 0
        public static int[] BuildDefaultExtended(int[] basic)
        {
            var w = new int[2 * TermCount];
            Array.Copy(basic, w, Math.Min(basic.Length, 2 * BasicTermCount));
            for (int type = (int)PieceType.Knight; type <= (int)PieceType.Queen; type++)
            {
                int linear = Mobility + type - (int)PieceType.Knight;
                for (int count = 0; count < MobilityTableSize[type]; count++)
                {
                    int term = MobilityTable + MobilityTableStart[type] + count;
                    w[2 * term] = (count - MobilityBase[type]) * basic[2 * linear];
                    w[2 * term + 1] = (count - MobilityBase[type]) * basic[2 * linear + 1];
                }
            }
            for (int units = 0; units < KingDangerSize; units++)
                w[2 * (KingDanger + units)] = Math.Min(KingAttackMaxPenalty, units * units / 4);
            return w;
        }

        // Full starting weights from extended ones. The king danger inputs reproduce the old formula:
        // danger = 16 * units (units = 2/2/3/5 per zone square for N/B/R/Q), penalty = danger^2 / 1024 = units^2 / 4
        public static int[] BuildDefaultFull(int[] extended)
        {
            var w = (int[])extended.Clone();
            if (w.Length < 2 * TermCount) Array.Resize(ref w, 2 * TermCount);
            for (int type = (int)PieceType.Knight; type <= (int)PieceType.Queen; type++)
                w[2 * (DangerZoneAttack + type - (int)PieceType.Knight)] = 16 * KingAttackWeight[type];
            return w;
        }
    }
}
