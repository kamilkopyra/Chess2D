# Opening book

`elite_book.txt` is built from the [Lichess Elite Database](https://database.nikonoel.fr/) (November 2025:
games of 2500+ rated players against 2300+, no bullet), which is a filtered subset of the
[lichess.org open database](https://database.lichess.org/) (CC0).

It contains the first 16 plies (8 moves each side) of 280,246 games: every position played in at least
50 games, with the moves chosen in at least 10 games and at least 3% of the games from that position.

Format, one position per line (tab separated, `#` starts a comment):

```
<piece placement> <side to move> <castling>	<uci move>:<games> <uci move>:<games> ...
```

Loaded with `ChessEngine.OpeningBook.Parse`; used by `Bot_v9` and newer.
