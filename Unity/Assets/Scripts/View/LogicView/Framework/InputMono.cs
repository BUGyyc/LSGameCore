using Lockstep.Collision2D;
using Lockstep.Game;
using Lockstep.GameCore;
using Lockstep.Math;
using UnityEngine;
using Debug = Lockstep.Logging.Debug;
using Lockstep.GameCore.Input;

namespace Lockstep.Game
{
    public class InputMono : UnityEngine.MonoBehaviour
    {
        private static bool IsReplay => Launcher.Instance?.IsVideoMode ?? false;
        [HideInInspector] public int floorMask;
        public float camRayLength = 100;

        public bool hasHitFloor;
        public LVector2 mousePos;
        public LVector2 inputUV;
        public bool isInputFire;
        public int skillId;
        public bool isSpeedUp;

        private BaseInputAction clientInput;

        void Start()
        {
            floorMask = LayerMask.GetMask("Floor");

            clientInput = new BaseInputAction();

            clientInput.Enable();
            clientInput.Player.Enable();

            //clientInput.Player.Fire.performed +=
        }

        private void OnEnable()
        {
            //clientInput.Enable();
            //clientInput.Player.Enable();
        }

        private void OnDisable()
        {
            //clientInput.Disable();
            //clientInput.Player.Disable();
        }




        public void Update()
        {
            if (World.Instance != null && !IsReplay)
            {

                //if (clientInput.Player.Move.IsPressed() == false) return;

                //NOTE：移动和镜头移动合并，比较特殊
                var look = clientInput.Player.Look.ReadValue<Vector2>();
                var move = clientInput.Player.Move.ReadValue<Vector2>();

                //inputUV = new LVector2(move.x.ToLFloat(), move.y.ToLFloat());

                //LogMaster.I("input:" + inputUV + "  " + move);
                var moveDir = CharacterMoveFirstPerson.OnCharacterMoveInput(move, Camera.main.transform.forward);
                inputUV = moveDir;
                skillId = clientInput.Player.Fire.IsPressed() ? 1 : 0;


                //LogMaster.I("isInputFire:" + isInputFire);
                //return;

                //--------------------------------------------------------

                //float h = Input.GetAxisRaw("Horizontal");
                //float v = Input.GetAxisRaw("Vertical");
                //inputUV = new LVector2(h.ToLFloat(), v.ToLFloat());



                //isInputFire = Input.GetButton("Fire1");
                //hasHitFloor = Input.GetMouseButtonDown(1);
                //if (hasHitFloor)
                //{
                //    Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                //    RaycastHit floorHit;
                //    if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
                //    {
                //        mousePos = floorHit.point.ToLVector2XZ();
                //    }
                //}

                //skillId = 0;
                //for (int i = 0; i < 6; i++)
                //{
                //    if (Input.GetKey(KeyCode.Keypad1 + i))
                //    {
                //        skillId = i + 1;
                //    }
                //}

                isSpeedUp = Input.GetKeyDown(KeyCode.Space);
                GameInputService.CurGameInput = new PlayerInput()
                {
                    mousePos = mousePos,
                    inputUV = inputUV,
                    isInputFire = isInputFire,
                    skillId = skillId,
                    isSpeedUp = isSpeedUp,
                };
            }
        }

        public LVector2 GetMoveDir(Vector3 keyBoard, Vector3 cameraDir)
        {
            return default;
        }
    }
}