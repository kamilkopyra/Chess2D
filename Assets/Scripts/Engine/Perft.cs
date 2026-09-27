using System.Collections.Generic;

namespace ChessEngine
{
    // Perft: liczba wszystkich pozycji osiągalnych w dokładnie `depth` ruchach.
    // Porównanie ze znanymi wynikami sprawdza, czy generator ruchów jest bezbłędny
    // (roszady, en passant, promocje, związania, szachy).
    public static class Perft
    {
        public static long Count(Position position, int depth)
        {
            if (depth == 0) return 1;

            var moves = new List<Move>(48);
            position.GenerateLegalMoves(moves);
            if (depth == 1) return moves.Count;

            long nodes = 0;
            foreach (Move move in moves)
            {
                position.MakeMove(move);
                nodes += Count(position, depth - 1);
                position.UnmakeMove();
            }
            return nodes;
        }

        // Wynik osobno dla każdego ruchu z pozycji (przydatne do szukania błędów)
        public static Dictionary<string, long> Divide(Position position, int depth)
        {
            var result = new Dictionary<string, long>();
            foreach (Move move in position.GetLegalMoves())
            {
                position.MakeMove(move);
                result[move.ToString()] = Count(position, depth - 1);
                position.UnmakeMove();
            }
            return result;
        }
    }
}
