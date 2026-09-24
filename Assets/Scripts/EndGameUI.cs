using UnityEngine;

public class EndGameUI : MonoBehaviour
{
    public GameObject endPanel;
    public GameObject boardContainer;
    public GameObject backToBoard;
    public GameObject patPanel;
    public GameObject FiftyRule; 

    public void ShowEndScreen()
    {
        if (endPanel != null)
        {
            endPanel.SetActive(true);
        }
    }

    public void ShowPatScreen() 
    {
        if (patPanel != null) 
        { 
            patPanel.SetActive(true);
        
        }
    
    }

    public void Show50RuleScreen()
    {

        if (FiftyRule != null)
        {
            FiftyRule.SetActive(true);
        }
    }

}
