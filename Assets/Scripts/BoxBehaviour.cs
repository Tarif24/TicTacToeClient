using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoxBehaviour : MonoBehaviour
{
    public Button button;
    public UIController uiController;

    public void OnBoxClicked()
    {
        if (uiController.GetGameState() == GameStates.PlayerMove)
        {
            button.interactable = false;
            button.GetComponentsInChildren<TextMeshProUGUI>()[0].text = uiController.marker;
            uiController.CheckForWin();
            if (!uiController.didWin)
            {
                uiController.CheckForDraw();
            }
            uiController.lastButtonClicked = GetComponent<Button>();
            NetworkClientProcessing.SendSelectionToOpponent();
        }
    }


}
