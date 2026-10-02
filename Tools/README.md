# Engine matches and Elo

Tools for measuring the strength of the Chess2D bots outside Unity.

- `Chess2D.Uci/` – a console [UCI](https://www.chessprogramming.org/UCI) front-end. It compiles the engine sources
  from `Assets/Scripts/Engine` directly, so it always plays with the current version of the bots.
- `match.ps1` – plays a match with [cutechess-cli](https://github.com/cutechess/cutechess) and prints the Elo estimate.

## Requirements

- .NET SDK (10 or newer) – `dotnet --version`
- Internet access on the first run: cutechess-cli and [Stockfish](https://stockfishchess.org) are downloaded
  into `Tools/external/` (ignored by git)

## Running a match

From the `Tools` folder in PowerShell:

```powershell
# v2 at depth 3 vs Stockfish limited to 1320 Elo, 100 games
powershell -ExecutionPolicy Bypass -File .\match.ps1

# choose the bot, depth, opponent strength and number of games
powershell -ExecutionPolicy Bypass -File .\match.ps1 -Bot v2 -Depth 4 -StockfishElo 1500 -Games 200

# bot vs bot, e.g. to check that a new version beats the previous one
powershell -ExecutionPolicy Bypass -File .\match.ps1 -Bot v2 -Depth 3 -OpponentBot v1 -OpponentDepth 3

# SPRT: play until it's statistically clear whether v16 is better than v15 (at most 1000 games)
powershell -ExecutionPolicy Bypass -File .\match.ps1 -Bot v16 -OpponentBot v15 -Sprt -Games 1000 -TimeControl 20+0.2 -Concurrency 8
```

| Parameter | Default | Meaning |
|---|---|---|
| `-Bot` | `v2` | bot to test (`Bot_v2` → `v2`); new `Bot_vN` classes are picked up automatically |
| `-Depth` | `3` | search depth, for bots that take one in the constructor |
| `-OpponentBot` / `-OpponentDepth` | – | play against another Chess2D bot instead of Stockfish |
| `-StockfishElo` | `1320` | Stockfish strength (`UCI_Elo`, minimum 1320) |
| `-Games` | `100` | number of games (each opening is played with both colours) |
| `-TimeControl` | `60+0.6` | cutechess time control; Stockfish's `UCI_Elo` is calibrated at 60+0.6 |
| `-Concurrency` | `4` | games played in parallel |
| `-MaxMoves` | `200` | games longer than this are adjudicated as draws |
| `-Sprt` | off | stop the match as soon as the result is statistically clear; `-Games` becomes the upper limit |
| `-Elo0` / `-Elo1` | `0` / `10` | SPRT hypotheses: H0 "not stronger than Elo0", H1 "stronger by at least Elo1" |
| `-BotSource` / `-OpponentSource` | this repository | build that engine from another copy of the repository (another branch or commit, e.g. a `git worktree`) |
| `-BotLabel` / `-OpponentLabel` | – | suffix for the engine name in the PGN/log, e.g. `-OpponentLabel old` → `Chess2D-v18-old` |
| `-Fastchess` | off | play with [fastchess](https://github.com/Disservin/fastchess) instead of cutechess-cli: games start from an opening book, each opening is played once with each colour, the bots' own book is off, SPRT statistics count game pairs (fewer games needed) |
| `-Openings` | see below | opening book for `-Fastchess`: a file name in `Tools/external/openings` or a full path. Default: `UHO_4060_v2.epd` against another bot (unbalanced openings, fewer draws), `8moves_v3.pgn` against Stockfish (balanced, as the `UCI_Elo` scale assumes). Books come from [official-stockfish/books](https://github.com/official-stockfish/books) and are downloaded on first use |

Games (PGN) and logs are saved in `Tools/matches/`.

## Reading the result

cutechess prints `Elo difference: X +/- Y`. Against Stockfish the script also prints
`Estimated Elo ≈ StockfishElo + X`.

- The margin `Y` shrinks with more games: ~100 games give roughly ±70, ~400 games roughly ±35.
- If one side wins every game the difference is `inf` – choose a weaker or stronger opponent.
- With `-Sprt` the match ends with `H1 was accepted` (the bot is stronger by about Elo1 or more)
  or `H0 was accepted` (it isn't stronger than Elo0). Big improvements finish in ~100 games,
  small ones can take several hundred - that's the price of a reliable answer for a small difference.
- The number is on Stockfish's `UCI_Elo` scale (engine ratings). It does not translate 1:1 to human
  ratings on lichess or chess.com.

## Tuning the evaluation (Texel tuning)

`Chess2D.Tune` fits the weights of `TunableEvaluation` (the evaluation of Bot_v22: material, piece-square
tables, pawn structure, king shelter, mobility, threats, outposts) to positions from real games.
Put the data in `Tools/tuning/` (ignored by git).

```powershell
cd Tools
dotnet build Chess2D.Tune -c Release -o bin/tune

# 1. quiet positions from PGN files: "FEN;result" (1 = White won, 0.5 = draw, 0 = Black won)
bin/tune/Chess2D.Tune.exe extract --out tuning/positions.txt --per-game 8 --max 2000000 games.pgn more.pgn

# 2. (optional) a Stockfish evaluation for every position: "FEN;result;centipawns"
bin/tune/Chess2D.Tune.exe label --data tuning/positions.txt --out tuning/labelled.txt --stockfish external/stockfish/.../stockfish.exe --threads 8 --nodes 5000

# 3. tune: writes a C# file with the weights and prints the old -> new values
bin/tune/Chess2D.Tune.exe tune --data tuning/labelled.txt --epochs 2000 --lambda 0.5 --out tuning/TunedWeights.cs
```

| Command / option | Meaning |
|---|---|
| `extract --per-game` | quiet positions taken per game (positions of one game are strongly correlated) |
| `extract --skip-plies` | opening plies skipped (default 16) |
| `label --threads` / `--nodes` | Stockfish processes in parallel / nodes searched per position; the job can be stopped and resumed |
| `tune --lambda` | target = lambda × game result + (1 − lambda) × Stockfish's expected score (only with labelled data) |
| `tune --epochs` / `--rate` | gradient descent (Adam) steps and step size |

A position is "quiet" when the side to move is not in check, has no capture that wins material (SEE > 0)
and it isn't a lone-king endgame (the mop-up terms are not tuned). Copy the resulting `TunedWeights.cs`
to `Assets/Scripts/Engine/bots/` and test the bot that uses it with an SPRT match.

## Using the engine in a GUI

`Tools/bin/uci/Chess2D.Uci.exe` (built by `match.ps1`, or with
`dotnet build Chess2D.Uci -c Release -o bin/uci`) is a normal UCI engine: it can be added to Cute Chess, Arena
or other chess GUIs to play against it or watch it play. Options: `Bot` (v0, v1, v2, ...) and `Depth`.
