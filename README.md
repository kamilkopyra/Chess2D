# Chess2D

A 2D chess game built in Unity, together with a chess engine written from scratch in plain C#.
It started as a learning exercise (a two-player chess game) and grew into a series of bots, from a
random-move player (v0) to a bitboard alpha-beta engine with a Texel-tuned evaluation (v25).

![Main window](gallery/game1.png)

## Contents

- [The game](#the-game)
- [The engine](#the-engine)
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
- Play against another person on the same computer or against any bot version (v0 … v25), as White,
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

Experiments that did **not** make it (kept for reference, not in the code):
contempt (scores draws slightly below 0: fewer draws, but ±0 Elo), quiet checks in quiescence (±0),
a bucketed transposition table with ageing and packed entries (−31), the full feature set with a fully tunable
king danger and more threats / material terms (−15; the extra evaluation cost outweighed the gain).

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
| v19 – v25 | not measured against Stockfish yet | | see below |

v19 – v25 were only tested against their predecessors. Head-to-head gains overstate the gain against other
engines: from v14 to v18 the head-to-head SPRTs added up to about +300, while against Stockfish the gain was
about +120. Applying a similar discount to the head-to-head gains of v19 – v25 (about +500 in total) puts
**v25 roughly in the 2550 – 2700 range** on this scale. This is an estimate until v25 plays Stockfish 2400 / 2600.

## How the bots are tested

All matches are played outside Unity through the UCI front-end (`Tools/Chess2D.Uci`) with
[cutechess-cli](https://github.com/cutechess/cutechess) or [fastchess](https://github.com/Disservin/fastchess),
driven by `Tools/match.ps1`. Details and all options are in [Tools/README.md](Tools/README.md).

- **SPRT** (sequential probability ratio test, hypotheses 0 / +30 Elo, 5% error each way): a new version plays
  the previous one until it is statistically clear whether it is at least ~30 Elo stronger (H1) or not stronger (H0).
- **Openings:** with fastchess every game pair starts from the same position of the `8moves_v3` book (each side
  plays it with both colours), the bots' own book is switched off; this removes most of the luck of the opening.
- **Time control:** 20+0.2 for version-vs-version tests, 60+0.6 against Stockfish (its `UCI_Elo` is calibrated there).
- **Analysis scripts** checked draws for missed forced mates and thrown-away wins (which led to v12 and to the
  conclusion that most thrown-away wins are missed tactics, not reluctance to win).

## Tuning the evaluation

`Tools/Chess2D.Tune` fits the evaluation weights (Texel tuning):

1. `extract`: quiet positions (not in check, no winning capture) from PGN files, with the game result.
2. `label` (optional): a Stockfish evaluation of every position (5000 nodes, several processes in parallel).
3. `tune`: finds the scaling constant K, then minimises the squared error between `sigmoid(eval)` and the target
   (game result, or a blend with Stockfish's expected score) with Adam gradient descent. The king danger of the
   full feature set is non-linear (squared) and handled with the chain rule. Writes a C# file with the weights.

The tunable evaluation with its default weights scores exactly like the hand-written one (checked by unit tests),
so tuning always starts from the known evaluation. See [Tools/README.md](Tools/README.md) for the commands.

## Tools and tests

| Tool | Purpose |
|---|---|
| `Tools/Chess2D.Uci` | UCI front-end: any bot can play in chess GUIs and match tools; reports depth, score, nodes, nps |
| `Tools/match.ps1` | Builds the engine and plays matches (Stockfish or another bot, SPRT, opening books, engines built from different branches/commits) |
| `Tools/Chess2D.Tune` | Texel tuning: position extraction, Stockfish labelling, weight fitting |

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
│   │   └── bots/               # Bot_v0 … Bot_v25, Evaluation, TunableEvaluation, tuned weights, BotFactory
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
