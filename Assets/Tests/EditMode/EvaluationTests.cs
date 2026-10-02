using System;
using System.Linq;
using NUnit.Framework;

namespace ChessEngine.Tests
{
    public class EvaluationTests
    {
        [Test]
        public void StartPositionIsEqual()
        {
            Assert.AreEqual(0, Evaluation.Evaluate(Position.StartPosition()));
        }

        // The same position with colours swapped (and the board flipped) must get the same score
        // for the side to move. Catches mistakes in mirroring the tables for Black.
        [TestCase("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3")]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1")]
        [TestCase("4k3/8/8/3P4/8/8/8/4K3 b - - 0 1")]
        public void IsSymmetricForBothColours(string fen)
        {
            Assert.AreEqual(Evaluation.Evaluate(Position.FromFen(fen)), Evaluation.Evaluate(Position.FromFen(Mirror(fen))));
        }

        [Test]
        public void KnightInTheCentreIsBetterThanInTheCorner()
        {
            int centre = Evaluation.Evaluate(Position.FromFen("4k3/8/8/8/4N3/8/8/4K3 w - - 0 1"));
            int corner = Evaluation.Evaluate(Position.FromFen("4k3/8/8/8/8/8/8/N3K3 w - - 0 1"));
            Assert.Greater(centre, corner);
        }

        [Test]
        public void AdvancedPawnIsBetterInTheEndgame()
        {
            int advanced = Evaluation.Evaluate(Position.FromFen("4k3/8/3P4/8/8/8/8/4K3 w - - 0 1"));
            int home = Evaluation.Evaluate(Position.FromFen("4k3/8/8/8/8/8/3P4/4K3 w - - 0 1"));
            Assert.Greater(advanced, home);
        }

        [Test]
        public void KingPrefersTheCentreInTheEndgame()
        {
            int centre = Evaluation.Evaluate(Position.FromFen("4k3/8/8/8/4K3/8/8/8 w - - 0 1"));
            int corner = Evaluation.Evaluate(Position.FromFen("4k3/8/8/8/8/8/8/K7 w - - 0 1"));
            Assert.Greater(centre, corner);
        }

        [TestCase("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3")]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1")]
        [TestCase("2r3k1/1P3pp1/8/3p4/8/2P2P2/2P3PR/6K1 b - - 0 1")]
        public void StructureIsSymmetricForBothColours(string fen)
        {
            Assert.AreEqual(Evaluation.Structure(Position.FromFen(fen)), Evaluation.Structure(Position.FromFen(Mirror(fen))));
        }

        [Test]
        public void StructureIsZeroInTheStartPosition()
        {
            Assert.AreEqual(0, Evaluation.Structure(Position.StartPosition()));
        }

        // Each pair: the first position is better for White than the second because of one structural feature
        [TestCase("4k3/8/8/3P4/8/8/8/4K3 w - - 0 1", "4k3/4p3/8/3P4/8/8/8/4K3 w - - 0 1")]         // passed pawn vs pawn that can be stopped
        [TestCase("4k3/p7/8/8/8/8/P1P5/4K3 w - - 0 1", "4k3/p7/8/8/8/2P5/2P5/4K3 w - - 0 1")]     // healthy vs doubled pawns
        [TestCase("4k3/pp6/8/8/8/8/PP6/4K3 w - - 0 1", "4k3/pp6/8/8/8/8/P1P5/4K3 w - - 0 1")]     // connected vs isolated pawns
        [TestCase("4k3/8/8/8/8/8/8/2B1KB2 w - - 0 1", "4k3/8/8/8/8/8/8/1NB1K3 w - - 0 1")]        // bishop pair vs bishop and knight
        [TestCase("4k3/pp6/8/8/8/8/PP6/4KR2 w - - 0 1", "4k3/pp3p2/8/8/8/8/PP3P2/4KR2 w - - 0 1")] // rook on an open vs closed file
        public void StructureRewardsBetterFeature(string better, string worse)
        {
            Assert.Greater(Evaluation.Structure(Position.FromFen(better)), Evaluation.Structure(Position.FromFen(worse)));
        }

        [TestCase("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3")]
        [TestCase("r4rk1/pp3ppp/2n5/8/8/5N2/PP3P1P/R4RK1 b - - 0 1")]
        [TestCase("2kr4/ppp5/8/8/8/8/5PP1/6K1 w - - 0 1")]
        public void KingSafetyIsSymmetricForBothColours(string fen)
        {
            Assert.AreEqual(Evaluation.KingSafety(Position.FromFen(fen)), Evaluation.KingSafety(Position.FromFen(Mirror(fen))));
        }

        [Test]
        public void KingSafetyIsZeroInTheStartPosition()
        {
            Assert.AreEqual(0, Evaluation.KingSafety(Position.StartPosition()));
        }

        // Each pair: White's king is safer in the first position (same material, middlegame)
        [TestCase("r4rk1/ppp2ppp/8/8/8/8/PPP2PPP/R2Q1RK1 w - - 0 1", "r4rk1/ppp2ppp/8/8/8/6P1/PPP2P1P/R2Q1RK1 w - - 0 1")] // intact vs advanced g-pawn
        [TestCase("r4rk1/ppp2ppp/8/8/8/8/PPP2PPP/R2Q1RK1 w - - 0 1", "r4rk1/ppp2ppp/8/8/8/8/PPP2PP1/R2Q1RK1 w - - 0 1")] // intact vs missing h-pawn
        public void KingSafetyRewardsPawnShelter(string safer, string lessSafe)
        {
            Assert.Greater(Evaluation.KingSafety(Position.FromFen(safer)), Evaluation.KingSafety(Position.FromFen(lessSafe)));
        }

