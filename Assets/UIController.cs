using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    GameObject usernameInputField;
    GameObject passwordInputField;
    GameObject gameIDInputField;

    GameStates gameState;

    [SerializeField]
    GameObject loginPage;
    [SerializeField]
    GameObject enterGameID;
    [SerializeField]
    GameObject lookingForPlayer;
    [SerializeField]
    GameObject game;
    [SerializeField]
    GameObject win;
    [SerializeField]
    GameObject lose;

    public bool isNewAccount;

    public string currentGameID;

    public List<Text[]> TicTacToeGrid;
    public Text[] TopRow = new Text[3]; 
    public Text[] MidRow = new Text[3]; 
    public Text[] BotRow = new Text[3]; 

    private void Start()
    {
        SetGameState(GameStates.Login);

        Object[] GameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));

        foreach (Object go in GameObjects)
        {
            if (go.name == "UsernameInputField")
                usernameInputField = (GameObject)go;
            else if (go.name == "PasswordInputField")
                passwordInputField = (GameObject)go;
            else if (go.name == "GameIDInputField")
                gameIDInputField = (GameObject)go;
        }

        TicTacToeGrid = new List<Text[]>();
        TicTacToeGrid.Add(TopRow);
        TicTacToeGrid.Add(MidRow);
        TicTacToeGrid.Add(BotRow);
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

    public string GetGameIDFromInput()
    {

        return gameIDInputField.GetComponentsInChildren<Text>()[1].text;
    }

    public GameStates GetGameState()
    {
        return gameState;
    }

    public void SetGameState(GameStates state)
    {
        gameState = state;

        switch (gameState) 
        {
            case GameStates.Login:
                loginPage.SetActive(true);
                enterGameID.SetActive(false);
                lookingForPlayer.SetActive(false);
                game.SetActive(false);
                win.SetActive(false);
                lose.SetActive(false);
                break;

            case GameStates.EnterGameID:
                loginPage.SetActive(false);
                enterGameID.SetActive(true);
                lookingForPlayer.SetActive(false);
                game.SetActive(false);
                win.SetActive(false);
                lose.SetActive(false);
                break;

            case GameStates.LookingForPlayer:
                loginPage.SetActive(false);
                enterGameID.SetActive(false);
                lookingForPlayer.SetActive(true);
                game.SetActive(false);
                win.SetActive(false);
                lose.SetActive(false);
                break;

            case GameStates.PlayerMove:
                loginPage.SetActive(false);
                enterGameID.SetActive(false);
                lookingForPlayer.SetActive(false);
                game.SetActive(true);
                win.SetActive(false);
                lose.SetActive(false);
                break;

            case GameStates.OpponentMove:
                loginPage.SetActive(false);
                enterGameID.SetActive(false);
                lookingForPlayer.SetActive(false);
                game.SetActive(true);
                win.SetActive(false);
                lose.SetActive(false);
                break;

            case GameStates.Win:
                loginPage.SetActive(false);
                enterGameID.SetActive(false);
                lookingForPlayer.SetActive(false);
                game.SetActive(false);
                win.SetActive(true);
                lose.SetActive(false);
                break;

            case GameStates.Lose:
                loginPage.SetActive(false);
                enterGameID.SetActive(false);
                lookingForPlayer.SetActive(false);
                game.SetActive(false);
                win.SetActive(false);
                lose.SetActive(true);
                break;

            default:
                break;

        }
    }
}
