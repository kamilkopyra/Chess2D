# Chess2D

A 2D chess game built in Unity, together with a chess engine written from scratch in plain C#.
It started as a learning exercise (a two-player chess game) and grew into a series of bots, from a
random-move player (v0) to a bitboard alpha-beta engine with a Texel-tuned evaluation (v25), then made
faster and pruned harder (v26+).

![Main window](gallery/game1.png)

## Contents

- [The game](#the-game)
- [The engine](#the-engine)
- [How the engine thinks](#how-the-engine-thinks)
- [Bot versions](#bot-versions)
- [Strength (Elo estimates)](#strength-elo-estimates)
- [How the bots are tested](#how-the-bots-are-tested)
- [Tuning the evaluation](#tuning-the-evaluation)
- [Tools and tests](#tools-and-tests)
- [Project structure](#project-structure)
- [Credits](#credits)

## The game

- Full chess rules: castling, en passant, promotion, check, checkmate, stalemate, the 50-move rule,
  threefold repetition and insufficient material
- Play against another person on the same computer or against any bot version (v0 … v28), as White,
  Black or a random colour. Strength settings: search depth (fixed-depth bots) or time per move (bots v11+)
- The bot thinks in the background, the game stays responsive; the depth it reached is shown next to its name
- Premoves (queued while the bot is thinking, played right after its move)
- Analysis marks like on chess.com: right-click a square to mark it, right-drag to draw an arrow
  (knight-shaped for knight jumps); left click clears them
- Legal move hints, last move / selected piece / check highlighting, smooth piece movement
- Main menu, settings, promotion dialog and game-over screen (rematch, view board, back to menu)
- Settings saved between sessions: 6 board themes, 5 piece sets, coordinates and hints on/off,
  opponent, colour and bot strength

### Running the game

Requirements: Unity **6000.6.2f1** (Unity 6), installed through Unity Hub.

1. In Unity Hub go to **Projects → Add → Add project from disk** and pick this folder (`Chess2D`).
   If Hub says "No projects found", select the parent folder instead.
2. Open the project and the scene `Assets/Scenes/SampleScene.unity`.
3. Press **Play**. Choose the opponent in **Settings** (it applies from the next game).

The first launch takes a few minutes while Unity rebuilds the `Library` folder.

| Action | Input |
|---|---|
| Select a piece / move / premove | Left mouse button |
| Mark a square / draw an arrow | Right click / right drag |
| Open the menu during a game | `Esc` or the **Menu** button |

## The engine

`Assets/Scripts/Engine` is a separate assembly (`ChessEngine.asmdef`) with no Unity dependencies, so the
same code runs in the game, in the console tools (UCI front-end, tuner) and in the unit tests.

| Part | What it does |
|---|---|
| `Position` | Board state as bitboards (6 piece types + 2 colours) kept in sync with a 64-square mailbox; make/unmake with an undo stack, null move, Zobrist hash, repetition and 50-move detection, FEN |
| `Bitboards`, `Bits` | Precomputed attack masks, **magic bitboards** for rooks and bishops, bit tricks (popcount, lowest/highest bit) with portable fallbacks for Unity's .NET Standard 2.1 |
| Move generation | Fully legal moves in one pass: checks and pins are found once and only legal moves are generated; a captures-only generator for quiescence search |
| `See` | Static exchange evaluation: the material balance of an exchange on one square, with x-rays |
| `Evaluation`, `TunableEvaluation` | Hand-written evaluation (v10–v22) and the same evaluation with all numbers in one weight array (v23+), in three feature sets: basic, extended, full |
| `OpeningBook` | Opening book built from the Lichess Elite Database (2025-11, 280k games), weighted random moves |
| `Perft` | Move-generator correctness check against known node counts |
| `bots/Bot_vN` | One class per bot version; `BotFactory` finds them by name, so a new version is just a new file |

## How the engine thinks

A description of every technique the engine uses, in the form it has in the current best version (v26),
with the version that introduced it. Numbers in the text are the actual values in the code.

### Board representation

- **Bitboards (v19).** The board is stored as 64-bit integers, one bit per square: one per piece type and one
  per colour. "All white knights" is a single number, "squares attacked by a pawn" is a shift, and questions
  like "is anything between these two squares" are one AND. A 64-square array (mailbox) is kept in sync for
  "what stands on this square".
- **Magic bitboards (v19).** Rook and bishop moves depend on the pieces in the way. For each square, the
  blocking pieces are multiplied by a precomputed "magic" number, and the top bits of the result index a table
  holding the attacked squares for exactly that set of blockers. A slider's moves cost one multiplication and
  one table lookup. The queen is a rook plus a bishop.
- **Legal move generation (v17, on bitboards since v19).** Checks and pins are found once per position. A
  pinned piece may only move along the line of its pin, and in check only moves that capture the checker,
  block the check or move the king are generated. Every generated move is legal; no "make the move and see
  whether the king is attacked" is needed. Quiescence search uses a cheaper captures-only generator and checks
  legality after making the capture.
- **Zobrist hashing (v6).** Every (piece, square), the side to move, castling rights and the en passant file
  have a random 64-bit number. The position's hash is the XOR of those that apply, and it is updated
  incrementally when a move is made. It identifies positions in the transposition table, the evaluation cache
  and repetition detection.
- **Make / unmake.** Moves are made and taken back on one board with an undo stack instead of copying it.

### Search

- **Negamax (v2).** The engine looks ahead a number of plies (half-moves). A position's score is always seen
  from the side to move, so the score of a move is minus the opponent's best score after it.
- **Alpha-beta pruning (v3).** The search keeps a window: alpha (the score we are already sure of) and beta (the
  score the opponent will not allow). As soon as one reply shows that a move is worse than an alternative found
  earlier, the remaining replies are not searched (a cutoff). Same result as a full search, far fewer positions:
  with good move ordering it searches about twice as deep in the same time.
- **Iterative deepening (v5) and time management (v11).** Depth 1, then 2, 3... until the time for the move
  runs out. Each iteration orders moves with what the previous one learned, so the repetition costs little.
  The clock is checked every 2048 nodes; an unfinished iteration is thrown away and the move of the last
  complete one is played. A new depth is not started when more than half of the time is gone (it would not
  finish). When several moves share the best score, one of them is picked at random.
- **Transposition table (v6).** The same position is often reached by different move orders. 2^20 entries
  (about 24 MB) keyed by the Zobrist hash store the score, the depth it was searched to, the kind of score
  (exact, at least, at most: alpha-beta often only proves a bound) and the best move. A stored score is reused
  when it was searched at least as deep as needed now; the best move is tried first in any case. Newer entries
  replace older ones, and the table is kept between moves.
- **Principal variation search (v18).** The first move (expected to be the best thanks to ordering) is searched
  with the full window; every other move only with a null window (alpha, alpha + 1), which answers "is it better
  than alpha?" much faster. Only when the answer is yes is the move searched again with the full window.
- **Quiescence search (v7).** At depth 0 the position is not evaluated straight away when captures are pending:
  only captures and promotions are searched until the position is quiet, otherwise the engine would evaluate
  the middle of an exchange (the horizon effect). The side to move may also "stand pat" (take the static score
  without capturing). In check, every legal reply is searched instead.
- **Check extension (v13).** In check the remaining depth grows by one ply: checks are forcing and often lead
  to tactics that a fixed depth would cut in half.
- **Draws and mates (v8, v12).** A repeated position or the 50-move rule scores 0 inside the search, so the
  engine avoids draws when ahead and looks for them when behind. A mate scores `1 000 000 − distance in plies`,
  so a faster mate is preferred (and a slower defeat); mate scores are stored in the table relative to the
  node so they stay correct when the position is reached at another distance.

### Move ordering

Alpha-beta prunes most when the best move is searched first. Moves are sorted in this order:

1. the best move of the previous iteration (principal variation, v5);
2. the transposition table's best move (v6);
3. good captures, those that don't lose material according to SEE, and promotions; among them "most valuable
   victim, least valuable attacker" (MVV-LVA, v4): pawn takes queen before queen takes pawn;
4. two killer moves per ply (v5): quiet moves that caused a cutoff in a sibling position at the same ply;
5. the other quiet moves by history (v14): every quiet move that causes a cutoff gets `depth²` points for its
   (side, from, to); all values are halved at every new move and whenever one gets too large;
6. losing captures (negative SEE, v20), the worst last.

**Static exchange evaluation (SEE, v20)** plays out all captures on one square in "least valuable attacker
first" order, including pieces behind others (x-rays), and returns the material result without making any
move. It sorts captures, and in quiescence the losing ones are not searched at all.

### Pruning and reductions

These skip or shorten branches that are very unlikely to change the result. Null move, reverse futility,
futility and late move pruning are never used in check, on the principal variation or when the window is near
a mate score; LMR is not used in check.

- **Null move pruning (v13).** "Let the opponent move twice": the side to move passes and the position is
  searched 2 plies shallower (R = 2, from depth 3, only when the static score is already at least beta). If we are still above beta after giving a free move, a real
  move will almost certainly be too, and the node is cut. Not used with only king and pawns (zugzwang, where
  passing would be the best move, is common there) or twice in a row.
- **Reverse futility pruning (v18).** Near the leaves (depth ≤ 3): if the static score minus 120 × depth is
  still at least beta, the node returns beta without searching.
- **Futility pruning (v18).** At depth 1 and 2: if the static score plus 150 (depth 1) or 300 (depth 2) can't
  reach alpha, quiet moves that don't give check are skipped; captures and checks are still searched.
- **Late move pruning (LMP, v21).** At depth ≤ 3, after 3 + depth² quiet moves (4, 7, 12) have been searched,
  the remaining quiet moves that don't give check and aren't killers are skipped: thanks to ordering, a late quiet move is rarely
  the best one.
- **Late move reductions (LMR, v14).** From the 4th move on, at depth ≥ 3, quiet moves (not killers, not
  checks) are searched 1 ply shallower, 2 plies from the 9th move at depth ≥ 6. If such a move unexpectedly
  beats alpha, it is searched again at full depth.
- **Delta pruning (v18).** In quiescence, a capture is skipped when even winning the captured piece plus a
  200 margin wouldn't bring the score up to alpha.
- **Improving (v27, not in v26).** The static score is compared with the one two plies earlier (the same side
  to move). When the position is getting worse, LMP keeps half as many quiet moves and LMR reduces one ply
  more; when it is improving, reverse futility cuts with a smaller margin.

### Evaluation

The static evaluation scores a quiet position in centipawns (100 = one pawn) from the side to move's view.

- **Tapered evaluation (v10).** Every term has a middlegame and an endgame value. The game phase is 0 – 24 from
  the material on the board (knight and bishop 1, rook 2, queen 4) and the two scores are blended by it, so
  e.g. the king wants shelter in the middlegame and the centre in the endgame without a sudden switch.
- **Material and piece-square tables (v10).** A value per piece and a bonus or penalty for every piece on
  every square (knights in the centre, pawns advancing, the king behind its pawns).
- **Pawn structure (v15, v25).** Passed pawns by rank; isolated, doubled and backward pawns; pawns side by side
  (phalanx) and defended pawns; passed pawn details: blocked, free path to promotion, distance of both kings,
  a rook behind it.
- **Pieces (v15, v22, v25).** Bishop pair, bad bishops (own pawns on their colour), trapped bishops and rooks,
  rooks on open and half-open files and on the 7th rank, knight outposts (protected by a pawn, unreachable for
  enemy pawns), minor pieces behind pawns, mobility (safe squares) with a separate value for each number of
  squares per piece type, space behind the own pawn chain, a bonus for having the move (tempo).
- **King safety (v16, v22, v25).** Pawn shelter in front of the king and open files next to it; enemy pawn
  storms; weak squares around the king; attacks on the king zone added up into attack units and turned into
  a penalty through a tuned 100-entry table (only with two or more attackers); squares from which each enemy
  piece type could give a safe check.
- **Threats (v22).** A pawn attacking a piece, minor pieces attacking rooks or queens, undefended pieces under
  attack.
- **Drawish endgames (v25).** The endgame part is scaled down with opposite-coloured bishops (to 1/2 without
  other pieces, 3/4 with them) and when the stronger side has no pawns and is less than a rook up (to 1/4).
- **Mop-up (v12).** With at least 400 points more material (e.g. a rook) against a side without pawns, a bonus drives the losing king to
  the edge (10 per square of distance from the centre) and brings the winning king closer (4 per square of
  distance saved), so the engine can mate with K+Q or K+R against K. Against a lone king the activity terms
  are left out, they only distract from the mating plan.
- **Texel tuning (v23 – v25).** All ~1300 numbers above live in one weight array and were fitted to 2.5M
  positions from real games (see [Tuning the evaluation](#tuning-the-evaluation)).

### Speed

- **Evaluation cache (v26).** 2^18 entries: position hash → score. With iterative deepening and transpositions
  the same positions are evaluated again and again; each is computed once.
- **Pawn cache (v26).** 2^16 entries keyed by both sides' pawn bitboards: the pawn-only terms (passed, isolated,
  doubled, backward pawns and the like, space). Pawns move rarely, so most evaluations find their structure.
- **No allocations in the search (v17).** Move lists and score arrays are allocated once per ply and reused.
- **Opening book (v9).** In the game the first moves come from a book built from the Lichess Elite Database
  (280k games), picked at random weighted by how often they were played. Engine tests with fastchess switch it
  off and start from fixed opening positions instead.

## Bot versions

Every version is the previous one plus the listed change. "SPRT" means a sequential test against the previous
version that stopped once the result was statistically clear (see [How the bots are tested](#how-the-bots-are-tested));
"+X Elo" is the measured difference against the previous version, not an absolute rating.

### Search basics (v0 – v11)

| Version | What was added | Result vs previous |
|---|---|---|
| **v0** | Random legal move | – |
| **v1** | Looks one move ahead and takes the best material balance (mate in one first) | – |
| **v2** | Negamax search to a fixed depth, material evaluation, mate and stalemate detection | 10-0 vs v1 (depth 3) |
| **v3** | Alpha-beta pruning: same moves as v2 at the same depth, but far fewer positions searched, so it can go deeper | same depth: +7 (equal, as expected); depth 5 vs v2 depth 4: 93-1-6, ≈ +550 |
| **v4** | Move ordering: captures first, most valuable victim / least valuable attacker (MVV-LVA) | depth 6 vs v3 depth 5: 78-10-12, ≈ +290 |
| **v5** | Iterative deepening with the principal variation tried first, killer moves | same play as v4, only faster |
| **v6** | Transposition table (Zobrist hashing, exact / lower / upper bounds, best move reused for ordering) | same play, faster; reaches deeper |
| **v7** | Quiescence search: captures are played out before evaluating, no more horizon blunders mid-exchange | depth 6 vs v6 depth 6: 67-1-32, ≈ +275 |
| **v8** | Draws inside the search (repetition, 50-move rule): avoids them when ahead, seeks them when behind | +21 |
| **v9** | Opening book (Lichess Elite) | +17 |
| **v10** | Positional evaluation: piece-square tables, middlegame / endgame blend by game phase | 93-1-6, ≈ +550 |
| **v11** | Time management: iterative deepening until the time budget runs out, unfinished iterations discarded | +56 ± 45 |

### Stronger search (v12 – v21)

| Version | What was added | Result vs previous |
|---|---|---|
| **v12** | Mating: mate distance in the score (faster mates preferred), mop-up evaluation for K+Q / K+R vs K | +21 ± 45; v11 threw away 14 forced mates in its draws, v12 none |
| **v13** | Check extension (search one ply deeper in check) and null move pruning | +131 ± 65 |
| **v14** | History heuristic (quiet moves ordered by past cutoffs) and late move reductions (LMR) | +108 ± 60 |
| **v15** | Pawn structure and piece placement: passed, doubled, isolated pawns, bishop pair, rooks on open files and the 7th rank | SPRT +69 ± 46 |
| **v16** | King safety: pawn shelter in front of the king, open files next to it | SPRT +33 ± 26 |
| **v17** | Speed: faster legal move generation (pins), no allocations in the search, captures-only generator for quiescence, one-pass evaluation | SPRT +111 ± 64 |
| **v18** | Principal variation search (PVS), reverse futility, futility and delta pruning | SPRT +90 ± 55 |
| **v19** | Bitboards and magic bitboards (same search and evaluation as v18, about 2.5× faster) | SPRT +186 ± 94 vs v18 on the old board |
| **v20** | Static exchange evaluation: losing captures ordered last and skipped in quiescence | SPRT +61 ± 42 |
| **v21** | Late move pruning (LMP): near the leaves, late quiet moves are skipped entirely | SPRT +34 ± 27 |

### Better evaluation (v22 – v25)

| Version | What was added | Result vs previous |
|---|---|---|
| **v22** | Piece activity: mobility (safe squares), attacks on the enemy king zone, threats (pawn attacks a piece, minor attacks rook/queen, hanging pieces), knight outposts | SPRT +42 ± 32 |
| **v23** | Texel tuning: all ~830 evaluation weights fitted to the results of 1.5M positions from Lichess Elite games and own test games | SPRT +36 ± 29 |
| **v24** | The same features re-tuned on 2.5M positions with better targets: half the game result, half Stockfish's evaluation of the position | SPRT +50 ± 37 |
| **v25** | Extended feature set, ~1300 tuned weights: tempo, mobility per number of squares, a king danger table, safe checks, pawn storms, weak squares around the king, backward / phalanx / supported pawns, passed pawn details (blocked, free path, king distances, rook behind), bad and trapped bishops, trapped rooks, minor pieces behind pawns, space, scale-down of drawish endgames (opposite-coloured bishops, no pawns and less than a rook up) | SPRT +106 ± 60 (stopped after 91 games, so the exact number is likely too high) |

### Speed and pruning (v26 – v28)

From here on a version is tested against the version it is built on (named in the table), with SPRT 0 / +15.

| Version | What was added | Result |
|---|---|---|
| **v26** | v25 made faster without changing its play: an evaluation cache (position hash → score) and a pawn cache (the pawn-only terms stored per pawn structure); about 1.3× faster, identical search tree | SPRT +57 ± 31 vs v25 |
| **v27** | "Improving": the static score is compared with the one two plies earlier; when the position is getting worse, LMP keeps half as many quiet moves and LMR reduces one ply more, when it is improving reverse futility cuts with a smaller margin; about 1.5× fewer nodes to the same depth. First tested on top of v25, then moved onto v26 | +26 ± 19 on top of v25 (SPRT); on top of v26 not decided: −8 ± 45 head-to-head (164 games, stopped), +15 against Stockfish 2800 (see below) |
| **v28** | v27 with the full feature set (tunable king danger inputs, threats by each piece type, pawn-dependent knight / rook values, rook pair, king distance to pawns), which lost as a v25 variant only because of its cost. On top of v26 the same change measured +13 ± 17 after 902 games (stopped, a small gain) | against Stockfish 2800 no better than v26 (see below) |

**v26 is the current best tested version.** v27 and v28 stay in the code but were not confirmed: the gain of
"improving" shrank once v26 already searched deeper, and the full feature set doesn't pay for its cost.

Experiments that did **not** make it (kept for reference, not in the code):
contempt (scores draws slightly below 0: fewer draws, but ±0 Elo), quiet checks in quiescence (±0),
a bucketed transposition table with ageing and packed entries (−31), the full feature set with a fully tunable
king danger and more threats / material terms (−15 on top of v25; with the caches it became v28), lazy
evaluation in quiescence (skip the full score when material and piece-square tables alone are 400 cp outside the
window: −6 ± 20, the time saved was too small).

## Strength (Elo estimates)

Ratings are on the **Stockfish `UCI_Elo` scale** (Stockfish 19 with limited strength), 60+0.6 seconds per game,
100 games per match unless noted. They are not FIDE or online ratings, and ±65 is the typical margin of one match.

| Version | Opponent | Score (W-L-D) | Estimate |
|---|---|---|---|
| v2 (depth 4) | Stockfish 1320 | 28-252-3 | ≈ 950 (below Stockfish's minimum, rough) |
| v3 (depth 5) | Stockfish 1320 | 31-63-6 | ≈ 1200 |
| v10 (depth 6) | Stockfish 1800 | 47-42-11 | ≈ 1820 |
| v11 | Stockfish 2000 | 46-34-20 | ≈ 2040 |
| v12 | Stockfish 2000 / 2200 | 46-50-4 / 38-54-8 | ≈ 1990 – 2140 |
| v14 | Stockfish 2400 | 30-61-9 | ≈ 2290 |
| v15 | Stockfish 2400 | 44-50-6 | ≈ 2380 |
| v16 | Stockfish 2400 | 46-44-10 | ≈ 2405 |
| v17 | Stockfish 2400 | 48-41-11 | ≈ 2425 |
| v18 | Stockfish 2400 | 48-45-7 | ≈ 2410 |

From v19 on the matches were played with fastchess and the balanced `8moves_v3` openings (each opening twice,
colours reversed) instead of cutechess with the bots' own book:

| Version | Opponent | Score (W-L-D) | Estimate |
|---|---|---|---|
| v18 (bridge to the old setup) | Stockfish 2400 | 58-37-5 | ≈ 2475 |
| v19 | Stockfish 2600 | 42-44-14 | ≈ 2595 |
| v20 | Stockfish 2600 | 49-34-17 | ≈ 2655 |
| v21 | Stockfish 2600 | 45-44-11 | ≈ 2605 |
| v22 | Stockfish 2600 | 57-25-18 | ≈ 2715 |
| v23 | Stockfish 2600 | 64-22-14 | ≈ 2755 |
| v24 | Stockfish 2600 | 60-21-19 | ≈ 2745 |
| v25 (250 games) | Stockfish 2600 | 173-44-33 | ≈ 2800 (± 46) |
| v25 (250 games) | Stockfish 2800 | 60-107-83 | ≈ 2735 (± 36) |
| v26 (250 games) | Stockfish 2800 | 74-108-68 | ≈ 2752 (± 37) |
| v27 (250 games) | Stockfish 2800 | 77-101-72 | ≈ 2767 (± 37) |
| v28 (250 games) | Stockfish 2800 | 67-105-78 | ≈ 2747 (± 36) |

**v25 is about 2750 – 2760** on this scale (both v25 matches combined, weighted by their error), and
**v26 – v28 are about 2750 – 2770**: the differences between them are smaller than one match can separate. The match
against the closer opponent (Stockfish 2800) is the more reliable one: the `UCI_Elo` scale is not perfectly
linear, so a result far from 50% against a weaker setting overstates the rating a little.

The v18 bridge (≈ 2475 here, ≈ 2410 in the old setup) is within one match's error, so the two tables are
roughly comparable. Neighbouring versions (v19 – v21, v23 – v24) are closer to each other than one match can
separate; the head-to-head SPRTs are the better guide for those steps. Overall v18 → v25 gained about +300
against Stockfish.

## How the bots are tested

All matches are played outside Unity through the UCI front-end (`Tools/Chess2D.Uci`) with
[cutechess-cli](https://github.com/cutechess/cutechess) or [fastchess](https://github.com/Disservin/fastchess),
driven by `Tools/match.ps1`. Details and all options are in [Tools/README.md](Tools/README.md).

- **SPRT** (sequential probability ratio test, 5% error each way): a new version plays the previous one until it is
  statistically clear whether it is stronger (H1) or not (H0). Hypotheses 0 / +30 Elo up to v25; from v26 on
  0 / +15, which detects smaller gains at the cost of longer tests (up to 2000 games).
- **Openings:** with fastchess every game pair starts from the same position of the `8moves_v3` book (each side
  plays it with both colours), the bots' own book is switched off; this removes most of the luck of the opening.
- **Time control:** 20+0.2 for version-vs-version tests, 60+0.6 against Stockfish (its `UCI_Elo` is calibrated there).
- **Analysis scripts** checked draws for missed forced mates and thrown-away wins (which led to v12 and to the
  conclusion that most thrown-away wins are missed tactics, not reluctance to win).

## Tuning the evaluation

`Tools/Chess2D.Tune` fits the evaluation weights (Texel tuning):

1. `extract`: quiet positions (not in check, no winning capture) from PGN files or a stream (a compressed Lichess
   database month through `zstd -dc`), with the game result; filters for rating and time control, duplicates removed.
2. `label` (optional): a Stockfish evaluation of every position (5000 nodes, several processes in parallel,
   streamed, resumable; about 1300 positions/s on 20 threads of an i7-14700HX).
3. `tune`: finds the scaling constant K, then minimises the squared error between `sigmoid(eval)` and the target
   (game result, or a blend with Stockfish's expected score) with Adam gradient descent. The king danger of the
   full feature set is non-linear (squared) and handled with the chain rule. Writes a C# file with the weights.

The tunable evaluation with its default weights scores exactly like the hand-written one (checked by unit tests),
so tuning always starts from the known evaluation. See [Tools/README.md](Tools/README.md) for the commands.

## Next steps

1. **NNUE**: replace the hand-written evaluation with a small neural network (768 inputs → 256 → 1, updated
   incrementally as moves are made) trained on about 100M positions from one month of Lichess games (rated
   1800+ plus a share of weaker games, no bullet), each labelled by Stockfish at 5000 nodes. The data is being
   produced on a second machine (`Tools/worker`), training will run in PyTorch on the GPU.
2. **SPSA**: tune the search parameters (futility / reverse futility margins, LMR and LMP, null move, delta
   pruning) together by playing many short games, after the network is in, since the margins depend on the
   evaluation's scale.

## Tools and tests

| Tool | Purpose |
|---|---|
| `Tools/Chess2D.Uci` | UCI front-end: any bot can play in chess GUIs and match tools; reports depth, score, nodes, nps |
| `Tools/match.ps1` | Builds the engine and plays matches (Stockfish or another bot, SPRT, opening books, engines built from different branches/commits) |
| `Tools/Chess2D.Tune` | Texel tuning: position extraction, Stockfish labelling, weight fitting |
| `Tools/worker` | Scripts for a Linux machine that produces training data unattended (download, extract, label, compress) with a status page |

Unit tests (NUnit, Unity Test Runner → EditMode) cover perft results, FEN, hashing, repetitions, the bitboard
move generator against the reference generator, magic bitboards, SEE, opening book, evaluation symmetry
(mirrored positions score the same) and the tunable evaluation against the hand-written one.

## Project structure

```
Assets/
├── Scripts/
│   ├── Engine/                 # the chess engine (no Unity references)
│   │   ├── Position.cs         # board, bitboards, make/unmake, move generation, FEN
│   │   ├── Bitboards.cs, Bits.cs, Attacks.cs, Zobrist.cs, Types.cs
│   │   ├── See.cs, Perft.cs, OpeningBook.cs
│   │   └── bots/               # Bot_v0 … Bot_v28, Evaluation, TunableEvaluation, tuned weights, BotFactory
│   ├── PieceMover.cs           # input, moves, bot opponent (background search), premoves, game end
│   ├── PieceView.cs            # a piece on the board (sprite, animation)
│   ├── BoardCreator.cs         # board, coordinates, themes, piece skins
│   ├── BoardAnnotations.cs     # right-click marks and arrows
│   ├── LightAvaliableMoves.cs  # highlights (hints, last move, check, premove)
│   ├── UIManager.cs            # all UI (UI Toolkit), created at runtime
│   ├── GameSettings.cs         # settings saved in PlayerPrefs
│   └── SpriteFactory.cs, EndGame.cs
├── Resources/
│   ├── Books/elite_book.txt    # opening book
│   ├── Pieces/<set>/           # piece sprites (see LICENSES.md)
│   └── UI/                     # stylesheet, theme, font
├── Tests/EditMode/             # NUnit tests of the engine
└── Scenes/SampleScene.unity
Tools/                          # UCI front-end, match script, tuner (see Tools/README.md)
```

## Credits

- Piece sets come from [lichess](https://github.com/lichess-org/lila) and keep their original licenses
  (GPLv2+, CC BY-NC-SA 4.0, Apache 2.0, AGPLv3+). Details are in `Assets/Resources/Pieces/LICENSES.md`.
- Font: [Poppins](https://fonts.google.com/specimen/Poppins), SIL Open Font License (`Assets/Resources/UI/Fonts/OFL.txt`).
- Opening book and tuning positions: [Lichess Elite Database](https://database.nikonoel.fr/).
- Test openings: [official-stockfish/books](https://github.com/official-stockfish/books) (`8moves_v3`).
- Testing and labelling: [Stockfish](https://stockfishchess.org/), [cutechess](https://github.com/cutechess/cutechess),
  [fastchess](https://github.com/Disservin/fastchess).
- Piece-square tables of the hand-written evaluation start from the
  [Simplified Evaluation Function](https://www.chessprogramming.org/Simplified_Evaluation_Function).
