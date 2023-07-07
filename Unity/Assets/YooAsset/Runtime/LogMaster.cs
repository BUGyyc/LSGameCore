/*
 * @Author: delevin.ying
 * @Date: 2023-05-24 13:53:22
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-05-24 16:48:47
 */


// using UnityEngine;
// using Lockstep.Game;
// using System.Diagnostics;
using System.Diagnostics;

namespace YooAsset
{
    internal static class LogMaster
    {
        /// <summary>
        /// ! NET 相关的 log
        /// </summary>
        /// <param name="str"></param>
        public static void N(params string[] args)
        {
            //    System.Diagnostics.Debug.Log($"<color=red> {string.Join(",", args)}   </color>");
        }

        public static void L(params string[] str)
        {
            int tickValue = 0;
            //#if UNITY_EDITOR
            // if (World.Instance != null)
            //     tickValue = World.Instance.Tick;
            //#endif

            UnityEngine.Debug.LogFormat(
                $"<color=yellow> [Tick:{tickValue}] {string.Join(",", str)}   </color>"
            );
        }

        [Conditional("DEBUG")]
        public static void Log(string args)
        {
            UnityEngine.Debug.Log($"<color=yellow> {args}   </color>");
        }

        public static void Log(string str1, string str2)
        {
            UnityEngine.Debug.Log($"<color=yellow> {str1}   </color>" + $" {str2}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void I(params string[] args)
        {
            UnityEngine.Debug.LogFormat($"<color=yellow> {string.Join(",", args)}   </color>");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void S(string str1, string str2)
        {
            UnityEngine.Debug.LogFormat($"<color=yellow>{str1}</color>  {str2}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void A(string str1)
        {
            UnityEngine.Debug.Log($"{str1}");
        }

        public static void E(params string[] str)
        {
            // int tickValue = 0;
            // #if UNITY_EDITOR
            // if (World.Instance != null)
            //     tickValue = World.Instance.Tick;
            // #endif
            UnityEngine.Debug.LogErrorFormat($"<color=red>{string.Join(",", str)}   </color>");
        }

        // public static void L(this Entity self, params string[] str)
        // {
        //     Debug.LogFormat($"{self} info:  ", str);
        // }
    }
}
