/// ============================================================
/// 文件名: GameConnection.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 游戏连接组装层：WebSocketConnection（传输）+ NetMessageRegistry（分发）
///       + ThrottledSender（高频限频）+ 进房看门狗。对外发送 API 与旧
///       GameServerClient 同名同签名（ConnectToRoom 除外，语义更明确）。
///       消息处理逻辑自 GameServerClient.DispatchMessage 逐行搬运（R3 不改逻辑）。
/// 引用：Framework/Network/*, Protocol/*, RoomSession.cs, NetworkEvents.cs
/// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Framework.Core;
using DualEnigma.Framework.Network;
using DualEnigma.Core;
using DualEnigma.Data;

namespace DualEnigma.Network
{
    /// <summary>游戏连接（组装框架件 + 业务消息注册 + 发送 API）</summary>
    public class GameConnection : Singleton<GameConnection>
    {
        /// <summary>进房 Ack 看门狗（秒）：握手成功但未收到 ConnectAck（房满/房不存在/已开局被拒）</summary>
        private const float CONNECT_ACK_TIMEOUT = 5f;

        /// <summary>心跳间隔（秒），与服务端 HeartbeatManager 对齐</summary>
        private const float HEARTBEAT_INTERVAL = 1f;

        /// <summary>请求回执超时（秒）：与看门狗一致；code=-1 仅提示不自动断线（连续超时升级策略归后续计划）</summary>
        private const float REQUEST_TIMEOUT = 5f;

        private WebSocketConnection _conn;
        private NetMessageRegistry _registry;
        private ThrottledSender _highFreqThrottle;
        private RequestTracker _tracker;

        private NetworkConfig _config;

        /// <summary>进房看门狗计时（-1=未启动）</summary>
        private float _ackWatchdog = -1f;

        private string ServerUrl
        {
            get
            {
                string url = _config != null ? _config.GameServerWsUrl : null;
                return string.IsNullOrEmpty(url) ? "ws://localhost:8080/game" : url;
            }
        }

        /// <summary>应用层心跳往返延迟（毫秒）；-1 = 未知</summary>
        public float RttMs => _conn != null ? _conn.RttMs : -1f;

        protected override void OnSingletonInitialized()
        {
            _config = DataManager.Instance.LoadConfig<NetworkConfig>("NetworkConfig");

            // 框架件组装
            _conn = gameObject.AddComponent<WebSocketConnection>();
            _conn.OnMessageReceived += OnRawMessage;
            _conn.OnAbnormalDisconnected += OnAbnormalDisconnected;
            _registry = new NetMessageRegistry();
            _highFreqThrottle = new ThrottledSender(_config != null ? _config.HighFrequencyRate : 20f);
            _tracker = new RequestTracker();

            RegisterHandlers();
            Debug.Log("[GameConnection] 游戏连接初始化完成");
        }

        protected override void OnDestroy()
        {
            if (_conn != null)
            {
                _conn.OnMessageReceived -= OnRawMessage;
                _conn.OnAbnormalDisconnected -= OnAbnormalDisconnected;
            }
            base.OnDestroy();
        }

        // ============================================================
        //  连接管理
        // ============================================================

        /// <summary>
        /// 连接并请求进房。roomCode 空 → 创建新房间（自动匹配）；非空 → 加入指定房间。
        /// </summary>
        public async void ConnectToRoom(string roomCode)
        {
            if (_conn.IsConnected)
            {
                Debug.Log("[GameConnection] 已连接，忽略重复连接请求");
                return;
            }

            RoomSession.Instance.BeginConnecting();
            Debug.Log($"[GameConnection] 连接 {ServerUrl} (roomCode=\"{roomCode}\")");

            try
            {
                await _conn.ConnectAsync(new Uri(ServerUrl));
            }
            catch (Exception)
            {
                // 握手失败：ServerUnreachable 统一文案（审查报告 J 相关路径收口）
                RoomSession.Instance.MarkDisconnected(false, NetConnErrorText.ToMessage(NetConnError.ServerUnreachable));
                return;
            }

            RoomSession.Instance.MarkConnected();

            // 传输层就绪 → 发送进房请求（携带登录 Token，服务端校验后注册在线状态）
            // reqId 登记：成功 resp(0) 紧邻 ConnectAck 前；失败 2001/2002/2003 回执后服务器关闭会话
            string token = AuthService.HasInstance && AuthService.Instance.IsLoggedIn
                ? AuthService.Instance.Token : "";
            C2S_Connect connectMsg = new C2S_Connect
            {
                data = new C2S_Connect.Data { roomCode = roomCode ?? "", token = token }
            };
            connectMsg.reqId = _tracker.Register(NetProto.Connect, REQUEST_TIMEOUT, (code, message) =>
                OnConnectResp(code, message));
            await _conn.SendAsync(NetJson.ToJson(connectMsg));

            // 进房看门狗：握手成功但无 ConnectAck → ConnectTimeout 兜底（审查报告 J）
            _ackWatchdog = 0f;

            _conn.StartHeartbeat(HEARTBEAT_INTERVAL, () => NetJson.ToJson(new C2S_Heartbeat()));
        }

