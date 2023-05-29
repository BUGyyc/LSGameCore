#define DEBUG_FRAME_DELAY
using System;
using System.Collections.Generic;
using System.Linq;
using Lockstep.Math;
using Lockstep.Serialization;
using Lockstep.Util;
using NetMsg.Common;
using Debug = Lockstep.Logging.Debug;

namespace Lockstep.Game
{
    public interface IFrameBuffer
    {
        void ForcePushDebugFrame(ServerFrame frame);
        void PushLocalFrame(ServerFrame frame);
        void PushServerFrames(ServerFrame[] frames, bool isNeedDebugCheck = true);
        void PushMissServerFrames(ServerFrame[] frames, bool isNeedDebugCheck = true);
        void OnPlayerPing(Msg_G2C_PlayerPing msg);
        ServerFrame GetFrame(int tick);
        ServerFrame GetServerFrame(int tick);
        ServerFrame GetLocalFrame(int tick);
        void SetClientTick(int tick);
        void SendInput(Msg_PlayerInput input);

        void DoUpdate(float deltaTime);
        int NextTickToCheck { get; }
        int MaxServerTickInBuffer { get; }
        bool IsNeedRollback { get; }
        int MaxContinueServerTick { get; }
        int CurTickInServer { get; }
        int PingVal { get; }
        int DelayVal { get; }
    }

    public class FrameBuffer : IFrameBuffer
    {
        /// <summary>
        /// ! 预测相关代码》》？
        /// </summary>
        public class PredictCountHelper
        {
            public PredictCountHelper(SimulatorService simulatorService, FrameBuffer cmdBuffer)
            {
                this._cmdBuffer = cmdBuffer;
                this._simulatorService = simulatorService;
            }

            //TODO:  ？？丢失的帧？？？？
            public int missTick = -1;
            /// <summary>
            /// ! 下一个需要检验的 帧号
            /// </summary>
            public int nextCheckMissTick = 0;

            /// <summary>
            /// ! 是否有丢失帧
            /// </summary>
            public bool hasMissTick;

            private SimulatorService _simulatorService;
            private FrameBuffer _cmdBuffer;

            /// <summary>
            /// 计时器
            /// </summary>
            private float _timer;

            //TODO: 间隔检测的时间阈值
            private float _checkInterval = 0.5f;

            //TODO:
            private float _incPercent = 0.3f;

            //TODO:
            private float _targetPreSendTick;

            //TODO:
            private float _oldPercent = 0.6f;

            public void DoUpdate(float deltaTime)
            {
                _timer += deltaTime;
                if (_timer > _checkInterval)
                {
                    _timer = 0;
                    if (!hasMissTick)
                    {

                        //!  awesome ------------------------------->  why  ????????
                        var preSend = _cmdBuffer._maxPing * 1.0f / NetworkDefine.UPDATE_DELTATIME;
                        _targetPreSendTick =
                            _targetPreSendTick * _oldPercent + preSend * (1 - _oldPercent);

                        var targetPreSendTick = LMath.Clamp(
                            (int)System.Math.Ceiling(_targetPreSendTick),
                            1,
                            60
                        );
#if UNITY_EDITOR
                        //if (targetPreSendTick != _simulatorService.PreSendInputCount)
                        {
                            Debug.LogWarning(
                                $"Shrink preSend buffer old:{_simulatorService.PreSendInputCount} new:{_targetPreSendTick} "
                                    + $"PING: min:{_cmdBuffer._minPing} max:{_cmdBuffer._maxPing} avg:{_cmdBuffer.PingVal}"
                            );
                        }
#endif
                        _simulatorService.PreSendInputCount = targetPreSendTick;
                    }

                    hasMissTick = false;
                }

                if (missTick != -1)
                {
                    var delayTick = _simulatorService.TargetTick - missTick;
                    var targetPreSendTick =
                        _simulatorService.PreSendInputCount
                        + (int)System.Math.Ceiling(delayTick * _incPercent);
                    targetPreSendTick = LMath.Clamp(targetPreSendTick, 1, 60);
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"Expend preSend buffer old:{_simulatorService.PreSendInputCount} new:{targetPreSendTick}"
                    );
#endif
                    _simulatorService.PreSendInputCount = targetPreSendTick;
                    nextCheckMissTick = _simulatorService.TargetTick;
                    missTick = -1;
                    hasMissTick = true;
                }
            }
        }

