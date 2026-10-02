using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessEngine;

namespace Chess2D.Tune
{
    // Texel tuning of TunableEvaluation.
    //
    //   Chess2D.Tune extract --out positions.txt [--per-game 8] [--skip-plies 16] [--max 2000000] games1.pgn games2.pgn ...
    //       Quiet positions from finished games, one per line: "FEN;result" (1 = White won, 0.5 = draw, 0 = Black won).
    //
    //   Chess2D.Tune label --data positions.txt --out labelled.txt --stockfish stockfish.exe [--threads 4] [--nodes 5000]
    //       Adds a Stockfish evaluation to every position: "FEN;result;score" (see Label.cs).
    //
    //   Chess2D.Tune tune --data positions.txt [--epochs 500] [--rate 1] [--max 2000000] [--lambda 0.5] [--out TunedWeights.cs]
    //       Finds the scaling constant K, then the weights that best predict the target
    //       (mean squared error of sigmoid(eval) against it), and writes them as a C# file.
    //       Target = lambda * game result + (1 - lambda) * Stockfish's expected score, when the data has
    //       Stockfish scores; just the game result otherwise.
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: Chess2D.Tune extract|tune ... (see the comment at the top of Program.cs)");
                return 1;
            }
            var options = ParseOptions(args.Skip(1).ToArray(), out List<string> files);
            switch (args[0])
            {
                case "extract": return Extract(options, files);
                case "label": return Label.Run(options);
                case "tune": return Tune(options);
                default:
                    Console.WriteLine($"Unknown command '{args[0]}'");
                    return 1;
            }
        }

        static Dictionary<string, string> ParseOptions(string[] args, out List<string> files)
        {
            var options = new Dictionary<string, string>();
            files = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--") && i + 1 < args.Length) options[args[i].Substring(2)] = args[++i];
                else files.Add(args[i]);
            }
            return options;
        }

        static int Int(Dictionary<string, string> o, string name, int fallback) =>
            o.TryGetValue(name, out string v) ? int.Parse(v, CultureInfo.InvariantCulture) : fallback;

        static double Double(Dictionary<string, string> o, string name, double fallback) =>
            o.TryGetValue(name, out string v) ? double.Parse(v, CultureInfo.InvariantCulture) : fallback;

        // ===== extract =====

        static int Extract(Dictionary<string, string> options, List<string> pgnFiles)
        {
            string output = options.TryGetValue("out", out string o) ? o : "positions.txt";
            int perGame = Int(options, "per-game", 8);
            int skipPlies = Int(options, "skip-plies", 16);
            int max = Int(options, "max", 2_000_000);
            var random = new Random(12345);
            var trace = new TunableEvaluation.Trace();

            int games = 0, skipped = 0, written = 0;
            var watch = Stopwatch.StartNew();
            using var writer = new StreamWriter(output, false, new UTF8Encoding(false));
            foreach (string file in pgnFiles)
            {
                foreach (PgnGame game in PgnGame.ReadAll(file))
                {
                    if (written >= max) break;
                    string label = game.Result switch { "1-0" => "1", "0-1" => "0", "1/2-1/2" => "0.5", _ => null };
                    if (label == null) { skipped++; continue; }

                    Position position;
                    try { position = Position.FromFen(game.StartFen); }
                    catch { skipped++; continue; }

                    var quiet = new List<string>();
                    int ply = 0;
                    bool broken = false;
                    foreach (string san in game.Moves)
                    {
                        if (!San.TryParse(position, san, out Move move)) { broken = true; break; }
                        position.MakeMove(move);
                        ply++;
                        if (ply >= skipPlies && IsQuiet(position, trace)) quiet.Add(position.ToFen());
                    }
                    if (broken) { skipped++; continue; }
                    games++;

                    // A few random quiet positions per game: positions of one game are strongly correlated
                    for (int i = 0; i < perGame && quiet.Count > 0 && written < max; i++)
                    {
                        int pick = random.Next(quiet.Count);
                        writer.Write(quiet[pick]);
                        writer.Write(';');
                        writer.WriteLine(label);
                        quiet.RemoveAt(pick);
                        written++;
                    }

                    if (games % 20000 == 0) Console.WriteLine($"  {games} games, {written} positions ({watch.Elapsed:mm\\:ss})");
                }
            }
            Console.WriteLine($"Games used: {games}, skipped: {skipped}, positions written: {written} -> {output}");
            return 0;
        }

        // Quiet = the static evaluation can be trusted: not in check, no capture that wins material (SEE > 0),
        // and not a lone-king / mop-up position (those terms aren't tuned)
        static bool IsQuiet(Position position, TunableEvaluation.Trace trace)
        {
            if (position.InCheck) return false;
            var captures = new List<Move>();
            position.GeneratePseudoLegalCaptures(captures);
            foreach (Move move in captures)
            {
                if (See.Evaluate(position, move) > 0) return false;
            }
            TunableEvaluation.Collect(position, trace);
            return !trace.LoneKing && trace.MopUp == 0;
        }

        // ===== tune =====

        // All positions in compact form: for position i, its terms are Terms[Start[i] .. Start[i + 1])
        sealed class Dataset
        {
            public int Count;
            public int[] Start;
            public ushort[] Terms;
            public short[] Coefficients;
            public byte[] Phase;
            public float[] Fixed;     // the untuned king attack, already blended (White's view)
            public float[] Result;
        }

        static int Tune(Dictionary<string, string> options)
        {
            string data = options.TryGetValue("data", out string d) ? d : "positions.txt";
            string output = options.TryGetValue("out", out string o) ? o : "TunedWeights.cs";
            int epochs = Int(options, "epochs", 500);
            double rate = Double(options, "rate", 1.0);
            int max = Int(options, "max", 2_000_000);
            double lambda = Double(options, "lambda", 0.5);

            var watch = Stopwatch.StartNew();
            Dataset set = Load(data, max, lambda);
            Console.WriteLine($"Loaded {set.Count} positions, {set.Terms.Length} coefficients ({watch.Elapsed:mm\\:ss})");

            int n = 2 * TunableEvaluation.TermCount;
            var weights = TunableEvaluation.Default.Select(x => (double)x).ToArray();

            double k = FitK(set, weights);
            double startError = Error(set, weights, k);
            Console.WriteLine($"K = {k:F4}, error with the current weights: {startError:F6}");

            // Adam: per-weight step sizes from running averages of the gradient and its square
            var m = new double[n];
            var v = new double[n];
            const double beta1 = 0.9, beta2 = 0.999, epsilon = 1e-8;
            for (int epoch = 1; epoch <= epochs; epoch++)
            {
                double[] gradient = Gradient(set, weights, k);
                for (int i = 0; i < n; i++)
                {
                    m[i] = beta1 * m[i] + (1 - beta1) * gradient[i];
                    v[i] = beta2 * v[i] + (1 - beta2) * gradient[i] * gradient[i];
                    double mHat = m[i] / (1 - Math.Pow(beta1, epoch));
                    double vHat = v[i] / (1 - Math.Pow(beta2, epoch));
                    weights[i] -= rate * mHat / (Math.Sqrt(vHat) + epsilon);
                }
                if (epoch % 50 == 0 || epoch == epochs)
                    Console.WriteLine($"  epoch {epoch,5}: error {Error(set, weights, k):F6} ({watch.Elapsed:mm\\:ss})");
            }

            int[] tuned = weights.Select(x => (int)Math.Round(x)).ToArray();
            double endError = Error(set, tuned.Select(x => (double)x).ToArray(), k);
            Console.WriteLine($"Error: {startError:F6} -> {endError:F6} (rounded weights)");
            WriteWeights(output, tuned, k, set.Count, startError, endError);
            PrintChanges(tuned);
            Console.WriteLine($"Weights written to {output}");
            return 0;
        }

        // Stockfish's centipawns -> expected score, on the usual Elo-like scale
        static double ExpectedScore(double centipawns) => 1.0 / (1.0 + Math.Pow(10, -centipawns / 400));

        static Dataset Load(string path, int max, double lambda)
        {
            var start = new List<int> { 0 };
            var terms = new List<ushort>();
            var coefficients = new List<short>();
            var phase = new List<byte>();
            var fixedScores = new List<float>();
            var results = new List<float>();
            var trace = new TunableEvaluation.Trace();

            foreach (string line in File.ReadLines(path))
            {
                if (results.Count >= max) break;
                // "FEN;result" or "FEN;result;stockfish centipawns"
                string[] fields = line.Split(';');
                if (fields.Length < 2) continue;
                var position = Position.FromFen(fields[0]);
                double result = double.Parse(fields[1], CultureInfo.InvariantCulture);
                if (fields.Length >= 3)
                {
                    double centipawns = double.Parse(fields[2], CultureInfo.InvariantCulture);
                    result = lambda * result + (1 - lambda) * ExpectedScore(centipawns);
                }

                TunableEvaluation.Collect(position, trace);
                for (int i = 0; i < trace.Terms.Count; i++)
                {
                    terms.Add((ushort)trace.Terms[i]);
                    coefficients.Add((short)trace.Coefficients[i]);
                }
                start.Add(terms.Count);
                phase.Add((byte)trace.Phase);
                fixedScores.Add(trace.KingAttack * trace.Phase / (float)TunableEvaluation.MaxPhase + trace.MopUp);
                results.Add((float)result);
            }

            return new Dataset
            {
                Count = results.Count,
                Start = start.ToArray(),
                Terms = terms.ToArray(),
                Coefficients = coefficients.ToArray(),
                Phase = phase.ToArray(),
                Fixed = fixedScores.ToArray(),
                Result = results.ToArray(),
            };
        }

        // Evaluation (White's view) of position i with real-valued weights
        static double Eval(Dataset set, int i, double[] w)
        {
            double mg = 0, eg = 0;
            for (int j = set.Start[i]; j < set.Start[i + 1]; j++)
            {
                int term = set.Terms[j];
                mg += set.Coefficients[j] * w[2 * term];
                eg += set.Coefficients[j] * w[2 * term + 1];
            }
            int phase = set.Phase[i];
            return (mg * phase + eg * (TunableEvaluation.MaxPhase - phase)) / TunableEvaluation.MaxPhase + set.Fixed[i];
        }

        // Expected score of White for an evaluation: 1 / (1 + 10^(-K * eval / 400))
        static double Sigmoid(double eval, double k) => 1.0 / (1.0 + Math.Pow(10, -k * eval / 400));

        static double Error(Dataset set, double[] w, double k)
        {
            double total = 0;
            object gate = new object();
            Parallel.For(0, Environment.ProcessorCount, () => 0.0, (part, _, sum) =>
            {
                for (int i = part; i < set.Count; i += Environment.ProcessorCount)
                {
                    double diff = set.Result[i] - Sigmoid(Eval(set, i, w), k);
                    sum += diff * diff;
                }
                return sum;
            }, sum => { lock (gate) total += sum; });
            return total / set.Count;
        }

        // Gradient of the error with respect to every weight
        static double[] Gradient(Dataset set, double[] w, double k)
        {
            int n = w.Length;
            var total = new double[n];
            object gate = new object();
            double scale = Math.Log(10) * k / 400;
            Parallel.For(0, Environment.ProcessorCount, () => new double[n], (part, _, g) =>
            {
                for (int i = part; i < set.Count; i += Environment.ProcessorCount)
                {
                    double s = Sigmoid(Eval(set, i, w), k);
                    // d(error)/d(eval) for this position
                    double dEval = -2 * (set.Result[i] - s) * s * (1 - s) * scale;
                    double mgShare = set.Phase[i] / (double)TunableEvaluation.MaxPhase;
                    for (int j = set.Start[i]; j < set.Start[i + 1]; j++)
                    {
                        int term = set.Terms[j];
                        double c = dEval * set.Coefficients[j];
                        g[2 * term] += c * mgShare;
                        g[2 * term + 1] += c * (1 - mgShare);
                    }
                }
                return g;
            }, g => { lock (gate) for (int i = 0; i < n; i++) total[i] += g[i]; });
            for (int i = 0; i < n; i++) total[i] /= set.Count;
            return total;
        }

        // K that makes the current evaluation predict the results best (golden-section search)
        static double FitK(Dataset set, double[] w)
        {
            double a = 0.1, b = 3.0;
            double golden = (Math.Sqrt(5) - 1) / 2;
            double c = b - golden * (b - a), d = a + golden * (b - a);
            double fc = Error(set, w, c), fd = Error(set, w, d);
            for (int i = 0; i < 40; i++)
            {
                if (fc < fd) { b = d; d = c; fd = fc; c = b - golden * (b - a); fc = Error(set, w, c); }
                else { a = c; c = d; fc = fd; d = a + golden * (b - a); fd = Error(set, w, d); }
            }
            return (a + b) / 2;
        }

        static void WriteWeights(string path, int[] w, double k, int positions, double startError, double endError)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace ChessEngine");
            sb.AppendLine("{");
            sb.AppendLine("    // Evaluation weights found by Texel tuning (Tools/Chess2D.Tune), in the layout of TunableEvaluation:");
            sb.AppendLine("    // Weights[2 * term] for the middlegame, Weights[2 * term + 1] for the endgame.");
            sb.AppendLine(FormattableString.Invariant($"    // {positions} positions, K = {k:F4}, error {startError:F6} -> {endError:F6}"));
            sb.AppendLine("    public static class TunedWeights");
            sb.AppendLine("    {");
            sb.AppendLine("        public static readonly int[] Weights =");
            sb.AppendLine("        {");
            for (int term = 0; term < TunableEvaluation.TermCount; term++)
            {
                sb.AppendLine($"            {w[2 * term],5}, {w[2 * term + 1],5},   // {TunableEvaluation.TermName(term)}");
            }
            sb.AppendLine("        };");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            File.WriteAllText(path, sb.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
        }

        // The terms outside the piece-square tables, old -> new
        static void PrintChanges(int[] tuned)
        {
            Console.WriteLine();
            Console.WriteLine($"{"term",-22} {"middlegame",18} {"endgame",18}");
            for (int term = 0; term < TunableEvaluation.TermCount; term++)
            {
                if (term >= TunableEvaluation.PieceSquare && term < TunableEvaluation.Passed) continue;
                int[] old = TunableEvaluation.Default;
                Console.WriteLine($"{TunableEvaluation.TermName(term),-22} {old[2 * term],7} -> {tuned[2 * term],-7} {old[2 * term + 1],7} -> {tuned[2 * term + 1],-7}");
            }
        }
    }
}
