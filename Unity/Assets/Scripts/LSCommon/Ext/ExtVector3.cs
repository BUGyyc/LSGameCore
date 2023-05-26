/*
 * @Author: delevin.ying
 * @Date: 2023-05-26 14:16:27
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-05-26 14:18:32
 */
using Lockstep.Math;
using UnityEngine;

public static class ExtVector3
{
    public static Vector3 CleanY(this Vector3 self)
    {
        return new Vector3(self.x, 0, self.z);
    }

    
}
