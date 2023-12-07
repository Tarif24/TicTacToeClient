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
        button.interactable = false;
        button.GetComponentsInChildren<TextMeshProUGUI>()[0].text = uiController.marker;
    }
}
