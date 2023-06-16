/*
 * @Author: delevin.ying 
 * @Date: 2023-05-24 11:45:45 
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-06-16 16:05:54
 */


public static class APP{

    // public 

    public static bool QuickDebugSinglePlayer = false;

    public static bool DebugMissNetPackage = false;

    public static bool DebugRollBack = false;

    /// <summary>
    /// 生成的小怪数量
    /// </summary>
    public static int MaxEnemyCount  = 0;



    //!  游戏内的战斗配置在 GameConfig 中 


#region  模拟客户端网络抖动
    /// <summary>
    /// 客户端模拟网络抖动延迟
    /// </summary>
    public static readonly bool DebugClientNetDelay = false;

    public static float DebugClientNetDelayPercent = 0.9f;

    public static int DebugMaxClientNetDelayFrameCount = 40;

    public static int DebugMinClientNetDelayFrameCount = 30;

    /// <summary>
    /// 通过Min、Max 随机出来的比较帧号
    /// </summary>
    public static int DebugClientNetDelayCompareFrame;

    /// <summary>
    /// 上一次的延迟帧号
    /// </summary>
    public static int DebugClientNetDelayStartFrame;

#endregion


}