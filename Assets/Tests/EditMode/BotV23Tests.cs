using System.Collections.Generic;
using NUnit.Framework;

namespace ChessEngine.Tests
{
    public class BotV23Tests
    {
        const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
        const string Position3 = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
        const string Position4 = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
        const string Position5 = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";

        // GivesCheck (decided before the move is made) must agree with making the move and looking,
        // for every legal move in every position of a small perft tree
        [TestCase(Position.StartFen, 3)]
        [TestCase(Kiwipete, 3)]
        [TestCase(Position3, 4)]
        [TestCase(Position4, 3)]
        [TestCase(Position5, 3)]
        public void GivesCheckMatchesMakeMove(string fen, int depth)
        {
            var position = Position.FromFen(fen);
            int checks = CheckGivesCheck(position, depth);
            Assert.Greater(checks, 0);
            Assert.AreEqual(Position.FromFen(fen).ToFen(), position.ToFen());
        }

        // Returns the number of checking moves seen, so the test knows checks were actually covered
        private static int CheckGivesCheck(Position position, int depth)
        {
            int checks = 0;
            var moves = new List<Move>();
            position.GenerateLegalMoves(moves);
            foreach (var move in moves)
            {
                bool predicted = Bot_v23.GivesCheck(position, move);
                position.MakeMove(move);
                bool actual = position.InCheck;
                Assert.AreEqual(actual, predicted, "Move {0} in {1}", move, position);
                if (actual) checks++;
                if (depth > 1) checks += CheckGivesCheck(position, depth - 1);
                position.UnmakeMove();
            }
            return checks;
        }

        [TestCase("6k1/5ppp/8/8/8/8/5PPP/3R2K1 w - - 0 1", "d1d8")]                            // back rank mate
        [TestCase("r1bqkbnr/pppp1ppp/2n5/4p3/2B1P3/5Q2/PPPP1PPP/RNB1K1NR w KQkq - 0 1", "f3f7")] // scholar's mate
        public void FindsMateInOne(string fen, string expected)
        {
            var saved = OpeningBook.Default;
            OpeningBook.Default = null;
            try
            {
                var bot = new Bot_v23(4);
                Move move = bot.ChooseMove(Position.FromFen(fen));
                Assert.AreEqual(expected, move.ToString());
            }
            finally
            {
                OpeningBook.Default = saved;
            }
        }
    }
}
