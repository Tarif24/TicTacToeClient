using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Text;
using Unity.Networking.Transport.Relay;

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
        #region Check Input and Send Msg

        if ((uiController.GetGameState() == GameStates.PlayerMove || uiController.GetGameState() == GameStates.OpponentMove) && Input.GetKeyDown(KeyCode.A))
        {
            SendMessageToOpponent("Hello Opponent");
        }    

        #endregion

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

                            ProcessServerGameIDResponse(gameState);
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
            Debug.Log("Change to play scene");
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

    public void ProcessServerGameIDResponse(int gameState)
    {
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
}

