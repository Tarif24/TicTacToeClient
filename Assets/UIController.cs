using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
   
    GameObject usernameInputField;
   
    GameObject passwordInputField;

    public bool isNewAccount;

    private void Start()
    {
        Object[] GameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));

        foreach (Object go in GameObjects)
        {
            if (go.name == "UsernameInputField")
                usernameInputField = (GameObject)go;
            else if (go.name == "PasswordInputField")
                passwordInputField = (GameObject)go;
        }
    }

    public void LoginButton()
    {
        isNewAccount = false;
    }

    public void CreateAccountButton()
    {
        isNewAccount = true;
    }

    public string GetUsernameFromInput()
    {

        return usernameInputField.GetComponentsInChildren<Text>()[1].text;

    }

    public string GetPasswordFromInput()
    {

        return passwordInputField.GetComponentsInChildren<Text>()[1].text;

    }
}
