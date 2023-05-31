//using Lockstep.Network.Server;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.UI;
using Lockstep.Network;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System;

public class LaunchServer : MonoBehaviour
{
    public InputField IpIF;
    public InputField PortIF;

    public InputField NumberIF;

    public Button connectBtn;

    public GameObject gameManager;

    public string successTip = "房间已创建，等待链接";

    public string GetLocalIp()
    {
        ///获取本地的IP地址
        string AddressIP = string.Empty;
        foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
            {
                AddressIP = _IPAddress.ToString();
            }
        }
        return AddressIP;
    }
    // Start is called before the first frame update
    void Start()
    {
        IpIF.text = GetLocalIp();
        int Port = 9000;
        int Size = 2;

        Port = (int)UnityEngine.Random.Range(5000, 20000);
        GUIUtility.systemCopyBuffer = "Lockstep.Random.Port#" + Port;

        PortIF.text = Port.ToString();

        NumberIF.text = Size.ToString();

        connectBtn.onClick.AddListener(() =>
        {
            ConnectBattle();
        });

#if UNITY_EDITOR
        if (APP.QuickDebugSinglePlayer)
        {
            NumberIF.text = "1";
            ConnectBattle();
        }
#endif
    }

    private void ConnectBattle()
    {
        string ip = IpIF.text;
        int port = int.Parse(PortIF.text);
        NetSetting.IP = ip;
        NetSetting.Port = port;
        NetSetting.Number = int.Parse(NumberIF.text);

        Debug.Log($"[Server]  BtnStart   {ip}  {port}   {NetSetting.Number} ");

        gameManager.SetActive(true);
        connectBtn.enabled = false;
        var txt = connectBtn.GetComponentInChildren<Text>();

        if (txt != null)
        {
            txt.text = successTip;
        }
    }
}

