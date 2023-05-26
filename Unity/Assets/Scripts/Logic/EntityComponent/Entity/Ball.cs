/*
 * @Author: delevin.ying
 * @Date: 2023-05-25 15:33:37
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-05-26 16:49:18
 */
using System;
using Lockstep.Game;
using Lockstep.Math;

namespace Lockstep.Game
{
    [Serializable]
    public partial class Ball : BaseEntity
    {
        public CBullet bullet = new CBullet();

        protected override void BindRef()
        {
            base.BindRef();
            RegisterComponent(bullet);
        }

        public override void DoUpdate(LFloat deltaTime)
        {
            //LogMaster.L(" bullet--- " + bullet.ToString());

            var deltaLV3 = deltaTime*bullet.speedLV3;
            this.transform.pos += deltaLV3.ToLVector2_Y();
            this.transform.y += deltaLV3.y;
        }
    }
}
