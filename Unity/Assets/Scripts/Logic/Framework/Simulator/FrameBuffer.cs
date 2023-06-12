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

            /// <summary>
            /// //TODO: 间隔检测的时间阈值
            /// </summary>
            private float _checkInterval = 0.5f;

            /// <summary>
            /// //TODO: 魔法值，只是表示一种倾向比例
            /// </summary>
            private float _incPercent = 0.3f;

            /// <summary>
            /// //TODO: 临时值
            /// </summary>
            private float _targetPreSendTick;

            /// <summary>
            /// //TODO: ????? 魔法值，只是表示一种倾向比例
            /// </summary>
            private float _oldPercent = 0.6f;

            /// <summary>
            /// ! 主要目的在于：根据 Ping 、以及丢帧情况，设定一个相对合理的 输入窗口 发送 Size
            /// </summary>
            /// <param name="deltaTime"></param>
            public void DoUpdate(float deltaTime)
            {
                _timer += deltaTime;
                if (_timer > _checkInterval)
                {
                    _timer = 0;
                    if (hasMissTick == false)
                    {
                        //! 没有丢失帧

                        //!  awesome ------------------------------->  why  ????????

                        //! 通过一个局部最大 ping 值，预估能进行发送的，网络差的客户端 ping 值大，那么 preSend 更大
                        var preSend = _cmdBuffer._maxPing * 1.0f / NetworkDefine.UPDATE_DELTATIME;

                        //根据比例算出能发送的帧数量
                        _targetPreSendTick =
                            _targetPreSendTick * _oldPercent + preSend * (1 - _oldPercent);

                        //! 得到能发送的帧数量 , 不希望超前太多，所以 Clamp 
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
                        //最后设定，能进行发送的帧数量
                        _simulatorService.PreSendInputCount = targetPreSendTick;
                    }

                    hasMissTick = false;
                }

                if (missTick != -1)
                {
                    //! 有记录到丢失的帧号

                    //! 最大的预测帧号：相当于未来帧号
                    //! 丢失的帧号
                    //! 求得 延迟的 总帧数
                    var delayTick = _simulatorService.TargetTick - missTick;

                    //! 求得预备发送的帧号，相当于从 PreSendInputCount 到 targetPreSendTick 得到一个发送窗口
                    var targetPreSendTick =
                        _simulatorService.PreSendInputCount
                        + (int)System.Math.Ceiling(delayTick * _incPercent);

                    //限定范围
                    targetPreSendTick = LMath.Clamp(targetPreSendTick, 1, 60);
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"Expend preSend buffer old:{_simulatorService.PreSendInputCount} new:{targetPreSendTick}"
                    );
#endif
                    //! 重新设定了发送窗口的最大值 帧号
                    _simulatorService.PreSendInputCount = targetPreSendTick;
                    //?? 下一个要检测的 Miss 帧号
                    nextCheckMissTick = _simulatorService.TargetTick;
                    missTick = -1;
                    hasMissTick = true;
                }
            }
        }

        /// for debug
        /// <summary>
        /// TODO:
        /// </summary>
        public static byte __debugMainActorID;

        //buffers
        /// <summary>
        /// 最大预测帧数
        /// </summary>
        private int _maxClientPredictFrameCount;

        /// <summary>
        /// 帧缓冲的大小
        /// </summary>
        private int _bufferSize;

        /// <summary>
        /// ???  2 个 快照帧号间隔？？？？
        /// </summary>
        private int _spaceRollbackNeed;

        /// <summary>
        /// ????  最大的服务器帧号 , 防止溢出的边界帧号？？？？？
        /// </summary>
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

        /// <summary>
        /// 全局游戏历史中，最小的 Ping
        /// </summary>
        private long _historyMinPing = Int64.MaxValue;

        /// <summary>
        /// 局部时间内的最小的 Ping
        /// </summary>
        private long _minPing = Int64.MaxValue;

        /// <summary>
        /// 局部时间内的最大的 Ping
        /// </summary>
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

        /// <summary>
        /// 记录帧号对应的时间戳
        /// </summary>
        /// <typeparam name="int"></typeparam>
        /// <typeparam name="long"></typeparam>
        /// <returns></returns>
        Dictionary<int, long> _tick2SendTimestamp = new Dictionary<int, long>();
        #endregion
        /// the tick client need run in next update
        /// <summary>
        /// 客户端的下一帧
        /// </summary>
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

        /// <summary>
        /// ! 最新一个被缓冲验证的帧号
        /// </summary>
        /// <value></value>
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

            LogMaster.L(
                $"[FrameBuffer] bufferSize:{bufferSize} snapshotFrameInterval:{snapshotFrameInterval} maxClientPredictFrameCount:{maxClientPredictFrameCount}"
            );
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
            //! Ping = 发送到接收的消耗
            var ping = LTime.realtimeSinceStartupMS - msg.sendTimestamp;
            _pings.Add(ping);
            if (ping > _maxPing)
                _maxPing = ping;
            if (ping < _minPing)
            {
                _minPing = ping;
                //! 通过 Ping 值，去评估服务器的起始时间戳
                _guessServerStartTimestamp =
                    (LTime.realtimeSinceStartupMS - msg.timeSinceServerStart) - _minPing / 2;
            }

            //Debug.Log("OnPlayerPing " + ping);
        }

        /// <summary>
        ///! 服务器回应丢失的帧数据
        /// </summary>
        /// <param name="frames"></param>
        /// <param name="isNeedDebugCheck"></param>
        public void PushMissServerFrames(ServerFrame[] frames, bool isNeedDebugCheck = true)
        {
            PushServerFrames(frames, isNeedDebugCheck);
            _networkService.SendMissFrameRepAck(MaxContinueServerTick + 1);
        }

        /// <summary>
        /// 客户端模式强制写入 Server、Client 缓冲中
        /// </summary>
        /// <param name="data"></param>
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

                LogMaster.L($"[Client] PushServerFrames {data.tick} ");

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
                    //! 溢出
                    //to avoid ringBuffer override the frame that have not been checked
                    return;
                }

                //Debug.Log("PushServerFramesSucc" + data.tick);
                if (data.tick > MaxServerTickInBuffer)
                {
                    //TODO:
                    MaxServerTickInBuffer = data.tick;
                }

                //转换为数组索引
                var targetIdx = data.tick % _bufferSize;
                if (_serverBuffer[targetIdx] == null || _serverBuffer[targetIdx].tick != data.tick)
                {
                    //! 将接受到的缓冲写入到 Server缓冲中
                    _serverBuffer[targetIdx] = data;
                    if (
                        data.tick > _predictHelper.nextCheckMissTick
                        && data.Inputs[LocalId].IsMiss
                        && _predictHelper.missTick == -1
                    )
                    {
                        //!  有丢失帧号，标记
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
            //! 设定 发送的输入窗口 Size
            _predictHelper.DoUpdate(deltaTime);
            //! 当前帧号
            int worldTick = _simulatorService.World.Tick;
            //! 更新Ping、时间戳
            UpdatePingVal(deltaTime);

            //Debug.Assert(nextTickToCheck <= nextClientTick, "localServerTick <= localClientTick ");
            //Confirm frames
            //! 验证帧缓冲数据
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

            //找到准确验证的帧号最大值
            int tick = NextTickToCheck;
            for (; tick <= MaxServerTickInBuffer; tick++)
            {
                var idx = tick % _bufferSize;
                if (_serverBuffer[idx] == null || _serverBuffer[idx].tick != tick)
                {
                    break;
                }
            }
            //! 得到 Server 缓冲中，最后一个被验证的 帧号
            MaxContinueServerTick = tick - 1;
            if (MaxContinueServerTick <= 0)
                return;

            //! 丢失了太多帧，或者客户端预测太超前了，服务器广播得到的 帧缓冲 太滞后了
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
                //! 计算局部平均 Delay
                DelayVal = (int)(_delays.Sum() / LMath.Max(_delays.Count, 1));
                //清空
                _delays.Clear();
                //! 计算局部平均 Ping
                PingVal = (int)(_pings.Sum() / LMath.Max(_pings.Count, 1)); 
                //清空
                _pings.Clear();

                if (_minPing < _historyMinPing && _simulatorService._gameStartTimestampMs != -1)
                {
                    //! 更新历史最小
                    _historyMinPing = _minPing;
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"Recalc _gameStartTimestampMs {_simulatorService._gameStartTimestampMs} _guessServerStartTimestamp:{_guessServerStartTimestamp}"
                    );
#endif
                    //更新时间戳，为了更加精准
                    _simulatorService._gameStartTimestampMs = LMath.Min(
                        _guessServerStartTimestamp,
                        _simulatorService._gameStartTimestampMs
                    );
                }

                //! 重置
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

            LogMaster.L($"[Client]  SendInput input.tick {input.Tick}");

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
