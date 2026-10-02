using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChessEngine;

namespace Chess2D.Uci
{
    // Minimal UCI (Universal Chess Interface) front-end for the Chess2D bots.
    // Usage: Chess2D.Uci.exe [--bot v2] [--depth 4]
    // The bot and depth can also be changed through UCI options ("setoption name Bot value v1").
    public static class Program
    {
        public static int Main(string[] args)
        {
            string botName = "v2";
            int? depthArg = null;
            bool useBook = true;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--bot" && i + 1 < args.Length) botName = args[++i];
                else if (args[i] == "--depth" && i + 1 < args.Length) depthArg = int.Parse(args[++i]);
                else if (args[i] == "--no-book") useBook = false;
                else if (args[i] == "--list")
                {
                    Console.WriteLine(string.Join(", ", BotFactory.AvailableBots()));
                    return 0;
                }
            }

            if (!BotFactory.Exists(botName))
            {
                Console.Error.WriteLine($"Unknown bot '{botName}'. Available: {string.Join(", ", BotFactory.AvailableBots())}");
                return 1;
            }

            // Fixed-depth bots default to depth 3. For bots with a time limit the depth is only an upper bound:
            // without --depth (or with --depth 0) the clock alone decides how deep they search.
            int depth = depthArg ?? (BotFactory.IsTimed(botName) ? 0 : 3);
            if (depth == 0) depth = 64;

            // Opening book copied next to the exe from Assets/Resources/Books (used by Bot_v9 and newer)
            string bookPath = System.IO.Path.Combine(AppContext.BaseDirectory, "elite_book.txt");
            if (useBook && System.IO.File.Exists(bookPath))
            {
                OpeningBook.Default = OpeningBook.Parse(System.IO.File.ReadAllText(bookPath));
            }

            new UciEngine(botName, depth).Run();
            return 0;
        }
    }

    public class UciEngine
    {
        private string botName;
        private int depth;
        private int hashMb = 32;
        private BotBase bot;
        private Position position = Position.StartPosition();

        public UciEngine(string botName, int depth)
        {
            this.botName = botName;
            this.depth = depth;
            CreateBot();
        }

        // Creates the bot; the Hash option applies to bots with a resizable transposition table
        void CreateBot()
        {
            bot = BotFactory.Create(botName, depth);
            if (bot is IHashSizeBot hashBot) hashBot.HashMb = hashMb;
        }

        public void Run()
        {
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                string[] tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0) continue;

                switch (tokens[0])
                {
                    case "uci":
                        Console.WriteLine($"id name Chess2D {botName}");
                        Console.WriteLine("id author Kamil Kopyra");
                        Console.WriteLine($"option name Bot type combo default {botName} " +
                                          string.Join(" ", BotFactory.AvailableBots().Select(b => "var " + b)));
                        Console.WriteLine($"option name Depth type spin default {depth} min 1 max 64");
                        Console.WriteLine($"option name Hash type spin default {hashMb} min 1 max 1024");
                        Console.WriteLine("uciok");
                        break;

                    case "isready":
                        Console.WriteLine("readyok");
                        break;

                    case "setoption":
                        SetOption(tokens);
                        break;

                    case "ucinewgame":
                        position = Position.StartPosition();
                        if (bot is IHashSizeBot newGameBot) newGameBot.ClearTable();
                        break;

                    case "position":
                        SetPosition(tokens);
                        break;

                    case "go":
                        Go(tokens);
                        break;

                    case "d": // debug helper, not part of UCI: print the current position
                        Console.WriteLine(position);
                        break;

                    case "quit":
                        return;

                    // "stop", "ponderhit" etc.: the search is synchronous, so there is nothing to stop
                }
            }
        }

        // setoption name <Name> value <Value>
        void SetOption(string[] tokens)
        {
            int nameIndex = Array.IndexOf(tokens, "name");
            int valueIndex = Array.IndexOf(tokens, "value");
            if (nameIndex < 0 || valueIndex < 0 || valueIndex + 1 >= tokens.Length) return;

            string name = string.Join(" ", tokens.Skip(nameIndex + 1).Take(valueIndex - nameIndex - 1));
            string value = tokens[valueIndex + 1];

            if (name.Equals("Bot", StringComparison.OrdinalIgnoreCase) && BotFactory.Exists(value))
                botName = value;
            else if (name.Equals("Depth", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int d))
                depth = d;
            else if (name.Equals("Hash", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int mb))
            {
                // Only resizes the table: the bot (and what it learned so far) stays
                hashMb = Math.Max(1, Math.Min(1024, mb));
                if (bot is IHashSizeBot hashBot) hashBot.HashMb = hashMb;
                return;
            }
            else
                return;

            CreateBot();
        }

        // position startpos [moves e2e4 e7e5 ...]
        // position fen <6 FEN fields> [moves ...]
        void SetPosition(string[] tokens)
        {
            int movesIndex = Array.IndexOf(tokens, "moves");
            int end = movesIndex < 0 ? tokens.Length : movesIndex;

            if (tokens.Length > 1 && tokens[1] == "fen")
                position = Position.FromFen(string.Join(" ", tokens.Skip(2).Take(end - 2)));
            else
                position = Position.StartPosition();

            if (movesIndex < 0) return;

            for (int i = movesIndex + 1; i < tokens.Length; i++)
            {
                Move move = position.GetLegalMoves().Find(m => m.ToString() == tokens[i]);
                if (move.ToString() != tokens[i])
                {
                    Console.WriteLine($"info string illegal move {tokens[i]} in position {position.ToFen()}");
                    return;
                }
                position.MakeMove(move);
            }
        }

        // go [wtime <ms>] [btime <ms>] [winc <ms>] [binc <ms>] [movetime <ms>] ...
        void Go(string[] tokens)
        {
            if (position.GetLegalMoves().Count == 0)
            {
                Console.WriteLine("bestmove 0000");
                return;
            }

            if (bot is ITimedBot timedBot)
            {
                int? budget = TimeBudget(tokens);
                if (budget.HasValue) timedBot.MoveTimeMs = budget.Value;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            Move move = bot.ChooseMove(position);
            int reachedDepth = bot is ITimedBot timed ? timed.LastDepth : depth;
            Console.WriteLine($"info depth {reachedDepth} time {watch.ElapsedMilliseconds}");
            Console.WriteLine($"bestmove {move}");
        }

        // Time for this move: "movetime" if given, otherwise a share of the remaining clock.
        // Roughly 1/25 of the remaining time plus most of the increment, never more than 1/8 of what's left,
        // minus a margin for communication with the GUI and process scheduling. With an almost empty clock
        // the bot then plays quickly and wins time back through the increment.
        int? TimeBudget(string[] tokens)
        {
            int? Value(string name)
            {
                int i = Array.IndexOf(tokens, name);
                return i >= 0 && i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out int v) ? v : (int?)null;
            }

            const int Overhead = 50;

            int? moveTime = Value("movetime");
            if (moveTime.HasValue) return Math.Max(10, moveTime.Value - Overhead);

            bool white = position.SideToMove == Side.White;
            int? timeLeft = Value(white ? "wtime" : "btime");
            if (!timeLeft.HasValue) return null;
            int increment = Value(white ? "winc" : "binc") ?? 0;

            int budget = timeLeft.Value / 25 + increment * 3 / 4;
            budget = Math.Min(budget, timeLeft.Value / 8);
            return Math.Max(10, budget - Overhead);
        }
    }
}
