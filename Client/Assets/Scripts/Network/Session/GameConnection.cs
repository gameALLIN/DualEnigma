/// ============================================================
/// ============================================================
/// 文件名: GameConnection.cs
/// 创建时间: 2026-08-22
/// 最后更新: 2026-08-22（PC-1 切换 Protobuf 二进制协议）
/// 作者: DualEnigma
/// 描述: 游戏连接组装层：WebSocketConnection（二进制传输）+ ThrottledSender
///       （高频限频）+ RequestTracker（reqId 回执）+ 进房看门狗。
///       协议为 Protobuf Envelope oneof（Generated/Game.cs，Dualenigma.V1）：
///       接收 ParseFrom → BodyCase switch 分发（注册表退役）；
///       发送构造 Envelope → ToByteArray 二进制帧。
///       消息处理逻辑自 JSON 版逐行搬运（语义零变化：reqId/看门狗/RTT/时钟差值法）。
/// 引用：Framework/Network/*, Protocol/Generated/*, ProtoMapping.cs, RoomSession.cs
/// ============================================================

using System;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;
using DualEnigma.Framework.Core;
using DualEnigma.Framework.Network;
using DualEnigma.Core;
using DualEnigma.Data;
using DualEnigma.V1;
using Pb = DualEnigma.V1;

namespace DualEnigma.Network
{
    /// <summary>游戏连接（组装框架件 + 协议分发 + 发送 API）</summary>
    public class GameConnection : Singleton<GameConnection>
    {
        /// <summary>进房 Ack 看门狗（秒）：握手成功但未收到 ConnectAck（房满/房不存在/已开局被拒）</summary>
        private const float CONNECT_ACK_TIMEOUT = 5f;

        /// <summary>心跳间隔（秒），与服务端 HeartbeatManager 对齐</summary>
        private const float HEARTBEAT_INTERVAL = 1f;

        /// <summary>请求回执超时（秒）：与看门狗一致；code=-1 仅提示不自动断线（连续超时升级策略归后续计划）</summary>
        private const float REQUEST_TIMEOUT = 5f;

        private WebSocketConnection _conn;
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

            // 框架件组装（协议分发为 Envelope.BodyCase switch，注册表已退役）
            _conn = gameObject.AddComponent<WebSocketConnection>();
            _conn.OnMessageReceived += OnRawMessage;
            _conn.OnAbnormalDisconnected += OnAbnormalDisconnected;
            _highFreqThrottle = new ThrottledSender(_config != null ? _config.HighFrequencyRate : 20f);
            _tracker = new RequestTracker();

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
            var connectEnv = new Envelope
            {
                ReqId = _tracker.Register(NetProto.Connect, REQUEST_TIMEOUT, (code, message) =>
                    OnConnectResp(code, message)),
                Connect = new Pb.C2S_Connect { RoomCode = roomCode ?? "", Token = token },
            };
            await _conn.SendAsync(connectEnv.ToByteArray());

            // 进房看门狗：握手成功但无 ConnectAck → ConnectTimeout 兜底（审查报告 J）
            _ackWatchdog = 0f;

            _conn.StartHeartbeat(HEARTBEAT_INTERVAL,
                () => new Envelope { Heartbeat = new Pb.C2S_Heartbeat() }.ToByteArray());
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

            var env = new Envelope
            {
                ReqId = _tracker.Register(NetProto.StartGame, REQUEST_TIMEOUT, (code, message) =>
                    OnStartGameResp(code, message)),
                StartGame = new Pb.C2S_StartGame(),
            };
            _ = _conn.SendAsync(env.ToByteArray());
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

        /// <summary>上报本地角色高频状态（内部 20Hz 节流，ThrottledSender；reqId 豁免恒 0）</summary>
        public void SendHighFreqState(Vector2 position, Vector2 velocity, string animState, bool facing, int hp, float shelterEnergy)
        {
            if (!_conn.IsConnected) return;

            if (_highFreqThrottle != null && !_highFreqThrottle.Tick(Time.deltaTime))
                return;

            var env = new Envelope
            {
                HighFreqState = new Pb.C2S_HighFreqState
                {
                    Position = new Pb.Vec2 { X = position.x, Y = position.y },
                    Velocity = new Pb.Vec2 { X = velocity.x, Y = velocity.y },
                    AnimState = animState,
                    Facing = facing,
                    Hp = hp,
                    ShelterEnergy = shelterEnergy,
                },
            };
            _ = _conn.SendAsync(env.ToByteArray());
        }

