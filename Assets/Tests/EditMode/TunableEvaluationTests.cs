using NUnit.Framework;

namespace ChessEngine.Tests
{
    public class TunableEvaluationTests
    {
        // With the default weights the tunable evaluation must be exactly the v22 evaluation,
        // on every position of random games (all phases, lone kings and mop-up included)
        [TestCase(Position.StartFen)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("r1bq1rk1/pp2bppp/2n1pn2/3p4/2PP4/2N1PN2/PP2BPPP/R2QKB1R w KQ - 0 8")]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1")]
        [TestCase("4k3/8/8/8/8/8/8/R3K2Q w - - 0 1")]
        public void DefaultWeightsGiveTheSameScoreAsEvaluation(string fen)
        {
            var random = new System.Random(fen.GetHashCode());
            for (int game = 0; game < 20; game++)
            {
                var position = Position.FromFen(fen);
                for (int ply = 0; ply < 150; ply++)
                {
                    Assert.AreEqual(Evaluation.EvaluateWithActivity(position),
                                    TunableEvaluation.Evaluate(position, TunableEvaluation.Default),
                                    position.ToFen());
                    var moves = position.GetLegalMoves();
                    if (moves.Count == 0) break;
                    position.MakeMove(moves[random.Next(moves.Count)]);
                }
            }
        }

        // Extended set with its starting weights: the same score as the basic set, except in endgames it scales down
        // (the mobility and king danger tables reproduce the old formulas, all new terms start at 0).
        // And the trace gives the same score as the direct evaluation, for random weights too.
        [TestCase(Position.StartFen)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("r1bq1rk1/pp2bppp/2n1pn2/3p4/2PP4/2N1PN2/PP2BPPP/R2QKB1R w KQ - 0 8")]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1")]
        [TestCase("2kr3r/ppp2ppp/2n1b3/2b1p3/4P3/2NPB3/PPP2PPP/2KR1B1R b - - 0 1")]
        public void ExtendedSetIsConsistent(string fen)
        {
            var random = new System.Random(fen.GetHashCode());
            var randomWeights = new int[2 * TunableEvaluation.TermCount];
            for (int i = 0; i < randomWeights.Length; i++) randomWeights[i] = random.Next(-50, 51);
            var trace = new TunableEvaluation.Trace();

            for (int game = 0; game < 15; game++)
            {
                var position = Position.FromFen(fen);
                for (int ply = 0; ply < 150; ply++)
                {
                    TunableEvaluation.Collect(position, trace, true);
                    int stm = position.SideToMove == Side.White ? 1 : -1;
                    if (trace.EgScale == TunableEvaluation.FullScale)
                    {
                        Assert.AreEqual(TunableEvaluation.Evaluate(position, TunableEvaluation.Default),
                                        TunableEvaluation.Evaluate(position, TunableEvaluation.DefaultExtended, true), position.ToFen());
                    }
                    Assert.AreEqual(stm * trace.Evaluate(randomWeights),
                                    TunableEvaluation.Evaluate(position, randomWeights, true), position.ToFen());

                    var moves = position.GetLegalMoves();
                    if (moves.Count == 0) break;
                    position.MakeMove(moves[random.Next(moves.Count)]);
                }
            }
        }

        // The same position with colours swapped must get the same score for the side to move (all features)
        [TestCase("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3")]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("2kr3r/ppp2ppp/2n1b3/2b1p3/4P3/2NPB3/PPP2PPP/2KR1B1R b - - 0 1")]
        [TestCase("8/5pk1/6p1/3P4/2K5/8/5PPP/8 w - - 0 1")]
        [TestCase("4k3/8/8/2b5/8/8/3B4/4K3 w - - 0 1")]
        public void ExtendedSetIsSymmetricForBothColours(string fen)
        {
            var random = new System.Random(7);
            var weights = new int[2 * TunableEvaluation.TermCount];
            for (int i = 0; i < weights.Length; i++) weights[i] = random.Next(-50, 51);
            Assert.AreEqual(TunableEvaluation.Evaluate(Position.FromFen(fen), weights, true),
                            TunableEvaluation.Evaluate(Position.FromFen(EvaluationTests.MirrorFen(fen)), weights, true));
        }