        /// for debug
        public static byte __debugMainActorID;

        //buffers
        private int _maxClientPredictFrameCount;
        private int _bufferSize;
        private int _spaceRollbackNeed;
        private int _maxServerOverFrameCount;

        /// <summary>
        /// ! 接收到的帧缓冲,来自于服务器
        /// ! 客户端的帧缓冲
        /// </summary>
        #region  帧缓冲
        private ServerFrame[] _serverBuffer;
        private ServerFrame[] _clientBuffer;
        #endregion

        #region  Ping Core
        //ping
        /// <summary>
        /// ! 通过历史，求得的平均 Ping 值。局部平均
        /// </summary>
        /// <value></value>
        public int PingVal { get; private set; }

        /// <summary>
        /// 记录历史 ping 值
        /// </summary>
        /// <typeparam name="long"></typeparam>
        /// <returns></returns>
        private List<long> _pings = new List<long>();

        /// <summary>
        /// ! 猜测的服务器时间戳
        /// </summary>
        private long _guessServerStartTimestamp = Int64.MaxValue;
        private long _historyMinPing = Int64.MaxValue;
        private long _minPing = Int64.MaxValue;
        private long _maxPing = Int64.MinValue;

        /// <summary>
        /// ! 历史平均的 网络延迟。局部平均
        /// </summary>
        /// <value></value>
        public int DelayVal { get; private set; }

        /// <summary>
        /// 累计时间戳，辅助计算
        /// </summary>
        private float _pingTimer;

        /// <summary>
        /// ! 历史延迟
        /// </summary>
        /// <typeparam name="long"></typeparam>
        /// <returns></returns>
        private List<long> _delays = new List<long>();
        Dictionary<int, long> _tick2SendTimestamp = new Dictionary<int, long>();
        #endregion
        /// the tick client need run in next update
        private int _nextClientTick;

        /// <summary>
        /// ! 当前Server所在帧号
        /// </summary>
        /// <value></value>
        public int CurTickInServer { get; private set; }

        /// <summary>
        /// ! 下一个需要检验的帧号
        /// </summary>
        /// <value></value>
        public int NextTickToCheck { get; private set; }

        /// <summary>
        /// ! 缓冲中最大的帧号
        /// </summary>
        /// <value></value>
        public int MaxServerTickInBuffer { get; private set; } = -1;

        /// <summary>
        /// ! 是否需要回滚
        /// </summary>
        /// <value></value>
        public bool IsNeedRollback { get; private set; }
        public int MaxContinueServerTick { get; private set; }

        /// <summary>
        /// 本地ID
        /// </summary>
        public byte LocalId;

        public INetworkService _networkService;

        private PredictCountHelper _predictHelper;
        private SimulatorService _simulatorService;

        public FrameBuffer(
            SimulatorService _simulatorService,
            INetworkService networkService,
            int bufferSize,
            int snapshotFrameInterval,
            int maxClientPredictFrameCount
        )
        {
            this._simulatorService = _simulatorService;
            _predictHelper = new PredictCountHelper(_simulatorService, this);
            //! 缓冲的最大尺寸
            this._bufferSize = bufferSize;
            this._networkService = networkService;
            //! 设定一个最大预测帧号
            this._maxClientPredictFrameCount = maxClientPredictFrameCount;
            _spaceRollbackNeed = snapshotFrameInterval * 2;
            _maxServerOverFrameCount = bufferSize - _spaceRollbackNeed;
            _serverBuffer = new ServerFrame[bufferSize];
            _clientBuffer = new ServerFrame[bufferSize];
        }

        /// <summary>
        /// 设置Client Tick，自动累计到下一个帧号
        /// </summary>
        /// <param name="tick"></param>
        public void SetClientTick(int tick)
        {
            _nextClientTick = tick + 1;
        }

        /// <summary>
        /// ! 塞入帧缓冲到 本地记录内
        /// </summary>
        /// <param name="frame"></param>
        public void PushLocalFrame(ServerFrame frame)
        {
            var sIdx = frame.tick % _bufferSize;
            Debug.Assert(
                _clientBuffer[sIdx] == null || _clientBuffer[sIdx].tick <= frame.tick,
                "Push local frame error!"
            );
            _clientBuffer[sIdx] = frame;
        }

