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
            int depth = 3;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--bot" && i + 1 < args.Length) botName = args[++i];
                else if (args[i] == "--depth" && i + 1 < args.Length) depth = int.Parse(args[++i]);
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

            new UciEngine(botName, depth).Run();
            return 0;
        }
    }

    // Finds bot classes by name: every class deriving from BotBase called Bot_vN is available as "vN".
    // New bot versions show up automatically without changing this tool.
    public static class BotFactory
    {
        static readonly Dictionary<string, Type> bots = typeof(BotBase).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BotBase)) && !t.IsAbstract && t.Name.StartsWith("Bot_"))
            .ToDictionary(t => t.Name.Substring("Bot_".Length), t => t, StringComparer.OrdinalIgnoreCase);

        public static IEnumerable<string> AvailableBots() => bots.Keys.OrderBy(k => k);

        public static bool Exists(string name) => bots.ContainsKey(name);

        // Bots with an int constructor parameter (e.g. Bot_v2(int depth)) get the search depth
        public static BotBase Create(string name, int depth)
        {
            Type type = bots[name];
            ConstructorInfo withDepth = type.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(int));

            return withDepth != null
                ? (BotBase)withDepth.Invoke(new object[] { depth })
                : (BotBase)Activator.CreateInstance(type);
        }
    }

    public class UciEngine
    {
        private string botName;
        private int depth;
        private BotBase bot;
        private Position position = Position.StartPosition();

        public UciEngine(string botName, int depth)
        {
            this.botName = botName;
            this.depth = depth;
            bot = BotFactory.Create(botName, depth);
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
                        Console.WriteLine($"option name Depth type spin default {depth} min 1 max 10");
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
                        break;

                    case "position":
                        SetPosition(tokens);
                        break;

                    case "go":
                        Go();
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
            else
                return;

            bot = BotFactory.Create(botName, depth);
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

        void Go()
        {
            if (position.GetLegalMoves().Count == 0)
            {
                Console.WriteLine("bestmove 0000");
                return;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            Move move = bot.ChooseMove(position);
            Console.WriteLine($"info depth {depth} time {watch.ElapsedMilliseconds}");
            Console.WriteLine($"bestmove {move}");
        }
    }
}
