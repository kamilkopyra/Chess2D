using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chess2D.Tune
{
    // Adds a Stockfish evaluation to every position:  "FEN;result"  ->  "FEN;result;score"
    // (score in centipawns from White's point of view; mates are written as +/-10000).
    // Several Stockfish processes work in parallel, each with one thread and a fixed node budget.
    // An existing output file is continued (positions already labelled are skipped), so the job can be stopped.
    public static class Label
    {
        public static int Run(Dictionary<string, string> options)
        {
            string input = options.TryGetValue("data", out string d) ? d : "positions.txt";
            string output = options.TryGetValue("out", out string o) ? o : "labelled.txt";
            string stockfish = options.TryGetValue("stockfish", out string s) ? s : null;
            int workers = options.TryGetValue("threads", out string t) ? int.Parse(t) : 4;
            int nodes = options.TryGetValue("nodes", out string n) ? int.Parse(n) : 5000;
            if (stockfish == null || !File.Exists(stockfish))
            {
                Console.WriteLine("--stockfish <path to stockfish.exe> is required");
                return 1;
            }

            string[] lines = File.ReadAllLines(input);
            int done = File.Exists(output) ? File.ReadAllLines(output).Length : 0;
            Console.WriteLine($"{lines.Length} positions, {done} already labelled; {workers} Stockfish processes, {nodes} nodes each");

            // Results come back out of order; they are written in input order through a small reorder buffer
            var results = new ConcurrentDictionary<int, string>();
            int next = done;
            var watch = Stopwatch.StartNew();
            using var writer = new StreamWriter(output, true, new UTF8Encoding(false));
            object writeGate = new object();

            Parallel.For(0, workers, new ParallelOptions { MaxDegreeOfParallelism = workers }, worker =>
            {
                using var engine = new UciEngine(stockfish);
                for (int i = done + worker; i < lines.Length; i += workers)
                {
                    string line = lines[i];
                    string fen = line.Substring(0, line.LastIndexOf(';'));
                    int score = engine.Evaluate(fen, nodes);
                    results[i] = line + ";" + score.ToString(CultureInfo.InvariantCulture);

                    lock (writeGate)
                    {
                        while (results.TryRemove(next, out string ready))
                        {
                            writer.WriteLine(ready);
                            next++;
                            if (next % 50000 == 0)
                            {
                                writer.Flush();
                                double rate = (next - done) / Math.Max(1, watch.Elapsed.TotalSeconds);
                                Console.WriteLine($"  {next}/{lines.Length} ({rate:F0}/s, about {(lines.Length - next) / rate / 60:F0} min left)");
                            }
                        }
                    }
                }
            });
            Console.WriteLine($"Done: {next} positions labelled -> {output}");
            return 0;
        }

        // One Stockfish process talking UCI
        private sealed class UciEngine : IDisposable
        {
            private readonly Process process;

            public UciEngine(string path)
            {
                process = Process.Start(new ProcessStartInfo(path)
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                Send("uci");
                WaitFor("uciok");
                Send("setoption name Threads value 1");
                Send("setoption name Hash value 16");
                Send("isready");
                WaitFor("readyok");
            }

            // Score after a search of `nodes` nodes, in centipawns from White's point of view
            public int Evaluate(string fen, int nodes)
            {
                Send("ucinewgame");
                Send("position fen " + fen);
                Send("go nodes " + nodes);
                int score = 0;
                string line;
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    if (line.StartsWith("bestmove")) break;
                    int i = line.IndexOf(" score ", StringComparison.Ordinal);
                    if (i < 0 || line.Contains("lowerbound") || line.Contains("upperbound")) continue;
                    string[] parts = line.Substring(i + 7).Split(' ');
                    int value = int.Parse(parts[1], CultureInfo.InvariantCulture);
                    score = parts[0] == "mate" ? (value > 0 ? 10000 : -10000) : value;
                }
                bool whiteToMove = fen.Split(' ')[1] == "w";
                return whiteToMove ? score : -score;
            }

            private void Send(string command)
            {
                process.StandardInput.WriteLine(command);
                process.StandardInput.Flush();
            }

            private void WaitFor(string token)
            {
                string line;
                while ((line = process.StandardOutput.ReadLine()) != null && !line.StartsWith(token)) { }
            }

            public void Dispose()
            {
                try
                {
                    Send("quit");
                    if (!process.WaitForExit(2000)) process.Kill();
                }
                catch { }
                process.Dispose();
            }
        }
    }
}