        /// <summary>
        /// !客户端接收到的心跳包
        /// !
        /// </summary>
        /// <param name="msg"></param>
        public void OnPlayerPing(Msg_G2C_PlayerPing msg)
        {
            //PushServerFrames(frames, isNeedDebugCheck);
            var ping = LTime.realtimeSinceStartupMS - msg.sendTimestamp;
            _pings.Add(ping);
            if (ping > _maxPing)
                _maxPing = ping;
            if (ping < _minPing)
            {
                _minPing = ping;
                _guessServerStartTimestamp =
                    (LTime.realtimeSinceStartupMS - msg.timeSinceServerStart) - _minPing / 2;
            }

            //Debug.Log("OnPlayerPing " + ping);
        }

        public void PushMissServerFrames(ServerFrame[] frames, bool isNeedDebugCheck = true)
        {
            PushServerFrames(frames, isNeedDebugCheck);
            _networkService.SendMissFrameRepAck(MaxContinueServerTick + 1);
        }

        public void ForcePushDebugFrame(ServerFrame data)
        {
            var targetIdx = data.tick % _bufferSize;
            _serverBuffer[targetIdx] = data;
            _clientBuffer[targetIdx] = data;
        }

        /// <summary>
        /// ! 客户端接收到服务器广播的数据
        /// </summary>
        /// <param name="frames"></param>
        /// <param name="isNeedDebugCheck"></param>
        public void PushServerFrames(ServerFrame[] frames, bool isNeedDebugCheck = true)
        {
            var count = frames.Length;
            for (int i = 0; i < count; i++)
            {
                var data = frames[i];
                //Debug.Log("PushServerFrames" + data.tick);
                if (_tick2SendTimestamp.TryGetValue(data.tick, out var sendTimeStamp))
                {
                    //记录本地时间与发送时间戳的差值，? 通常只有本地输入才有记录到
                    var delay = LTime.realtimeSinceStartupMS - sendTimeStamp;
                    //! 记录延迟
                    _delays.Add(delay);
                    _tick2SendTimestamp.Remove(data.tick);
                }

                if (data.tick < NextTickToCheck)
                {
                    //! 未被验证的 指令
                    return;
                }

                if (data.tick > CurTickInServer)
                {
                    //! 把记录的当前帧号刷新，缓冲中最大的
                    CurTickInServer = data.tick;
                }

                if (data.tick >= NextTickToCheck + _maxServerOverFrameCount - 1)
                {
                    //to avoid ringBuffer override the frame that have not been checked
                    return;
                }

                //Debug.Log("PushServerFramesSucc" + data.tick);
                if (data.tick > MaxServerTickInBuffer)
                {

                    MaxServerTickInBuffer = data.tick;
                }

                var targetIdx = data.tick % _bufferSize;
                if (_serverBuffer[targetIdx] == null || _serverBuffer[targetIdx].tick != data.tick)
                {
                    _serverBuffer[targetIdx] = data;
                    if (
                        data.tick > _predictHelper.nextCheckMissTick
                        && data.Inputs[LocalId].IsMiss
                        && _predictHelper.missTick == -1
                    )
                    {

                        //!  有丢失帧号
                        _predictHelper.missTick = data.tick;
                    }
                }
            }
        }

