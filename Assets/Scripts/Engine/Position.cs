using System;
using System.Collections.Generic;
using System.Text;

namespace ChessEngine
{
    // Pełny stan partii szachowej jako czyste dane, bez Unity.
    // Ruchy wykonuje się przez MakeMove i cofa przez UnmakeMove (stos historii),
    // dzięki czemu bot może szybko przeszukiwać warianty.
    public sealed class Position
    {
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        private readonly Piece[] board = new Piece[64];
        private readonly int[] kingSquare = new int[2];

        public Side SideToMove { get; private set; }
        public CastlingRights Castling { get; private set; }
        public int EnPassantSquare { get; private set; } = Square.None;
        public int HalfmoveClock { get; private set; }
        public int FullmoveNumber { get; private set; } = 1;
        public ulong Hash { get; private set; }

        private struct Undo
        {
            public Move Move;
            public Piece Captured;
            public CastlingRights Castling;
            public int EnPassant;
            public int Halfmove;
            public ulong Hash;
        }

        private readonly List<Undo> history = new List<Undo>();

        // Maska praw do roszady: ruch z/na dane pole kasuje odpowiednie prawa
        // (ruch królem, ruch wieżą, zbicie wieży w rogu)
        private static readonly CastlingRights[] CastlingMask = BuildCastlingMask();

        public Piece this[int square] => board[square];
        public Piece At(int file, int rank) => board[Square.Make(file, rank)];
        public int KingSquare(Side side) => kingSquare[(int)side];
        public bool InCheck => IsSquareAttacked(kingSquare[(int)SideToMove], SideToMove.Opponent());

        // Liczba ruchów wykonanych od wczytania pozycji
        public int MovesPlayed => history.Count;

        public bool TryGetLastMove(out Move move)
        {
            move = history.Count > 0 ? history[history.Count - 1].Move : default;
            return history.Count > 0;
        }

        private Position() { }

        public static Position StartPosition() => FromFen(StartFen);

        public Position Clone()
        {
            var copy = new Position
            {
                SideToMove = SideToMove,
                Castling = Castling,
                EnPassantSquare = EnPassantSquare,
                HalfmoveClock = HalfmoveClock,
                FullmoveNumber = FullmoveNumber,
                Hash = Hash,
            };
            Array.Copy(board, copy.board, 64);
            Array.Copy(kingSquare, copy.kingSquare, 2);
            copy.history.AddRange(history);
            return copy;
        }

        // ===== Wykonywanie i cofanie ruchów =====

        // Wykonuje ruch bez sprawdzania legalności (ruch musi pochodzić z generatora)
        public void MakeMove(Move move)
        {
            Side us = SideToMove;
            int from = move.From, to = move.To;
            Piece moving = board[from];

            int captureSquare = move.IsEnPassant ? to + (us == Side.White ? -8 : 8) : to;
            Piece captured = move.IsEnPassant || move.IsCapture ? board[captureSquare] : Piece.None;

            history.Add(new Undo
            {
                Move = move,
                Captured = captured,
                Castling = Castling,
                EnPassant = EnPassantSquare,
                Halfmove = HalfmoveClock,
                Hash = Hash,
            });

            // Zdejmujemy z hasha stare prawa do roszady i en passant, dodamy nowe na końcu
            Hash ^= Zobrist.Castling[(int)Castling] ^ EnPassantKey();

            if (!captured.IsEmpty) RemovePiece(captureSquare);

            RemovePiece(from);
            PutPiece(to, move.IsPromotion ? new Piece(move.Promotion, us) : moving);

            if ((move.Flags & MoveFlags.CastleKingside) != 0)
            {
                RemovePiece(from + 3);
                PutPiece(from + 1, new Piece(PieceType.Rook, us));
            }
            else if ((move.Flags & MoveFlags.CastleQueenside) != 0)
            {
                RemovePiece(from - 4);
                PutPiece(from - 1, new Piece(PieceType.Rook, us));
            }

            if (moving.Type == PieceType.King) kingSquare[(int)us] = to;

            Castling &= CastlingMask[from] & CastlingMask[to];
            EnPassantSquare = (move.Flags & MoveFlags.DoublePawnPush) != 0 ? (from + to) / 2 : Square.None;
            HalfmoveClock = moving.Type == PieceType.Pawn || !captured.IsEmpty ? 0 : HalfmoveClock + 1;
            if (us == Side.Black) FullmoveNumber++;
            SideToMove = us.Opponent();

            Hash ^= Zobrist.BlackToMove ^ Zobrist.Castling[(int)Castling] ^ EnPassantKey();
        }

