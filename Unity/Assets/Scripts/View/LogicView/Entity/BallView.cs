using Lockstep.Game;
using Lockstep.Math;
using UnityEngine;

namespace Lockstep.Game {
    public class BallView : BaseEntityView, IEntityView {
        // public UIFloatBar uiFloatBar;
        //public Entity entity;
        public Ball ball;
        // protected bool isDead => entity?.isDead ?? true;

        public override void BindEntity(BaseEntity e, BaseEntity oldEntity = null){
            base.BindEntity(e,oldEntity);
            e.EntityView = this;
            this.ball = e as Ball;
        }



        public override void OnDead(){
            GameObject.Destroy(gameObject);
        }

        public override void OnRollbackDestroy(){
            GameObject.Destroy(gameObject);
        }

        private void Update(){
            var pos = ball.transform.Pos3.ToVector3();
            transform.position = Vector3.Lerp(transform.position, pos, 0.3f);
            var deg = ball.transform.deg.ToFloat();
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, deg, 0), 0.3f);
        }    
    }
}