        /// <summary>
        /// ! 帧缓冲逻辑
        /// </summary>
        /// <param name="deltaTime"></param>
        public void DoUpdate(float deltaTime)
        {
            //! 客户端发送一个 Ping 包
            _networkService.SendPing(_simulatorService.LocalActorId, LTime.realtimeSinceStartupMS);
            _predictHelper.DoUpdate(deltaTime);
            //! 当前帧号
            int worldTick = _simulatorService.World.Tick;
            UpdatePingVal(deltaTime);

            //Debug.Assert(nextTickToCheck <= nextClientTick, "localServerTick <= localClientTick ");
            //Confirm frames
            IsNeedRollback = false;
            while (NextTickToCheck <= MaxServerTickInBuffer && NextTickToCheck < worldTick)
            {
                var sIdx = NextTickToCheck % _bufferSize;
                //! 本地预测的计算结果
                var cFrame = _clientBuffer[sIdx];
                //! 服务器广播接收到的缓冲
                var sFrame = _serverBuffer[sIdx];
                if (
                    cFrame == null
                    || cFrame.tick != NextTickToCheck
                    || sFrame == null
                    || sFrame.tick != NextTickToCheck
                )
                    break;
                //Check client guess input match the real input
                if (object.ReferenceEquals(sFrame, cFrame) || sFrame.Equals(cFrame))
                {
                    //! 通过验证的帧号，推进
                    NextTickToCheck++;
                }
                else
                {
                    //! 对比后，发现不一致，需要进行回滚
                    IsNeedRollback = true;
                    LogMaster.E("[RollBack]  本地对比 帧数据不一致，进行回滚    ");

                    break;
                }
            }

            //Request miss frame data
            int tick = NextTickToCheck;
            for (; tick <= MaxServerTickInBuffer; tick++)
            {
                var idx = tick % _bufferSize;
                if (_serverBuffer[idx] == null || _serverBuffer[idx].tick != tick)
                {
                    break;
                }
            }

            MaxContinueServerTick = tick - 1;
            if (MaxContinueServerTick <= 0)
                return;
            if (
                MaxContinueServerTick < CurTickInServer // has some middle frame pack was lost
                || _nextClientTick > MaxContinueServerTick + (_maxClientPredictFrameCount - 3) //client has predict too much
            )
            {
                //! 请求丢失的帧数据
                Debug.Log("SendMissFrameReq " + MaxContinueServerTick);
                _networkService.SendMissFrameReq(MaxContinueServerTick);
            }
        }

        /// <summary>
        /// ! 更新心跳包相关的时间戳数据
        /// </summary>
        /// <param name="deltaTime"></param>
        private void UpdatePingVal(float deltaTime)
        {
            _pingTimer += deltaTime;
            //TODO: ?? 不同客户端的计算结果，如何保证
            if (_pingTimer > 0.5f)
            {
                _pingTimer = 0;
                DelayVal = (int)(_delays.Sum() / LMath.Max(_delays.Count, 1));
                _delays.Clear();
                PingVal = (int)(_pings.Sum() / LMath.Max(_pings.Count, 1));
                //! 清理历史值
                _pings.Clear();

                if (_minPing < _historyMinPing && _simulatorService._gameStartTimestampMs != -1)
                {
                    _historyMinPing = _minPing;
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"Recalc _gameStartTimestampMs {_simulatorService._gameStartTimestampMs} _guessServerStartTimestamp:{_guessServerStartTimestamp}"
                    );
#endif
                    _simulatorService._gameStartTimestampMs = LMath.Min(
                        _guessServerStartTimestamp,
                        _simulatorService._gameStartTimestampMs
                    );
                }

                _minPing = Int64.MaxValue;
                _maxPing = Int64.MinValue;
            }
        }

        public void SendInput(Msg_PlayerInput input)
        {
            //! 记录帧号对应的时间戳
            _tick2SendTimestamp[input.Tick] = LTime.realtimeSinceStartupMS;
#if DEBUG_SHOW_INPUT
            var cmd = input.Commands[0];
            var playerInput = new Deserializer(cmd.content).Parse<Lockstep.Game.PlayerInput>();
            if (playerInput.inputUV != LVector2.zero)
            {
                Debug.Log($"SendInput tick:{input.Tick} uv:{playerInput.inputUV}");
            }
#endif
            _networkService.SendInput(input);
        }


        /// <summary>
        /// ! 取帧数据，如果有服务器帧数据，那么就用服务器帧数据，如果没有就用本地帧数据
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        public ServerFrame GetFrame(int tick)
        {
            var sFrame = GetServerFrame(tick);
            if (sFrame != null)
            {
                return sFrame;
            }

            return GetLocalFrame(tick);
        }

        public ServerFrame GetServerFrame(int tick)
        {
            if (tick > MaxServerTickInBuffer)
            {
                return null;
            }

            return _GetFrame(_serverBuffer, tick);
        }

        public ServerFrame GetLocalFrame(int tick)
        {
            if (tick >= _nextClientTick)
            {
                return null;
            }

            return _GetFrame(_clientBuffer, tick);
        }

        private ServerFrame _GetFrame(ServerFrame[] buffer, int tick)
        {
            var idx = tick % _bufferSize;
            var frame = buffer[idx];
            if (frame == null)
                return null;
            if (frame.tick != tick)
                return null;
            return frame;
        }
    }
}
