using System.Collections.Generic;
using NUnit.Framework;

namespace ChessEngine.Tests
{
    public class BitboardTests
    {
        static readonly string[] Fens =
        {
            Position.StartFen,
            "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
            "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1",
            "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1",
            "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8",
            "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10",
            // En passant that would expose the king along the rank, and en passant out of check
            "8/8/8/K2pP2q/8/8/8/7k w - d6 0 1",
            "8/8/8/3pP3/4K3/8/8/7k w - d6 0 1",
        };

        [Test]
        public void PortableBitHelpersMatchRuntimeOnes()
        {
            var random = new System.Random(7);
            for (int i = 0; i < 100000; i++)
            {
                ulong bits = NextRandom(random);
                // Also sparse numbers and single bits
                if (i % 3 == 1) bits &= NextRandom(random) & NextRandom(random);
                if (i % 3 == 2) bits = 1UL << random.Next(64);
                if (bits == 0) continue;

                int count = 0, lowest = -1, highest = -1;
                for (int sq = 0; sq < 64; sq++)
                {
                    if ((bits & (1UL << sq)) == 0) continue;
                    count++;
                    if (lowest < 0) lowest = sq;
                    highest = sq;
                }

                Assert.AreEqual(count, Bits.PopCount(bits));
                Assert.AreEqual(count, Bits.PopCountPortable(bits));
                Assert.AreEqual(lowest, Bits.LowestSquare(bits));
                Assert.AreEqual(lowest, Bits.LowestSquarePortable(bits));
                Assert.AreEqual(highest, Bits.HighestSquare(bits));
                Assert.AreEqual(highest, Bits.HighestSquarePortable(bits));
            }
            Assert.AreEqual(0, Bits.PopCountPortable(0));
            Assert.AreEqual(64, Bits.PopCountPortable(ulong.MaxValue));
        }

        [Test]
        public void MagicAttacksMatchRayWalk()
        {
            var random = new System.Random(3);
            for (int sq = 0; sq < 64; sq++)
            {
                for (int i = 0; i < 2000; i++)
                {
                    ulong occupied = NextRandom(random);
                    if (i % 2 == 1) occupied &= NextRandom(random);
                    if (i % 4 == 3) occupied &= NextRandom(random);
                    if (i == 0) occupied = 0;

                    Assert.AreEqual(Bitboards.SlidingAttacksSlow(sq, occupied, false), Bitboards.RookAttacks(sq, occupied));
                    Assert.AreEqual(Bitboards.SlidingAttacksSlow(sq, occupied, true), Bitboards.BishopAttacks(sq, occupied));
                    Assert.AreEqual(Bitboards.SlidingAttacksSlow(sq, occupied, false) | Bitboards.SlidingAttacksSlow(sq, occupied, true),
                                    Bitboards.QueenAttacks(sq, occupied));
                }
            }
        }

        [Test]
        public void BetweenAndLineMasks()
        {
            Assert.AreEqual(Bit("b2") | Bit("c3"), Bitboards.Between[Square.Parse("a1") * 64 + Square.Parse("d4")]);
            Assert.AreEqual(Bit("f1") | Bit("g1"), Bitboards.Between[Square.Parse("h1") * 64 + Square.Parse("e1")]);
            Assert.AreEqual(0UL, Bitboards.Between[Square.Parse("a1") * 64 + Square.Parse("b3")]);
            Assert.AreEqual(Bitboards.Files[4], Bitboards.Line[Square.Parse("e2") * 64 + Square.Parse("e7")]);
            Assert.AreEqual(Bitboards.Ranks[0], Bitboards.Line[Square.Parse("c1") * 64 + Square.Parse("d1")]);
            Assert.AreEqual(0UL, Bitboards.Line[Square.Parse("a1") * 64 + Square.Parse("b3")]);
        }