        /// <summary>
        /// C2S_Connect 回执处理：成功(0) 只进日志（ConnectAck 随后到达触发 EnterRoom）；
        /// 失败 → 先取消看门狗（防双提示）→ 发布 NetworkErrorEvent → 自动断开回主界面初始按钮态。
        /// </summary>
        private void OnConnectResp(int code, string message)
        {
            if (code == (int)NetErrorCode.Ok)
            {
                Debug.Log("[GameConnection] 进房请求受理成功（resp 0）");
                return;
            }

            _ackWatchdog = -1f; // 收到回执先取消看门狗，只提示一次
            EventBus.Instance.Publish(new NetworkErrorEvent
            {
                code = code,
                message = message,
                source = NetProto.Connect,
            });
            DisconnectWithReason(NetConnError.ClosedByServer, keepSessionEvent: false);
            Debug.LogWarning($"[GameConnection] 进房失败 resp({code}): {message}");
        }

        /// <summary>主动断开（退出房间/重置）。会话状态经统一出口清零。</summary>
        public async void Disconnect()
        {
            _ackWatchdog = -1f;
            await _conn.CloseAsync(); // 主动关闭不触发异常事件
            RoomSession.Instance.MarkDisconnected(true);
            Debug.Log("[GameConnection] 已主动断开");
        }

        private void OnAbnormalDisconnected(string transportReason)
        {
            // 看门狗计时中断开 = 进房阶段断线 → ConnectTimeout 语义；否则 ClosedByServer
            bool wasAwaitingAck = _ackWatchdog >= 0f;
            _ackWatchdog = -1f;

            NetConnError error = wasAwaitingAck ? NetConnError.ConnectTimeout : NetConnError.ClosedByServer;
            RoomSession.Instance.MarkDisconnected(false, NetConnErrorText.ToMessage(error));
        }

        // ============================================================
        //  发送 API（与旧 GameServerClient 同名同签名）
        // ============================================================

        /// <summary>房主请求开始对局（服务端校验房主身份 + 满员后广播 GameStart；失败回执驱动状态栏提示）</summary>
        public void RequestStartGame()
        {
            if (!_conn.IsConnected)
            {
                Debug.LogWarning("[GameConnection] 未连接，无法请求开局");
                return;
            }

            C2S_StartGame msg = new C2S_StartGame();
            msg.reqId = _tracker.Register(NetProto.StartGame, REQUEST_TIMEOUT, (code, message) =>
                OnStartGameResp(code, message));
            _ = _conn.SendAsync(NetJson.ToJson(msg));
        }

        /// <summary>开局回执：成功只进日志（UI 仍由 S2C_GameStart 事件驱动）；失败发布 NetworkErrorEvent</summary>
        private void OnStartGameResp(int code, string message)
        {
            if (code == (int)NetErrorCode.Ok)
            {
                Debug.Log("[GameConnection] 开局请求受理成功（resp 0）");
                return;
            }

            EventBus.Instance.Publish(new NetworkErrorEvent
            {
                code = code,
                message = message,
                source = NetProto.StartGame,
            });
            Debug.LogWarning($"[GameConnection] 开局失败 resp({code}): {message}");
        }

