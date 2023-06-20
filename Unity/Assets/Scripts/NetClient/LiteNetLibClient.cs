using System;
using LiteNetLib;
using Lockstep.Network.Client;

public class LiteNetLibClient : INetwork
{
    private readonly EventBasedNetListener _listener = new EventBasedNetListener();

    private NetManager _client;

    public NetManager GetClient => _client;

    public event Action<byte[]> DataReceived;

    public const int Disconnect_Timeout = 30000;

    public bool Connected => _client.FirstPeer?.ConnectionState == ConnectionState.Connected;

    public int Ping => _client.FirstPeer != null ? _client.FirstPeer.Ping : -999;

    public void Start()
    {
        _listener.NetworkReceiveEvent += (fromPeer, dataReader, channlNumber, deliveryMethod) =>
        {
            DataReceived?.Invoke(dataReader.GetRemainingBytes());

            dataReader.Recycle();
        };

        _client = new NetManager(_listener) { DisconnectTimeout = Disconnect_Timeout };

        _client.Start();
    }

    public void Connect(string serverIp, int port)
    {
        _client.Connect(serverIp, port, NetSetting.ConnectKey);
    }

    /// <summary>
    /// 通过可靠传输，发包
    /// </summary>
    /// <param name="data"></param>
    public void Send(byte[] data)
    {
        _client.FirstPeer.Send(data, DeliveryMethod.ReliableOrdered);
    }

    public void Update()
    {
        _client.PollEvents();
    }

    public void Stop()
    {
        _client.Stop();
    }
}
