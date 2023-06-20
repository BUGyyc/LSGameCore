using UnityEditor.PackageManager;
using UnityEngine;
using static UnityEngine.InputSystem.HID.HID;

public class LiteClientMono : MonoBehaviour
{
    public string ServerIp = "127.0.0.1";
    public int ServerPort = 9050;

    private readonly LiteNetLibClient _client = new LiteNetLibClient();
    public bool Connected => _client.Connected;

    private void Awake()
    {
        ServerIp = NetSetting.IP;
        ServerPort = (int)NetSetting.Port;


    }

    private void Start()
    {
        _client.Start();

        _client.Connect(ServerIp, ServerPort);
    }
    void Update()
    {
        _client.Update();
    }

}