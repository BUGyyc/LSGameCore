/*
 * @Author: delevin.ying
 * @Date: 2023-05-26 14:11:06
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-05-26 14:13:58
 */
using Lockstep.Math;
using UnityEngine;

namespace Lockstep.GameCore
{
    public interface ICharacterMove
    {
        public LVector3 CharacterMoveInput(Vector3 inputDir, Vector3 cameraForward);
    }
}