        /// <summary>上报本地角色高频状态（内部 20Hz 节流，ThrottledSender）</summary>
        public void SendHighFreqState(Vector2 position, Vector2 velocity, string animState, bool facing, int hp, float shelterEnergy)
        {
            if (!_conn.IsConnected) return;

            if (_highFreqThrottle != null && !_highFreqThrottle.Tick(Time.deltaTime))
                return;

            _ = _conn.SendAsync(NetJson.ToJson(new C2S_HighFreqState
            {
                data = new C2S_HighFreqState.Data
                {
                    position = new NetVec2 { x = position.x, y = position.y },
                    velocity = new NetVec2 { x = velocity.x, y = velocity.y },
                    animState = animState,
                    facing = facing,
                    hp = hp,
                    shelterEnergy = shelterEnergy
                }
            }));
        }

        /// <summary>上报碎片接住（携带碰撞瞬间碎片坐标供服务器几何判定同接；resp(0) 为服务器权威确认锚点）</summary>
        public void SendFragmentCaught(int fragmentId, float posX, float posY)
        {
            if (!_conn.IsConnected) return;

            C2S_FragmentCaught msg = new C2S_FragmentCaught
            {
                data = new C2S_FragmentCaught.Data { fragmentId = fragmentId, posX = posX, posY = posY }
            };
            msg.reqId = _tracker.Register(NetProto.FragmentCaught, REQUEST_TIMEOUT, (code, message) =>
                OnFragmentCaughtResp(code, message));
            _ = _conn.SendAsync(NetJson.ToJson(msg));
        }

        /// <summary>碎片上报回执：成功=权威确认锚点（日志）；被拒(4002)=警告日志，对局不中断</summary>
        private void OnFragmentCaughtResp(int code, string message)
        {
            if (code == (int)NetErrorCode.Ok)
            {
                Debug.Log("[GameConnection] 碎片上报确认（resp 0）");
                return;
            }

            EventBus.Instance.Publish(new NetworkErrorEvent
            {
                code = code,
                message = message,
                source = NetProto.FragmentCaught,
            });
            Debug.LogWarning($"[GameConnection] 碎片上报被拒 resp({code}): {message}");
        }

        // ============================================================
        //  主线程泵：看门狗扫描
        // ============================================================

        private void Update()
        {
            // 请求回执超时扫描（code=-1 回调）
            _tracker?.Tick(Time.unscaledDeltaTime);

            // 进房看门狗：ConnectAck 超时 → 本地判定失败（服务器对进房失败无回包时兜底；
            // 服务器有回执时 OnConnectResp 已先取消看门狗）
            if (_ackWatchdog >= 0f)
            {
                _ackWatchdog += Time.unscaledDeltaTime;
                if (_ackWatchdog >= CONNECT_ACK_TIMEOUT)
                {
                    _ackWatchdog = -1f;
                    DisconnectWithReason(NetConnError.ConnectTimeout);
                }
            }
        }

        /// <summary>带原因断开（看门狗/回执失败路径）：清看门狗 → 主动关流 → 异常语义事件</summary>
        private async void DisconnectWithReason(NetConnError error, bool keepSessionEvent = true)
        {
            _ackWatchdog = -1f;
            _tracker?.Clear();
            await _conn.CloseAsync();

            if (keepSessionEvent)
                RoomSession.Instance.MarkDisconnected(false, NetConnErrorText.ToMessage(error));
            else
                RoomSession.Instance.MarkDisconnected(true); // 已由 NetworkErrorEvent 提示，不重复发断线事件
        }

        // ============================================================
        //  消息注册（自 GameServerClient.DispatchMessage 逐行搬运，逻辑不变）
        // ============================================================

        private void OnRawMessage(string json)
        {
            _registry.Dispatch(json);
        }

