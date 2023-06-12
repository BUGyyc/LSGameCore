using System.Collections.Generic;
using Lockstep.Collision2D;
using Lockstep.Math;
using Lockstep.Util;
using Lockstep.Game;

namespace Lockstep.Game {
    public class HashSystem : BaseSystem {
        public override void DoUpdate(LFloat deltaTime){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"deltaTime: {deltaTime} ");

            //_commonStateService.Hash = GetHash(_gameStateService);
        }

        //{string.Format("{0:yyyyMMddHHmmss}", DateTime.Now)}_
   
    }
}