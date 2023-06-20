using System;
using System.Collections.Generic;
using Lockstep.Game;
using Lockstep.Math;
using Lockstep.Serialization;
using Lockstep.Util;
using NetMsg.Common;
using UnityEngine;
using Debug = Lockstep.Logging.Debug;
using Msg_HashCode = NetMsg.Common.Msg_HashCode;
using Msg_PlayerInput = NetMsg.Common.Msg_PlayerInput;

public interface IIncommingMessage
{
    T Parse<T>();
    byte[] GetRawBytes();
}

namespace Lockstep.Game
{
    public interface IRoomMsgManager
    {
        void Init(IRoomMsgHandler msgHandler);
        void SendInput(Msg_PlayerInput msg);
        void SendMissFrameReq(int missFrameTick);
        void SendMissFrameRepAck(int missFrameTick);
        void SendHashCodes(int firstHashTick, List<int> allHashCodes, int startIdx, int count);

        void SendGameEvent(byte[] data);
        void SendLoadingProgress(byte progress);

        void ConnectToGameServer(Msg_C2G_Hello helloBody, IPEndInfo _gameTcpEnd, bool isReconnect);
        void OnLevelLoadProgress(float progress);
    }

    public class RoomMsgManager : IRoomMsgManager
    {
        private delegate void DealNetMsg(BaseMsg data);

        private delegate BaseMsg ParseNetMsg(Deserializer reader);

        /// <summary>
        ///! 游戏GameCore状态
        /// </summary>
        public EGameState CurGameState = EGameState.Idle;

        #region NormalNet

        // private NetClient _netUdp;
        // private NetClient _netTcp;

        #endregion

        #region LiteNetLib

        private LiteNetLibClient _client = new LiteNetLibClient();

        #endregion

        private float _curLoadProgress;
        private float _framePursueRate;

        public float FramePursueRate
        {
            get { return _framePursueRate; }
            set { _framePursueRate = System.Math.Max(System.Math.Min(1f, value), 0f); }
        }

        private float _nextSendLoadProgressTimer;
        private IRoomMsgHandler _handler;

        protected string _gameHash;
        protected int _curMapId;
        protected byte _localId;
        protected int _roomId;

        protected IPEndInfo _gameUdpEnd;
        protected IPEndInfo _gameTcpEnd;
        protected MessageHello helloBody;

        protected bool HasConnGameTcp;
        protected bool HasConnGameUdp;
        protected bool HasRecvGameDta;
        protected bool HasFinishedLoadLevel;

        public void Init(IRoomMsgHandler msgHandler)
        {
            _maxMsgId = (byte)System.Math.Min((int)EMsgSC.EnumCount, (int)byte.MaxValue);
            _allMsgDealFuncs = new DealNetMsg[_maxMsgId];
            _allMsgParsers = new ParseNetMsg[_maxMsgId];
            RegisterMsgHandlers();
            _handler = msgHandler;

            #region  NormalNet
            // _netUdp = _netTcp = new NetClient(); //TODO Login
            // _netTcp.DoStart();
            // _netTcp.NetMsgHandler = OnNetMsg;
            #endregion

            #region LiteNetLib

            _client.Start();

            _client.Connect(NetSetting.IP, NetSetting.Number);

            _client.DataReceived += NetworkOnDataReceived;

            Debug.Log($"[Client] 发起链接  {NetSetting.IP} {NetSetting.Port}");

            #endregion
        }

