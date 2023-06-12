using Lockstep.Math;
using UnityEngine;

namespace Lockstep.Game {
    public class AnimatorView : MonoBehaviour, IAnimatorView {
        public Animation animComp;
        public Transform rootTrans;
        public AnimationState animState;
        private CAnimator cAnim;
        private Animator anim;
        public LFloat speed;

        void Start(){
            if (animComp == null) {
                animComp = GetComponent<Animation>();
                if (animComp == null) {
                    animComp = GetComponentInChildren<Animation>();
                }
            }
        }

        public void SetInteger(string name, int val){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"val: {val} ");

            anim.SetInteger(name, val);
        }

        public void SetTrigger(string name){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"");

            anim.SetTrigger(name);
        }

        public void Play(string name, bool isCross){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"");

            animState = animComp[name];
            var state = animComp[name];
            if (state != null) {
                if (isCross) {
                    animComp.CrossFade(name);
                }
                else {
                    animComp.Play(name);
                }
            }
        }

        public void LateUpdate(){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"");

            if (cAnim.curAnimBindInfo != null && cAnim.curAnimBindInfo.isMoveByAnim) {
                rootTrans.localPosition = Vector3.zero;
            }
        }

        public void Sample(LFloat time){        //NOTE: AutoCreate LockstepLog
        LogMaster.L($"time: {time} ");

            if (Application.isPlaying) {
                return;
            }

            if (animState == null) return;
            if (!Application.isPlaying) {
                animComp.Play();
            }

            animState.enabled = true;
            animState.weight = 1;
            animState.time = time.ToFloat();
            animComp.Sample();
            if (!Application.isPlaying) {
                animState.enabled = false;
            }
        }
    }
}