        /// <summary>上报碎片接住（携带碰撞瞬间碎片坐标供服务器几何判定同接；resp(0) 为服务器权威确认锚点）</summary>
        public void SendFragmentCaught(int fragmentId, float posX, float posY)
        {
            if (!_conn.IsConnected) return;

            var env = new Envelope
            {
                ReqId = _tracker.Register(NetProto.FragmentCaught, REQUEST_TIMEOUT, (code, message) =>
                    OnFragmentCaughtResp(code, message)),
                FragmentCaught = new Pb.C2S_FragmentCaught
                {
                    FragmentId = fragmentId, PosX = posX, PosY = posY,
                },
            };
            _ = _conn.SendAsync(env.ToByteArray());
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
        //  协议分发（Protobuf Envelope.BodyCase switch；逻辑自 JSON 版逐行搬运）
        // ============================================================

        /// <summary>接收一帧二进制信封：ParseFrom（坏帧丢弃）→ BodyCase 路由</summary>
        private void OnRawMessage(byte[] payload)
        {
            Envelope env;
            try
            {
                env = Envelope.Parser.ParseFrom(payload);
            }
            catch (InvalidProtocolBufferException e)
            {
                Debug.LogWarning($"[GameConnection] 坏帧丢弃（{payload.Length}B）: {e.Message}");
                return;
            }

            switch (env.BodyCase)
            {
                case Envelope.BodyOneofCase.PhaseChange:
                {
                    // 时钟差值法：剩余 = phaseEndTime - 信封 timestamp（同为服务器时钟）
                    GamePhase phase = ProtoMapping.ToGamePhase(env.PhaseChange.Phase);
                    float remaining = (env.PhaseChange.PhaseEndTime - env.Timestamp) / 1000f;
                    GameStateMachine.Instance.ApplyServerPhase(phase, remaining);
                    break;
                }

                case Envelope.BodyOneofCase.ConnectAck:
                {
                    _ackWatchdog = -1f; // 进房成功，取消看门狗
                    RoomSession.Instance.EnterRoom(env.ConnectAck.PlayerId, env.ConnectAck.RoomCode);
                    break;
                }

                case Envelope.BodyOneofCase.GameStart:
                {
                    EventBus.Instance.Publish(new RoomGameStartEvent
                    {
                        chapter = env.GameStart.Chapter,
                        section = env.GameStart.Section,
                        round = env.GameStart.Round,
                    });
                    break;
                }

                case Envelope.BodyOneofCase.PlayerJoined:
                {
                    EventBus.Instance.Publish(new PlayerJoinedRoomEvent
                    {
                        playerId = env.PlayerJoined.PlayerId,
                        playerCount = env.PlayerJoined.PlayerCount,
                    });
                    break;
                }

                case Envelope.BodyOneofCase.HighFreqStateS2C:
                {
                    EventBus.Instance.Publish(new HighFreqStateReceivedEvent
                    {
                        playerId = (byte)env.HighFreqStateS2C.PlayerId,
                        position = ProtoMapping.ToVector2(env.HighFreqStateS2C.Position),
                        velocity = ProtoMapping.ToVector2(env.HighFreqStateS2C.Velocity),
                        animState = env.HighFreqStateS2C.AnimState,
                        facing = env.HighFreqStateS2C.Facing,
                    });
                    break;
                }

                case Envelope.BodyOneofCase.MidFreqState:
                {
                    byte opponent = RoomSession.Instance.OpponentId;
                    foreach (var p in env.MidFreqState.Players)
                    {
                        if (p.PlayerId == opponent)
                            RoomSession.Instance.UpdateOpponentStats(p.Hp, p.ShelterEnergy); // float（proto 精度升级）
                    }
                    break;
                }

                case Envelope.BodyOneofCase.OpponentDisconnect:
                {
                    EventBus.Instance.Publish(new OpponentDisconnectEvent
                    {
                        playerId = env.PlayerId, // 离开者在信封层
                        state = env.OpponentDisconnect.State,
                    });
                    break;
                }

                case Envelope.BodyOneofCase.FragmentDropPlan:
                {
                    if (env.FragmentDropPlan.Plan.Count == 0) break;

                    var plan = new List<DualEnigma.Fragment.FragmentDropPlan>(env.FragmentDropPlan.Plan.Count);
                    foreach (var item in env.FragmentDropPlan.Plan)
                    {
                        plan.Add(new DualEnigma.Fragment.FragmentDropPlan
                        {
                            FragmentId = item.FragmentId,
                            Type = (DualEnigma.Fragment.FragmentType)item.Type, // 0/1/2 顺序已核对一致
                            Position = ProtoMapping.ToVector2(item.Position),
                            DropTime = item.DropTime,
                            Seed = unchecked((uint)item.Seed), // long→uint 截断，两端一致即可保证确定性
                        });
                    }

                    if (DualEnigma.Fragment.FragmentSystem.HasInstance)
                        DualEnigma.Fragment.FragmentSystem.Instance.ExecuteDropPlan(plan);
                    break;
                }

                case Envelope.BodyOneofCase.FragmentResult:
                {
                    // 只处理对方接住：自己接住的本地已完成（上报发生在收集完成之后）
                    if (env.FragmentResult.PlayerId != RoomSession.Instance.LocalPlayerId
                        && DualEnigma.Fragment.FragmentSystem.HasInstance)
                    {
                        DualEnigma.Fragment.FragmentSystem.Instance.OnFragmentCollected(
                            env.FragmentResult.FragmentId, (byte)env.FragmentResult.PlayerId, false);
                    }
                    break;
                }

                case Envelope.BodyOneofCase.HeartbeatAck:
                {
                    _conn.NotifyHeartbeatAck(); // RTT 刷新（传输层）
                    break;
                }

                case Envelope.BodyOneofCase.Resp:
                {
                    _tracker.OnResp(env.Resp.ReqId, env.Resp.Code, env.Resp.Message);
                    break;
                }

                default:
                    // 预留消息（BuildingPlace 等）与未知 case：静默忽略
                    break;
            }
        }
    }
}