        public void UnmakeMove()
        {
            if (history.Count == 0) throw new InvalidOperationException("Brak ruchu do cofnięcia");

            Undo undo = history[history.Count - 1];
            history.RemoveAt(history.Count - 1);

            Move move = undo.Move;
            SideToMove = SideToMove.Opponent();
            Side us = SideToMove;
            if (us == Side.Black) FullmoveNumber--;

            // Tu nie liczymy hasha na bieżąco, bo przywracamy go z historii
            Piece moved = move.IsPromotion ? new Piece(PieceType.Pawn, us) : board[move.To];
            board[move.To] = Piece.None;
            board[move.From] = moved;
            if (moved.Type == PieceType.King) kingSquare[(int)us] = move.From;

            if ((move.Flags & MoveFlags.CastleKingside) != 0)
            {
                board[move.From + 1] = Piece.None;
                board[move.From + 3] = new Piece(PieceType.Rook, us);
            }
            else if ((move.Flags & MoveFlags.CastleQueenside) != 0)
            {
                board[move.From - 1] = Piece.None;
                board[move.From - 4] = new Piece(PieceType.Rook, us);
            }

            if (!undo.Captured.IsEmpty)
            {
                int captureSquare = move.IsEnPassant ? move.To + (us == Side.White ? -8 : 8) : move.To;
                board[captureSquare] = undo.Captured;
            }

            Castling = undo.Castling;
            EnPassantSquare = undo.EnPassant;
            HalfmoveClock = undo.Halfmove;
            Hash = undo.Hash;
        }

        private void PutPiece(int square, Piece piece)
        {
            board[square] = piece;
            Hash ^= Zobrist.PieceSquare[piece.Index, square];
        }

        private void RemovePiece(int square)
        {
            Hash ^= Zobrist.PieceSquare[board[square].Index, square];
            board[square] = Piece.None;
        }

        // Pole en passant wchodzi do hasha tylko wtedy, gdy strona na ruchu faktycznie może bić w przelocie.
        // Inaczej ta sama pozycja miałaby różne hashe i powtórzenia by się nie wykrywały.
        private ulong EnPassantKey()
        {
            if (EnPassantSquare == Square.None) return 0;

            Side us = SideToMove;
            foreach (int sq in Attacks.Pawn[(int)us.Opponent()][EnPassantSquare])
                if (board[sq].Is(PieceType.Pawn, us))
                    return Zobrist.EnPassantFile[Square.File(EnPassantSquare)];
            return 0;
        }

        private ulong ComputeHash()
        {
            ulong hash = 0;
            for (int sq = 0; sq < 64; sq++)
                if (!board[sq].IsEmpty)
                    hash ^= Zobrist.PieceSquare[board[sq].Index, sq];

            if (SideToMove == Side.Black) hash ^= Zobrist.BlackToMove;
            return hash ^ Zobrist.Castling[(int)Castling] ^ EnPassantKey();
        }

        // Do testów: czy hash liczony przyrostowo zgadza się z liczonym od zera
        public bool HashIsConsistent() => Hash == ComputeHash();

        // ===== Ataki =====

