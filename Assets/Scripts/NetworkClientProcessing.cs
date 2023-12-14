using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

static public class NetworkClientProcessing
{
    #region SetUp

    static NetworkClient networkClient;
    static UIController uiController;

    static public void SetNetworkClient(NetworkClient NC)
    {
        networkClient = NC;
    }
    static public NetworkClient GetNetworkClient()
    {
        return networkClient;
    }

    static public void SetUIController(UIController UIC)
    {
        uiController = UIC;
    }

    #endregion

    #region Send Data Functions

    static public void SendMessageToServer(string msg)
    {
        networkClient.SendMessageToServer(msg);
    }

    static public void SendUsernameAndPasswordToServer()
    {
        networkClient.SendUsernameAndPasswordToServer();
    }

    static public void SendGameID()
    {
        networkClient.SendGameID();
    }

    static public void SendBackOut()
    {
        networkClient.SendBackOut();
    }

    static public void SendSelectionToOpponent()
    {
        networkClient.SendSelectionToOpponent();
    }

    static public void SendChatMessage()
    {
        networkClient.SendChatMessage();
    }

    static public void SendAllSelectionsToServer()
    {
        networkClient.SendAllSelectionsToServer();
    }

    #endregion

    #region Recive Data Functions

    static public void ProcessDataTypeFromServer(DataStreamReader streamReader)
    {
        int dataSignifier = streamReader.ReadInt();

        switch (dataSignifier)
        {
            case DataSignifiers.ServerLoginResponse:
                int sizeOfDataBuffer = streamReader.ReadInt();
                NativeArray<byte> buffer = new NativeArray<byte>(sizeOfDataBuffer, Allocator.Persistent);
                streamReader.ReadBytes(buffer);
                byte[] byteBuffer = buffer.ToArray();
                string msg = Encoding.Unicode.GetString(byteBuffer);
                buffer.Dispose();

                ProcessServerLoginResponse(msg);
                break;

            case DataSignifiers.ServerGameIDResponse:
                int gameState = streamReader.ReadInt();

                int sizeOfDataBuffer3 = streamReader.ReadInt();
                NativeArray<byte> buffer3 = new NativeArray<byte>(sizeOfDataBuffer3, Allocator.Persistent);
                streamReader.ReadBytes(buffer3);
                byte[] byteBuffer3 = buffer3.ToArray();
                string marker = Encoding.Unicode.GetString(byteBuffer3);
                buffer3.Dispose();

                ProcessServerGameIDResponse(gameState, marker);
                break;

            case DataSignifiers.ServerGameRoomKick:
                ProcessServerGameRoomKick();
                break;

            case DataSignifiers.ServerSendToLookingForPlayer:
                ProcessServerSendToLookingForPlayer();
                break;

            case DataSignifiers.MessageToOpponent:
                int sizeOfDataBuffer2 = streamReader.ReadInt();
                NativeArray<byte> buffer2 = new NativeArray<byte>(sizeOfDataBuffer2, Allocator.Persistent);
                streamReader.ReadBytes(buffer2);
                byte[] byteBuffer2 = buffer2.ToArray();
                string msg2 = Encoding.Unicode.GetString(byteBuffer2);
                buffer2.Dispose();

                ProcessMessageFromOpponent(msg2);
                break;

            case DataSignifiers.SelectionToOpponent:
                int x = streamReader.ReadInt();
                int y = streamReader.ReadInt();
                int outcome = streamReader.ReadInt();

                int sizeOfDataBuffer4 = streamReader.ReadInt();
                NativeArray<byte> buffer4 = new NativeArray<byte>(sizeOfDataBuffer4, Allocator.Persistent);
                streamReader.ReadBytes(buffer4);
                byte[] byteBuffer4 = buffer4.ToArray();
                string marker2 = Encoding.Unicode.GetString(byteBuffer4);
                buffer4.Dispose();

                ProcessSelectionFromOpponent(x, y, outcome, marker2);
                break;

            case DataSignifiers.AllSelectionsToObserver:
                ProcessSelectionsToObserver();
                break;

            case DataSignifiers.AllSelectionsToObserverFinal:

                List<int[]> allSelections = new List<int[]>();

                while (streamReader.ReadInt() == 1)
                {
                    int[] temp = new int[3];

                    temp[0] = streamReader.ReadInt();
                    temp[1] = streamReader.ReadInt();
                    temp[2] = streamReader.ReadInt();

                    allSelections.Add(temp);
                }

                ProcessSelectionsToObserverFinal(allSelections);
                break;

        }
    }

    static public void ProcessServerLoginResponse(string response)
    {
        string[] loginResponse = response.Split(',');

        uiController.DisplayLoginMessage(loginResponse[1]);

        if (loginResponse[0] == "YES")
        {
            uiController.SetGameState(GameStates.EnterGameID);
        }
    }

    static public void ProcessServerGameIDResponse(int gameState, string marker)
    {
        uiController.marker = marker;
        uiController.SetGameState((GameStates)gameState);
    }

    static public void ProcessServerGameRoomKick()
    {
        uiController.SetGameState(GameStates.EnterGameID);
    }

    static public void ProcessServerSendToLookingForPlayer()
    {
        uiController.SetGameState(GameStates.LookingForPlayer);
    }

    static public void ProcessMessageFromOpponent(string msg)
    {
        uiController.DisplayChatMessage(msg);
    }

    static public void ProcessSelectionFromOpponent(int x, int y, int outcome, string marker)
    {
        if (uiController.GetGameState() != GameStates.Observer)
        {
            uiController.SetGameState(GameStates.PlayerMove);

            if (uiController.marker == "X")
            {
                uiController.TicTacToeGrid[x][y].GetComponentsInChildren<TextMeshProUGUI>()[0].text = "O";
            }
            else
            {
                uiController.TicTacToeGrid[x][y].GetComponentsInChildren<TextMeshProUGUI>()[0].text = "X";
            }

            uiController.TicTacToeGrid[x][y].interactable = false;

            if (outcome == 1)
            {
                uiController.SetGameState(GameStates.Lose);
            }
            else if (outcome == 2)
            {
                uiController.SetGameState(GameStates.Draw);
            }
        }
        else
        {
            uiController.TicTacToeGrid[x][y].GetComponentsInChildren<TextMeshProUGUI>()[0].text = marker;
            uiController.TicTacToeGrid[x][y].interactable = false;

            if (outcome == 1 || outcome == 2)
            {
                uiController.SetGameState(GameStates.Finish);
            }
        }
    }

    static public void ProcessSelectionsToObserver()
    {
        SendAllSelectionsToServer();
    }
   
    static public void ProcessSelectionsToObserverFinal(List<int[]> allSelections)
    {
        foreach (int[] s in allSelections)
        {
            if (s[2] == 0)
            {
                uiController.TicTacToeGrid[s[0]][s[1]].GetComponentsInChildren<TextMeshProUGUI>()[0].text = "X";
            }
            else
            {
                uiController.TicTacToeGrid[s[0]][s[1]].GetComponentsInChildren<TextMeshProUGUI>()[0].text = "O";
            }

            uiController.TicTacToeGrid[s[0]][s[1]].interactable = false;
        }
    }

    #endregion
}
