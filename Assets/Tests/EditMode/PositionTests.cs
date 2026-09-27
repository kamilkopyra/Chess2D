using NUnit.Framework;

namespace ChessEngine.Tests
{
    public class PositionTests
    {
        [TestCase(Position.StartFen)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("8/8/4k3/8/3pP3/8/8/4K3 b - e3 0 1")]
        public void FenRoundTrip(string fen)
        {
            Assert.AreEqual(fen, Position.FromFen(fen).ToFen());
        }

        [Test]
        public void HashStaysConsistentDuringRandomGame()
        {
            var random = new System.Random(1);
            var position = Position.StartPosition();

            for (int ply = 0; ply < 300; ply++)
            {
                var moves = position.GetLegalMoves();
                if (moves.Count == 0) break;
                position.MakeMove(moves[random.Next(moves.Count)]);
                Assert.IsTrue(position.HashIsConsistent(), $"Hash rozjechał się w ruchu {ply}");
            }
        }

        [TestCase("rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3", GameStatus.Checkmate)]
        [TestCase("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1", GameStatus.Stalemate)]
        [TestCase("8/8/4k3/8/8/4K3/8/7R w - - 100 80", GameStatus.FiftyMoveRule)]
        [TestCase("8/8/4k3/8/8/4K3/8/8 w - - 0 1", GameStatus.InsufficientMaterial)]
        [TestCase("8/8/4k3/8/8/4KB2/8/8 w - - 0 1", GameStatus.InsufficientMaterial)]
        [TestCase("8/8/4k3/8/8/4KN2/8/8 w - - 0 1", GameStatus.InsufficientMaterial)]
        [TestCase("8/8/2b1k3/8/8/4KB2/8/8 w - - 0 1", GameStatus.InsufficientMaterial)] // gońce na polach tego samego koloru
        [TestCase("8/8/3bk3/8/8/4KB2/8/8 w - - 0 1", GameStatus.Ongoing)]              // gońce na różnych kolorach
        [TestCase("8/8/4k3/8/8/4KR2/8/8 w - - 0 1", GameStatus.Ongoing)]
        public void DetectsGameStatus(string fen, GameStatus expected)
        {
            Assert.AreEqual(expected, Position.FromFen(fen).GetStatus());
        }

        [Test]
        public void DetectsThreefoldRepetition()
        {
            var position = Position.StartPosition();
            string[] moves = { "g1f3", "g8f6", "f3g1", "f6g8", "g1f3", "g8f6", "f3g1", "f6g8" };

            foreach (string uci in moves)
            {
                Assert.AreNotEqual(GameStatus.ThreefoldRepetition, position.GetStatus());
                position.MakeMove(position.GetLegalMoves().Find(m => m.ToString() == uci));
            }

            Assert.AreEqual(GameStatus.ThreefoldRepetition, position.GetStatus());
        }

        [Test]
        public void CastlingThroughAttackedSquareIsIllegal()
        {
            // Czarna wieża na f8 atakuje f1, więc krótka roszada białych jest zabroniona, długa dozwolona
            var position = Position.FromFen("5r1k/8/8/8/8/8/8/R3K2R w KQ - 0 1");
            var moves = position.GetLegalMoves();

            Assert.IsFalse(moves.Exists(m => m.ToString() == "e1g1"));
            Assert.IsTrue(moves.Exists(m => m.ToString() == "e1c1"));
        }

        [Test]
        public void QueensideCastlingAllowedWhenOnlyB1IsAttacked()
        {
            // Czarna wieża na b8 atakuje b1; to nie blokuje długiej roszady
            var position = Position.FromFen("1r5k/8/8/8/8/8/8/R3K3 w Q - 0 1");
            Assert.IsTrue(position.GetLegalMoves().Exists(m => m.ToString() == "e1c1"));
        }

        [Test]
        public void EnPassantCaptureOfCheckingPawnIsLegal()
        {
            // Czarny pion po ruchu d7-d5 szachuje białego króla na e4; zbicie w przelocie exd6 musi być dozwolone
            var position = Position.FromFen("8/8/8/3pP3/4K3/8/8/7k w - d6 0 1");
            Assert.IsTrue(position.InCheck);
            Assert.IsTrue(position.GetLegalMoves().Exists(m => m.ToString() == "e5d6" && m.IsEnPassant));
        }
    }
}
