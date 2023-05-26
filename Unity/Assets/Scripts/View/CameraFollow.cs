using UnityEngine;
using System.Collections;
using Lockstep.Game;

namespace Lockstep.Game
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform _target;

        public CameraType cameraType;

        public bool DeFellow = true;

        public Transform target
        {
            get => _target;
            set
            {
                _target = value;
                if (_target != null)
                    offset = transform.position - _target.position;
            }
        } // The position that that camera will be following.

        public float smoothing = 5f; // The speed with which the camera will be following.


        Vector3 offset; // The initial offset from the target.

        bool hasSetVisualCamera;
        void Update()
        {
            //if (DeFellow) return;
            if (_target == null)
            {
                target = World.MyPlayerTrans as Transform;
            }
            Vector3 targetCamPos = default;
            switch (cameraType)
            {
                case CameraType.RTS:


                    if (_target == null) return;

                    targetCamPos = target.position + offset;

                    transform.position = Vector3.Lerp(transform.position, targetCamPos, 0.1f);
                    break;

                case CameraType.FPS:
                    if (_target == null) return;

                    if (hasSetVisualCamera == false)
                    {
                        var vac = target.GetComponentInChildren<Cinemachine.CinemachineVirtualCamera>();

                        if (vac != null)
                        {
                            vac.enabled = true;
                            hasSetVisualCamera = true;
                        }
                    }

                    //targetCamPos = target.position + 1.8f * Vector3.up;
                    //transform.position = Vector3.Lerp(transform.position, targetCamPos, 0.3f);

                    //var angleV3 = target.rotation.eulerAngles;
                    //var newQ = Quaternion.Euler(0, angleV3.y, 0);

                    //transform.rotation = Quaternion.Lerp(transform.rotation, newQ, 0.1f);

                    break;
            }
        }
    }


    public enum CameraType
    {
        RTS,
        FPS,
        ACT,
        TPS,
    }
}