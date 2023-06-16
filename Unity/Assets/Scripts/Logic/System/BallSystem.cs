/*
 * @Author: delevin.ying 
 * @Date: 2023-05-26 16:39:53 
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-05-26 16:40:23
 */
using System.Collections.Generic;
using Lockstep.Game;
using Lockstep.Math;
using UnityEngine;
using Debug = Lockstep.Logging.Debug;
using Lockstep.GameCore;

namespace Lockstep.Game
{
    public class BallSystem : BaseSystem
    {
        private Ball[] allBall => _gameStateService.GetBalls();

        public override void DoStart()
        {
           
        }

        public override void DoUpdate(LFloat deltaTime)
        {


           
            foreach (var ball in allBall)
            {
                ball.DoUpdate(deltaTime);
            }
        }
    }
}