        // Random walks that make moves, unmake them and make/unmake null moves: the bitboards must always
        // match the mailbox, and a clone must match too
        [Test]
        public void BitboardsStayConsistentWithMailbox()
        {
            var random = new System.Random(11);
            foreach (string fen in Fens)
            {
                var position = Position.FromFen(fen);
                Assert.IsTrue(position.BitboardsAreConsistent(), fen);

                var played = new List<bool>(); // true = null move
                for (int step = 0; step < 2000; step++)
                {
                    int action = random.Next(10);
                    if (action < 3 && played.Count > 0)
                    {
                        if (played[played.Count - 1]) position.UnmakeNullMove();
                        else position.UnmakeMove();
                        played.RemoveAt(played.Count - 1);
                    }
                    else if (action == 3 && !position.InCheck)
                    {
                        position.MakeNullMove();
                        played.Add(true);
                    }
                    else
                    {
                        var moves = position.GetLegalMoves();
                        if (moves.Count == 0 || played.Count > 200)
                        {
                            position = Position.FromFen(fen);
                            played.Clear();
                            continue;
                        }
                        position.MakeMove(moves[random.Next(moves.Count)]);
                        played.Add(false);
                    }

                    Assert.IsTrue(position.BitboardsAreConsistent(), $"{position.ToFen()} after step {step}");
                    Assert.IsTrue(position.HashIsConsistent(), $"{position.ToFen()} after step {step}");
                    if (step % 50 == 0) Assert.IsTrue(position.Clone().BitboardsAreConsistent());
                }

                while (played.Count > 0)
                {
                    if (played[played.Count - 1]) position.UnmakeNullMove();
                    else position.UnmakeMove();
                    played.RemoveAt(played.Count - 1);
                }
                Assert.AreEqual(Position.FromFen(fen).ToFen(), position.ToFen());
                Assert.IsTrue(position.BitboardsAreConsistent());
            }
        }

        // The bitboard generators must give exactly the same moves IN THE SAME ORDER as the old mailbox
        // generator (MailboxReference below): bots sort moves with a stable sort, so the order of equally
        // scored moves decides which one is searched first, and older bots must play exactly the same chess.
        [Test]
        public void GeneratorsMatchMailboxReferenceExactly()
        {
            var random = new System.Random(5);
            var fast = new List<Move>();
            var scratch = new List<Move>();
            var actual = new List<Move>();

            foreach (string fen in Fens)
            {
                for (int game = 0; game < 10; game++)
                {
                    var position = Position.FromFen(fen);
                    for (int ply = 0; ply < 120; ply++)
                    {
                        string where = position.ToFen();

                        // The pseudo-legal generators append to the list (they don't clear it)
                        var pseudo = MailboxReference.PseudoLegal(position, false);
                        actual.Clear();
                        position.GeneratePseudoLegalMoves(actual);
                        CollectionAssert.AreEqual(pseudo, actual, $"pseudo-legal in {where}");

                        actual.Clear();
                        position.GeneratePseudoLegalCaptures(actual);
                        CollectionAssert.AreEqual(MailboxReference.PseudoLegal(position, true), actual, $"captures in {where}");

                        var legal = MailboxReference.Legal(position);
                        CollectionAssert.AreEqual(legal, position.GetLegalMoves(), $"legal in {where}");
                        position.GenerateLegalMovesFast(fast, scratch);
                        CollectionAssert.AreEqual(legal, fast, $"fast legal in {where}");
                        Assert.AreEqual(legal.Count > 0, position.HasAnyLegalMove(), where);

                        ulong occupied = position.Occupied;
                        for (int sq = 0; sq < 64; sq++)
                        {
                            ulong attackers = position.AttackersTo(sq, occupied);
                            for (int side = 0; side < 2; side++)
                            {
                                bool expected = MailboxReference.IsSquareAttacked(position, sq, (Side)side);
                                if (expected != position.IsSquareAttacked(sq, (Side)side)
                                    || expected != ((attackers & position.Pieces((Side)side)) != 0))
                                {
                                    Assert.Fail($"attacks on {Square.ToName(sq)} by {(Side)side} in {where}");
                                }
                            }
                        }

                        if (legal.Count == 0) break;
                        position.MakeMove(legal[random.Next(legal.Count)]);
                    }
                }
            }
        }