        // Full set: with its starting weights the same score as the extended set (the king danger inputs reproduce
        // the old table) wherever the endgame scale is the same; the trace (with the non-linear king danger) gives
        // the same score as the direct evaluation for random weights; colours mirrored give the same score
        [TestCase(Position.StartFen)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("r1bq1rk1/pp2bppp/2n1pn2/3p4/2PP4/2N1PN2/PP2BPPP/R2QKB1R w KQ - 0 8")]
        [TestCase("2kr3r/ppp2ppp/2n1b3/2b1p3/4P3/2NPB3/PPP2PPP/2KR1B1R b - - 0 1")]
        [TestCase("r4rk1/1pp2ppp/p1nq1n2/3p4/3P1B2/2NQ1N2/PPP2PPP/R4RK1 w - - 0 1")]
        public void FullSetIsConsistent(string fen)
        {
            var random = new System.Random(fen.GetHashCode());
            var randomWeights = new int[2 * TunableEvaluation.TermCount];
            for (int i = 0; i < randomWeights.Length; i++) randomWeights[i] = random.Next(-50, 51);
            var full = new TunableEvaluation.Trace();
            var extended = new TunableEvaluation.Trace();

            for (int game = 0; game < 15; game++)
            {
                var position = Position.FromFen(fen);
                for (int ply = 0; ply < 150; ply++)
                {
                    TunableEvaluation.Collect(position, full, FeatureSet.Full);
                    TunableEvaluation.Collect(position, extended, FeatureSet.Extended);
                    int stm = position.SideToMove == Side.White ? 1 : -1;
                    if (full.EgScale == extended.EgScale)
                    {
                        Assert.AreEqual(TunableEvaluation.Evaluate(position, TunableEvaluation.DefaultExtended, FeatureSet.Extended),
                                        TunableEvaluation.Evaluate(position, TunableEvaluation.DefaultFull, FeatureSet.Full), position.ToFen());
                    }
                    Assert.AreEqual(stm * full.Evaluate(randomWeights),
                                    TunableEvaluation.Evaluate(position, randomWeights, FeatureSet.Full), position.ToFen());
                    Assert.AreEqual(TunableEvaluation.Evaluate(position, randomWeights, FeatureSet.Full),
                                    TunableEvaluation.Evaluate(Position.FromFen(EvaluationTests.MirrorFen(position.ToFen())), randomWeights, FeatureSet.Full),
                                    "mirrored " + position.ToFen());

                    var moves = position.GetLegalMoves();
                    if (moves.Count == 0) break;
                    position.MakeMove(moves[random.Next(moves.Count)]);
                }
            }
        }

        [Test]
        public void WrongBishopWithRookPawnIsScaledDown()
        {
            var trace = new TunableEvaluation.Trace();
            // White: dark-squared bishop, a-pawns, promotion square a8 is light; the black king sits on b8
            TunableEvaluation.Collect(Position.FromFen("1k6/8/P7/8/P7/8/3B4/4K3 w - - 0 1"), trace, FeatureSet.Full);
            Assert.Less(trace.EgScale, 16);
            // The same with a light-squared bishop: it controls a8, no scaling
            TunableEvaluation.Collect(Position.FromFen("1k6/8/P7/8/P7/8/4B3/4K3 w - - 0 1"), trace, FeatureSet.Full);
            Assert.AreEqual(TunableEvaluation.FullScale, trace.EgScale);
        }

        [Test]
        public void OppositeBishopsScaleTheEndgameDown()
        {
            var trace = new TunableEvaluation.Trace();
            TunableEvaluation.Collect(Position.FromFen("4k3/5p2/8/2b5/8/8/4BP2/4K3 w - - 0 1"), trace, true);    // c5 dark, e2 light
            Assert.Less(trace.EgScale, TunableEvaluation.FullScale);
            TunableEvaluation.Collect(Position.FromFen("4k3/5p2/8/2b5/8/8/3B1P2/4K3 w - - 0 1"), trace, true);    // d2 dark too: same colour
            Assert.AreEqual(TunableEvaluation.FullScale, trace.EgScale);
        }

        [Test]
        public void EveryTermHasAName()
        {
            for (int term = 0; term < TunableEvaluation.TermCount; term++)
            {
                StringAssert.DoesNotStartWith("Term ", TunableEvaluation.TermName(term));
            }
        }

        // The trace lists each term once, and only terms with a non-zero coefficient
        [Test]
        public void TraceHasNoDuplicateOrZeroTerms()
        {
            var trace = new TunableEvaluation.Trace();
            TunableEvaluation.Collect(Position.FromFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1"), trace);
            CollectionAssert.AllItemsAreUnique(trace.Terms);
            CollectionAssert.DoesNotContain(trace.Coefficients, 0);
        }
    }
}
