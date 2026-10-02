using System.Collections.Generic;
using NUnit.Framework;

namespace ChessEngine.Tests
{
    // Packing of moves and scores in the compact transposition table of Bot_v21
    public class TranspositionTableTests
    {
        const int MateScore = 1_000_000;
        const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
        const string Position4 = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
        const string Position5 = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
        const string EnPassant = "rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3";
        const string Promotions = "r3k2r/1P4P1/8/8/8/8/1p4p1/R3K2R b KQkq - 0 1";

        // Every move generated in the position and two plies below it survives packing unchanged
        [TestCase(Position.StartFen)]
        [TestCase(Kiwipete)]
        [TestCase(Position4)]
        [TestCase(Position5)]
        [TestCase(EnPassant)]
        [TestCase(Promotions)]
        public void PackedMovesUnpackToGeneratedMoves(string fen)
        {
            var position = Position.FromFen(fen);
            int checkedMoves = CheckMoves(position, 3);
            Assert.Greater(checkedMoves, 0);
        }

        static int CheckMoves(Position position, int depth)
        {
            if (depth == 0) return 0;
            int count = 0;
            List<Move> moves = position.GetLegalMoves();
            foreach (var move in moves)
            {
                ushort packed = Bot_v21.PackMove(move);
                Assert.AreNotEqual(0, packed, move.ToString());
                Assert.AreEqual(move, Bot_v21.UnpackMove(packed), move.ToString());
                count++;

                position.MakeMove(move);
                count += CheckMoves(position, depth - 1);
                position.UnmakeMove();
            }
            return count;
        }

        [Test]
        public void SpecialMovesKeepTheirFlags()
        {
            var moves = new[]
            {
                new Move(Square.Parse("e2"), Square.Parse("e4"), MoveFlags.DoublePawnPush),
                new Move(Square.Parse("e1"), Square.Parse("g1"), MoveFlags.CastleKingside),
                new Move(Square.Parse("e8"), Square.Parse("c8"), MoveFlags.CastleQueenside),
                new Move(Square.Parse("e5"), Square.Parse("f6"), MoveFlags.Capture | MoveFlags.EnPassant),
                new Move(Square.Parse("h7"), Square.Parse("h8"), MoveFlags.None, PieceType.Queen),
                new Move(Square.Parse("b2"), Square.Parse("a1"), MoveFlags.Capture, PieceType.Knight),
                new Move(Square.Parse("a8"), Square.Parse("h1"), MoveFlags.Capture),
                new Move(Square.Parse("h8"), Square.Parse("a1")),
            };
            foreach (var move in moves)
            {
                Assert.AreEqual(move, Bot_v21.UnpackMove(Bot_v21.PackMove(move)), move.ToString());
            }
        }

        [Test]
        public void NoMovePacksToZero()
        {
            Assert.AreEqual(0, Bot_v21.PackMove(default));
            Assert.AreEqual(default(Move), Bot_v21.UnpackMove(0));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(250)]
        [TestCase(-3456)]
        [TestCase(29999)]
        [TestCase(-30000)]
        public void NormalScoresAreKept(int score)
        {
            Assert.AreEqual(score, Bot_v21.UnpackScore(Bot_v21.PackScore(score)));
        }

        [TestCase(31000, 30000)]
        [TestCase(-500000, -30000)]
        public void LargeNormalScoresAreClamped(int score, int expected)
        {
            Assert.AreEqual(expected, Bot_v21.UnpackScore(Bot_v21.PackScore(score)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(7)]
        [TestCase(63)]
        [TestCase(126)]
        [TestCase(999)]
        public void MateScoresAreKept(int distance)
        {
            Assert.AreEqual(MateScore - distance, Bot_v21.UnpackScore(Bot_v21.PackScore(MateScore - distance)));
            Assert.AreEqual(-(MateScore - distance), Bot_v21.UnpackScore(Bot_v21.PackScore(-(MateScore - distance))));
        }

        // Window bounds beyond mate scores (+/-Infinity) become +/-MateScore
        [Test]
        public void ScoresBeyondMateAreClampedToMate()
        {
            Assert.AreEqual(MateScore, Bot_v21.UnpackScore(Bot_v21.PackScore(10_000_001)));
            Assert.AreEqual(-MateScore, Bot_v21.UnpackScore(Bot_v21.PackScore(-10_000_000)));
        }

        // A mate found 9 plies from the root at a node at ply 4 (mate in 5 plies from there), stored and then
        // reached again at ply 6: it must be 11 plies from the root there
        [TestCase(1)]
        [TestCase(-1)]
        public void MateDistanceSurvivesTheTable(int sign)
        {
            int score = sign * (MateScore - 9);
            short packed = Bot_v21.PackScore(Bot_v21.ScoreToTable(score, 4));
            Assert.AreEqual(sign * (MateScore - 5), Bot_v21.UnpackScore(packed));
            Assert.AreEqual(sign * (MateScore - 11), Bot_v21.ScoreFromTable(Bot_v21.UnpackScore(packed), 6));
            Assert.AreEqual(score, Bot_v21.ScoreFromTable(Bot_v21.UnpackScore(packed), 4));
        }

        [Test]
        public void PackedScoresAreOrdered()
        {
            int[] scores = { -MateScore, -(MateScore - 50), -30000, -100, 0, 100, 30000, MateScore - 50, MateScore };
            for (int i = 1; i < scores.Length; i++)
            {
                Assert.Less(Bot_v21.PackScore(scores[i - 1]), Bot_v21.PackScore(scores[i]));
            }
        }

        [Test]
        public void HashSizeCanBeChanged()
        {
            var bot = new Bot_v21(3);
            Assert.AreEqual(Bot_v21.DefaultHashMb, bot.HashMb);
            bot.HashMb = 3;
            Assert.AreEqual(3, bot.HashMb);
            bot.HashMb = 0;
            Assert.AreEqual(1, bot.HashMb);
            bot.ClearTable();
            Assert.IsTrue(Position.StartPosition().GetLegalMoves().Contains(bot.ChooseMove(Position.StartPosition())));
        }
    }
}
