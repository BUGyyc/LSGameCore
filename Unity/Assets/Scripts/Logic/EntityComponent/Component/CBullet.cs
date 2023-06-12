/*
 * @Author: delevin.ying 
 * @Date: 2023-05-26 16:30:47 
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-05-26 16:44:56
 */
using System;
using Lockstep.Collision2D;
using Lockstep.Game;
using Lockstep.Math;

namespace Lockstep.Game {
    
    [Serializable]
    public partial class CBullet : Component {
        // public Ball ball => (Ball) entity;
        // public PlayerInput input => player.input;
        public LVector3 speedLV3;
        
        // static LFloat _sqrStopDist = new LFloat(true, 40);
        // public LFloat speed => player.moveSpd;
        // public bool hasReachTarget = false;
        // public bool needMove = true;

        public override void DoUpdate(LFloat deltaTime){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"deltaTime: {deltaTime} ");

            // if (!entity.rigidbody.isOnFloor) {
            //     return;
            // }

            // var needChase = input.inputUV.sqrMagnitude > new LFloat(true, 10);
            // if (needChase) {
            //     var dir = input.inputUV.normalized;
            //     transform.pos = transform.pos + dir * speed * deltaTime;
            //     var targetDeg = dir.ToDeg();
            //     transform.deg = CTransform2D.TurnToward(targetDeg, transform.deg, player.turnSpd * deltaTime, out var hasReachDeg);
            // }

            // hasReachTarget = !needChase;
        }
    }
}