        public bool IsSquareAttacked(int square, Side by)
        {
            // Pion strony "by" atakuje square, jeśli stoi na polu, które atakowałby pion przeciwnika z square
            foreach (int sq in Attacks.Pawn[(int)by.Opponent()][square])
                if (board[sq].Is(PieceType.Pawn, by)) return true;

            foreach (int sq in Attacks.Knight[square])
                if (board[sq].Is(PieceType.Knight, by)) return true;

            foreach (int sq in Attacks.King[square])
                if (board[sq].Is(PieceType.King, by)) return true;

            int[][] rays = Attacks.Rays[square];
            for (int d = 0; d < 8; d++)
            {
                PieceType slider = d < Attacks.FirstDiagonal ? PieceType.Rook : PieceType.Bishop;
                foreach (int sq in rays[d])
                {
                    Piece p = board[sq];
                    if (p.IsEmpty) continue;
                    if (p.Color == by && (p.Type == slider || p.Type == PieceType.Queen)) return true;
                    break;
                }
            }

            return false;
        }

        // ===== Generowanie ruchów =====

        public List<Move> GetLegalMoves()
        {
            var moves = new List<Move>(48);
            GenerateLegalMoves(moves);
            return moves;
        }

        public void GenerateLegalMoves(List<Move> result)
        {
            result.Clear();
            var pseudo = new List<Move>(64);
            GeneratePseudoLegalMoves(pseudo);

            Side us = SideToMove;
            foreach (Move move in pseudo)
            {
                MakeMove(move);
                if (!IsSquareAttacked(kingSquare[(int)us], us.Opponent())) result.Add(move);
                UnmakeMove();
            }
        }

        public bool HasAnyLegalMove()
        {
            var pseudo = new List<Move>(64);
            GeneratePseudoLegalMoves(pseudo);

            Side us = SideToMove;
            foreach (Move move in pseudo)
            {
                MakeMove(move);
                bool legal = !IsSquareAttacked(kingSquare[(int)us], us.Opponent());
                UnmakeMove();
                if (legal) return true;
            }
            return false;
        }

        // Legalny ruch z pola from na pole to (dla kliknięć). Przy promocji trzeba podać figurę.
        public bool TryFindLegalMove(int from, int to, PieceType promotion, out Move move)
        {
            foreach (Move m in GetLegalMoves())
            {
                if (m.From == from && m.To == to && m.Promotion == promotion)
                {
                    move = m;
                    return true;
                }
            }
            move = default;
            return false;
        }

        // Ruchy "prawie legalne": zgodne z zasadami ruchu figur, ale mogą zostawić własnego króla w szachu
        public void GeneratePseudoLegalMoves(List<Move> moves)
        {
            Side us = SideToMove, them = us.Opponent();

            for (int sq = 0; sq < 64; sq++)
            {
                Piece p = board[sq];
                if (p.IsEmpty || p.Color != us) continue;

                switch (p.Type)
                {
                    case PieceType.Pawn:
                        GeneratePawnMoves(sq, us, them, moves);
                        break;
                    case PieceType.Knight:
                        GenerateStepMoves(sq, Attacks.Knight[sq], them, moves);
                        break;
                    case PieceType.King:
                        GenerateStepMoves(sq, Attacks.King[sq], them, moves);
                        break;
                    case PieceType.Bishop:
                        GenerateSlidingMoves(sq, Attacks.FirstDiagonal, 8, them, moves);
                        break;
                    case PieceType.Rook:
                        GenerateSlidingMoves(sq, Attacks.FirstOrthogonal, 4, them, moves);
                        break;
                    case PieceType.Queen:
                        GenerateSlidingMoves(sq, 0, 8, them, moves);
                        break;
                }
            }

            GenerateCastlingMoves(us, them, moves);
        }

