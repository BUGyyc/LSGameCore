using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
public class LaunchClient : MonoBehaviour
{
    public InputField IpIF;
    public InputField PortIF;

    public Button connectBtn;

    public GameObject gameManager;

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

// #if UNITY_EDITOR
        string buffer = GUIUtility.systemCopyBuffer;
        if (buffer.Contains("Lockstep.Random.Port#"))
        {
            var strs = buffer.Replace("Lockstep.Random.Port#", "");
            Port = int.Parse(strs);
            //  PortIF.text = strs;
        }

// #endif
        PortIF.text = Port.ToString();

        connectBtn.onClick.AddListener(() =>
        {
            string ip = IpIF.text;
            int port = int.Parse(PortIF.text);

            Debug.Log($" {ip}  {port} ");

            NetSetting.IP = ip;
            NetSetting.Port = (uint)port;
            
            gameManager.SetActive(true);
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
