using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Text;
using Unity.Networking.Transport.Relay;
using TMPro;
using System.Collections.Generic;

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
        NetworkClientProcessing.SetNetworkClient(this);
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

        #region Manage Network Events

        NetworkEvent.Type networkEventType;
        DataStreamReader streamReader;
        NetworkPipeline pipelineUsedToSendEvent;

        while (PopNetworkEventAndCheckForData(out networkEventType, out streamReader, out pipelineUsedToSendEvent))
        {
            switch (networkEventType)
            {
                case NetworkEvent.Type.Connect:
                    Debug.Log("We are now connected to the server");
                    break;
                case NetworkEvent.Type.Data:

                    NetworkClientProcessing.ProcessDataTypeFromServer(streamReader);

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

    public void SendChatMessage()
    {
        if (uiController.GetGameState() == GameStates.PlayerMove || uiController.GetGameState() == GameStates.OpponentMove)
        {
            string temp = uiController.GetChatTextFromInput();

            SendMessageToOpponent(temp);
        }
    }

    public void SendAllSelectionsToServer()
    {
        string temp = uiController.currentGameID;

        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(temp);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);

        DataStreamWriter streamWriter;
        networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out streamWriter);
        streamWriter.WriteInt(DataSignifiers.AllSelectionsToObserver);
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (uiController.TicTacToeGrid[i][j].GetComponentsInChildren<TextMeshProUGUI>()[0].text != " ")
                {
                    streamWriter.WriteInt(1);
                    streamWriter.WriteInt(i);
                    streamWriter.WriteInt(j);
                    if (uiController.TicTacToeGrid[i][j].GetComponentsInChildren<TextMeshProUGUI>()[0].text == "X")
                    {
                        streamWriter.WriteInt(0);
                    }
                    else
                    {
                        streamWriter.WriteInt(1);
                    }
                }
            }
        }

        streamWriter.WriteInt(0);

        networkDriver.EndSend(streamWriter);

        buffer.Dispose();
    }
}