        [Test]
        public void AttackersToFindsAllAttackersAndXrays()
        {
            // d4 is attacked by: white rook a4, knight e2, pawn e3; black bishop a7, queen d8, king e5.
            // The white queen on f2 is behind the pawn on e3.
            var position = Position.FromFen("3q4/b7/8/4k3/R7/4P3/4NQ2/4K3 w - - 0 1");
            int d4 = Square.Parse("d4");
            ulong expected = Bit("a4") | Bit("e2") | Bit("e3") | Bit("a7") | Bit("d8") | Bit("e5");
            Assert.AreEqual(expected, position.AttackersTo(d4, position.Occupied));

            // With the pawn removed from the occupancy the queen behind it shows up (x-ray, as SEE will use it)
            ulong withoutPawn = position.Occupied & ~Bit("e3");
            Assert.AreEqual(expected | Bit("f2"), position.AttackersTo(d4, withoutPawn));
        }

        static ulong Bit(string square) => 1UL << Square.Parse(square);

        static ulong NextRandom(System.Random random)
        {
            var bytes = new byte[8];
            random.NextBytes(bytes);
            return System.BitConverter.ToUInt64(bytes, 0);
        }

        // The mailbox move generator from before bitboards (Bot_v18 era), working only through the public
        // indexer. Kept here as the reference for the exact set and order of moves.
        static class MailboxReference
        {
            public static List<Move> Legal(Position position)
            {
                var result = new List<Move>();
                Side us = position.SideToMove;
                foreach (Move move in PseudoLegal(position, false))
                {
                    position.MakeMove(move);
                    if (!IsSquareAttacked(position, position.KingSquare(us), us.Opponent())) result.Add(move);
                    position.UnmakeMove();
                }
                return result;
            }

            public static bool IsSquareAttacked(Position position, int square, Side by)
            {
                foreach (int sq in Attacks.Pawn[(int)by.Opponent()][square])
                    if (position[sq].Is(PieceType.Pawn, by)) return true;
                foreach (int sq in Attacks.Knight[square])
                    if (position[sq].Is(PieceType.Knight, by)) return true;
                foreach (int sq in Attacks.King[square])
                    if (position[sq].Is(PieceType.King, by)) return true;

                int[][] rays = Attacks.Rays[square];
                for (int d = 0; d < 8; d++)
                {
                    PieceType slider = d < Attacks.FirstDiagonal ? PieceType.Rook : PieceType.Bishop;
                    foreach (int sq in rays[d])
                    {
                        Piece p = position[sq];
                        if (p.IsEmpty) continue;
                        if (p.Color == by && (p.Type == slider || p.Type == PieceType.Queen)) return true;
                        break;
                    }
                }
                return false;
            }

            public static List<Move> PseudoLegal(Position position, bool capturesOnly)
            {
                var moves = new List<Move>();
                Side us = position.SideToMove, them = us.Opponent();

                for (int sq = 0; sq < 64; sq++)
                {
                    Piece p = position[sq];
                    if (p.IsEmpty || p.Color != us) continue;

                    switch (p.Type)
                    {
                        case PieceType.Pawn: Pawn(position, sq, us, them, moves, capturesOnly); break;
                        case PieceType.Knight: Step(position, sq, Attacks.Knight[sq], them, moves, capturesOnly); break;
                        case PieceType.King: Step(position, sq, Attacks.King[sq], them, moves, capturesOnly); break;
                        case PieceType.Bishop: Slide(position, sq, 4, 8, them, moves, capturesOnly); break;
                        case PieceType.Rook: Slide(position, sq, 0, 4, them, moves, capturesOnly); break;
                        case PieceType.Queen: Slide(position, sq, 0, 8, them, moves, capturesOnly); break;
                    }
                }

                if (!capturesOnly) Castling(position, us, them, moves);
                return moves;
            }

