/*
 * @Author: delevin.ying
 * @Date: 2023-05-24 15:58:47
 * @Last Modified by:   delevin.ying
 * @Last Modified time: 2023-05-24 15:58:47
 */
#define DEBUG_FRAME_DELAY
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lockstep.ECS;
using Lockstep.Math;
using Lockstep.Serialization;
using Lockstep.Util;
using Lockstep.Game;
using NetMsg.Common;
// #if UNITY_EDITOR
// using UnityEngine;
// #endif
using Debug = Lockstep.Logging.Debug;
using Logger = Lockstep.Logging.Logger;

namespace Lockstep.Game
{
    public class SimulatorService : BaseGameService, ISimulatorService, IDebugService
    {
        public static SimulatorService Instance { get; private set; }

        /// <summary>
        /// 回滚帧号
        /// </summary>
        public int __debugRollbackToTick;

        //FIXME:
        public const long MinMissFrameReqTickDiff = 10;

        //! TODO: 提前帧号？？？？  实际与追帧有关
        public const long MaxSimulationMsPerFrame = 20;

        /// <summary>
        ///! 最大预测帧号
        /// </summary>
        public const int MaxPredictFrameCount = 30;

        /// <summary>
        /// 局部历史 平均 ping
        /// </summary>
        public int PingVal => _cmdBuffer?.PingVal ?? 0;

        /// <summary>
        /// 局部历史 平均 delay
        /// </summary>
        public int DelayVal => _cmdBuffer?.DelayVal ?? 0;

        // components
        public World World => _world;
        private World _world;
        private IFrameBuffer _cmdBuffer;

        /// <summary>
        /// Hash 计算相关
        /// </summary>
        private HashHelper _hashHelper;

        //TODO: 本地操作记录，可持久化至本地，待验证
        private DumpHelper _dumpHelper;

        // game status
        private Msg_G2C_GameStartInfo _gameStartInfo;
        public byte LocalActorId { get; private set; }

        /// <summary>
        /// 全部玩家
        /// </summary>
        private byte[] _allActors;

        /// <summary>
        /// 玩家数量
        /// </summary>
        private int _actorCount => _allActors.Length;
        private PlayerInput[] _playerInputs => _world.PlayerInputs;
        public bool IsRunning { get; set; }

        //! 根据网络状况调整预测帧数
        //! TODO: 这里的预测，相当于关闭了
        /// frame count that need predict(TODO should change according current network's delay)
        public int FramePredictCount = 0; //~~~

        /// <summary>
        /// 游戏开局的时间戳
        /// </summary>
        public long _gameStartTimestampMs = -1;

        /// <summary>
        /// 始于游戏开局的帧号
        /// </summary>
        private int _tickSinceGameStart;

        /// <summary>
        /// 目标帧号，模拟预测的目标最大帧号
        /// </summary>
        public int TargetTick => _tickSinceGameStart + FramePredictCount;

        // input preSend
        /// <summary>
        /// ?? 需要发送的输入指令
        /// NOTE： 记录当前环境下，能进行广播的最大数量帧
        /// </summary>
        public int PreSendInputCount = 1; //~~~
        public int inputTick = 0;

        /// <summary>
        /// ! 根据网络波动而变化的 最大可输入 帧数据，相当于 窗口 Size
        /// </summary>
        public int inputTargetTick => _tickSinceGameStart + PreSendInputCount;

        //video mode
        private Msg_RepMissFrame _videoFrames;
        private bool _isInitVideo = false;
        private int _tickOnLastJumpTo;
        private long _timestampOnLastJumpToMs;

        private bool _isDebugRollback = false;

        //refs
        private IManagerContainer _mgrContainer;
        private IServiceContainer _serviceContainer;

        public int snapshotFrameInterval = 1;
        private bool _hasRecvInputMsg;

        public SimulatorService()
        {
            Instance = this;
        }