        // The one-pass evaluation of Bot_v17 must give exactly the same score as its separate parts
        [TestCase("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3")]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("8/8/8/4k3/8/8/8/K6Q w - - 0 1")]   // mop-up applies (White)
        [TestCase("8/8/8/4k3/8/8/8/K6Q b - - 0 1")]
        [TestCase("4k3/8/8/8/8/1r6/8/4K3 w - - 0 1")] // mop-up applies (Black)
        [TestCase("4k3/8/8/8/8/1r6/7P/4K3 b - - 0 1")]
        public void FullEvaluationEqualsItsParts(string fen)
        {
            var position = Position.FromFen(fen);
            Assert.AreEqual(Evaluation.EvaluateWithKingSafety(position) + Evaluation.MopUp(position), Evaluation.EvaluateFull(position));
        }

        // The bitboard evaluation must give exactly the same scores as the old one (EvaluationReference)
        // for every evaluation function, on many positions from random games (including mop-up endgames)
        [Test]
        public void BitboardEvaluationMatchesReference()
        {
            string[] fens =
            {
                Position.StartFen,
                "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
                "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1",
                "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1",
                "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10",
                "6k1/5ppp/8/8/8/8/PPP2PPP/3R2K1 w - - 0 1",
                "8/8/8/4k3/8/8/8/K6Q w - - 0 1",
                "4k3/8/8/8/8/1r6/7P/4K3 b - - 0 1",
                "2r3k1/pp3ppp/8/3p4/3P4/8/PP3PPP/2R3K1 w - - 0 1",
            };

            var random = new System.Random(2);
            int checkedPositions = 0;
            foreach (string fen in fens)
            {
                for (int game = 0; game < 15; game++)
                {
                    var position = Position.FromFen(fen);
                    for (int ply = 0; ply < 150; ply++)
                    {
                        if (Evaluation.Evaluate(position) != EvaluationReference.Evaluate(position)
                            || Evaluation.EvaluateWithStructure(position) != EvaluationReference.EvaluateWithStructure(position)
                            || Evaluation.EvaluateWithKingSafety(position) != EvaluationReference.EvaluateWithKingSafety(position)
                            || Evaluation.EvaluateFull(position) != EvaluationReference.EvaluateFull(position)
                            || Evaluation.MopUp(position) != EvaluationReference.MopUp(position)
                            || Evaluation.Structure(position) != EvaluationReference.Structure(position)
                            || Evaluation.KingSafety(position) != EvaluationReference.KingSafety(position))
                        {
                            Assert.Fail($"different evaluation in {position.ToFen()}");
                        }
                        checkedPositions++;

                        var moves = position.GetLegalMoves();
                        if (moves.Count == 0) break;
                        position.MakeMove(moves[random.Next(moves.Count)]);
                    }
                }
            }
            Assert.Greater(checkedPositions, 10000);
        }

        [TestCase("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3")]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("r4rk1/pp3ppp/2n5/3N4/8/5Q2/PP3PPP/R4RK1 b - - 0 1")]
        [TestCase("6k1/5ppp/8/3n4/8/2B5/5PPP/3R2K1 w - - 0 1")]
        public void ActivityIsSymmetricForBothColours(string fen)
        {
            Assert.AreEqual(Evaluation.Activity(Position.FromFen(fen)), Evaluation.Activity(Position.FromFen(Mirror(fen))));
        }

        [Test]
        public void ActivityIsZeroInTheStartPosition()
        {
            Assert.AreEqual(0, Evaluation.Activity(Position.StartPosition()));
        }

        // Each pair: the first position is better for White than the second because of one activity feature
        [TestCase("4k3/8/8/8/3N4/8/8/4K3 w - - 0 1", "4k3/8/8/8/8/8/8/N3K3 w - - 0 1")]                 // mobile vs cornered knight
        [TestCase("4k3/8/8/3p4/4P3/8/8/4K3 w - - 0 1", "4k3/8/8/3p4/8/4P3/8/4K3 w - - 0 1")]             // nothing to attack: equal-ish
        [TestCase("4k3/8/8/3n4/4P3/8/8/4K3 w - - 0 1", "4k3/8/8/3n4/8/4P3/8/4K3 w - - 0 1")]             // pawn attacks a knight
        [TestCase("4k3/p7/8/4N3/3P4/8/8/4K3 w - - 0 1", "4k3/p7/8/8/3P4/2N5/8/4K3 w - - 0 1")]           // knight outpost vs at home
        [TestCase("6k1/5pp1/8/6NQ/8/8/5PPP/6K1 w - - 0 1", "6k1/5pp1/8/8/8/8/5PPP/QN4K1 w - - 0 1")]    // attacking the king zone
        public void ActivityRewardsBetterFeature(string better, string worse)
        {
            Assert.GreaterOrEqual(Evaluation.Activity(Position.FromFen(better)), Evaluation.Activity(Position.FromFen(worse)));
        }

        // Flips the board vertically and swaps the colours of all pieces, the side to move and castling rights
        public static string MirrorFen(string fen) => Mirror(fen);

        private static string Mirror(string fen)
        {
            string[] f = fen.Split(' ');
            string board = string.Join("/", f[0].Split('/').Reverse().Select(SwapCase));
            string side = f[1] == "w" ? "b" : "w";
            string castling = f[2] == "-" ? "-" : new string(SwapCase(f[2]).OrderBy(c => char.IsLower(c)).ThenBy(c => c == 'q' || c == 'Q').ToArray());
            string ep = f[3] == "-" ? "-" : $"{f[3][0]}{(char)('1' + '8' - f[3][1])}";
            return $"{board} {side} {castling} {ep} {f[4]} {f[5]}";
        }

        private static string SwapCase(string s) =>
            new string(s.Select(c => char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c)).ToArray());
    }
}
