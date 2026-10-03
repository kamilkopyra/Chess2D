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
    //   Chess2D.Tune extract --out positions.txt [--per-game 8] [--skip-plies 16] [--max 2000000]
    //                        [--min-elo 1800] [--low-elo-keep 0.25] [--min-seconds 180] [--dedup-bits 32] games1.pgn ...
    //       Quiet positions from finished games, one per line: "FEN;result" (1 = White won, 0.5 = draw, 0 = Black won).
    //       A file name "-" reads the games from standard input (zstd -dc lichess.pgn.zst | ... -).
    //       --min-elo: games where both players have at least this rating are used; of the other games only the
    //       fraction --low-elo-keep. --min-seconds: games whose estimated duration (base + 40 * increment, the way
    //       Lichess counts it) is shorter are skipped (180 = no bullet). --dedup-bits: positions already written
    //       (by Zobrist hash, in a table of 2^bits bits) are skipped; when the table fills up, a few new positions
    //       are skipped too (about positions / 2^bits of them).
    //
    //   Chess2D.Tune label --data positions.txt --out labelled.txt --stockfish stockfish.exe [--threads 4] [--nodes 5000]
    //                      [--clear-hash]
    //       Adds a Stockfish evaluation to every position: "FEN;result;score" (see Label.cs).
    //
    //   Chess2D.Tune tune --data positions.txt [--epochs 500] [--rate 1] [--max 2000000] [--lambda 0.5] [--features full]
    //                     [--out TunedWeights.cs] [--name TunedWeights] [--threads N]
    //       Finds the scaling constant K, then the weights that best predict the target
    //       (mean squared error of sigmoid(eval) against it), and writes them as a C# file.
    //       Target = lambda * game result + (1 - lambda) * Stockfish's expected score, when the data has
    //       Stockfish scores; just the game result otherwise.
    //       --features basic|extended|full picks the feature set of TunableEvaluation (default basic; --extended is
    //       short for --features extended); tuning starts from Default / DefaultExtended / DefaultFull.
    //       --name is the class name in the written file.
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
                if (args[i].StartsWith("--"))
                {
                    // "--name value", or a flag without a value ("--extended")
                    bool hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--");
                    options[args[i].Substring(2)] = hasValue ? args[++i] : "true";
                }
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
            int minElo = Int(options, "min-elo", 0);
            double lowEloKeep = Double(options, "low-elo-keep", 1.0);
            int minSeconds = Int(options, "min-seconds", 0);
            int dedupBits = Int(options, "dedup-bits", 0);
            var random = new Random(12345);
            var trace = new TunableEvaluation.Trace();
            ulong[] seen = dedupBits > 0 ? new ulong[(1L << dedupBits) / 64] : null;
            ulong seenMask = dedupBits > 0 ? (1UL << dedupBits) - 1 : 0;

            long read = 0;
            int games = 0, skipped = 0, written = 0, duplicates = 0;
            var watch = Stopwatch.StartNew();

            // Decided from the headers alone, before the moves are parsed
            bool Keep(PgnGame game)
            {
                read++;
                if (read % 1_000_000 == 0)
                    Console.WriteLine($"  {read} games read, {games} used, {written} positions, {duplicates} duplicates ({watch.Elapsed:hh\\:mm\\:ss})");
                if (written >= max) return false;
                if (minSeconds > 0 && EstimatedSeconds(game) < minSeconds) return false;
                if (minElo > 0 && (Elo(game, "WhiteElo") < minElo || Elo(game, "BlackElo") < minElo)
                    && random.NextDouble() >= lowEloKeep) return false;
                return true;
            }

            using var writer = new StreamWriter(output, false, new UTF8Encoding(false), 1 << 20);
            foreach (string file in pgnFiles)
            {
                foreach (PgnGame game in PgnGame.ReadAll(file, Keep))
                {
                    if (written >= max) break;
                    string label = game.Result switch { "1-0" => "1", "0-1" => "0", "1/2-1/2" => "0.5", _ => null };
                    if (label == null) { skipped++; continue; }

                    Position position;
                    try { position = Position.FromFen(game.StartFen); }
                    catch { skipped++; continue; }

                    var quiet = new List<string>();
                    var quietHashes = new List<ulong>();
                    int ply = 0;
                    bool broken = false;
                    foreach (string san in game.Moves)
                    {
                        if (!San.TryParse(position, san, out Move move)) { broken = true; break; }
                        position.MakeMove(move);
                        ply++;
                        if (ply >= skipPlies && IsQuiet(position, trace))
                        {
                            quiet.Add(position.ToFen());
                            quietHashes.Add(position.Hash);
                        }
                    }
                    if (broken) { skipped++; continue; }
                    games++;

                    // A few random quiet positions per game: positions of one game are strongly correlated
                    // A duplicate doesn't use up the game's quota: another position of the game is tried instead
                    int taken = 0;
                    while (taken < perGame && quiet.Count > 0 && written < max)
                    {
                        int pick = random.Next(quiet.Count);
                        string fen = quiet[pick];
                        ulong hash = quietHashes[pick];
                        quiet.RemoveAt(pick);
                        quietHashes.RemoveAt(pick);
                        if (seen != null)
                        {
                            ulong bit = hash & seenMask;
                            ulong flag = 1UL << (int)(bit & 63);
                            if ((seen[bit >> 6] & flag) != 0) { duplicates++; continue; }
                            seen[bit >> 6] |= flag;
                        }
                        writer.Write(fen);
                        writer.Write(';');
                        writer.WriteLine(label);
                        written++;
                        taken++;
                    }

                    if (games % 20000 == 0 && read < 1_000_000)
                        Console.WriteLine($"  {games} games, {written} positions ({watch.Elapsed:mm\\:ss})");
                }
            }
            Console.WriteLine($"Games read: {read}, used: {games}, skipped: {skipped}, duplicates: {duplicates}, " +
                              $"positions written: {written} -> {output} ({watch.Elapsed:hh\\:mm\\:ss})");
            return 0;
        }

        // Rating from a header ("?" or missing counts as 0)
        static int Elo(PgnGame game, string header) =>
            game.Headers.TryGetValue(header, out string v) && int.TryParse(v, out int elo) ? elo : 0;

        // Estimated duration of a game in seconds from the TimeControl header ("180+2" -> 180 + 40 * 2),
        // the way Lichess sorts games into bullet / blitz / rapid; correspondence ("-") or missing counts as long
        static int EstimatedSeconds(PgnGame game)
        {
            if (!game.Headers.TryGetValue("TimeControl", out string tc)) return int.MaxValue;
            int plus = tc.IndexOf('+');
            if (plus < 0 || !int.TryParse(tc.Substring(0, plus), out int baseSeconds)
                || !int.TryParse(tc.Substring(plus + 1), out int increment)) return int.MaxValue;
            return baseSeconds + 40 * increment;
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
            public byte[] EgScale;    // the endgame part is multiplied by EgScale / 64
            // King danger inputs (full set): for position i, entries DangerStart[i] .. DangerStart[i + 1];
            // DangerSide[j] = 0 when White attacks (adds the penalty), 1 when Black attacks (subtracts it)
            public int[] DangerStart;
            public ushort[] DangerTerm;
            public short[] DangerCoefficient;
            public byte[] DangerSide;
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
            FeatureSet features = FeatureSet.Basic;
            if (options.ContainsKey("extended")) features = FeatureSet.Extended;
            if (options.TryGetValue("features", out string f)) features = (FeatureSet)Enum.Parse(typeof(FeatureSet), f, true);
            string className = options.TryGetValue("name", out string name) ? name : "TunedWeights";
            Workers = Int(options, "threads", Environment.ProcessorCount);

            var watch = Stopwatch.StartNew();
            Dataset set = Load(data, max, lambda, features);
            Console.WriteLine($"Feature set: {features}");
            Console.WriteLine($"Loaded {set.Count} positions, {set.Terms.Length} coefficients ({watch.Elapsed:mm\\:ss})");

            int n = 2 * TunableEvaluation.TermCount;
            int[] startWeights = features == FeatureSet.Full ? TunableEvaluation.DefaultFull
                               : features == FeatureSet.Extended ? TunableEvaluation.DefaultExtended
                               : TunableEvaluation.Default;
            var weights = startWeights.Select(x => (double)x).ToArray();

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
            WriteWeights(output, className, features, tuned, k, set.Count, startError, endError);
            PrintChanges(startWeights, tuned, features);
            Console.WriteLine($"Weights written to {output}");
            return 0;
        }

        // Stockfish's centipawns -> expected score, on the usual Elo-like scale
        static double ExpectedScore(double centipawns) => 1.0 / (1.0 + Math.Pow(10, -centipawns / 400));

        // Threads used for the error and the gradient (--threads; all cores by default)
        static int Workers = Environment.ProcessorCount;

        static Dataset Load(string path, int max, double lambda, FeatureSet features)
        {
            var start = new List<int> { 0 };
            var terms = new List<ushort>();
            var coefficients = new List<short>();
            var phase = new List<byte>();
            var fixedScores = new List<float>();
            var egScales = new List<byte>();
            var dangerStart = new List<int> { 0 };
            var dangerTerm = new List<ushort>();
            var dangerCoefficient = new List<short>();
            var dangerSide = new List<byte>();
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

                TunableEvaluation.Collect(position, trace, features);
                for (int i = 0; i < trace.Terms.Count; i++)
                {
                    terms.Add((ushort)trace.Terms[i]);
                    coefficients.Add((short)trace.Coefficients[i]);
                }
                start.Add(terms.Count);
                phase.Add((byte)trace.Phase);
                egScales.Add((byte)trace.EgScale);
                for (int side = 0; side < 2; side++)
                {
                    for (int i = 0; i < trace.DangerTerms[side].Count; i++)
                    {
                        dangerTerm.Add((ushort)trace.DangerTerms[side][i]);
                        dangerCoefficient.Add((short)trace.DangerCoefficients[side][i]);
                        dangerSide.Add((byte)side);
                    }
                }
                dangerStart.Add(dangerTerm.Count);
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
                EgScale = egScales.ToArray(),
                DangerStart = dangerStart.ToArray(),
                DangerTerm = dangerTerm.ToArray(),
                DangerCoefficient = dangerCoefficient.ToArray(),
                DangerSide = dangerSide.ToArray(),
                Result = results.ToArray(),
            };
        }

        // King danger of both attacking sides for position i (real-valued weights)
        static void Dangers(Dataset set, int i, double[] w, out double white, out double black)
        {
            white = black = 0;
            for (int j = set.DangerStart[i]; j < set.DangerStart[i + 1]; j++)
            {
                double value = set.DangerCoefficient[j] * w[2 * set.DangerTerm[j]];
                if (set.DangerSide[j] == 0) white += value; else black += value;
            }
        }

        // The king danger penalty (as TunableEvaluation.DangerPenalty, but real-valued) and its derivative
        static double Penalty(double danger) => danger <= 0 ? 0 : Math.Min(TunableEvaluation.DangerCap, danger * danger / 1024);
        static double PenaltySlope(double danger) =>
            danger <= 0 || danger * danger / 1024 >= TunableEvaluation.DangerCap ? 0 : 2 * danger / 1024;

        // Evaluation (White's view) of position i with real-valued weights
        static double Eval(Dataset set, int i, double[] w)
        {
            double mg = 0, eg = 0;
            if (set.DangerStart[i + 1] > set.DangerStart[i])
            {
                Dangers(set, i, w, out double white, out double black);
                mg += Penalty(white) - Penalty(black);
            }
            for (int j = set.Start[i]; j < set.Start[i + 1]; j++)
            {
                int term = set.Terms[j];
                mg += set.Coefficients[j] * w[2 * term];
                eg += set.Coefficients[j] * w[2 * term + 1];
            }
            int phase = set.Phase[i];
            eg *= set.EgScale[i] / (double)TunableEvaluation.FullScale;
            return (mg * phase + eg * (TunableEvaluation.MaxPhase - phase)) / TunableEvaluation.MaxPhase + set.Fixed[i];
        }

        // Expected score of White for an evaluation: 1 / (1 + 10^(-K * eval / 400))
        static double Sigmoid(double eval, double k) => 1.0 / (1.0 + Math.Pow(10, -k * eval / 400));

        static double Error(Dataset set, double[] w, double k)
        {
            double total = 0;
            object gate = new object();
            Parallel.For(0, Workers, () => 0.0, (part, _, sum) =>
            {
                for (int i = part; i < set.Count; i += Workers)
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
            Parallel.For(0, Workers, () => new double[n], (part, _, g) =>
            {
                for (int i = part; i < set.Count; i += Workers)
                {
                    double s = Sigmoid(Eval(set, i, w), k);
                    // d(error)/d(eval) for this position
                    double dEval = -2 * (set.Result[i] - s) * s * (1 - s) * scale;
                    double mgShare = set.Phase[i] / (double)TunableEvaluation.MaxPhase;
                    double egShare = (1 - mgShare) * set.EgScale[i] / TunableEvaluation.FullScale;
                    for (int j = set.Start[i]; j < set.Start[i + 1]; j++)
                    {
                        int term = set.Terms[j];
                        double c = dEval * set.Coefficients[j];
                        g[2 * term] += c * mgShare;
                        g[2 * term + 1] += c * egShare;
                    }

                    // King danger: d(penalty)/d(weight) = penalty'(danger) * input, in the middlegame part
                    if (set.DangerStart[i + 1] > set.DangerStart[i])
                    {
                        Dangers(set, i, w, out double white, out double black);
                        double slopeWhite = PenaltySlope(white), slopeBlack = PenaltySlope(black);
                        for (int j = set.DangerStart[i]; j < set.DangerStart[i + 1]; j++)
                        {
                            double slope = set.DangerSide[j] == 0 ? slopeWhite : -slopeBlack;
                            g[2 * set.DangerTerm[j]] += dEval * mgShare * slope * set.DangerCoefficient[j];
                        }
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

        static void WriteWeights(string path, string className, FeatureSet features, int[] w, double k, int positions,
                                 double startError, double endError)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace ChessEngine");
            sb.AppendLine("{");
            sb.AppendLine("    // Evaluation weights found by Texel tuning (Tools/Chess2D.Tune), in the layout of TunableEvaluation:");
            sb.AppendLine("    // Weights[2 * term] for the middlegame, Weights[2 * term + 1] for the endgame.");
            sb.AppendLine($"    // Feature set: {features} (TunableEvaluation.Evaluate(position, weights, FeatureSet.{features})).");
            sb.AppendLine(FormattableString.Invariant($"    // {positions} positions, K = {k:F4}, error {startError:F6} -> {endError:F6}"));
            sb.AppendLine($"    public static class {className}");
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

        // The single-value terms of the tuned feature set (no tables), old -> new
        static void PrintChanges(int[] old, int[] tuned, FeatureSet features)
        {
            Console.WriteLine();
            Console.WriteLine($"{"term",-30} {"middlegame",18} {"endgame",18}");
            for (int term = 0; term < TunableEvaluation.TermCount; term++)
            {
                if (!TunableEvaluation.IsScalarTerm(term)) continue;
                if (features == FeatureSet.Basic && term >= TunableEvaluation.BasicTermCount) continue;
                if (features == FeatureSet.Extended && term >= TunableEvaluation.ThreatByMinor) continue;
                if (features != FeatureSet.Basic && term >= TunableEvaluation.Mobility && term < TunableEvaluation.PawnThreat) continue;   // replaced by the tables
                Console.WriteLine($"{TunableEvaluation.TermName(term),-30} {old[2 * term],7} -> {tuned[2 * term],-7} {old[2 * term + 1],7} -> {tuned[2 * term + 1],-7}");
            }
        }
    }
}
