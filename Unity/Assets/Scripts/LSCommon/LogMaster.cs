/*
 * @Author: delevin.ying 
 * @Date: 2023-05-24 13:53:22 
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-05-24 16:48:47
 */


using UnityEngine;
using Lockstep.Game;

public static class LogMaster
{
    public static void L(params string[] str)
    {
        int tickValue = 0;
        //#if UNITY_EDITOR
        if (World.Instance != null) tickValue = World.Instance.Tick;
        //#endif

        Debug.LogFormat($"<color=yellow> [Tick:{tickValue}]  info:  {string.Join(",", str)}   </color>");
    }


    public static void E(params string[] str)
    {
        int tickValue = 0;
// #if UNITY_EDITOR
        if (World.Instance != null) tickValue = World.Instance.Tick;
// #endif
        Debug.LogErrorFormat($"<color=red> tick:{tickValue}  err:    {string.Join(",", str)}   </color>");
    }


    // public static void L(this Entity self, params string[] str)
    // {
    //     Debug.LogFormat($"{self} info:  ", str);
    // }

}