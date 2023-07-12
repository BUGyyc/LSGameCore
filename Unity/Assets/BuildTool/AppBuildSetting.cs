using System.Collections.Generic;
using UnityEngine;
using AppBuild;
using UnityEditor;

[CreateAssetMenu(fileName = "AppBuildSetting", menuName = "Create AppBuildSetting")]
public class AppBuildSetting : ScriptableObject
{
    /// <summary>
    /// 版本号
    /// </summary>
    public string appVersion;

    public BuildTarget platform;

    /// <summary>
    /// 标记
    /// </summary>
    public AppTag tag;

    public AppChannel channel;

    /// <summary>
    /// 备注描述
    /// </summary>
    public string desc;
}

namespace AppBuild
{
    public enum AppTag
    {
        Normal = 0,

        Upgrade = 1,

        Fixed = 2
    }

    public enum AppChannel
    {
        Normal = 0,

        Steam_PC = 1,

        EPIC_PC = 2
    }
}
