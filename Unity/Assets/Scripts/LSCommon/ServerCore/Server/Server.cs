using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Lockstep.Game;
using Lockstep.Logging;
using Lockstep.Network;
using Lockstep.Util;
using NetMsg.Common;

using Server.LiteNetLib;

namespace Lockstep.FakeServer.Server
{
    public class Server //: IMessageDispatcher
    {
        #region LiteNetLib
        //! 使用 LiteNetLibServer
        protected LiteNetLibServer _liteServer;
        Lockstep.Network.Server.Room room;
        #endregion

        //network
        // public static IPEndPoint serverIpPoint;
        //private NetOuterProxy _netProxy = new NetOuterProxy();

        /// <summary>
        /// 更新间隔
        /// </summary>
        private const double UpdateInterval = NetworkDefine.UPDATE_DELTATIME / 1000.0f; //frame rate = 30

        /// <summary>
        /// 上一次更新的时间戳
        /// </summary>
        private DateTime _lastUpdateTimeStamp;

        /// <summary>
        /// 启动的时间戳
        /// </summary>
        private DateTime _startUpTimeStamp;
        private double _deltaTime;

        /// <summary>
        /// 始于启动的时间戳
        /// </summary>
        private double _timeSinceStartUp;

        //user mgr
        private Game _game;

        /// <summary>
        /// ID 映射 玩家
        /// </summary>
        /// <typeparam name="long"></typeparam>
        /// <typeparam name="Player"></typeparam>
        /// <returns></returns>
        private Dictionary<long, Player> _id2Player = new Dictionary<long, Player>();

        /// <summary>
        /// ID 生成计数器
        /// </summary>
        private static int _idCounter = 0;

        /// <summary>
        /// 当前房间内的玩家数量
        /// </summary>
        private int _curCount = 0;

        public void Start()
        {
#if UNITY_EDITOR
            NetSetting.AutoCreateRandomPort();
#endif

            #region LiteNetLib
            _liteServer = new LiteNetLibServer();
            room = new Lockstep.Network.Server.Room(_liteServer, (int)NetSetting.Number);

            room.Open((int)NetSetting.Port);
            #endregion

            UnityEngine.Debug.Log(
                $"房间开启 RoomPlayerNumber{NetSetting.Number}  port {NetSetting.Port}  "
            );

            // serverIpPoint = NetworkUtil.ToIPEndPoint(NetSetting.IP, (int)NetSetting.Port);

            //_netProxy.MessageDispatcher = this;
            //_netProxy.MessagePacker = MessagePacker.Instance;
            //! 这里存在疑虑，TCP可能造成延迟更大
            // _netProxy.Awake(NetworkProtocol.TCP, serverIpPoint);
            _startUpTimeStamp = _lastUpdateTimeStamp = DateTime.Now;

            LogMaster.L($"[Server] {NetSetting.IP} {NetSetting.Port} {NetSetting.Number} ");
        }

        //public void Dispatch(Session session, Packet packet)
        //{
        //    ushort opcode = packet.Opcode();
        //    // if (opcode == 39)
        //    // {
        //    //     int i = 0;
        //    // }

        //    var message = session.Network.MessagePacker.DeserializeFrom(
        //        opcode,
        //        packet.Bytes,
        //        Packet.Index,
        //        packet.Length - Packet.Index
        //    );
        //    OnNetMsg(session, opcode, message as BaseMsg);
        //}
        
        //void OnNetMsg(Session session, ushort opcode, BaseMsg msg)
        //{
        //    var type = (EMsgSC)opcode;
        //    switch (type)
        //    {
        //        //login
        //        // case EMsgSC.L2C_JoinRoomResult:
        //        case EMsgSC.C2L_JoinRoom:
        //            OnPlayerConnect(session, msg);
        //            return;
        //        case EMsgSC.C2L_LeaveRoom:
        //            OnPlayerQuit(session, msg);
        //            return;
        //        //room
        //    }
        //    var player = session.GetBindInfo<Player>();
        //    _game?.OnNetMsg(player, opcode, msg);
        //}

        /// <summary>
        /// Server 的驱动核心
        /// </summary>
        public void Update()
        {
            #region LiteNetLib
            _liteServer.PollEvents();
            #endregion
            var now = DateTime.Now;
            _deltaTime = (now - _lastUpdateTimeStamp).TotalSeconds;
            if (_deltaTime > UpdateInterval)
            {
                _lastUpdateTimeStamp = now;
                _timeSinceStartUp = (now - _startUpTimeStamp).TotalSeconds;
                //! 间隔驱动 Tick 逻辑
                DoUpdate();
            }
        }

        public void DoUpdate()
        {
            //check frame inputs
            var fDeltaTime = (float)_deltaTime;
            var fTimeSinceStartUp = (float)_timeSinceStartUp;
            _game?.DoUpdate(fDeltaTime);
        }

        //void OnPlayerConnect(Session session, BaseMsg message)
        //{
        //    //TODO load from db
        //    var info = new Player();
        //    info.UserId = _idCounter++;
        //    info.PeerTcp = session;
        //    info.PeerUdp = session;
        //    _id2Player[info.UserId] = info;
        //    session.BindInfo = info;
        //    _curCount++;
        //    if (_curCount >= NetSetting.Number)
        //    {
        //        //TODO temp code
        //        _game = new Game();
        //        var players = new Player[_curCount];
        //        int i = 0;
        //        foreach (var player in _id2Player.Values)
        //        {
        //            player.LocalId = (byte)i;
        //            player.Game = _game;
        //            players[i] = player;
        //            i++;
        //        }
        //        _game.DoStart(0, 0, 0, players, "123");
        //    }

        //    LogMaster.I("[Server] OnPlayerConnect count:" + _curCount + " ");
        //}

        //void OnPlayerQuit(Session session, BaseMsg message)
        //{
        //    var player = session.GetBindInfo<Player>();
        //    if (player == null)
        //        return;
        //    _curCount--;
        //    LogMaster.I("[Server] OnPlayerQuit count:   {_curCount} ");
        //    _id2Player.Remove(player.UserId);
        //    if (_curCount == 0)
        //    {
        //        _game = null;
        //    }
        //}
    }
}
