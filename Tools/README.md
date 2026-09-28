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

Games (PGN) and logs are saved in `Tools/matches/`.

## Reading the result

cutechess prints `Elo difference: X +/- Y`. Against Stockfish the script also prints
`Estimated Elo ≈ StockfishElo + X`.

- The margin `Y` shrinks with more games: ~100 games give roughly ±70, ~400 games roughly ±35.
- If one side wins every game the difference is `inf` – choose a weaker or stronger opponent.
- The number is on Stockfish's `UCI_Elo` scale (engine ratings). It does not translate 1:1 to human
  ratings on lichess or chess.com.

## Using the engine in a GUI

`Tools/bin/uci/Chess2D.Uci.exe` (built by `match.ps1`, or with
`dotnet build Chess2D.Uci -c Release -o bin/uci`) is a normal UCI engine: it can be added to Cute Chess, Arena
or other chess GUIs to play against it or watch it play. Options: `Bot` (v0, v1, v2, ...) and `Depth`.