        private void RegisterHandlers()
        {
            // 阶段切换：需要信封 timestamp（时钟差值法：剩余 = phaseEndTime - timestamp）
            _registry.Register<S2C_PhaseChange>(NetProto.PhaseChange, (envelope, msg) =>
            {
                if (msg?.data == null) return;
                if (Enum.TryParse(msg.data.phase, out GamePhase phase))
                {
                    float remaining = (msg.data.phaseEndTime - msg.timestamp) / 1000f;
                    GameStateMachine.Instance.ApplyServerPhase(phase, remaining);
                }
            });

            _registry.Register<S2C_ConnectAck>(NetProto.ConnectAck, msg =>
            {
                string roomCode = msg.data != null ? msg.data.roomCode : "";
                int playerId = msg.data != null ? msg.data.playerId : 0;

                _ackWatchdog = -1f; // 进房成功，取消看门狗
                RoomSession.Instance.EnterRoom(playerId, roomCode);
            });

            _registry.Register<S2C_GameStart>(NetProto.GameStart, msg =>
            {
                EventBus.Instance.Publish(new RoomGameStartEvent
                {
                    chapter = msg.data?.chapter ?? 1,
                    section = msg.data?.section ?? 1,
                    round = msg.data?.round ?? 1
                });
            });

            _registry.Register<S2C_PlayerJoined>(NetProto.PlayerJoined, msg =>
            {
                EventBus.Instance.Publish(new PlayerJoinedRoomEvent
                {
                    playerId = msg.data?.playerId ?? 0,
                    playerCount = msg.data?.playerCount ?? 1
                });
            });

            _registry.Register<S2C_HighFreqState>(NetProto.HighFreqStateS2C, msg =>
            {
                if (msg?.data == null) return;
                EventBus.Instance.Publish(new HighFreqStateReceivedEvent
                {
                    playerId = (byte)msg.data.playerId,
                    position = msg.data.position != null
                        ? new Vector2(msg.data.position.x, msg.data.position.y) : Vector2.zero,
                    velocity = msg.data.velocity != null
                        ? new Vector2(msg.data.velocity.x, msg.data.velocity.y) : Vector2.zero,
                    animState = msg.data.animState,
                    facing = msg.data.facing
                });
            });

            _registry.Register<S2C_MidFreqState>(NetProto.MidFreqState, msg =>
            {
                if (msg?.data?.players == null) return;
                byte opponent = RoomSession.Instance.OpponentId;
                foreach (S2C_MidFreqState.PlayerData p in msg.data.players)
                {
                    if (p.playerId == opponent)
                        RoomSession.Instance.UpdateOpponentStats(p.hp, p.shelterEnergy);
                }
            });

            _registry.Register<S2C_OpponentDisconnect>(NetProto.OpponentDisconnect, msg =>
            {
                EventBus.Instance.Publish(new OpponentDisconnectEvent
                {
                    playerId = msg.playerId,
                    state = msg.data?.state ?? ""
                });
            });

            _registry.Register<S2C_FragmentDropPlan>(NetProto.FragmentDropPlan, msg =>
            {
                if (msg?.data?.plan == null || msg.data.plan.Count == 0) return;

                var plan = new List<DualEnigma.Fragment.FragmentDropPlan>(msg.data.plan.Count);
                foreach (S2C_FragmentDropPlan.PlanItem item in msg.data.plan)
                {
                    plan.Add(new DualEnigma.Fragment.FragmentDropPlan
                    {
                        FragmentId = item.fragmentId,
                        Type = (DualEnigma.Fragment.FragmentType)item.type, // 0/1/2 顺序已核对一致
                        Position = item.position != null
                            ? new Vector2(item.position.x, item.position.y) : Vector2.zero,
                        DropTime = item.dropTime,
                        Seed = unchecked((uint)item.seed) // long→uint 截断，两端一致即可保证确定性
                    });
                }

                if (DualEnigma.Fragment.FragmentSystem.HasInstance)
                    DualEnigma.Fragment.FragmentSystem.Instance.ExecuteDropPlan(plan);
            });

            _registry.Register<S2C_FragmentResult>(NetProto.FragmentResult, msg =>
            {
                if (msg?.data == null) return;

                // 只处理对方接住：自己接住的本地已完成（上报发生在收集完成之后）
                if (msg.data.playerId != RoomSession.Instance.LocalPlayerId
                    && DualEnigma.Fragment.FragmentSystem.HasInstance)
                {
                    DualEnigma.Fragment.FragmentSystem.Instance.OnFragmentCollected(
                        msg.data.fragmentId, (byte)msg.data.playerId, false);
                }
            });

            _registry.Register<S2C_HeartbeatAck>(NetProto.HeartbeatAck, msg =>
            {
                _conn.NotifyHeartbeatAck(); // RTT 刷新（传输层）
            });

            // 统一回执派发（R5）：reqId → RequestTracker 回调
            _registry.Register<S2C_Resp>(NetProto.Resp, msg =>
            {
                if (msg?.data == null) return;
                _tracker.OnResp(msg.data.reqId, msg.data.code, msg.data.message);
            });
        }
    }
}
