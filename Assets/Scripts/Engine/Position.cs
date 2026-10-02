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

        // Bitboards kept in sync with `board` (bit index = square index): one per piece type
        // (indexed by PieceType, index 0 unused), one per side, and all occupied squares.
        private readonly ulong[] typeBits = new ulong[7];
        private readonly ulong[] sideBits = new ulong[2];
        private ulong occupied;

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

        public ulong Occupied => occupied;
        public ulong Pieces(Side side) => sideBits[(int)side];
        public ulong Pieces(PieceType type) => typeBits[(int)type];
        public ulong Pieces(PieceType type, Side side) => typeBits[(int)type] & sideBits[(int)side];

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
            Array.Copy(typeBits, copy.typeBits, typeBits.Length);
            Array.Copy(sideBits, copy.sideBits, 2);
            copy.occupied = occupied;
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
            SetSquare(move.To, Piece.None);
            SetSquare(move.From, moved);
            if (moved.Type == PieceType.King) kingSquare[(int)us] = move.From;

            if ((move.Flags & MoveFlags.CastleKingside) != 0)
            {
                SetSquare(move.From + 1, Piece.None);
                SetSquare(move.From + 3, new Piece(PieceType.Rook, us));
            }
            else if ((move.Flags & MoveFlags.CastleQueenside) != 0)
            {
                SetSquare(move.From - 1, Piece.None);
                SetSquare(move.From - 4, new Piece(PieceType.Rook, us));
            }

            if (!undo.Captured.IsEmpty)
            {
                int captureSquare = move.IsEnPassant ? move.To + (us == Side.White ? -8 : 8) : move.To;
                SetSquare(captureSquare, undo.Captured);
            }

            Castling = undo.Castling;
            EnPassantSquare = undo.EnPassant;
            HalfmoveClock = undo.Halfmove;
            Hash = undo.Hash;
        }

        // Passes the turn without moving a piece - not a legal chess move, used by null move pruning in search.
        // Must be undone with UnmakeNullMove.
        public void MakeNullMove()
        {
            history.Add(new Undo
            {
                Move = default,
                Captured = Piece.None,
                Castling = Castling,
                EnPassant = EnPassantSquare,
                Halfmove = HalfmoveClock,
                Hash = Hash,
            });

            Hash ^= EnPassantKey() ^ Zobrist.BlackToMove;
            EnPassantSquare = Square.None;
            // Positions before a null move can't count as repetitions of positions after it
            HalfmoveClock = 0;
            if (SideToMove == Side.Black) FullmoveNumber++;
            SideToMove = SideToMove.Opponent();
        }

        public void UnmakeNullMove()
        {
            Undo undo = history[history.Count - 1];
            history.RemoveAt(history.Count - 1);

            SideToMove = SideToMove.Opponent();
            if (SideToMove == Side.Black) FullmoveNumber--;
            EnPassantSquare = undo.EnPassant;
            HalfmoveClock = undo.Halfmove;
            Hash = undo.Hash;
        }

        private void PutPiece(int square, Piece piece)
        {
            SetSquare(square, piece);
            Hash ^= Zobrist.PieceSquare[piece.Index, square];
        }

        private void RemovePiece(int square)
        {
            Hash ^= Zobrist.PieceSquare[board[square].Index, square];
            SetSquare(square, Piece.None);
        }

        // Puts a piece (or Piece.None) on a square and updates the bitboards; doesn't touch the hash
        private void SetSquare(int square, Piece piece)
        {
            ulong bit = 1UL << square;
            Piece old = board[square];
            if (!old.IsEmpty)
            {
                typeBits[(int)old.Type] ^= bit;
                sideBits[(int)old.Color] ^= bit;
                occupied ^= bit;
            }
            if (!piece.IsEmpty)
            {
                typeBits[(int)piece.Type] ^= bit;
                sideBits[(int)piece.Color] ^= bit;
                occupied ^= bit;
            }
            board[square] = piece;
        }

        // Pole en passant wchodzi do hasha tylko wtedy, gdy strona na ruchu faktycznie może bić w przelocie.
        // Inaczej ta sama pozycja miałaby różne hashe i powtórzenia by się nie wykrywały.
        private ulong EnPassantKey()
        {
            if (EnPassantSquare == Square.None) return 0;

            Side us = SideToMove;
            if ((Bitboards.PawnAttacks[(int)us.Opponent()][EnPassantSquare] & typeBits[(int)PieceType.Pawn] & sideBits[(int)us]) != 0)
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

        // For tests: do the bitboards describe exactly the pieces on the board (and the king squares)?
        public bool BitboardsAreConsistent()
        {
            var types = new ulong[7];
            var sides = new ulong[2];
            for (int sq = 0; sq < 64; sq++)
            {
                Piece p = board[sq];
                if (p.IsEmpty) continue;
                types[(int)p.Type] |= 1UL << sq;
                sides[(int)p.Color] |= 1UL << sq;
            }

            for (int t = 0; t < 7; t++)
                if (types[t] != typeBits[t]) return false;
            if (sides[0] != sideBits[0] || sides[1] != sideBits[1]) return false;
            if (occupied != (sides[0] | sides[1])) return false;

            for (int s = 0; s < 2; s++)
                if ((typeBits[(int)PieceType.King] & sideBits[s]) != 1UL << kingSquare[s]) return false;
            return true;
        }

        // ===== Ataki =====

        public bool IsSquareAttacked(int square, Side by) => IsSquareAttacked(square, by, occupied);

        // The same with a different set of occupied squares (e.g. with our king lifted off the board)
        private bool IsSquareAttacked(int square, Side by, ulong occupiedSquares)
        {
            ulong attackers = sideBits[(int)by];

            // Pion strony "by" atakuje square, jeśli stoi na polu, które atakowałby pion przeciwnika z square
            if ((Bitboards.PawnAttacks[(int)by.Opponent()][square] & typeBits[(int)PieceType.Pawn] & attackers) != 0) return true;
            if ((Bitboards.KnightAttacks[square] & typeBits[(int)PieceType.Knight] & attackers) != 0) return true;
            if ((Bitboards.KingAttacks[square] & typeBits[(int)PieceType.King] & attackers) != 0) return true;

            ulong queens = typeBits[(int)PieceType.Queen];
            ulong rooks = (typeBits[(int)PieceType.Rook] | queens) & attackers;
            if (rooks != 0 && (Bitboards.RookAttacks(square, occupiedSquares) & rooks) != 0) return true;
            ulong bishops = (typeBits[(int)PieceType.Bishop] | queens) & attackers;
            return bishops != 0 && (Bitboards.BishopAttacks(square, occupiedSquares) & bishops) != 0;
        }

        // Bitboard of all pieces of both sides that attack `square`, with sliders blocked by `occupiedSquares`.
        // Pieces are taken from the current position; pass a smaller `occupiedSquares` to see x-ray attackers
        // behind removed pieces (and mask the removed pieces out of the result yourself), e.g. for SEE.
        public ulong AttackersTo(int square, ulong occupiedSquares)
        {
            ulong queens = typeBits[(int)PieceType.Queen];
            ulong pawns = typeBits[(int)PieceType.Pawn];
            return (Bitboards.PawnAttacks[(int)Side.Black][square] & pawns & sideBits[(int)Side.White])
                 | (Bitboards.PawnAttacks[(int)Side.White][square] & pawns & sideBits[(int)Side.Black])
                 | (Bitboards.KnightAttacks[square] & typeBits[(int)PieceType.Knight])
                 | (Bitboards.KingAttacks[square] & typeBits[(int)PieceType.King])
                 | (Bitboards.RookAttacks(square, occupiedSquares) & (typeBits[(int)PieceType.Rook] | queens))
                 | (Bitboards.BishopAttacks(square, occupiedSquares) & (typeBits[(int)PieceType.Bishop] | queens));
        }

        // ===== Generowanie ruchów =====

        public List<Move> GetLegalMoves()
        {
            var moves = new List<Move>(48);
            GenerateLegalMoves(moves);
            return moves;
        }

        // Reference implementation: every pseudo-legal move is made and the king is checked.
        // Slower than GenerateLegalMovesFast, kept as an independent check for tests and Perft.Count.
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

        // Same moves in the same order as GenerateLegalMoves, but faster (used by Bot_v17 and newer):
        // checks and pins are found once on bitboards and only legal moves are generated.
        // `pseudo` is no longer needed (kept so the signature doesn't change); it is only cleared.
        public void GenerateLegalMovesFast(List<Move> result, List<Move> pseudo)
        {
            result.Clear();
            pseudo.Clear();
            GenerateMoves(result, false, true);
        }

        public bool HasAnyLegalMove()
        {
            var moves = new List<Move>(64);
            GenerateMoves(moves, false, true);
            return moves.Count > 0;
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
        public void GeneratePseudoLegalMoves(List<Move> moves) => GenerateMoves(moves, false, false);

        // Only captures (including en passant) and promotions, pseudo-legal - for quiescence search (Bot_v17 and newer).
        // Same moves as GeneratePseudoLegalMoves filtered to IsCapture || IsPromotion, without generating the rest.
        public void GeneratePseudoLegalCaptures(List<Move> moves) => GenerateMoves(moves, true, false);

        // One generator for all the variants. The moves come in a fixed order (the same as in the old mailbox
        // generator, which the bots' move ordering relies on for ties): pieces by square a1..h8; for each piece
        // its targets in the order of Attacks.Knight / Attacks.King / Attacks.Rays (direction by direction,
        // nearest square first); castling last.
        // legalOnly: skip moves that leave our king in check (pins and checks found once, on bitboards).
        private void GenerateMoves(List<Move> moves, bool capturesOnly, bool legalOnly)
        {
            Side us = SideToMove, them = us.Opponent();
            ulong own = sideBits[(int)us], enemy = sideBits[(int)them];
            int king = kingSquare[(int)us];

            // Squares the non-king pieces may move to: with one checker capture it or block, with two only
            // the king can move
            ulong checkMask = ~0UL;
            ulong pinned = 0;
            bool inCheck = false;
            if (legalOnly)
            {
                ulong checkers = AttackersTo(king, occupied) & enemy;
                if (checkers != 0)
                {
                    inCheck = true;
                    checkMask = (checkers & (checkers - 1)) != 0
                        ? 0
                        : checkers | Bitboards.Between[king * 64 + Bits.LowestSquare(checkers)];
                }
                pinned = PinnedPieces(king, own, enemy);
            }

            // Captures only: enemy pieces; otherwise anything but our own pieces
            ulong targetMask = capturesOnly ? enemy : ~own;

            ulong pieces = own;
            while (pieces != 0)
            {
                int sq = Bits.PopLowest(ref pieces);

                // A pinned piece may only move along the line through our king and itself
                ulong allowed = checkMask;
                if ((pinned & (1UL << sq)) != 0) allowed &= Bitboards.Line[king * 64 + sq];

                switch (board[sq].Type)
                {
                    case PieceType.Pawn:
                        GeneratePawnMoves(sq, us, enemy, allowed, moves, capturesOnly, legalOnly);
                        break;
                    case PieceType.Knight:
                        AddStepMoves(sq, Attacks.Knight[sq], Bitboards.KnightAttacks[sq] & targetMask & allowed, enemy, moves);
                        break;
                    case PieceType.Bishop:
                        AddSlidingMoves(sq, Bitboards.BishopAttacks(sq, occupied) & targetMask & allowed,
                                        Attacks.FirstDiagonal, 8, enemy, moves);
                        break;
                    case PieceType.Rook:
                        AddSlidingMoves(sq, Bitboards.RookAttacks(sq, occupied) & targetMask & allowed,
                                        Attacks.FirstOrthogonal, 4, enemy, moves);
                        break;
                    case PieceType.Queen:
                        AddSlidingMoves(sq, Bitboards.QueenAttacks(sq, occupied) & targetMask & allowed,
                                        0, 8, enemy, moves);
                        break;
                    case PieceType.King:
                        ulong targets = Bitboards.KingAttacks[sq] & targetMask;
                        if (legalOnly)
                        {
                            // The target must not be attacked - with the king lifted off the board,
                            // so it doesn't hide the squares behind it
                            ulong withoutKing = occupied ^ (1UL << sq);
                            ulong safe = 0;
                            ulong t = targets;
                            while (t != 0)
                            {
                                int to = Bits.PopLowest(ref t);
                                if (!IsSquareAttacked(to, them, withoutKing)) safe |= 1UL << to;
                            }
                            targets = safe;
                        }
                        AddStepMoves(sq, Attacks.King[sq], targets, enemy, moves);
                        break;
                }
            }

            // Castling out of check is not allowed (GenerateCastlingMoves checks that too)
            if (!capturesOnly && !inCheck) GenerateCastlingMoves(us, them, moves);
        }

        // Our pieces that are the only piece between our king and an enemy slider on the same line
        private ulong PinnedPieces(int king, ulong own, ulong enemy)
        {
            ulong queens = typeBits[(int)PieceType.Queen];
            // Enemy sliders that would attack the king if none of our pieces were in the way
            ulong snipers = ((Bitboards.RookAttacks(king, enemy) & (typeBits[(int)PieceType.Rook] | queens))
                           | (Bitboards.BishopAttacks(king, enemy) & (typeBits[(int)PieceType.Bishop] | queens))) & enemy;

            ulong pinned = 0;
            while (snipers != 0)
            {
                int sniper = Bits.PopLowest(ref snipers);
                ulong between = Bitboards.Between[king * 64 + sniper] & occupied;
                if (between != 0 && (between & (between - 1)) == 0) pinned |= between & own;
            }
            return pinned;
        }

        // allowed: squares the pawn may move to (check and pin restrictions); en passant is checked separately
        private void GeneratePawnMoves(int sq, Side us, ulong enemy, ulong allowed, List<Move> moves, bool capturesOnly, bool legalOnly)
        {
            int forward = us == Side.White ? 8 : -8;
            int startRank = us == Side.White ? 1 : 6;
            int lastRank = us == Side.White ? 7 : 0;

            int one = sq + forward;
            // With capturesOnly a push is only generated when it promotes
            if (board[one].IsEmpty && (!capturesOnly || Square.Rank(one) == lastRank))
            {
                if ((allowed & (1UL << one)) != 0) AddPawnMove(sq, one, MoveFlags.None, us, moves);

                int two = one + forward;
                if (!capturesOnly && Square.Rank(sq) == startRank && board[two].IsEmpty && (allowed & (1UL << two)) != 0)
                    moves.Add(new Move(sq, two, MoveFlags.DoublePawnPush));
            }

            // Both capture targets in ascending square order (as in Attacks.Pawn)
            ulong targets = Bitboards.PawnAttacks[(int)us][sq];
            while (targets != 0)
            {
                int target = Bits.PopLowest(ref targets);
                if ((enemy & (1UL << target)) != 0)
                {
                    if ((allowed & (1UL << target)) != 0) AddPawnMove(sq, target, MoveFlags.Capture, us, moves);
                }
                else if (target == EnPassantSquare && (!legalOnly || EnPassantIsLegal(sq, target)))
                {
                    moves.Add(new Move(sq, target, MoveFlags.Capture | MoveFlags.EnPassant));
                }
            }
        }

        // En passant removes two pawns from one rank at once (which can expose the king sideways) and can
        // capture a checking pawn without landing on its square, so it is checked exactly: is our king
        // attacked after the capture?
        private bool EnPassantIsLegal(int from, int to)
        {
            Side us = SideToMove;
            int captured = to + (us == Side.White ? -8 : 8);
            ulong capturedBit = 1UL << captured;
            ulong occupiedAfter = occupied ^ (1UL << from) ^ (1UL << to) ^ capturedBit;
            ulong enemyAfter = sideBits[(int)us.Opponent()] & ~capturedBit;
            return (AttackersTo(kingSquare[(int)us], occupiedAfter) & enemyAfter) == 0;
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

        // Knight and king: adds the moves to `targets`, in the order of the `order` list
        private static void AddStepMoves(int sq, int[] order, ulong targets, ulong enemy, List<Move> moves)
        {
            for (int i = 0; i < order.Length && targets != 0; i++)
            {
                ulong bit = 1UL << order[i];
                if ((targets & bit) == 0) continue;
                targets ^= bit;
                moves.Add(new Move(sq, order[i], (enemy & bit) != 0 ? MoveFlags.Capture : MoveFlags.None));
            }
        }

        // Sliders: adds the moves to `targets` direction by direction, nearest square first.
        // Even directions (see Attacks) go towards higher squares, odd ones towards lower squares.
        private static void AddSlidingMoves(int sq, ulong targets, int firstDirection, int endDirection, ulong enemy, List<Move> moves)
        {
            for (int d = firstDirection; d < endDirection && targets != 0; d++)
            {
                ulong ray = targets & Bitboards.RayMask[sq * 8 + d];
                targets ^= ray;
                while (ray != 0)
                {
                    int to;
                    if ((d & 1) == 0)
                    {
                        to = Bits.LowestSquare(ray);
                    }
                    else
                    {
                        to = Bits.HighestSquare(ray);
                    }
                    ulong bit = 1UL << to;
                    ray ^= bit;
                    moves.Add(new Move(sq, to, (enemy & bit) != 0 ? MoveFlags.Capture : MoveFlags.None));
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
                    pos.SetSquare(sq, piece);
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
