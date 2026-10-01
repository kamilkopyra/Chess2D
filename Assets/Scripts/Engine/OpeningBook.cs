using System;
using System.Collections.Generic;

namespace ChessEngine
{
    // Opening book: for known opening positions, the moves strong players chose and how often.
    // Text format, one position per line (tab separated, '#' starts a comment):
    //     <piece placement> <side> <castling>\t<uci>:<games> <uci>:<games> ...
    // The en passant square is not part of the key.
    public sealed class OpeningBook
    {
        // Book used by bots that support it. Set by the host (Unity game or UCI tool) at startup.
        public static OpeningBook Default { get; set; }

        private readonly Dictionary<string, (string Move, int Weight)[]> positions =
            new Dictionary<string, (string, int)[]>();

        public int Count => positions.Count;

        public static OpeningBook Parse(string text)
        {
            var book = new OpeningBook();
            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                string[] parts = line.Split('\t');
                if (parts.Length != 2) continue;

                var moves = new List<(string, int)>();
                foreach (string entry in parts[1].Split(' '))
                {
                    int colon = entry.IndexOf(':');
                    if (colon > 0 && int.TryParse(entry.Substring(colon + 1), out int weight) && weight > 0)
                    {
                        moves.Add((entry.Substring(0, colon), weight));
                    }
                }
                if (moves.Count > 0) book.positions[parts[0]] = moves.ToArray();
            }
            return book;
        }

        // Picks a book move for the position, weighted by how often it was played.
        // Returns false when the position isn't in the book.
        public bool TryGetMove(Position position, Random random, out Move move)
        {
            move = default;
            if (!positions.TryGetValue(Key(position), out var entries)) return false;

            // Keep only moves that are legal here (protects against a broken or mismatched book)
            List<Move> legal = position.GetLegalMoves();
            var candidates = new List<(Move Move, int Weight)>();
            int total = 0;
            foreach (var (uci, weight) in entries)
            {
                Move found = legal.Find(m => m.ToString() == uci);
                if (found.ToString() != uci) continue;
                candidates.Add((found, weight));
                total += weight;
            }
            if (total == 0) return false;

            int pick = random.Next(total);
            foreach (var (candidate, weight) in candidates)
            {
                if (pick < weight)
                {
                    move = candidate;
                    return true;
                }
                pick -= weight;
            }
            return false;
        }

        // First three FEN fields: piece placement, side to move, castling rights
        private static string Key(Position position)
        {
            string[] fields = position.ToFen().Split(' ');
            return fields[0] + " " + fields[1] + " " + fields[2];
        }
    }
}