        public override void InitReference(
            IServiceContainer serviceContainer,
            IManagerContainer mgrContainer
        )
        {
            base.InitReference(serviceContainer, mgrContainer);
            _serviceContainer = serviceContainer;
            _mgrContainer = mgrContainer;
        }

        public override void DoStart()
        {
            snapshotFrameInterval = 1;
            if (_constStateService.IsVideoMode)
            {
                snapshotFrameInterval = _constStateService.SnapshotFrameInterval;
            }

            _cmdBuffer = new FrameBuffer(
                this,
                _networkService,
                2000,
                snapshotFrameInterval,
                MaxPredictFrameCount
            );
            _world = new World();
            _hashHelper = new HashHelper(_serviceContainer, _world, _networkService, _cmdBuffer);
            _dumpHelper = new DumpHelper(_serviceContainer, _world, _hashHelper);
        }

        public override void DoDestroy()
        {
            IsRunning = false;
            _dumpHelper.DumpAll();
        }

        public void OnGameCreate(
            int targetFps,
            byte localActorId,
            byte actorCount,
            bool isNeedRender = true
        )
        {
            FrameBuffer.__debugMainActorID = localActorId;
            var allActors = new byte[actorCount];
            for (byte i = 0; i < actorCount; i++)
            {
                allActors[i] = i;
            }

            Debug.Log($"GameCreate " + LocalActorId);

            //Init game status
            //_localActorId = localActorId;
            _allActors = allActors;
            _constStateService.LocalActorId = LocalActorId;
            _world.StartSimulate(_serviceContainer, _mgrContainer);
            EventHelper.Trigger(EEvent.LevelLoadProgress, 1f);
        }

        public void StartSimulate()
        {
            if (IsRunning)
            {
                Debug.LogError("Already started!");
                return;
            }

            IsRunning = true;
            if (_constStateService.IsClientMode)
            {
                _gameStartTimestampMs = LTime.realtimeSinceStartupMS;
            }

            _world.StartGame(_gameStartInfo, LocalActorId);
            Debug.Log($"[Client]  Game Start");
            LogMaster.L(
                $"-------------------------------游戏启动 ---------------------------------------------------------------------- "
            );
            EventHelper.Trigger(EEvent.SimulationStart, null);

            //! ??? 首帧落后较多，将多个输入发送出去，有风险
            while (inputTick < PreSendInputCount)
            {
                SendInputs(inputTick++);
            }
        }

        public void Trace(string msg, bool isNewLine = false, bool isNeedLogTrace = false)
        {
            _dumpHelper.Trace(msg, isNewLine, isNeedLogTrace);
        }

        /// <summary>
        ///! 特殊的重播模式，用来跳帧
        /// </summary>
        /// <param name="tick"></param>
        public void JumpTo(int tick)
        {
            if (tick + 1 == _world.Tick || tick == _world.Tick)
                return;
            tick = LMath.Min(tick, _videoFrames.frames.Length - 1);
            var time = LTime.realtimeSinceStartupMS + 0.05f;
            if (!_isInitVideo)
            {
                _constStateService.IsVideoLoading = true;
                while (_world.Tick < _videoFrames.frames.Length)
                {
                    var sFrame = _videoFrames.frames[_world.Tick];
                    Simulate(sFrame, true);
                    if (LTime.realtimeSinceStartupMS > time)
                    {
                        EventHelper.Trigger(
                            EEvent.VideoLoadProgress,
                            _world.Tick * 1.0f / _videoFrames.frames.Length
                        );
                        return;
                    }
                }

                _constStateService.IsVideoLoading = false;
                EventHelper.Trigger(EEvent.VideoLoadDone);
                _isInitVideo = true;
            }

            if (_world.Tick > tick)
            {
                RollbackTo(tick, _videoFrames.frames.Length, false);
            }

            while (_world.Tick <= tick)
            {
                var sFrame = _videoFrames.frames[_world.Tick];
                Simulate(sFrame, false);
            }

            _viewService.RebindAllEntities();
            _timestampOnLastJumpToMs = LTime.realtimeSinceStartupMS;
            _tickOnLastJumpTo = tick;
        }