        /// <summary>
        /// 客户端收到服务器的包
        /// </summary>
        /// <param name="rawData"></param>
        private void NetworkOnDataReceived(byte[] rawData)
        {
            byte[] source = Compressor.Decompress(rawData);
            var deserializer = new Lockstep.Core.Logic.Serialization.Utils.Deserializer(source);
            switch (deserializer.GetByte())
            {
                case NetProtocolDefine.Init:
                {
                    // Init init = new Init();
                    // init.Deserialize(deserializer);
                    // this.InitReceived?.Invoke(this, init);
                    break;
                }
                case NetProtocolDefine.Input:
                {
                    // uint tick = deserializer.GetUInt() + deserializer.GetByte();
                    // int @int = deserializer.GetInt();
                    // byte @byte = deserializer.GetByte();
                    // Lockstep.Core.Logic.Interfaces.ICommand[] array =
                    //     new Lockstep.Core.Logic.Interfaces.ICommand[@int];
                    // for (int i = 0; i < @int; i++)
                    // {
                    //     ushort uShort = deserializer.GetUShort();
                    //     if (_commandFactories.ContainsKey(uShort))
                    //     {
                    //         Lockstep.Core.Logic.Interfaces.ICommand command =
                    //             (Lockstep.Core.Logic.Interfaces.ICommand)
                    //                 Activator.CreateInstance(_commandFactories[uShort]);
                    //         command.Deserialize(deserializer);
                    //         array[i] = command;
                    //     }
                    // }
                    // base.Enqueue(new Input(tick, @byte, array));
                    break;
                }
            }
        }

        void OnNetMsg(ushort opcode, object msg)
        {
            var type = (EMsgSC)opcode;
            switch (type)
            {
                //login
                // case EMsgSC.L2C_JoinRoomResult:

                //room
                case EMsgSC.G2C_PlayerPing:
                    G2C_PlayerPing(msg);
                    break;
                case EMsgSC.G2C_Hello:
                    G2C_Hello(msg);
                    break;
                case EMsgSC.G2C_FrameData:
                    G2C_FrameData(msg);
                    break;
                case EMsgSC.G2C_RepMissFrame:
                    G2C_RepMissFrame(msg);
                    break;
                case EMsgSC.G2C_GameEvent:
                    G2C_GameEvent(msg);
                    break;
                case EMsgSC.G2C_GameStartInfo:
                    G2C_GameStartInfo(msg);
                    break;
                case EMsgSC.G2C_LoadingProgress:
                    G2C_LoadingProgress(msg);
                    break;
                case EMsgSC.G2C_AllFinishedLoaded:
                    G2C_AllFinishedLoaded(msg);
                    break;
            }
        }

        public void DoUpdate(LFloat deltaTime)
        {
            if (CurGameState == EGameState.Loading)
            {
                if (_nextSendLoadProgressTimer < Time.realtimeSinceStartup)
                {
                    SendLoadingProgress(CurProgress);
                }
            }

            #region LiteNetLib
            _client?.Update();
            #endregion
        }

        public void DoDestroy()
        {
            Debug.Log("DoDestroy");
            #region NormalNet
            // _netTcp.SendMessage(EMsgSC.C2L_LeaveRoom, new Msg_C2L_LeaveRoom().ToBytes());
            // _netUdp?.DoDestroy();
            // _netTcp?.DoDestroy();
            // _netTcp = null;
            // _netUdp = null;
            #endregion
        }

        void ResetStatus()
        {
            HasConnGameTcp = false;
            HasConnGameUdp = false;
            HasRecvGameDta = false;
            HasFinishedLoadLevel = false;
        }

        public byte CurProgress
        {
            get
            {
                if (_curLoadProgress > 1)
                    _curLoadProgress = 1;
                if (_curLoadProgress < 0)
                    _curLoadProgress = 0;
                if (IsReconnecting)
                {
                    var val =
                        (HasRecvGameDta ? 10 : 0)
                        + (HasConnGameUdp ? 10 : 0)
                        + (HasConnGameTcp ? 10 : 0)
                        + _curLoadProgress * 20
                        + FramePursueRate * 50;
                    return (byte)val;
                }
                else
                {
                    var val =
                        _curLoadProgress * 70
                        + (HasRecvGameDta ? 10 : 0)
                        + (HasConnGameUdp ? 10 : 0)
                        + (HasConnGameTcp ? 10 : 0);

                    return (byte)val;
                }
            }
        }

        public const float ProgressSendInterval = 0.3f;

        public void OnLevelLoadProgress(float progress)
        {
            _curLoadProgress = progress;
            if (CurProgress >= 100)
            {
                CurGameState = EGameState.PartLoaded;
                _nextSendLoadProgressTimer = Time.realtimeSinceStartup + ProgressSendInterval;
                SendLoadingProgress(CurProgress);
            }
        }

        public bool IsReconnecting { get; set; }

