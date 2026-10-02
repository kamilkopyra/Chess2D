using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChessEngine;

namespace Chess2D.Tune
{
    // A minimal PGN reader: headers, the starting position (FEN tag) and the main line of moves in SAN.
    // Comments {...}, variations (...), move numbers and NAGs ($1) are skipped.
    public sealed class PgnGame
    {
        public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();
        public readonly List<string> Moves = new List<string>();

        public string Result => Headers.TryGetValue("Result", out string r) ? r : "*";
        public string StartFen => Headers.TryGetValue("FEN", out string f) ? f : Position.StartFen;

        public static IEnumerable<PgnGame> ReadAll(string path)
        {
            using var reader = new StreamReader(path, Encoding.UTF8);
            PgnGame game = null;
            var movetext = new StringBuilder();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith("["))
                {
                    if (game != null && movetext.Length > 0)
                    {
                        game.ParseMovetext(movetext.ToString());
                        yield return game;
                        game = null;
                        movetext.Clear();
                    }
                    game ??= new PgnGame();
                    int space = line.IndexOf(' ');
                    int quote1 = line.IndexOf('"'), quote2 = line.LastIndexOf('"');
                    if (space > 1 && quote2 > quote1)
                        game.Headers[line.Substring(1, space - 1)] = line.Substring(quote1 + 1, quote2 - quote1 - 1);
                }
                else if (line.Length > 0 && game != null)
                {
                    movetext.Append(line).Append(' ');
                }
            }
            if (game != null)
            {
                game.ParseMovetext(movetext.ToString());
                yield return game;
            }
        }

        private void ParseMovetext(string text)
        {
            int depth = 0;      // nesting of variations
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '{')
                {
                    int end = text.IndexOf('}', i);
                    i = end < 0 ? text.Length : end + 1;
                    continue;
                }
                if (c == '(') { depth++; i++; continue; }
                if (c == ')') { depth--; i++; continue; }
                if (char.IsWhiteSpace(c)) { i++; continue; }

                int start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '{' && text[i] != '(' && text[i] != ')') i++;
                if (depth > 0) continue;
                string token = text.Substring(start, i - start);

                // Move numbers ("12." / "12..."), NAGs, results
                if (token.StartsWith("$") || token == "1-0" || token == "0-1" || token == "1/2-1/2" || token == "*") continue;
                int dot = token.LastIndexOf('.');
                if (dot >= 0)
                {
                    token = token.Substring(dot + 1);
                    if (token.Length == 0) continue;
                }
                Moves.Add(token);
            }
        }
    }

    public static class San
    {
        // Finds the legal move written in SAN (e.g. "Nbd7", "exd5", "e8=Q+", "O-O"); false if there is none
        public static bool TryParse(Position position, string san, out Move move)
        {
            move = default;
            san = san.TrimEnd('+', '#', '!', '?');
            List<Move> legal = position.GetLegalMoves();

            if (san == "O-O" || san == "0-0" || san == "O-O-O" || san == "0-0-0")
            {
                bool kingside = san.Length == 3;
                foreach (Move m in legal)
                {
                    if ((m.Flags & (kingside ? MoveFlags.CastleKingside : MoveFlags.CastleQueenside)) != 0)
                    {
                        move = m;
                        return true;
                    }
                }
                return false;
            }

            PieceType promotion = PieceType.None;
            int eq = san.IndexOf('=');
            if (eq >= 0)
            {
                promotion = PieceFromLetter(san[eq + 1]);
                san = san.Substring(0, eq);
            }
            else if (san.Length >= 3 && "QRBN".IndexOf(san[san.Length - 1]) >= 0 && char.IsDigit(san[san.Length - 2]))
            {
                // Promotion written without '=' (e.g. "e8Q")
                promotion = PieceFromLetter(san[san.Length - 1]);
                san = san.Substring(0, san.Length - 1);
            }

            if (san.Length < 2) return false;
            int to = Square.Parse(san.Substring(san.Length - 2));
            PieceType type = char.IsUpper(san[0]) ? PieceFromLetter(san[0]) : PieceType.Pawn;
            string disambiguation = san.Substring(type == PieceType.Pawn ? 0 : 1, san.Length - 2 - (type == PieceType.Pawn ? 0 : 1)).Replace("x", "");

            Move found = default;
            int matches = 0;
            foreach (Move m in legal)
            {
                if (m.To != to || m.Promotion != promotion || position[m.From].Type != type) continue;
                bool ok = true;
                foreach (char d in disambiguation)
                {
                    if (d >= 'a' && d <= 'h' && Square.File(m.From) != d - 'a') ok = false;
                    if (d >= '1' && d <= '8' && Square.Rank(m.From) != d - '1') ok = false;
                }
                if (!ok) continue;
                found = m;
                matches++;
            }
            move = found;
            return matches == 1;
        }

        private static PieceType PieceFromLetter(char c)
        {
            switch (char.ToUpperInvariant(c))
            {
                case 'N': return PieceType.Knight;
                case 'B': return PieceType.Bishop;
                case 'R': return PieceType.Rook;
                case 'Q': return PieceType.Queen;
                case 'K': return PieceType.King;
                default: return PieceType.None;
            }
        }
    }
}
