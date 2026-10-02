using NUnit.Framework;

namespace ChessEngine.Tests
{
    // Static exchange evaluation on hand-checked positions. Values: P 100, N 300, B 300, R 500, Q 900.
    public class SeeTests
    {
        // move in UCI notation (promotion letter included); the move must be legal in the position
        [TestCase("4k3/8/8/3p4/8/8/8/3QK3 w - - 0 1", "d1d5", 100, TestName = "Undefended pawn")]
        [TestCase("4k3/8/2p5/3p4/8/8/8/3QK3 w - - 0 1", "d1d5", -800, TestName = "QxP defended by pawn")]
        [TestCase("3qk3/8/8/8/3P4/2P5/8/4K3 b - - 0 1", "d8d4", -800, TestName = "QxP defended by pawn, black")]
        [TestCase("3rk3/8/8/3r4/8/8/8/3RK3 w - - 0 1", "d1d5", 0, TestName = "RxR defended")]
        [TestCase("1k1r4/1pp4p/p7/4p3/8/P5P1/1PP4P/2K1R3 w - - 0 1", "e1e5", 100, TestName = "CPW: RxP undefended")]
        [TestCase("1k1r3q/1ppn3p/p4b2/4p3/8/P2N2P1/1PP1R1BP/2K1Q3 w - - 0 1", "d3e5", -200, TestName = "CPW: NxP with x-rays")]
        [TestCase("3rk3/8/8/3p4/8/8/3R4/3RK3 w - - 0 1", "d2d5", 100, TestName = "X-ray: doubled rooks")]
        [TestCase("4k3/8/2p5/3p4/4P3/5B2/8/4K3 w - - 0 1", "e4d5", 100, TestName = "X-ray: bishop behind pawn")]
        [TestCase("3qk3/8/8/3n4/4P3/8/8/3RK3 w - - 0 1", "e4d5", 300, TestName = "Recapture not worth it")]
        [TestCase("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1", "e5d6", 100, TestName = "En passant undefended")]
        [TestCase("4k3/2p5/8/3pP3/8/8/8/4K3 w - d6 0 1", "e5d6", 0, TestName = "En passant defended")]
        [TestCase("4k3/2p5/8/3pP3/8/8/8/3RK3 w - d6 0 1", "e5d6", 100, TestName = "En passant reveals rook behind captured pawn")]
        [TestCase("1r2k3/P7/8/8/8/8/8/4K3 w - - 0 1", "a7b8q", 1300, TestName = "Promotion capture undefended")]
        [TestCase("1r2k3/P2n4/8/8/8/8/8/4K3 w - - 0 1", "a7b8q", 400, TestName = "Promotion capture defended")]
        [TestCase("1r2k3/P7/8/8/8/8/8/4K3 w - - 0 1", "a7a8q", -100, TestName = "Quiet promotion onto defended square")]
        [TestCase("4k3/8/8/3p4/2K5/8/8/8 w - - 0 1", "c4d5", 100, TestName = "King captures undefended pawn")]
        [TestCase("4k3/8/4p3/3p4/2K5/8/8/3R4 w - - 0 1", "d1d5", -300, TestName = "King recaptures last")]
        [TestCase("3rk3/8/4p3/3p4/2K5/8/8/3R4 w - - 0 1", "d1d5", -400, TestName = "King can't recapture into defended square")]
        [TestCase("4k3/8/8/2b5/8/4N3/8/4K3 w - - 0 1", "e3c4", 0, TestName = "Quiet move: SEE of a non-capture")]
        public void Evaluate(string fen, string uci, int expected)
        {
            var position = Position.FromFen(fen);
            Move move = FindMove(position, uci);
            Assert.AreEqual(expected, See.Evaluate(position, move));
        }

        [Test]
        public void KingCaptureIntoDefendedSquareIsLosing()
        {
            // Pseudo-legal only (d5 is defended by e6), as quiescence may see it
            var position = Position.FromFen("4k3/8/4p3/3p4/2K5/8/8/8 w - - 0 1");
            var move = new Move(Square.Parse("c4"), Square.Parse("d5"), MoveFlags.Capture);
            Assert.Less(See.Evaluate(position, move), 0);
        }

        [Test]
        public void QuietMoveToAttackedSquareLosesThePiece()
        {
            // Nd5 is attacked by the c6 pawn
            var position = Position.FromFen("4k3/8/2p5/8/8/4N3/8/4K3 w - - 0 1");
            Assert.AreEqual(-300, See.Evaluate(position, FindMove(position, "e3d5")));
        }

        [Test]
        public void IsAtLeastMatchesEvaluate()
        {
            var position = Position.FromFen("1k1r3q/1ppn3p/p4b2/4p3/8/P2N2P1/1PP1R1BP/2K1Q3 w - - 0 1");
            Move move = FindMove(position, "d3e5");
            Assert.IsTrue(See.IsAtLeast(position, move, -200));
            Assert.IsFalse(See.IsAtLeast(position, move, -199));
        }

        [Test]
        public void DoesNotChangeThePosition()
        {
            var position = Position.FromFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
            string fen = position.ToFen();
            foreach (Move move in position.GetLegalMoves())
            {
                See.Evaluate(position, move);
            }
            Assert.AreEqual(fen, position.ToFen());
        }

        // The bitboard SEE must agree with the first (mailbox) implementation on every move of random games
        // from positions full of exchanges, pins, en passant and promotions
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1")]
        [TestCase("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8")]
        [TestCase("1k1r3q/1ppn3p/p4b2/4p3/8/P2N2P1/1PP1R1BP/2K1Q3 w - - 0 1")]
        [TestCase(Position.StartFen)]
        public void MatchesReferenceImplementation(string fen)
        {
            var random = new System.Random(fen.GetHashCode());
            for (int game = 0; game < 20; game++)
            {
                var position = Position.FromFen(fen);
                for (int ply = 0; ply < 80; ply++)
                {
                    var moves = position.GetLegalMoves();
                    if (moves.Count == 0) break;
                    foreach (Move move in moves)
                    {
                        Assert.AreEqual(SeeReference.Evaluate(position, move), See.Evaluate(position, move),
                                        $"move {move} in {position.ToFen()}");
                    }
                    position.MakeMove(moves[random.Next(moves.Count)]);
                }
            }
        }

        private static Move FindMove(Position position, string uci)
        {
            foreach (Move move in position.GetLegalMoves())
            {
                if (move.ToString() == uci) return move;
            }
            Assert.Fail($"Move {uci} not legal in {position.ToFen()}");
            return default;
        }
    }
}
