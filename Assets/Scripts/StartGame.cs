using UnityEngine;

public class StartGame : MonoBehaviour
{
    public GameObject StartView;

    public void StartTheGame()
    {


        if (StartView != null)
        {
            StartView.SetActive(false);
        }
    }
}