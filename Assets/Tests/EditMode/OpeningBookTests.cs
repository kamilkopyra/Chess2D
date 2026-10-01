using NUnit.Framework;

namespace ChessEngine.Tests
{
    public class OpeningBookTests
    {
        const string Book =
            "# comment\n" +
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq\te2e4:3 d2d4:1\n" +
            "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq\tc7c5:5 e1e2:99\n";

        [Test]
        public void ParsesPositions()
        {
            Assert.AreEqual(2, OpeningBook.Parse(Book).Count);
        }

        [Test]
        public void PicksOnlyBookMovesWeightedByGames()
        {
            var book = OpeningBook.Parse(Book);
            var random = new System.Random(1);
            int e4 = 0, d4 = 0;

            for (int i = 0; i < 1000; i++)
            {
                Assert.IsTrue(book.TryGetMove(Position.StartPosition(), random, out Move move));
                if (move.ToString() == "e2e4") e4++;
                else if (move.ToString() == "d2d4") d4++;
                else Assert.Fail($"Not a book move: {move}");
            }

            // Weights 3:1, so e4 should be played roughly three times as often
            Assert.Greater(e4, 2 * d4);
        }

        [Test]
        public void IgnoresIllegalBookMoves()
        {
            var book = OpeningBook.Parse(Book);
            var position = Position.StartPosition();
            position.MakeMove(position.GetLegalMoves().Find(m => m.ToString() == "e2e4"));

            // "e1e2" is listed for black but is not a legal black move: only c7c5 may be returned
            for (int i = 0; i < 50; i++)
            {
                Assert.IsTrue(book.TryGetMove(position, new System.Random(i), out Move move));
                Assert.AreEqual("c7c5", move.ToString());
            }
        }

        [Test]
        public void ReturnsFalseOutsideTheBook()
        {
            var book = OpeningBook.Parse(Book);
            var position = Position.FromFen("8/8/4k3/8/8/4K3/8/7R w - - 0 1");
            Assert.IsFalse(book.TryGetMove(position, new System.Random(1), out _));
        }
    }
}
