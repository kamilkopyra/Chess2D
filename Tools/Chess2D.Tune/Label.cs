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
    // Both files are streamed, so the size of the data set is limited only by the disk.
    public static class Label
    {
        public static int Run(Dictionary<string, string> options)
        {
            string input = options.TryGetValue("data", out string d) ? d : "positions.txt";
            string output = options.TryGetValue("out", out string o) ? o : "labelled.txt";
            string stockfish = options.TryGetValue("stockfish", out string s) ? s : null;
            int workers = options.TryGetValue("threads", out string t) ? int.Parse(t) : 4;
            int nodes = options.TryGetValue("nodes", out string n) ? int.Parse(n) : 5000;
            // Clearing Stockfish's hash before every position (ucinewgame) costs little alone but stops the
            // processes from scaling (about 400 positions/s in total on 20 cores, against about 1500 without it).
            // Keeping the hash only lets one unrelated position see another's entries.
            bool clearHash = options.ContainsKey("clear-hash");
            if (stockfish == null || !File.Exists(stockfish))
            {
                Console.WriteLine("--stockfish <path to the Stockfish executable> is required");
                return 1;
            }

            long done = 0;
            if (File.Exists(output))
            {
                TrimPartialLine(output);
                using var existing = new StreamReader(output);
                while (existing.ReadLine() != null) done++;
            }
            Console.WriteLine($"{done} positions already labelled; {workers} Stockfish processes, {nodes} nodes each" +
                              (clearHash ? ", hash cleared before every position" : ""));

            // Reader -> queue -> workers -> results by index -> writer (in input order)
            var queue = new BlockingCollection<(long Index, string Line)>(workers * 64);
            var results = new ConcurrentDictionary<long, string>();
            long next = done;
            var watch = Stopwatch.StartNew();
            using var writer = new StreamWriter(output, true, new UTF8Encoding(false), 1 << 20);
            object writeGate = new object();

            var reader = Task.Run(() =>
            {
                using var lines = new StreamReader(input, Encoding.UTF8, false, 1 << 20);
                long index = 0;
                string line;
                while ((line = lines.ReadLine()) != null)
                {
                    if (index >= done) queue.Add((index, line));
                    index++;
                }
                queue.CompleteAdding();
            });

            var tasks = new Task[workers];
            for (int w = 0; w < workers; w++)
            {
                tasks[w] = Task.Factory.StartNew(() =>
                {
                    using var engine = new UciEngine(stockfish);
                    foreach (var (index, line) in queue.GetConsumingEnumerable())
                    {
                        string fen = line.Substring(0, line.LastIndexOf(';'));
                        int score = engine.Evaluate(fen, nodes, clearHash);
                        results[index] = line + ";" + score.ToString(CultureInfo.InvariantCulture);

                        lock (writeGate)
                        {
                            while (results.TryRemove(next, out string ready))
                            {
                                writer.WriteLine(ready);
                                next++;
                                if (next % 100000 == 0)
                                {
                                    writer.Flush();
                                    double rate = (next - done) / Math.Max(1, watch.Elapsed.TotalSeconds);
                                    Console.WriteLine($"  {next} labelled ({rate:F0}/s, {watch.Elapsed:d\\.hh\\:mm\\:ss})");
                                }
                            }
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }
            Task.WaitAll(tasks);
            reader.Wait();
            Console.WriteLine($"Done: {next} positions labelled -> {output} ({watch.Elapsed:d\\.hh\\:mm\\:ss})");
            return 0;
        }

        // A job stopped while writing can leave half a line at the end; it is cut off and labelled again
        private static void TrimPartialLine(string path)
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
            long end = file.Length;
            while (end > 0)
            {
                file.Position = end - 1;
                if (file.ReadByte() == '\n') break;
                end--;
            }
            if (end < file.Length)
            {
                Console.WriteLine($"Cut off an unfinished last line ({file.Length - end} bytes)");
                file.SetLength(end);
            }
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
            public int Evaluate(string fen, int nodes, bool clearHash)
            {
                if (clearHash) Send("ucinewgame");
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