        private void GeneratePawnMoves(int sq, Side us, Side them, List<Move> moves)
        {
            int forward = us == Side.White ? 8 : -8;
            int startRank = us == Side.White ? 1 : 6;

            int one = sq + forward;
            if (board[one].IsEmpty)
            {
                AddPawnMove(sq, one, MoveFlags.None, us, moves);

                int two = one + forward;
                if (Square.Rank(sq) == startRank && board[two].IsEmpty)
                    moves.Add(new Move(sq, two, MoveFlags.DoublePawnPush));
            }

            foreach (int target in Attacks.Pawn[(int)us][sq])
            {
                Piece victim = board[target];
                if (!victim.IsEmpty && victim.Color == them)
                    AddPawnMove(sq, target, MoveFlags.Capture, us, moves);
                else if (target == EnPassantSquare)
                    moves.Add(new Move(sq, target, MoveFlags.Capture | MoveFlags.EnPassant));
            }
        }

        private static void AddPawnMove(int from, int to, MoveFlags flags, Side us, List<Move> moves)
        {
            int lastRank = us == Side.White ? 7 : 0;
            if (Square.Rank(to) == lastRank)
            {
                moves.Add(new Move(from, to, flags, PieceType.Queen));
                moves.Add(new Move(from, to, flags, PieceType.Rook));
                moves.Add(new Move(from, to, flags, PieceType.Bishop));
                moves.Add(new Move(from, to, flags, PieceType.Knight));
            }
            else
            {
                moves.Add(new Move(from, to, flags));
            }
        }

        private void GenerateStepMoves(int sq, int[] targets, Side them, List<Move> moves)
        {
            foreach (int target in targets)
            {
                Piece p = board[target];
                if (p.IsEmpty) moves.Add(new Move(sq, target));
                else if (p.Color == them) moves.Add(new Move(sq, target, MoveFlags.Capture));
            }
        }

        private void GenerateSlidingMoves(int sq, int firstDirection, int endDirection, Side them, List<Move> moves)
        {
            int[][] rays = Attacks.Rays[sq];
            for (int d = firstDirection; d < endDirection; d++)
            {
                foreach (int target in rays[d])
                {
                    Piece p = board[target];
                    if (p.IsEmpty)
                    {
                        moves.Add(new Move(sq, target));
                        continue;
                    }
                    if (p.Color == them) moves.Add(new Move(sq, target, MoveFlags.Capture));
                    break;
                }
            }
        }

        private void GenerateCastlingMoves(Side us, Side them, List<Move> moves)
        {
            int baseSq = us == Side.White ? 0 : 56;
            int kingFrom = baseSq + 4;
            if (!board[kingFrom].Is(PieceType.King, us)) return;

            CastlingRights kingside = us == Side.White ? CastlingRights.WhiteKingside : CastlingRights.BlackKingside;
            CastlingRights queenside = us == Side.White ? CastlingRights.WhiteQueenside : CastlingRights.BlackQueenside;
            if ((Castling & (kingside | queenside)) == 0) return;

            // Nie wolno roszować z szacha
            if (IsSquareAttacked(kingFrom, them)) return;

            // Krótka: f i g puste, król nie przechodzi przez atakowane pole
            if ((Castling & kingside) != 0
                && board[baseSq + 7].Is(PieceType.Rook, us)
                && board[baseSq + 5].IsEmpty && board[baseSq + 6].IsEmpty
                && !IsSquareAttacked(baseSq + 5, them) && !IsSquareAttacked(baseSq + 6, them))
            {
                moves.Add(new Move(kingFrom, baseSq + 6, MoveFlags.CastleKingside));
            }

            // Długa: b, c, d puste; atakowane nie mogą być tylko c i d
            if ((Castling & queenside) != 0
                && board[baseSq].Is(PieceType.Rook, us)
                && board[baseSq + 1].IsEmpty && board[baseSq + 2].IsEmpty && board[baseSq + 3].IsEmpty
                && !IsSquareAttacked(baseSq + 3, them) && !IsSquareAttacked(baseSq + 2, them))
            {
                moves.Add(new Move(kingFrom, baseSq + 2, MoveFlags.CastleQueenside));
            }
        }

