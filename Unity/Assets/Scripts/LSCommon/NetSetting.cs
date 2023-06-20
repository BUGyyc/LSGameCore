using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetSetting
{
    public static string IP;
    public static int Port;

    public static int Number;

    public static readonly string  ConnectKey = "LockstepGame";
}


public static class NetProtocolDefine
{
    /// <summary>
    /// 游戏初始化指令
    /// </summary>
    public const byte Init = 0;

    // public const byte AAA = 1;
    /// <summary>
    /// 输入指令
    /// </summary>
    public const byte Input = 2;

    /// <summary>
    /// 验证是否同步
    /// </summary>
    public const byte CheckSync = 3;

    public const byte Ping = 4;

    public const string ConnectKey = "LockstepGame";
}
