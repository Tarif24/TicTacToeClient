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
    GameObject UIController;

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

        if (Input.GetKeyDown(KeyCode.A))
            SendMessageToServer("Hello server's world, sincerely your network client");

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
            if (pipelineUsedToSendEvent == reliableAndInOrderPipeline)
                Debug.Log("Network event from: reliableAndInOrderPipeline");
            else if (pipelineUsedToSendEvent == nonReliableNotInOrderedPipeline)
                Debug.Log("Network event from: nonReliableNotInOrderedPipeline");

            switch (networkEventType)
            {
                case NetworkEvent.Type.Connect:
                    Debug.Log("We are now connected to the server");
                    break;
                case NetworkEvent.Type.Data:
                    int dataSignifier = streamReader.ReadInt();

                    int sizeOfDataBuffer = streamReader.ReadInt();
                    NativeArray<byte> buffer = new NativeArray<byte>(sizeOfDataBuffer, Allocator.Persistent);
                    streamReader.ReadBytes(buffer);
                    byte[] byteBuffer = buffer.ToArray();
                    string msg = Encoding.Unicode.GetString(byteBuffer);
                    //ProcessReceivedMsg(msg);

                    if (dataSignifier == DataSignifiers.ServerLoginResponse)
                    {
                        ProcessServerLoginResponse(msg);
                    }

                    buffer.Dispose();
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
        string loginInfo = UIController.GetComponent<UIController>().GetUsernameFromInput() + "," + UIController.GetComponent<UIController>().GetPasswordFromInput();

        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(loginInfo);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);

        DataStreamWriter streamWriter;
        networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out streamWriter);
        if (UIController.GetComponent<UIController>().isNewAccount)
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

    public void ProcessServerLoginResponse(string response)
    {
        string[] loginResponse = response.Split(',');

        ProcessReceivedMsg(loginResponse[1]);

        if (loginResponse[0] == "YES")
        {
            Debug.Log("Change to play scene");
        }
        else if (loginResponse[0] == "NO")
        {
            Debug.Log("Look at server messages");
        }
    }

}

