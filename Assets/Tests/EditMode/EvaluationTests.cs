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

        // Flips the board vertically and swaps the colours of all pieces, the side to move and castling rights
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
