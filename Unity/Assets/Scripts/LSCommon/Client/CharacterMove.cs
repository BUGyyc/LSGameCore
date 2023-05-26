/*
 * @Author: delevin.ying
 * @Date: 2023-05-26 14:08:50
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-05-26 14:19:05
 */

using Lockstep.Math;
using UnityEngine;

namespace Lockstep.GameCore
{
    /// <summary>
    /// 第一人称的移动操作
    /// </summary>
    public class CharacterMoveFirstPerson : ICharacterMove
    {
        /// <summary>
        /// 用来和 MoveForward 比较的，算出移动角度和方向向量
        /// </summary>
        public static readonly Vector3 BaseMoveDir = new Vector3(0, 1, 0);


        public LVector3 CharacterMoveInput(Vector3 inputDir, Vector3 cameraForward)
        {
            throw new System.NotImplementedException();
        }

        public static LVector2 OnCharacterMoveInput(Vector2 inputDir, Vector3 cameraForward)
        {
            var moveState = MoveInput2MoveState(inputDir);

            if (moveState == CharacterMoveState.Idle) 
            {
                return LVector2.zero;
            }

            float angle = 0;
            switch (moveState)
            {
                case CharacterMoveState.左:
                    angle = -90;
                    break;
                case CharacterMoveState.右:
                    angle = 90;
                    break;
                case CharacterMoveState.前:
                    angle = 0f;
                    break;
                case CharacterMoveState.后:
                    angle = 180;
                    break;
                case CharacterMoveState.前左:
                    angle = -45;
                    break;
                case CharacterMoveState.前右:
                    angle = 45;
                    break;
                case CharacterMoveState.后左:
                    angle = -135;
                    break;
                case CharacterMoveState.后右:
                    angle = 135;
                    break;


            }


            Quaternion rotateQ = Quaternion.AngleAxis(angle, Vector3.up);
            var moveParams = rotateQ * cameraForward;
            return new LVector2(moveParams.x.ToLFloat(), moveParams.z.ToLFloat());
        }


        public static CharacterMoveState MoveInput2MoveState(Vector2 inputDir)
        {
            var tempDir = CharacterMoveState.Idle;
            if (inputDir.sqrMagnitude < 0.05f) return tempDir;

            if (inputDir.y >= 0.5f)
            {
                tempDir = tempDir | CharacterMoveState.前;
            }
            else if (inputDir.y <= -0.5f)
            {
                tempDir = tempDir | CharacterMoveState.后;
            }

            if (inputDir.x >= 0.5f)
            {
                tempDir = tempDir | CharacterMoveState.右;
            }
            else if (inputDir.x <= -0.5f)
            {
                tempDir = tempDir | CharacterMoveState.左;
            }


            return tempDir;
        }

        public static MoveDir _OnCharacterMoveInput(Vector3 inputDir, Vector3 cameraForward)
        {
            float angle = Vector3.Angle(BaseMoveDir, inputDir);
            if (inputDir.x < 0) angle = -angle;
            if (angle == 180 || angle == -180) angle = 180;
            //Vector3 cameraForward = Camera.main.transform.forward.CleanY();

            cameraForward = cameraForward.CleanY();

            cameraForward = cameraForward.normalized;

            Quaternion rotateQ = Quaternion.AngleAxis(angle, Vector3.up);
            cameraForward = rotateQ * cameraForward;




            var dir = inputDir.CleanY();
            var forward = cameraForward;

            var dotValue = Vector3.Dot(dir, forward);

            if (dotValue > 0.7f)
            {
                return MoveDir.前;
            }
            else if (dotValue < -0.7f)
            {
                return MoveDir.后;
            }

            var v3 = Vector3.Cross(dir, forward);
            if (v3.y > 0) return MoveDir.左;
            return MoveDir.右;
        }
    }

    public enum MoveDir
    {
        空 = 0,
        前,
        后,
        左,
        右
    }

    public enum CharacterMoveIndex
    {
        Idle = 0,
        Walk,
        Run,
        Speed,
        起步,

        急停,
        前,
        后,
        左,
        右,

        转身,
        下蹲,
    }

    public enum CharacterMoveState
    {
        Idle = 0,
        Walk = 1 << CharacterMoveIndex.Walk,
        Run = 1 << CharacterMoveIndex.Run,
        Speed = 1 << CharacterMoveIndex.Speed,

        起步 = 1 << CharacterMoveIndex.起步,
        急停 = 1 << CharacterMoveIndex.急停,

        前 = 1 << CharacterMoveIndex.前,
        后 = 1 << CharacterMoveIndex.后,
        左 = 1 << CharacterMoveIndex.左,
        右 = 1 << CharacterMoveIndex.右,

        前左 = 前 | 左,
        前右 = 前 | 右,
        后左 = 后 | 左,
        后右 = 后 | 右,

        转身 = 1 << CharacterMoveIndex.转身,

        下蹲 = 1 << CharacterMoveIndex.下蹲,

        起步_Walk_向前 = 起步 | Walk | 前,
        起步_Walk_向后 = 起步 | Walk | 后 | 转身,
        起步_Walk_向左 = 起步 | Walk | 左 | 转身,
        起步_Walk_后右 = 起步 | Walk | 右 | 转身,

        起步_Run_向前 = 起步 | Run | 前,
        起步_Run_向后 = 起步 | Run | 后 | 转身,
        起步_Run_向左 = 起步 | Run | 左 | 转身,
        起步_Run_后右 = 起步 | Run | 右 | 转身,

        起步_Speed_向前 = 起步 | Speed | 前,
        起步_Speed_向后 = 起步 | Speed | 后 | 转身,
        起步_Speed_向左 = 起步 | Speed | 左 | 转身,
        起步_Speed_后右 = 起步 | Speed | 右 | 转身,

        原地转身_向后 = Idle | 转身 | 后,
        原地转身_向左 = Idle | 转身 | 左,
        原地转身_向右 = Idle | 转身 | 右,

        Walk急停 = Walk | 急停,
        Walk急停转身_向后 = Walk | 急停 | 转身 | 后,
        Walk急停转身_向左 = Walk | 急停 | 转身 | 左,
        Walk急停转身_向右 = Walk | 急停 | 转身 | 右,

        Run急停 = Run | 急停,
        Run急停转身_向后 = Run | 急停 | 转身 | 后,
        Run急停转身_向左 = Run | 急停 | 转身 | 左,
        Run急停转身_向右 = Run | 急停 | 转身 | 右,

        Speed急停 = Speed | 急停,
        Speed急停转身_向后 = Speed | 急停 | 转身 | 后,
        Speed急停转身_向左 = Speed | 急停 | 转身 | 左,
        Speed急停转身_向右 = Speed | 急停 | 转身 | 右,

        其他Or技能
    }
}
