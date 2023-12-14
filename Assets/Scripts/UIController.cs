using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public string currentGameID;
    public Button lastButtonClicked = null;

    private void Start()
    {
        NetworkClientProcessing.SetUIController(this);
        SetGameState(GameStates.Login);

        TicTacToeGrid.Add(TopRow);
        TicTacToeGrid.Add(MidRow);
        TicTacToeGrid.Add(BotRow);

        BoardReset();

        Object[] GameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));

        foreach (Object go in GameObjects)
        {
            if (go.name == "UsernameInputField")
                usernameInputField = (GameObject)go;
            else if (go.name == "PasswordInputField")
                passwordInputField = (GameObject)go;
            else if (go.name == "GameIDInputField")
                gameIDInputField = (GameObject)go;
            else if (go.name == "ChatInputField")
                chatInputField = (GameObject)go;
        }
    }

    #region Login Processing Helper Functions

    public bool isNewAccount;

    public void LoginButton()
    {
        isNewAccount = false;
        SendUsernameAndPasswordToServer();
    }

    public void CreateAccountButton()
    {
        isNewAccount = true;
        SendUsernameAndPasswordToServer();
    }

    public void LogOut()
    {
        SetGameState(GameStates.Login);
    }

    #endregion

    #region Input Field Helper Functions

    GameObject usernameInputField;
    GameObject passwordInputField;
    GameObject gameIDInputField;
    GameObject chatInputField;

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

    public string GetChatTextFromInput()
    {

        return chatInputField.GetComponentsInChildren<Text>()[1].text;
    }

    #endregion

    #region GameState/UI Controller

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
    [SerializeField]
    GameObject draw;
    [SerializeField]
    GameObject finish;

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
                break;

            case GameStates.EnterGameID:
                loginPage.SetActive(false);
                enterGameID.SetActive(true);
                lookingForPlayer.SetActive(false);
                game.SetActive(false);
                win.SetActive(false);
                lose.SetActive(false);
                draw.SetActive(false);
                finish.SetActive(false);
                break;

            case GameStates.LookingForPlayer:
                enterGameID.SetActive(false);
                lookingForPlayer.SetActive(true);
                game.SetActive(false);
                win.SetActive(false);
                lose.SetActive(false);
                draw.SetActive(false);
                finish.SetActive(false);
                BoardReset();
                break;

            case GameStates.PlayerMove:
                enterGameID.SetActive(false);
                lookingForPlayer.SetActive(false);
                game.SetActive(true);
                break;

            case GameStates.OpponentMove:
                loginPage.SetActive(false);
                enterGameID.SetActive(false);
                lookingForPlayer.SetActive(false);
                game.SetActive(true);
                break;

            case GameStates.Observer:
                loginPage.SetActive(false);
                enterGameID.SetActive(false);
                lookingForPlayer.SetActive(false);
                game.SetActive(true);
                break;

            case GameStates.Win:
                win.SetActive(true);
                break;

            case GameStates.Lose:
                lose.SetActive(true);
                break;

            case GameStates.Draw:
                draw.SetActive(true);
                break;

            case GameStates.Finish:
                finish.SetActive(true);
                break;

        }
    }

    #endregion

    #region Check Board State Functions and Board Helper Functions

    public List<Button[]> TicTacToeGrid = new List<Button[]>();
    public Button[] TopRow = new Button[3];
    public Button[] MidRow = new Button[3];
    public Button[] BotRow = new Button[3];

    public string marker = "X";
    public bool didWin = false;
    public bool isDraw = false;

    public void BoardReset()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                TicTacToeGrid[i][j].interactable = true;
                TicTacToeGrid[i][j].GetComponentsInChildren<TextMeshProUGUI>()[0].text = " ";
            }
        }

        didWin = false;
        isDraw = false;
    }
        
    public void CheckForWin()
    {
        bool win = false;

        // Check rows
        for (int i = 0; i < 3; i++)
        {
            if (TicTacToeGrid[i][0].GetComponentsInChildren<TextMeshProUGUI>()[0].text != " " &&
            TicTacToeGrid[i][0].GetComponentsInChildren<TextMeshProUGUI>()[0].text == TicTacToeGrid[i][1].GetComponentsInChildren<TextMeshProUGUI>()[0].text &&
            TicTacToeGrid[i][1].GetComponentsInChildren<TextMeshProUGUI>()[0].text == TicTacToeGrid[i][2].GetComponentsInChildren<TextMeshProUGUI>()[0].text)
            {
                win = true;
            }
        }

        // Check columns
        for (int j = 0; j < 3; j++)
        {
            if (TicTacToeGrid[0][j].GetComponentsInChildren<TextMeshProUGUI>()[0].text != " " &&
            TicTacToeGrid[0][j].GetComponentsInChildren<TextMeshProUGUI>()[0].text == TicTacToeGrid[1][j].GetComponentsInChildren<TextMeshProUGUI>()[0].text &&
            TicTacToeGrid[1][j].GetComponentsInChildren<TextMeshProUGUI>()[0].text == TicTacToeGrid[2][j].GetComponentsInChildren<TextMeshProUGUI>()[0].text)
            {
                win = true;
            }
        }

        if (TicTacToeGrid[0][0].GetComponentsInChildren<TextMeshProUGUI>()[0].text != " " &&
            TicTacToeGrid[0][0].GetComponentsInChildren<TextMeshProUGUI>()[0].text == TicTacToeGrid[1][1].GetComponentsInChildren<TextMeshProUGUI>()[0].text &&
            TicTacToeGrid[1][1].GetComponentsInChildren<TextMeshProUGUI>()[0].text == TicTacToeGrid[2][2].GetComponentsInChildren<TextMeshProUGUI>()[0].text)
        {
            win = true;
        }

        if (TicTacToeGrid[0][2].GetComponentsInChildren<TextMeshProUGUI>()[0].text != " " && 
            TicTacToeGrid[0][2].GetComponentsInChildren<TextMeshProUGUI>()[0].text == TicTacToeGrid[1][1].GetComponentsInChildren<TextMeshProUGUI>()[0].text && 
            TicTacToeGrid[1][1].GetComponentsInChildren<TextMeshProUGUI>()[0].text == TicTacToeGrid[2][0].GetComponentsInChildren<TextMeshProUGUI>()[0].text)
        {
            win = true;
        }

        didWin = win;
    }

    public void CheckForDraw()
    {
        isDraw = true;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (TicTacToeGrid[i][j].GetComponentsInChildren<TextMeshProUGUI>()[0].text == " ")
                {
                    isDraw = false;
                }
            }
        }
    }

    #endregion

    #region On Screen Messages

    public GameObject textBox;
    public Transform chatLocation;
    public Canvas canvas;
    public GameObject textBoxLogin;
    public Transform loginTextLocation;

    public void DisplayChatMessage(string msg)
    {
        GameObject temp = Instantiate(textBox, chatLocation.position, Quaternion.identity);

        temp.GetComponent<TextMeshProUGUI>().text = msg;

        temp.transform.SetParent(canvas.transform);

        Destroy(temp, 5);
    }

    public void DisplayLoginMessage(string msg)
    {
        GameObject temp = Instantiate(textBoxLogin, loginTextLocation.position, Quaternion.identity);

        temp.GetComponent<TextMeshProUGUI>().text = msg;

        temp.transform.SetParent(canvas.transform);

        Destroy(temp, 5);
    }

    #endregion

    #region Send Game Data To Server Functions

    public void SendUsernameAndPasswordToServer()
    {
        NetworkClientProcessing.SendUsernameAndPasswordToServer();
    }

    public void SendGameID()
    {
        NetworkClientProcessing.SendGameID();
    }

    public void SendBackOut() 
    {
        NetworkClientProcessing.SendBackOut();
    }

    public void SendChatMessage()
    {
        NetworkClientProcessing.SendChatMessage();
    }

    #endregion
}