            static void Pawn(Position position, int sq, Side us, Side them, List<Move> moves, bool capturesOnly)
            {
                int forward = us == Side.White ? 8 : -8;
                int startRank = us == Side.White ? 1 : 6;
                int lastRank = us == Side.White ? 7 : 0;

                int one = sq + forward;
                if (position[one].IsEmpty && (!capturesOnly || Square.Rank(one) == lastRank))
                {
                    AddPawn(sq, one, MoveFlags.None, us, moves);
                    int two = one + forward;
                    if (!capturesOnly && Square.Rank(sq) == startRank && position[two].IsEmpty)
                        moves.Add(new Move(sq, two, MoveFlags.DoublePawnPush));
                }

                foreach (int target in Attacks.Pawn[(int)us][sq])
                {
                    Piece victim = position[target];
                    if (!victim.IsEmpty && victim.Color == them)
                        AddPawn(sq, target, MoveFlags.Capture, us, moves);
                    else if (target == position.EnPassantSquare)
                        moves.Add(new Move(sq, target, MoveFlags.Capture | MoveFlags.EnPassant));
                }
            }

            static void AddPawn(int from, int to, MoveFlags flags, Side us, List<Move> moves)
            {
                if (Square.Rank(to) == (us == Side.White ? 7 : 0))
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

            static void Step(Position position, int sq, int[] targets, Side them, List<Move> moves, bool capturesOnly)
            {
                foreach (int target in targets)
                {
                    Piece p = position[target];
                    if (p.IsEmpty)
                    {
                        if (!capturesOnly) moves.Add(new Move(sq, target));
                    }
                    else if (p.Color == them) moves.Add(new Move(sq, target, MoveFlags.Capture));
                }
            }

            static void Slide(Position position, int sq, int first, int end, Side them, List<Move> moves, bool capturesOnly)
            {
                for (int d = first; d < end; d++)
                {
                    foreach (int target in Attacks.Rays[sq][d])
                    {
                        Piece p = position[target];
                        if (p.IsEmpty)
                        {
                            if (!capturesOnly) moves.Add(new Move(sq, target));
                            continue;
                        }
                        if (p.Color == them) moves.Add(new Move(sq, target, MoveFlags.Capture));
                        break;
                    }
                }
            }

            static void Castling(Position position, Side us, Side them, List<Move> moves)
            {
                int baseSq = us == Side.White ? 0 : 56;
                int kingFrom = baseSq + 4;
                if (!position[kingFrom].Is(PieceType.King, us)) return;

                CastlingRights kingside = us == Side.White ? CastlingRights.WhiteKingside : CastlingRights.BlackKingside;
                CastlingRights queenside = us == Side.White ? CastlingRights.WhiteQueenside : CastlingRights.BlackQueenside;
                if ((position.Castling & (kingside | queenside)) == 0) return;
                if (IsSquareAttacked(position, kingFrom, them)) return;

                if ((position.Castling & kingside) != 0
                    && position[baseSq + 7].Is(PieceType.Rook, us)
                    && position[baseSq + 5].IsEmpty && position[baseSq + 6].IsEmpty
                    && !IsSquareAttacked(position, baseSq + 5, them) && !IsSquareAttacked(position, baseSq + 6, them))
                {
                    moves.Add(new Move(kingFrom, baseSq + 6, MoveFlags.CastleKingside));
                }

                if ((position.Castling & queenside) != 0
                    && position[baseSq].Is(PieceType.Rook, us)
                    && position[baseSq + 1].IsEmpty && position[baseSq + 2].IsEmpty && position[baseSq + 3].IsEmpty
                    && !IsSquareAttacked(position, baseSq + 3, them) && !IsSquareAttacked(position, baseSq + 2, them))
                {
                    moves.Add(new Move(kingFrom, baseSq + 2, MoveFlags.CastleQueenside));
                }
            }
        }
    }
}
