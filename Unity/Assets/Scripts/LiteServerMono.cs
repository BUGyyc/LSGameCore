/*
 * @Author: delevin.ying
 * @Date: 2023-06-19 12:01:17
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-06-20 14:50:01
 */

using Server.LiteNetLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class LiteServerMono : MonoBehaviour
{
    [Header("房间玩家数量")]
    [HideInInspector]
    public uint RoomPlayerNumber = 2;

    [Header("端口号")]
    [HideInInspector]
    public uint Port = 9000;

    private string _ip;
    LiteNetLibServer server;
    Lockstep.Network.Server.Room room;

    public GameObject liteClientObj;

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

    void Start()
    {
        Port = NetSetting.Port;
        RoomPlayerNumber = (uint)NetSetting.Number;
        server = new LiteNetLibServer();
        room = new Lockstep.Network.Server.Room(server, (int)RoomPlayerNumber);
        room.Open((int)Port);
        UnityEngine.Debug.Log($"房间开启 RoomPlayerNumber{RoomPlayerNumber}  port {Port}  ");
        StartCoroutine(AutoStartClient());
    }

    IEnumerator AutoStartClient()
    {
        yield return new WaitForSeconds(1);

        liteClientObj.SetActive(true);

        yield return null;
    }
    
    

    // Update is called once per frame
    void Update()
    {
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
    }
}
