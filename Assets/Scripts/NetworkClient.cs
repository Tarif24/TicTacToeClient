using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Text;
using Unity.Networking.Transport.Relay;
using TMPro;

public class NetworkClient : MonoBehaviour
{
    NetworkDriver networkDriver;
    NetworkConnection networkConnection;
    NetworkPipeline reliableAndInOrderPipeline;
    NetworkPipeline nonReliableNotInOrderedPipeline;
    const ushort NetworkPort = 54321;
    const string IPAddress = "192.168.1.6";

    [SerializeField]
    UIController uiController;

    void Start()
    {
        networkDriver = NetworkDriver.Create();
        reliableAndInOrderPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        nonReliableNotInOrderedPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage));
        networkConnection = default(NetworkConnection);
        NetworkEndpoint endpoint = NetworkEndpoint.Parse(IPAddress, NetworkPort, NetworkFamily.Ipv4);
        networkConnection = networkDriver.Connect(endpoint);
    }

    public void OnDestroy()
    {
        networkConnection.Disconnect(networkDriver);
        networkConnection = default(NetworkConnection);
        networkDriver.Dispose();
    }

    void Update()
    {
        networkDriver.ScheduleUpdate().Complete();

        #region Check for client to server connection

        if (!networkConnection.IsCreated)
        {
            Debug.Log("Client is unable to connect to server");
            return;
        }

        #endregion

        if (uiController.didSelect)
        {
            SendSelectionToOpponent();
        }

        #region Manage Network Events

        NetworkEvent.Type networkEventType;
        DataStreamReader streamReader;
        NetworkPipeline pipelineUsedToSendEvent;

        while (PopNetworkEventAndCheckForData(out networkEventType, out streamReader, out pipelineUsedToSendEvent))
        {
            //if (pipelineUsedToSendEvent == reliableAndInOrderPipeline)
            //    Debug.Log("Network event from: reliableAndInOrderPipeline");
            //else if (pipelineUsedToSendEvent == nonReliableNotInOrderedPipeline)
            //    Debug.Log("Network event from: nonReliableNotInOrderedPipeline");

            switch (networkEventType)
            {
                case NetworkEvent.Type.Connect:
                    Debug.Log("We are now connected to the server");
                    break;
                case NetworkEvent.Type.Data:
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
                            ProcessSelectionFromOpponent(x, y, outcome);
                            break;

                    }

                    
                    break;
                case NetworkEvent.Type.Disconnect:
                    Debug.Log("Client has disconnected from server");
                    networkConnection = default(NetworkConnection);
                    break;
            }
        }

        #endregion
    }

    private bool PopNetworkEventAndCheckForData(out NetworkEvent.Type networkEventType, out DataStreamReader streamReader, out NetworkPipeline pipelineUsedToSendEvent)
    {
        networkEventType = networkConnection.PopEvent(networkDriver, out streamReader, out pipelineUsedToSendEvent);

        if (networkEventType == NetworkEvent.Type.Empty)
            return false;
        return true;
    }

    private void ProcessReceivedMsg(string msg)
    {
        Debug.Log("Msg received = " + msg);
    }

    public void SendMessageToServer(string msg)
    {
        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(msg);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);

        DataStreamWriter streamWriter;
        networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out streamWriter);
        streamWriter.WriteInt(DataSignifiers.Message);
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);
        networkDriver.EndSend(streamWriter);

        buffer.Dispose();
    }

    public void SendUsernameAndPasswordToServer()
    {
        string loginInfo = uiController.GetUsernameFromInput() + "," + uiController.GetPasswordFromInput();

        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(loginInfo);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);

        DataStreamWriter streamWriter;
        networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out streamWriter);
        if (uiController.isNewAccount)
        {
            streamWriter.WriteInt(DataSignifiers.AccountSignup);
        }
        else
        {
            streamWriter.WriteInt(DataSignifiers.AccountSignin);
        }
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);
        networkDriver.EndSend(streamWriter);

        buffer.Dispose();
    }

    public void SendGameID()
    {
        string gameID = uiController.GetGameIDFromInput();

        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(gameID);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);

        DataStreamWriter streamWriter;
        networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out streamWriter);
        streamWriter.WriteInt(DataSignifiers.GameID);
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);
        networkDriver.EndSend(streamWriter);

        buffer.Dispose();

        uiController.SetGameState(GameStates.LookingForPlayer);

        uiController.currentGameID = gameID;
    }

    public void ProcessServerLoginResponse(string response)
    {
        string[] loginResponse = response.Split(',');

        ProcessReceivedMsg(loginResponse[1]);

        if (loginResponse[0] == "YES")
        {
            uiController.SetGameState(GameStates.EnterGameID);
        }
        else if (loginResponse[0] == "NO")
        {
            Debug.Log("Look at server messages");
        }
    }

    public void LogOut()
    {
        uiController.SetGameState(GameStates.Login);
    }

    public void ProcessServerGameIDResponse(int gameState, string marker)
    {
        uiController.marker = marker;
        uiController.SetGameState((GameStates)gameState);
    }

    public void SendBackOut()
    {
        string gameID = uiController.currentGameID;

        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(gameID);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);

        DataStreamWriter streamWriter;
        networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out streamWriter);
        streamWriter.WriteInt(DataSignifiers.BackOut);
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);
        networkDriver.EndSend(streamWriter);

        buffer.Dispose();

        uiController.SetGameState(GameStates.EnterGameID);
    }

    public void ProcessServerGameRoomKick()
    {
        uiController.SetGameState(GameStates.EnterGameID);
    }

    public void ProcessServerSendToLookingForPlayer()
    {
        uiController.SetGameState(GameStates.LookingForPlayer);
    }

    public void SendMessageToOpponent(string msg)
    {
        string temp = uiController.currentGameID + ',' + msg;

        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(temp);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);

        DataStreamWriter streamWriter;
        networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out streamWriter);
        streamWriter.WriteInt(DataSignifiers.MessageToOpponent);
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);
        networkDriver.EndSend(streamWriter);

        buffer.Dispose();
    }

    public void ProcessMessageFromOpponent(string msg)
    {
        Debug.Log(msg);
    }

    public void SendSelectionToOpponent()
    {
        uiController.SetGameState(GameStates.OpponentMove);

        int x = 0;
        int y = 0;
        int outcome = 0;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (uiController.TicTacToeGrid[i][j] == uiController.lastButtonClicked)
                {
                    x = i;
                    y = j;
                    uiController.didSelect = false;
                    break;
                }
            }
        }

        string temp = uiController.currentGameID;

        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(temp);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);

        DataStreamWriter streamWriter;
        networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out streamWriter);
        streamWriter.WriteInt(DataSignifiers.SelectionToOpponent);
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);
        streamWriter.WriteInt(x);
        streamWriter.WriteInt(y);

        if (uiController.didWin)
        {
            outcome = 1;
            uiController.SetGameState(GameStates.Win);
        }
        else if (uiController.isDraw)
        {
            uiController.SetGameState(GameStates.Draw);
            outcome = 2;
        }

        streamWriter.WriteInt(outcome);
        networkDriver.EndSend(streamWriter);

        buffer.Dispose();
    }

    public void ProcessSelectionFromOpponent(int x, int y, int outcome)
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

    public void SendChatMessage()
    {
        if (uiController.GetGameState() == GameStates.PlayerMove || uiController.GetGameState() == GameStates.OpponentMove)
        {
            string temp = uiController.GetChatTextFromInput();

            SendMessageToOpponent(temp);

            uiController.SetBlankChatTextFromInput();
        }
    }
}

