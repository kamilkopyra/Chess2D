using ChessEngine;

// Tłumaczy wynik partii z silnika na ekran końca gry (UIManager)
public static class EndGame
{
    // winner ma znaczenie tylko przy macie
    public static void Show(GameStatus status, Side winner)
    {
        var ui = UIManager.Instance;
        if (ui == null) return;

        switch (status)
        {
            case GameStatus.Checkmate:
                ui.ShowGameOver("Checkmate!", winner == Side.White ? "White wins" : "Black wins", winner == Side.White);
                break;
            case GameStatus.Stalemate:
                ui.ShowGameOver("Stalemate", "Draw – no legal moves", null);
                break;
            case GameStatus.FiftyMoveRule:
                ui.ShowGameOver("Draw", "50-move rule", null);
                break;
            case GameStatus.ThreefoldRepetition:
                ui.ShowGameOver("Draw", "Threefold repetition", null);
                break;
            case GameStatus.InsufficientMaterial:
                ui.ShowGameOver("Draw", "Insufficient material", null);
                break;
        }
    }
}