        public void ConnectToGameServer(
            Msg_C2G_Hello helloBody,
            IPEndInfo _gameTcpEnd,
            bool isReconnect
        )
        {
            IsReconnecting = isReconnect;
            ResetStatus();
            this.helloBody = helloBody.Hello;
            ConnectUdp();
            //TODO temp code
            SendTcp(EMsgSC.C2L_JoinRoom, new Msg_C2L_JoinRoom() { RoomId = 0 });
        }

        void ConnectUdp()
        {
            _handler.OnUdpHello(_curMapId, _localId);
        }

        #region tcp

        public Msg_G2C_GameStartInfo GameStartInfo { get; private set; }

        /// <summary>
        /// 客户端接收到服务器心跳包回应
        /// </summary>
        /// <param name="reader"></param>
        protected void G2C_PlayerPing(object reader)
        {
            var msg = reader as Msg_G2C_PlayerPing;
            EventHelper.Trigger(EEvent.OnPlayerPing, msg);
        }

        /// <summary>
        /// 回应包，单纯的设定了一个ID　后续可以和其他协议合并
        /// </summary>
        /// <param name="reader"></param>
        protected void G2C_Hello(object reader)
        {
            var msg = reader as Msg_G2C_Hello;
            EventHelper.Trigger(EEvent.OnServerHello, msg);
        }

        protected void G2C_GameEvent(object reader)
        {
            var msg = reader as Msg_G2C_GameEvent;
            _handler.OnGameEvent(msg.Data);
        }

        /// <summary>
        /// 游戏开局
        /// </summary>
        /// <param name="reader"></param>
        protected void G2C_GameStartInfo(object reader)
        {
            var msg = reader as Msg_G2C_GameStartInfo;
            HasRecvGameDta = true;
            GameStartInfo = msg;
            _handler.OnGameStartInfo(msg);
            //TODO temp code
            HasConnGameTcp = true;
            HasConnGameUdp = true;
            CurGameState = EGameState.Loading;
            _curLoadProgress = 1;
            EventHelper.Trigger(EEvent.OnGameCreate, msg);
            Debug.Log("G2C_GameStartInfo");
        }

        private short curLevel;

        /// <summary>
        /// ! 服务器通知客户端加载进度
        /// </summary>
        /// <param name="reader"></param>
        protected void G2C_LoadingProgress(object reader)
        {
            var msg = reader as Msg_G2C_LoadingProgress;
            _handler.OnLoadingProgress(msg.Progress);
        }

        protected void G2C_AllFinishedLoaded(object reader)
        {
            var msg = reader as Msg_G2C_AllFinishedLoaded;
            curLevel = msg.Level;
            _handler.OnAllFinishedLoaded(msg.Level);
        }

        public void SendGameEvent(byte[] msg)
        {
            SendTcp(EMsgSC.C2G_GameEvent, new Msg_C2G_GameEvent() { Data = msg });
        }

        /// <summary>
        /// 通知服务器，加载完成
        /// </summary>
        /// <param name="progress"></param>
        public void SendLoadingProgress(byte progress)
        {
            _nextSendLoadProgressTimer = Time.realtimeSinceStartup + ProgressSendInterval;
            if (!IsReconnecting)
            {
                SendTcp(
                    EMsgSC.C2G_LoadingProgress,
                    new Msg_C2G_LoadingProgress() { Progress = progress }
                );
            }
        }

        #endregion

        #region udp

        private byte _maxMsgId = byte.MaxValue;
        private DealNetMsg[] _allMsgDealFuncs;
        private ParseNetMsg[] _allMsgParsers;

        private void RegisterMsgHandlers()
        {
            RegisterNetMsgHandler(
                EMsgSC.G2C_RepMissFrame,
                G2C_RepMissFrame,
                ParseData<Msg_RepMissFrame>
            );
            RegisterNetMsgHandler(EMsgSC.G2C_FrameData, G2C_FrameData, ParseData<Msg_ServerFrames>);
        }

        private void RegisterNetMsgHandler(EMsgSC type, DealNetMsg func, ParseNetMsg parseFunc)
        {
            _allMsgDealFuncs[(int)type] = func;
            _allMsgParsers[(int)type] = parseFunc;
        }

