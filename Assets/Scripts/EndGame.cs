using UnityEngine;

// Pokazuje ekran końca gry (UIManager)
public class EndGame : MonoBehaviour
{
    public static void Checkmate(bool winnerIsWhite)
    {
        UIManager.Instance?.ShowGameOver(
            "Checkmate!",
            winnerIsWhite ? "White wins" : "Black wins",
            winnerIsWhite);
    }

    public static void Pat()
    {
        UIManager.Instance?.ShowGameOver("Stalemate", "Draw – no legal moves", null);
    }

    public static void DrawBy50MovesRule()
    {
        UIManager.Instance?.ShowGameOver("Draw", "50-move rule", null);
    }
}