        /// <summary>
        /// ! 驱动重播模式
        /// </summary>
        public void RunVideo()
        {
            if (_tickOnLastJumpTo == _world.Tick)
            {
                _timestampOnLastJumpToMs = LTime.realtimeSinceStartupMS;
                _tickOnLastJumpTo = _world.Tick;
            }

            var frameDeltaTime = (LTime.timeSinceLevelLoad - _timestampOnLastJumpToMs) * 1000;
            var targetTick =
                System.Math.Ceiling(frameDeltaTime / NetworkDefine.UPDATE_DELTATIME)
                + _tickOnLastJumpTo;
            while (_world.Tick <= targetTick)
            {
                if (_world.Tick < _videoFrames.frames.Length)
                {
                    var sFrame = _videoFrames.frames[_world.Tick];
                    Simulate(sFrame, false);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// !!!!!!!  Lockstep Client Core Logic
        /// </summary>
        /// <param name="deltaTime"></param>
        public void DoUpdate(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            if (_hasRecvInputMsg)
            {
                if (_gameStartTimestampMs == -1)
                {
                    //! 初始启动时间戳
                    _gameStartTimestampMs = LTime.realtimeSinceStartupMS;
                }
            }

            if (_gameStartTimestampMs <= 0)
            {
                return;
            }

            //! 计算帧号
            _tickSinceGameStart = (int)(
                (LTime.realtimeSinceStartupMS - _gameStartTimestampMs)
                / NetworkDefine.UPDATE_DELTATIME
            );
            if (_constStateService.IsVideoMode)
            {
                return;
            }

            #region  Debug Rollback
            if (__debugRollbackToTick > 0)
            {
                GetService<ICommonStateService>().IsPause = true;
                RollbackTo(__debugRollbackToTick, 0, false);
                __debugRollbackToTick = -1;
            }
            #endregion

            if (_commonStateService.IsPause)
            {
                return;
            }

            _cmdBuffer.DoUpdate(deltaTime);

            //client mode no network
            if (_constStateService.IsClientMode)
            {
                DoClientUpdate();
            }
            else
            {
                int tempTick = inputTick;
                //! 网络延迟大的情况下，InputTargetTick 更大，增多网络差客户端的预测，以及推进游戏
                while (inputTick <= inputTargetTick)
                {
                    if (APP.DebugClientNetDelay == false)
                    {
                        //! 客户端发送本地需要发送的输入指令
                        SendInputs(inputTick++);
                    }
                    else
                    {
                        //! 测试弱网络的情况下，网络包延迟
                        _DebugLowNet(tempTick);
                    }
                    tempTick++;
                }

                DoNormalUpdate();
            }
        }

        /// <summary>
        /// ! 测试弱网络情况
        /// </summary>
        public void _DebugLowNet(int tempTick)
        {
            if (APP.DebugClientNetDelayStartFrame == 0)
            {
                //开始随机
                bool DelayResult =
                    UnityEngine.Random.Range(0, 1f) <= APP.DebugClientNetDelayPercent;

                if (DelayResult)
                {
                    //随机出一个持续帧号
                    APP.DebugClientNetDelayCompareFrame = UnityEngine.Random.Range(
                        APP.DebugMinClientNetDelayFrameCount,
                        APP.DebugMaxClientNetDelayFrameCount
                    );
                    //记录当前帧号
                    APP.DebugClientNetDelayStartFrame = tempTick;
                }
                else
                {
                    SendInputs(inputTick++);
                }
            }
            else if (
                tempTick - APP.DebugClientNetDelayStartFrame >= APP.DebugClientNetDelayCompareFrame
            )
            {
                //清空
                APP.DebugClientNetDelayStartFrame = 0;
                APP.DebugClientNetDelayCompareFrame = 0;
            }
            else
            {
                LogMaster.L($"模拟网络延迟 tempTick {tempTick}    ");
            }
        }

        /// <summary>
        /// 纯客户端模式
        /// </summary>
        private void DoClientUpdate()
        {
            int maxRollbackCount = 5;
            if (
                _isDebugRollback
                && _world.Tick > maxRollbackCount
                && _world.Tick % maxRollbackCount == 0
            )
            {
                var rawTick = _world.Tick;
                var revertCount = LRandom.Range(1, maxRollbackCount);
                for (int i = 0; i < revertCount; i++)
                {
                    var input = new Msg_PlayerInput(
                        _world.Tick,
                        LocalActorId,
                        _inputService.GetDebugInputCmds()
                    );
                    var frame = new ServerFrame()
                    {
                        tick = rawTick - i,
                        _inputs = new Msg_PlayerInput[] { input }
                    };
                    _cmdBuffer.ForcePushDebugFrame(frame);
                }
                _debugService.Trace("RollbackTo " + (_world.Tick - revertCount));
                if (!RollbackTo(_world.Tick - revertCount, _world.Tick))
                {
                    _commonStateService.IsPause = true;
                    return;
                }

                while (_world.Tick < rawTick)
                {
                    var sFrame = _cmdBuffer.GetServerFrame(_world.Tick);
                    Logging.Debug.Assert(
                        sFrame != null && sFrame.tick == _world.Tick,
                        $" logic error: server Frame  must exist tick {_world.Tick}"
                    );
                    _cmdBuffer.PushLocalFrame(sFrame);
                    Simulate(sFrame);
                    if (_commonStateService.IsPause)
                    {
                        return;
                    }
                }
            }

            while (_world.Tick < TargetTick)
            {
                FramePredictCount = 0;
                var input = new Msg_PlayerInput(
                    _world.Tick,
                    LocalActorId,
                    _inputService.GetInputCmds()
                );
                var frame = new ServerFrame()
                {
                    tick = _world.Tick,
                    _inputs = new Msg_PlayerInput[] { input }
                };
                _cmdBuffer.PushLocalFrame(frame);
                _cmdBuffer.PushServerFrames(new ServerFrame[] { frame });
                Simulate(_cmdBuffer.GetFrame(_world.Tick));
                if (_commonStateService.IsPause)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// ! 帧同步。客户端方面的主要工作
        /// </summary>
        private void DoNormalUpdate()
        {
            var maxContinueServerTick = _cmdBuffer.MaxContinueServerTick;
            if ((_world.Tick - maxContinueServerTick) > MaxPredictFrameCount)
            {
                //! 防止网络很好的玩家，领先服务器太多帧号，理论上不会这样，除非单链路卡顿，或者服务器卡顿等奇怪原因
                //NOTE: 超过了最大预测帧号，不需要再执行，等待
                return;
            }

            //! 备份里面，最小的帧号
            var minTickToBackup = (
                maxContinueServerTick - (maxContinueServerTick % snapshotFrameInterval)
            );

            //! 追帧触发线 （20ms）
            var deadline = LTime.realtimeSinceStartupMS + MaxSimulationMsPerFrame;

            //! 当前帧 小于 服务器下发的准确帧 （这里的准确帧是通过了本地帧数据验证的 缓冲 ）
            while (_world.Tick < _cmdBuffer.CurTickInServer)
            {
                //! 进行追帧
                var tick = _world.Tick;
                var sFrame = _cmdBuffer.GetServerFrame(tick);
                if (sFrame == null)
                {
                    //! 没有拿到服务器帧，进行一次追帧
                    OnPursuingFrame();
                    return;
                }

                //TODO: 这里等同于 用 Server 缓冲写入 Client 缓冲？？？？
                _cmdBuffer.PushLocalFrame(sFrame);

                //! 立即模拟一帧
                Simulate(sFrame, tick == minTickToBackup);

                //! 这里其实是为了分散追帧压力，不用在一帧内追太多，及时退出 while ,然后下一帧接着追帧，
                if (LTime.realtimeSinceStartupMS > deadline)
                {
                    //! 理论上无法触发
                    //追帧
                    OnPursuingFrame();
                    return;
                }
            }

            if (_constStateService.IsPursueFrame)
            {
                //完成 PursueFrame
                _constStateService.IsPursueFrame = false;
                EventHelper.Trigger(EEvent.PursueFrameDone);
            }

            //! Roll back
            if (_cmdBuffer.IsNeedRollback)
            {
                //! 回滚 Core
                RollbackTo(_cmdBuffer.NextTickToCheck, maxContinueServerTick);
                //! 清理掉无用的 快照
                CleanUselessSnapshot(System.Math.Min(_cmdBuffer.NextTickToCheck - 1, _world.Tick));
                //! 本地的备份帧号
                minTickToBackup = System.Math.Max(minTickToBackup, _world.Tick + 1);

                //! 开始追帧，这里应该设定一个追帧极限，不然一帧模拟太多，容易造成极大的性能峰值
                while (_world.Tick <= maxContinueServerTick)
                {
                    var sFrame = _cmdBuffer.GetServerFrame(_world.Tick);
                    Logging.Debug.Assert(
                        sFrame != null && sFrame.tick == _world.Tick,
                        $" logic error: server Frame  must exist tick {_world.Tick}"
                    );

                    //将服务器广播的帧数据，直接塞入到本地
                    _cmdBuffer.PushLocalFrame(sFrame);
                    //通过这一帧数据，去模拟一帧游戏
                    Simulate(sFrame, _world.Tick == minTickToBackup);
                }
            }

            //! 进行预测模拟， FramePredictCount 一直是 0 ??? TODO: 预测窗口Size 要随网络状况波动
            while (_world.Tick <= TargetTick)
            {
                var curTick = _world.Tick;
                ServerFrame frame = null;
                var sFrame = _cmdBuffer.GetServerFrame(curTick);
                if (sFrame != null)
                {
                    //! 如果服务器有下发帧数据，就用服务器的帧数据，那么网络快的玩家可能作弊
                    frame = sFrame;
                }
                else
                {
                    //! 如果没有收到服务器广播帧
                    //! 那么就取本地帧
                    var cFrame = _cmdBuffer.GetLocalFrame(curTick);
                    //! 猜测其他玩家的输入
                    FillInputWithLastFrame(cFrame);
                    frame = cFrame;
                }

                //! 网络好的状况下，本地可能略快于服务器的广播，那么缓冲就是本地缓冲，本地缓冲会和serverBuffer 做对比，如果不一致就回滚
                _cmdBuffer.PushLocalFrame(frame);
                //! 进行预测
                Predict(frame, true);
            }

            //! 发送需要验证的 HashCode
            _hashHelper.CheckAndSendHashCodes();
        }

        /// <summary>
        /// !!!!!
        /// </summary>
        /// <param name="curTick"></param>
        void SendInputs(int curTick)
        {
            var input = new Msg_PlayerInput(curTick, LocalActorId, _inputService.GetInputCmds());
            //! 创建了一个帧缓冲 , 这里每帧都是创建，//TODO: 后续需要从对象池中取
            var cFrame = new ServerFrame();
            var inputs = new Msg_PlayerInput[_actorCount];
            inputs[LocalActorId] = input;
            cFrame.Inputs = inputs;
            cFrame.tick = curTick;

            LogMaster.L($"[Client]  手机到客户端输入 curTick {curTick}");

            //TODO: 客户端需要预测，所以对方客户端输入，用历史上一帧的结果进行预测
            FillInputWithLastFrame(cFrame);

            //TODO: 对于一次客户端输入，认为输入是可靠的，因此生成了一个 Client 缓冲
            _cmdBuffer.PushLocalFrame(cFrame);

            if (curTick > _cmdBuffer.MaxServerTickInBuffer)
            {
                LogMaster.L($"[Client]  client execute SendInput curTick {curTick}");
                //TODO: combine all history inputs into one Msg
                _cmdBuffer.SendInput(input);
            }
        }

        /// <summary>
        /// 模拟一帧
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="isNeedGenSnap"></param>
        private void Simulate(ServerFrame frame, bool isNeedGenSnap = true)
        {
            Step(frame, isNeedGenSnap);
        }

        /// <summary>
        /// 预测一帧
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="isNeedGenSnap"></param>
        private void Predict(ServerFrame frame, bool isNeedGenSnap = true)
        {
            Step(frame, isNeedGenSnap);
        }

        /// <summary>
        /// ! 回滚到指定帧
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="maxContinueServerTick"></param>
        /// <param name="isNeedClear"></param>
        /// <returns></returns>
        private bool RollbackTo(int tick, int maxContinueServerTick, bool isNeedClear = true)
        {
            _world.RollbackTo(tick, maxContinueServerTick, isNeedClear);
            var hash = _commonStateService.Hash;
            var curHash = _hashHelper.CalcHash();
            if (hash != curHash)
            {
                Debug.LogError(
                    $"tick:{tick} Rollback error: Hash isDiff oldHash ={hash}  curHash{curHash}"
                );
#if UNITY_EDITOR
                _dumpHelper.DumpToFile(true);
                return false;
#endif
            }
            return true;
        }

        /// <summary>
        /// ! Logic 推进
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="isNeedGenSnap"></param>
        void Step(ServerFrame frame, bool isNeedGenSnap = true)
        {
            //Debug.Log("Step: " + _world.Tick + " TargetTick: " + TargetTick);
            //! 设置新的帧号
            _commonStateService.SetTick(_world.Tick);
            //! 计算这一帧的 hash
            var hash = _hashHelper.CalcHash();
            _commonStateService.Hash = hash;
            //! 备份这一帧的数据
            _timeMachineService.Backup(_world.Tick);
            DumpFrame(hash);
            hash = _hashHelper.CalcHash(true);
            _hashHelper.SetHash(_world.Tick, hash);
            //! 处理帧缓冲中的数据
            ProcessInputQueue(frame);
            //! 真正的模拟这一帧
            _world.Step(isNeedGenSnap);
            _dumpHelper.OnFrameEnd();
            var tick = _world.Tick;
            _cmdBuffer.SetClientTick(tick);
            //clean useless snapshot
            if (isNeedGenSnap && tick % snapshotFrameInterval == 0)
            {
                CleanUselessSnapshot(System.Math.Min(_cmdBuffer.NextTickToCheck - 1, _world.Tick));
            }
        }

        private void CleanUselessSnapshot(int tick)
        {
            //TODO
        }

        private void DumpFrame(int hash)
        {
            if (_constStateService.IsClientMode)
            {
                _dumpHelper.DumpFrame(!_hashHelper.TryGetValue(_world.Tick, out var val));
            }
            else
            {
                _dumpHelper.DumpFrame(true);
            }
        }

        /// <summary>
        /// ! 除自己外，其他玩家都用上一次的历史输入
        /// </summary>
        /// <param name="frame"></param>
        private void FillInputWithLastFrame(ServerFrame frame)
        {
            int tick = frame.tick;
            var inputs = frame.Inputs;
            var lastServerInputs = tick == 0 ? null : _cmdBuffer.GetFrame(tick - 1)?.Inputs;
            var myInput = inputs[LocalActorId];
            //! 先覆盖所有
            for (int i = 0; i < _actorCount; i++)
            {
                inputs[i] = new Msg_PlayerInput(
                    tick,
                    _allActors[i],
                    lastServerInputs?[i]?.Commands
                );
            }
            //! 这里再覆盖一次，是因为循环中，全部玩家用了历史最后一帧的输入，但是本地玩家用客观的最新输入，这是当前的输入
            inputs[LocalActorId] = myInput;
        }

        /// <summary>
        /// ! 处理输入队列
        /// </summary>
        /// <param name="frame"></param>
        private void ProcessInputQueue(ServerFrame frame)
        {
            var inputs = frame.Inputs;
            foreach (var playerInput in _playerInputs)
            {
                //先清空，方便写入
                playerInput.Reset();
            }

            foreach (var input in inputs)
            {
                if (input.Commands == null)
                    continue;
                if (input.ActorId >= _playerInputs.Length)
                    continue;

                //! 这里描述为 InputEntity ，太牵强了~~~~~
                var inputEntity = _playerInputs[input.ActorId];
                foreach (var command in input.Commands)
                {
                    Logger.Trace(
                        this,
                        input.ActorId + " >> " + input.Tick + ": " + input.Commands.Count()
                    );
                    //! 把 Command 内的数据写入 inputEntity
                    _inputService.Execute(command, inputEntity);
                }
            }
        }

        /// <summary>
        /// ? 追帧
        /// </summary>
        void OnPursuingFrame()
        {
            _constStateService.IsPursueFrame = true;
            Debug.Log($"Purchase Servering curTick:" + _world.Tick);
            var progress = _world.Tick * 1.0f / _cmdBuffer.CurTickInServer;

            //! 等待帧号追上 服务器中的 帧号

            EventHelper.Trigger(EEvent.PursueFrameProcess, progress);
        }

        #region NetEvents

        void OnEvent_BorderVideoFrame(object param)
        {
            _videoFrames = param as Msg_RepMissFrame;
        }

        /// <summary>
        /// ! 服务器广播的帧数据
        /// </summary>
        /// <param name="param"></param>
        void OnEvent_OnServerFrame(object param)
        {
            var msg = param as Msg_ServerFrames;
            _hasRecvInputMsg = true;

            _cmdBuffer.PushServerFrames(msg.frames);
        }

        void OnEvent_OnServerMissFrame(object param)
        {
            Debug.Log($"OnEvent_OnServerMissFrame");
            var msg = param as Msg_RepMissFrame;
            _cmdBuffer.PushMissServerFrames(msg.frames, false);
        }

        /// <summary>
        /// ! 客户端接收到服务器心跳包
        /// </summary>
        /// <param name="param"></param>
        void OnEvent_OnPlayerPing(object param)
        {
            var msg = param as Msg_G2C_PlayerPing;
            _cmdBuffer.OnPlayerPing(msg);
        }

        void OnEvent_OnServerHello(object param)
        {
            var msg = param as Msg_G2C_Hello;
            LocalActorId = msg.LocalId;
            Debug.Log("OnEvent_OnServerHello " + LocalActorId);
        }

        void OnEvent_OnGameCreate(object param)
        {
            if (param is Msg_G2C_Hello msg)
            {
                OnGameCreate(60, msg.LocalId, msg.UserCount);
            }

            if (param is Msg_G2C_GameStartInfo smsg)
            {
                _gameStartInfo = smsg;
                OnGameCreate(60, 0, smsg.UserCount);
            }

            EventHelper.Trigger(EEvent.SimulationInit, null);
        }

        /// <summary>
        /// ! 客户端启动
        /// </summary>
        /// <param name="param"></param>
        void OnEvent_OnAllPlayerFinishedLoad(object param)
        {
            Debug.Log($"OnEvent_OnAllPlayerFinishedLoad");
            StartSimulate();
        }

        void OnEvent_LevelLoadDone(object param)
        {
            Debug.Log($"OnEvent_LevelLoadDone " + _constStateService.IsReconnecting);
            if (
                _constStateService.IsReconnecting
                || _constStateService.IsVideoMode
                || _constStateService.IsClientMode
            )
            {
                StartSimulate();
            }
        }

        #endregion
    }
}
