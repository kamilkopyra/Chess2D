using NUnit.Framework;

namespace ChessEngine.Tests
{
    // Znane wyniki perft z https://www.chessprogramming.org/Perft_Results
    public class PerftTests
    {
        const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
        const string Position3 = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
        const string Position4 = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
        const string Position5 = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";

        [TestCase(Position.StartFen, 1, 20)]
        [TestCase(Position.StartFen, 2, 400)]
        [TestCase(Position.StartFen, 3, 8902)]
        [TestCase(Position.StartFen, 4, 197281)]
        [TestCase(Kiwipete, 1, 48)]
        [TestCase(Kiwipete, 2, 2039)]
        [TestCase(Kiwipete, 3, 97862)]
        [TestCase(Position3, 4, 43238)]
        [TestCase(Position3, 5, 674624)]
        [TestCase(Position4, 3, 9467)]
        [TestCase(Position5, 3, 62379)]
        public void PerftMatchesKnownResults(string fen, int depth, long expected)
        {
            var position = Position.FromFen(fen);
            Assert.AreEqual(expected, Perft.Count(position, depth));

            // Po przeszukaniu pozycja musi wrócić dokładnie do stanu wyjściowego
            Assert.AreEqual(Position.FromFen(fen).ToFen(), position.ToFen());
            Assert.IsTrue(position.HashIsConsistent());
        }

        // The fast legal move generator (Bot_v17 and newer) must give the same counts
        [TestCase(Position.StartFen, 4, 197281)]
        [TestCase(Kiwipete, 3, 97862)]
        [TestCase(Position3, 5, 674624)]
        [TestCase(Position4, 3, 9467)]
        [TestCase(Position5, 3, 62379)]
        public void FastPerftMatchesKnownResults(string fen, int depth, long expected)
        {
            var position = Position.FromFen(fen);
            Assert.AreEqual(expected, Perft.CountFast(position, depth));
            Assert.AreEqual(Position.FromFen(fen).ToFen(), position.ToFen());
        }
    }
}
