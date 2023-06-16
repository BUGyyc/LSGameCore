using System.Collections;
using UnityEngine;
using System.Threading;
using Lockstep.Network;
using Server.LiteNetLib;

public class HostClient : MonoBehaviour
{
    // Lockstep.FakeServer.Server.Server server;
    [Header("房间玩家数量")]
    [HideInInspector]
    public uint RoomPlayerNumber = 2;

    [Header("端口号")]
    [HideInInspector]
    public uint Port = 9000;

    public GameObject clientCoreObj;

    private string _ip;
    LiteNetLibServer server;
    Lockstep.Network.Server.Room room;

    [HideInInspector]
    public string SetGet_str_ipAddress
    {
        set { _ip = value; }
        get
        {
            _ip = LiteNetLib.NetUtils.GetLocalIp(LiteNetLib.LocalAddrType.IPv4);
            return _ip;
        }
    }

    private void Awake()
    {
        GameObject.DontDestroyOnLoad(this);
    }

    // OneThreadSynchronizationContext contex;

    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR
        NetSetting.AutoCreateRandomPort();
#endif

        Port = (uint)NetSetting.Port;
        RoomPlayerNumber = (uint)NetSetting.Number;
        server = new LiteNetLibServer();

        room = new Lockstep.Network.Server.Room(server, (int)RoomPlayerNumber);

        room.Open((int)Port);

        LogMaster.I($"[ServerCore]  Start {Port} {RoomPlayerNumber}");

        // contex = new OneThreadSynchronizationContext();
        // SynchronizationContext.SetSynchronizationContext(contex);

        // server = new Lockstep.FakeServer.Server.Server();
        // server.Start();

        StartCoroutine(DelayStart());
    }

    // int count = 0;
    // Update is called once per frame
    void Update()
    {
        // contex.Update();
        // server.Update();

        server.PollEvents();
    }

    private void OnGUI()
    {
        if (room == null)
            return;

        GUI.Label(new Rect(0, 0, 300, 100), $"ip:{SetGet_str_ipAddress} ");
        GUI.Label(new Rect(0, 30, 300, 100), $"port:{Port} ");
        GUI.Label(
            new Rect(0, 60, 300, 100),
            $"RoomPlayer:{room.OnLivePlayerCount()} / {RoomPlayerNumber} "
        );

        //GUI.Label(new Rect(0, 90, 300, 100), $"ReadyPlayer: ");
    }

    public IEnumerator DelayStart()
    {
        yield return new UnityEngine.WaitForSeconds(1);

        clientCoreObj.SetActive(true);

        yield return null;
    }
}
