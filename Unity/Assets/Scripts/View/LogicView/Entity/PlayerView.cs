using Lockstep.Game;
using Lockstep.Math;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
namespace Lockstep.Game
{
    public class PlayerView : EntityView, IPlayerView
    {
        public Player Player;
        protected bool isDead => entity?.isDead ?? true;

        public override void BindEntity(BaseEntity e, BaseEntity oldEntity = null)
        {
            base.BindEntity(e, oldEntity);
            Player = e as Player;

            
            //Player.
        }

        //private void Update()
        //{
        //    if (World.MyPlayer == null) return;
        //    var isMaster = World.MyPlayer.EntityId == Player.EntityId;

        //    if (isMaster)
        //    {
        //        this.gameObject.GetComponentInChildren<CinemachineVirtualCamera>()?.gameObject.SetActive(true);
        //    }
        //}
    }
}