/*
 * @Author: delevin.ying
 * @Date: 2023-06-16 14:58:33
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-06-16 16:25:18
 */


using UnityEngine;
using System.Collections;

public class ClientCore : MonoBehaviour
{
    public string ServerIp = "127.0.0.1";

    // public int ServerPort = 9050;

    public bool Connected => _netClient.Connected;
    private readonly LiteNetLibClient _netClient = new LiteNetLibClient();

    void Start()
    {
        _netClient.Start();

#if UNITY_EDITOR
        NetSetting.GetAutoCreateRandomPort();
#endif

        StartCoroutine(AutoConnect());
    }

    public IEnumerator AutoConnect()
    {
        yield return new WaitForSeconds(2);

        while (!Connected)
        {
            _netClient.Connect(ServerIp, (int)NetSetting.Port);
            yield return new WaitForSeconds(1);
        }

        yield return null;
    }
}
