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