        private T ParseData<T>(Deserializer reader)
            where T : BaseMsg, new()
        {
            return reader.Parse<T>();
        }

        /// <summary>
        /// 上传 Ping 值，内容就是本地的时间戳
        /// </summary>
        /// <param name="localId"></param>
        /// <param name="timestamp"></param>
        public void SendPing(byte localId, long timestamp)
        {
            SendUdp(
                EMsgSC.C2G_PlayerPing,
                new Msg_C2G_PlayerPing() { localId = localId, sendTimestamp = timestamp }
            );
        }

        /// <summary>
        /// 上传 本地的输入
        /// </summary>
        /// <param name="msg"></param>
        public void SendInput(Msg_PlayerInput msg)
        {
            SendUdp(EMsgSC.C2G_PlayerInput, msg);
        }

        /// <summary>
        /// 向服务器请求 丢失的帧数据
        /// </summary>
        /// <param name="missFrameTick"></param>
        public void SendMissFrameReq(int missFrameTick)
        {
            SendUdp(EMsgSC.C2G_ReqMissFrame, new Msg_ReqMissFrame() { StartTick = missFrameTick });
        }

        /// <summary>
        /// 客户端 回应 服务器，重传的帧数据已接收
        /// </summary>
        /// <param name="missFrameTick"></param>
        public void SendMissFrameRepAck(int missFrameTick)
        {
            SendUdp(
                EMsgSC.C2G_RepMissFrameAck,
                new Msg_RepMissFrameAck() { MissFrameTick = missFrameTick }
            );
        }

        /// <summary>
        /// 告知服务器，HashCode
        /// </summary>
        /// <param name="firstHashTick"></param>
        /// <param name="allHashCodes"></param>
        /// <param name="startIdx"></param>
        /// <param name="count"></param>
        public void SendHashCodes(
            int firstHashTick,
            List<int> allHashCodes,
            int startIdx,
            int count
        )
        {
            Msg_HashCode msg = new Msg_HashCode();
            msg.StartTick = firstHashTick;
            msg.HashCodes = new int[count];
            for (int i = startIdx; i < count; i++)
            {
                msg.HashCodes[i] = allHashCodes[i];
            }

            SendUdp(EMsgSC.C2G_HashCode, msg);
        }

        public void SendUdp(EMsgSC msgId, ISerializable body)
        {
            var writer = new Serializer();
            body.Serialize(writer);
            #region NormalNet
            // _netUdp?.SendMessage(msgId, writer.CopyData());
            #endregion
        }

        public void SendTcp(EMsgSC msgId, BaseMsg body)
        {
            var writer = new Serializer();
            body.Serialize(writer);
            #region NormalNet
            // _netTcp?.SendMessage(msgId, writer.CopyData());
            #endregion
        }

        protected void G2C_UdpMessage(IIncommingMessage reader)
        {
            var bytes = reader.GetRawBytes();
            var data = new Deserializer(Compressor.Decompress(bytes));
            OnRecvMsg(data);
        }

        protected void OnRecvMsg(Deserializer reader)
        {
            var msgType = reader.ReadInt16();
            if (msgType >= _maxMsgId)
            {
                Debug.LogError($" send a Error msgType out of range {msgType}");
                return;
            }

            try
            {
                var _func = _allMsgDealFuncs[msgType];
                var _parser = _allMsgParsers[msgType];
                if (_func != null && _parser != null)
                {
                    _func(_parser(reader));
                }
                else
                {
                    Debug.LogError($" ErrorMsg type :no msg handler or parser {msgType}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($" Deal Msg Error :{(EMsgSC)(msgType)}  " + e);
            }
        }

        protected void G2C_FrameData(object reader)
        {
            var msg = reader as Msg_ServerFrames;
            _handler.OnServerFrames(msg);
        }

        /// <summary>
        /// ??
        /// </summary>
        /// <param name="reader"></param>
        protected void G2C_RepMissFrame(object reader)
        {
            var msg = reader as Msg_RepMissFrame;
            _handler.OnMissFrames(msg);
        }

        #endregion
    }
}
