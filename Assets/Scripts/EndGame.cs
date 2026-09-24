using UnityEngine;

public class EndGame: MonoBehaviour
{



    public static void EndTheGame() 
    {
        EndGameUI ui = GameObject.FindFirstObjectByType<EndGameUI>();

        if (ui != null)
        {
            ui.ShowEndScreen();
        }

    }
    public static void Pat()
    {
        EndGameUI ui = GameObject.FindFirstObjectByType<EndGameUI>();

        if (ui != null)
        {
            ui.ShowPatScreen();
        }

    }
    public static void DrawBy50MovesRule() 
    {
        EndGameUI ui = GameObject.FindFirstObjectByType<EndGameUI>();

        if (ui != null)
        {
            ui.Show50RuleScreen();
        }

    }

}