        // ===== Stan gry =====

        public GameStatus GetStatus()
        {
            if (!HasAnyLegalMove()) return InCheck ? GameStatus.Checkmate : GameStatus.Stalemate;
            if (HalfmoveClock >= 100) return GameStatus.FiftyMoveRule;
            if (IsThreefoldRepetition()) return GameStatus.ThreefoldRepetition;
            if (IsInsufficientMaterial()) return GameStatus.InsufficientMaterial;
            return GameStatus.Ongoing;
        }

        // Ta sama pozycja (ta sama strona na ruchu) wystąpiła trzeci raz.
        // Szukamy tylko od ostatniego bicia/ruchu pionem, bo wcześniejsze pozycje nie mogą się powtórzyć.
        public bool IsThreefoldRepetition()
        {
            int count = 1;
            int oldest = Math.Max(0, history.Count - HalfmoveClock);
            for (int i = history.Count - 2; i >= oldest; i -= 2)
            {
                if (history[i].Hash == Hash && ++count >= 3) return true;
            }
            return false;
        }

        // Repetition check for the search.
        // searchStart = MovesPlayed when the search began. A position repeated inside the search counts as
        // a draw right away (if repeating is good for one side, it can keep repeating). A position that only
        // repeats moves played before the search needs to have occurred twice already (threefold rule).
        public bool IsRepetition(int searchStart = 0)
        {
            int earlierOccurrences = 0;
            int oldest = Math.Max(0, history.Count - HalfmoveClock);
            for (int i = history.Count - 2; i >= oldest; i -= 2)
            {
                if (history[i].Hash != Hash) continue;
                if (i >= searchStart) return true;
                if (++earlierOccurrences >= 2) return true;
            }
            return false;
        }

        // Nikt nie może dać mata: K v K, K+goniec v K, K+skoczek v K, gońce tylko na polach jednego koloru
        public bool IsInsufficientMaterial()
        {
            int minors = 0, knights = 0;
            int bishopSquareColors = 0; // bit 0 = jasne pola, bit 1 = ciemne

            for (int sq = 0; sq < 64; sq++)
            {
                Piece p = board[sq];
                switch (p.Type)
                {
                    case PieceType.Pawn:
                    case PieceType.Rook:
                    case PieceType.Queen:
                        return false;
                    case PieceType.Knight:
                        minors++;
                        knights++;
                        break;
                    case PieceType.Bishop:
                        minors++;
                        bishopSquareColors |= (Square.File(sq) + Square.Rank(sq)) % 2 == 0 ? 2 : 1;
                        break;
                }
            }

            if (minors <= 1) return true;
            return knights == 0 && bishopSquareColors != 3;
        }

        // ===== FEN =====

        public static Position FromFen(string fen)
        {
            string[] parts = fen.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) throw new FormatException($"Niepełny FEN: '{fen}'");

            var pos = new Position();
            bool[] kingFound = new bool[2];

            string[] ranks = parts[0].Split('/');
            if (ranks.Length != 8) throw new FormatException($"FEN musi mieć 8 rzędów: '{fen}'");

            for (int i = 0; i < 8; i++)
            {
                int rank = 7 - i, file = 0;
                foreach (char c in ranks[i])
                {
                    if (char.IsDigit(c))
                    {
                        file += c - '0';
                        continue;
                    }
                    if (file > 7) throw new FormatException($"Za długi rząd w FEN: '{ranks[i]}'");

                    Piece piece = Piece.FromFenChar(c);
                    int sq = Square.Make(file, rank);
                    pos.board[sq] = piece;
                    if (piece.Type == PieceType.King)
                    {
                        pos.kingSquare[(int)piece.Color] = sq;
                        kingFound[(int)piece.Color] = true;
                    }
                    file++;
                }
                if (file != 8) throw new FormatException($"Zła długość rzędu w FEN: '{ranks[i]}'");
            }

