using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetSetting
{
    public static string IP;
    public static uint Port;

    public static int Number = 1;

    public const string NetConnectKey = "LSGameCore";

#if UNITY_EDITOR
    public const string AUTO_PORT_FLAG = "###AUTO_PORT###LS_GAME_CORE###";

    public static uint AutoCreateRandomPort()
    {
        uint val = (uint)UnityEngine.Random.Range(8000, 20000);
        Port = val;
        GUIUtility.systemCopyBuffer = AUTO_PORT_FLAG + val.ToString();
        return Port;
    }

    public static uint GetAutoCreateRandomPort()
    {
        string portContent = GUIUtility.systemCopyBuffer;
        if (portContent.Contains(AUTO_PORT_FLAG))
        {
            var str = portContent.Replace(AUTO_PORT_FLAG, "");
            // LogMaster.E(str);
            uint val = uint.Parse(str);
            Port = val;
        }
        return Port;
    }

#endif
}