            if (!kingFound[0] || !kingFound[1]) throw new FormatException("FEN musi mieć obu królów");

            pos.SideToMove = parts[1] == "b" ? Side.Black : Side.White;

            pos.Castling = CastlingRights.None;
            foreach (char c in parts[2])
            {
                switch (c)
                {
                    case 'K': pos.Castling |= CastlingRights.WhiteKingside; break;
                    case 'Q': pos.Castling |= CastlingRights.WhiteQueenside; break;
                    case 'k': pos.Castling |= CastlingRights.BlackKingside; break;
                    case 'q': pos.Castling |= CastlingRights.BlackQueenside; break;
                }
            }

            pos.EnPassantSquare = Square.Parse(parts[3]);
            pos.HalfmoveClock = parts.Length > 4 ? int.Parse(parts[4]) : 0;
            pos.FullmoveNumber = parts.Length > 5 ? int.Parse(parts[5]) : 1;
            pos.Hash = pos.ComputeHash();
            return pos;
        }

        public string ToFen()
        {
            var sb = new StringBuilder();
            for (int rank = 7; rank >= 0; rank--)
            {
                int empty = 0;
                for (int file = 0; file < 8; file++)
                {
                    Piece p = board[Square.Make(file, rank)];
                    if (p.IsEmpty)
                    {
                        empty++;
                        continue;
                    }
                    if (empty > 0) sb.Append(empty);
                    empty = 0;
                    sb.Append(p.ToFenChar());
                }
                if (empty > 0) sb.Append(empty);
                if (rank > 0) sb.Append('/');
            }

            sb.Append(SideToMove == Side.White ? " w " : " b ");

            if (Castling == CastlingRights.None) sb.Append('-');
            if ((Castling & CastlingRights.WhiteKingside) != 0) sb.Append('K');
            if ((Castling & CastlingRights.WhiteQueenside) != 0) sb.Append('Q');
            if ((Castling & CastlingRights.BlackKingside) != 0) sb.Append('k');
            if ((Castling & CastlingRights.BlackQueenside) != 0) sb.Append('q');

            sb.Append(' ').Append(Square.ToName(EnPassantSquare));
            sb.Append(' ').Append(HalfmoveClock).Append(' ').Append(FullmoveNumber);
            return sb.ToString();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int rank = 7; rank >= 0; rank--)
            {
                sb.Append(rank + 1).Append(' ');
                for (int file = 0; file < 8; file++)
                    sb.Append(board[Square.Make(file, rank)].ToFenChar()).Append(' ');
                sb.AppendLine();
            }
            sb.AppendLine("  a b c d e f g h");
            sb.Append(ToFen());
            return sb.ToString();
        }

        private static CastlingRights[] BuildCastlingMask()
        {
            var mask = new CastlingRights[64];
            for (int i = 0; i < 64; i++) mask[i] = CastlingRights.All;

            mask[Square.Make(0, 0)] = ~CastlingRights.WhiteQueenside & CastlingRights.All; // a1
            mask[Square.Make(4, 0)] = ~(CastlingRights.WhiteKingside | CastlingRights.WhiteQueenside) & CastlingRights.All; // e1
            mask[Square.Make(7, 0)] = ~CastlingRights.WhiteKingside & CastlingRights.All; // h1
            mask[Square.Make(0, 7)] = ~CastlingRights.BlackQueenside & CastlingRights.All; // a8
            mask[Square.Make(4, 7)] = ~(CastlingRights.BlackKingside | CastlingRights.BlackQueenside) & CastlingRights.All; // e8
            mask[Square.Make(7, 7)] = ~CastlingRights.BlackKingside & CastlingRights.All; // h8
            return mask;
        }
    }
}
