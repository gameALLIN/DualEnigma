/// ============================================================
/// 文件名: GameServerClient.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: game-server WebSocket 客户端。连接 /game 端点，
///       JSON 信封协议 {type, timestamp, playerId, data}。
///       收包经队列在主线程分发（EventBus），应用层心跳 1s，
///       断线发布 ServerDisconnectedEvent。
/// 引用：Server/network GameWebSocketHandler / MessageCodec / RoomManager
/// ============================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using DualEnigma.Framework.Core;
using DualEnigma.Core;
using DualEnigma.Data;

namespace DualEnigma.Network
{
    /// <summary>
    /// game-server WebSocket 客户端（全局单例）。
    /// 连接成功后发送 C2S_Connect（roomCode 空 = 创建/匹配房间），
    /// 收到 S2C_ConnectAck 时写入 NetworkSystem 房间码并发布 RoomConnectedEvent。
    /// </summary>
    public class GameServerClient : Singleton<GameServerClient>
    {
        /// <summary>应用层心跳间隔（秒），与服务端 HeartbeatManager 对齐</summary>
        private const float HEARTBEAT_INTERVAL = 1f;

        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        /// <summary>接收线程 → 主线程 的消息队列</summary>
        private readonly ConcurrentQueue<string> _receiveQueue = new ConcurrentQueue<string>();

        /// <summary>连接意图标记（正在连接/已连接）</summary>
        public bool IsConnected { get; private set; }

        private float _heartbeatTimer;
        private bool _disconnectPublished;

        // ── JSON 结构（与服务端 Message 子类字段一一对应）──

        [Serializable]
        private class MessageEnvelope { public string type; }

        [Serializable]
        private class ConnectData { public string roomCode = ""; }

        [Serializable]
        private class ConnectRequest
        {
            public string type = "C2S_Connect";
            public ConnectData data = new ConnectData();
        }

        [Serializable]
        private class HeartbeatRequest
        {
            public string type = "C2S_Heartbeat";
            public EmptyData data = new EmptyData();
        }

        [Serializable]
        private class StartGameRequest
        {
            public string type = "C2S_StartGame";
            public EmptyData data = new EmptyData();
        }

        [Serializable]
        private class EmptyData { }

        [Serializable]
        private class ConnectAckData { public int playerId; public string roomCode; }

        [Serializable]
        private class ConnectAckMessage
        {
            public string type;
            public ConnectAckData data;
        }

        [Serializable]
        private class GameStartData { public int chapter; public int section; public int round; }

        [Serializable]
        private class GameStartMessage
        {
            public string type;
            public GameStartData data;
        }

        [Serializable]
        private class OpponentDisconnectMessage
        {
            public string type;
            public int playerId;
        }

        [Serializable]
        private class PlayerJoinedData { public int playerId; public int playerCount; }

        [Serializable]
        private class PlayerJoinedMessage
        {
            public string type;
            public PlayerJoinedData data;
        }

        [Serializable]
        private class PhaseChangeData { public string phase; public int durationMs; public long phaseEndTime; }

        [Serializable]
        private class PhaseChangeMessage
        {
            public string type;
            public long timestamp;
            public PhaseChangeData data;
        }

        [Serializable]
        private class Vec2Data { public float x; public float y; }

        [Serializable]
        private class HighFreqData
        {
            public int playerId;   // 发送侧置 0（服务端按会话覆写）；接收侧读对方 ID
            public Vec2Data position;
            public Vec2Data velocity;
            public string animState;
            public bool facing;
            public int hp;
            public float shelterEnergy;
        }

        [Serializable]
        private class HighFreqRequest
        {
            public string type = "C2S_HighFreqState";
            public HighFreqData data;
        }

        [Serializable]
        private class HighFreqMessage
        {
            public string type;
            public HighFreqData data;
        }

        [Serializable]
        private class PlayerMidFreqData { public int playerId; public int hp; public int shelterEnergy; public int[] carriedFragments; }

        [Serializable]
        private class MidFreqData { public List<PlayerMidFreqData> players; }

        [Serializable]
        private class MidFreqMessage { public string type; public MidFreqData data; }

        [Serializable]
        private class DropPlanVec2 { public float x; public float y; }

        [Serializable]
        private class DropPlanItem
        {
            public int fragmentId;
            public int type;
            public DropPlanVec2 position;
            public float dropTime;
            public long seed;
        }

        [Serializable]
        private class DropPlanData { public List<DropPlanItem> plan; }

        [Serializable]
        private class FragmentDropPlanMessage { public string type; public DropPlanData data; }

        [Serializable]
        private class FragmentCaughtData { public int fragmentId; }

        [Serializable]
        private class FragmentCaughtRequest
        {
            public string type = "C2S_FragmentCaught";
            public FragmentCaughtData data = new FragmentCaughtData();
        }

        [Serializable]
        private class FragmentResultData { public int fragmentId; public int playerId; public int multiplier; public bool isSimultaneous; }

        [Serializable]
        private class FragmentResultMessage { public string type; public FragmentResultData data; }

        private NetworkConfig _config;

        protected override void OnSingletonInitialized()
        {
            _config = DataManager.Instance.LoadConfig<NetworkConfig>("NetworkConfig");
        }

        private string ServerUrl
        {
            get
            {
                string url = _config != null ? _config.GameServerWsUrl : null;
                return string.IsNullOrEmpty(url) ? "ws://localhost:8080/game" : url;
            }
        }

        // ============================================================
        //  连接管理
        // ============================================================

        /// <summary>
        /// 连接 game-server 并请求进房。
        /// roomCode 为空 → 创建新房间（自动匹配）；非空 → 加入指定好友房间。
        /// </summary>
        public async void Connect(string roomCode)
        {
            if (IsConnected || (_socket != null && _socket.State == WebSocketState.Connecting))
            {
                Debug.Log("[GameServerClient] 已连接或正在连接，忽略重复请求");
                return;
            }

            _disconnectPublished = false;
            _cts = new CancellationTokenSource();
            _socket = new ClientWebSocket();

            try
            {
                Debug.Log($"[GameServerClient] 连接 {ServerUrl} (roomCode=\"{roomCode}\")");
                using (var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                {
                    connectTimeout.CancelAfter(TimeSpan.FromSeconds(5));
                    await _socket.ConnectAsync(new Uri(ServerUrl), connectTimeout.Token);
                }

                IsConnected = true;

                // 连接建立 → 发送进房请求
                await SendJsonAsync(JsonUtility.ToJson(new ConnectRequest { data = new ConnectData { roomCode = roomCode ?? "" } }));

                _ = ReceiveLoopAsync(_cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameServerClient] 连接失败: {e.Message}");
                IsConnected = false;
                PublishDisconnected("无法连接服务器，请确认 game-server 已启动");
            }
        }

        /// <summary>主动断开（退出房间时调用，不发布异常断线事件）</summary>
        public async void Disconnect()
        {
            _disconnectPublished = true; // 主动断开不算异常
            try
            {
                if (_socket != null && _socket.State == WebSocketState.Open)
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client leave", cts.Token);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameServerClient] 关闭连接异常: {e.Message}");
            }
            finally
            {
                Cleanup();
            }
        }

        private void Cleanup()
        {
            IsConnected = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _socket?.Dispose();
            _socket = null;
        }

        /// <summary>
        /// 房主请求开始对局（服务端校验房主身份 + 满员后广播 GameStart）.
        /// </summary>
        public void RequestStartGame()
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[GameServerClient] 未连接，无法请求开局");
                return;
            }
            _ = SendJsonAsync(JsonUtility.ToJson(new StartGameRequest()));
        }

        /// <summary>上报本地角色高频状态（限频由 NetworkSystem 内部 20Hz 节流）</summary>
        public void SendHighFreqState(Vector2 position, Vector2 velocity, string animState, bool facing, int hp, float shelterEnergy)
        {
            if (!IsConnected) return;
            _ = SendJsonAsync(JsonUtility.ToJson(new HighFreqRequest
            {
                data = new HighFreqData
                {
                    playerId = 0,
                    position = new Vec2Data { x = position.x, y = position.y },
                    velocity = new Vec2Data { x = velocity.x, y = velocity.y },
                    animState = animState,
                    facing = facing,
                    hp = hp,
                    shelterEnergy = shelterEnergy
                }
            }));
        }

        /// <summary>上报碎片接住（本地收集完成时由 NetworkGameSync 调用）</summary>
        public void SendFragmentCaught(int fragmentId)
        {
            if (!IsConnected) return;
            _ = SendJsonAsync(JsonUtility.ToJson(new FragmentCaughtRequest { data = new FragmentCaughtData { fragmentId = fragmentId } }));
        }

        /// <summary>发送一行 JSON（线程安全，同一时刻仅一个 SendAsync）</summary>
        private async Task SendJsonAsync(string json)
        {
            ClientWebSocket socket = _socket;
            CancellationTokenSource cts = _cts;
            if (socket == null || cts == null || socket.State != WebSocketState.Open) return;

            await _sendLock.WaitAsync();
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
            }
            catch (Exception e)
            {
                // 发送失败多数由断开引起，接收循环会统一走断线流程
                Debug.LogWarning($"[GameServerClient] 发送失败: {e.Message}");
            }
            finally
            {
                _sendLock.Release();
            }
        }

        // ============================================================
        //  接收（后台线程收 → 主线程分发）
        // ============================================================

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            StringBuilder sb = new StringBuilder();

            try
            {
                while (!token.IsCancellationRequested && _socket.State == WebSocketState.Open)
                {
                    sb.Clear();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Debug.Log("[GameServerClient] 服务端关闭连接");
                            _receiveQueue.Enqueue(null); // 断线标记
                            return;
                        }
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    } while (!result.EndOfMessage);

                    _receiveQueue.Enqueue(sb.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                // 主动断开，正常退出
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameServerClient] 接收异常: {e.Message}");
                _receiveQueue.Enqueue(null);
            }
        }

        private void Update()
        {
            // 主线程分发：null = 连接断开标记
            while (_receiveQueue.TryDequeue(out string json))
            {
                if (json == null)
                {
                    PublishDisconnected("连接已断开");
                    Cleanup();
                    continue;
                }
                DispatchMessage(json);
            }

            // 应用层心跳
            if (IsConnected)
            {
                _heartbeatTimer += Time.unscaledDeltaTime;
                if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
                {
                    _heartbeatTimer = 0f;
                    _ = SendJsonAsync(JsonUtility.ToJson(new HeartbeatRequest()));
                }
            }
        }

        private void DispatchMessage(string json)
        {
            MessageEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<MessageEnvelope>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameServerClient] 消息解析失败: {e.Message}");
                return;
            }

            switch (envelope.type)
            {
                case "S2C_PhaseChange":
                {
                    PhaseChangeMessage msg = JsonUtility.FromJson<PhaseChangeMessage>(json);
                    if (msg?.data == null) break;
                    if (Enum.TryParse(msg.data.phase, out GamePhase phase))
                    {
                        // 双端时钟不可信：剩余时长 = 消息内 phaseEndTime - timestamp（同为服务器时钟）
                        float remaining = (msg.data.phaseEndTime - msg.timestamp) / 1000f;
                        GameStateMachine.Instance.ApplyServerPhase(phase, remaining);
                    }
                    break;
                }

                case "S2C_ConnectAck":
                {
                    ConnectAckMessage msg = JsonUtility.FromJson<ConnectAckMessage>(json);
                    string roomCode = msg.data != null ? msg.data.roomCode : "";
                    int playerId = msg.data != null ? msg.data.playerId : 0;

                    NetworkSystem.Instance.SetRoomCode(roomCode);
                    NetworkSystem.Instance.SetLocalPlayerId(playerId);
                    NetworkSystem.Instance.SetConnected(true);
                    Debug.Log($"[GameServerClient] 已加入房间 {roomCode} (playerId={playerId})");
                    EventBus.Instance.Publish(new RoomConnectedEvent { playerId = playerId, roomCode = roomCode });
                    break;
                }

                case "S2C_GameStart":
                {
                    GameStartMessage msg = JsonUtility.FromJson<GameStartMessage>(json);
                    EventBus.Instance.Publish(new RoomGameStartEvent
                    {
                        chapter = msg.data?.chapter ?? 1,
                        section = msg.data?.section ?? 1,
                        round = msg.data?.round ?? 1
                    });
                    break;
                }

                case "S2C_PlayerJoined":
                {
                    PlayerJoinedMessage msg = JsonUtility.FromJson<PlayerJoinedMessage>(json);
                    EventBus.Instance.Publish(new PlayerJoinedRoomEvent
                    {
                        playerId = msg.data?.playerId ?? 0,
                        playerCount = msg.data?.playerCount ?? 1
                    });
                    break;
                }

                case "S2C_HighFreqState":
                {
                    HighFreqMessage msg = JsonUtility.FromJson<HighFreqMessage>(json);
                    if (msg?.data == null) break;
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
                    break;
                }

                case "S2C_MidFreqState":
                {
                    MidFreqMessage msg = JsonUtility.FromJson<MidFreqMessage>(json);
                    if (msg?.data?.players == null) break;
                    byte opponent = NetworkSystem.Instance.OpponentId;
                    foreach (PlayerMidFreqData p in msg.data.players)
                    {
                        if (p.playerId == opponent)
                            NetworkSystem.Instance.SetOpponentStats(p.hp, p.shelterEnergy);
                    }
                    break;
                }

                case "S2C_OpponentDisconnect":
                {
                    OpponentDisconnectMessage msg = JsonUtility.FromJson<OpponentDisconnectMessage>(json);
                    EventBus.Instance.Publish(new OpponentDisconnectEvent { playerId = msg.playerId });
                    break;
                }

                case "S2C_FragmentDropPlan":
                {
                    FragmentDropPlanMessage msg = JsonUtility.FromJson<FragmentDropPlanMessage>(json);
                    if (msg?.data?.plan == null || msg.data.plan.Count == 0) break;

                    var plan = new List<DualEnigma.Fragment.FragmentDropPlan>(msg.data.plan.Count);
                    foreach (DropPlanItem item in msg.data.plan)
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
                    break;
                }

                case "S2C_FragmentResult":
                {
                    FragmentResultMessage msg = JsonUtility.FromJson<FragmentResultMessage>(json);
                    if (msg?.data == null) break;

                    // 只处理对方接住：自己接住的本地已完成（上报发生在收集完成之后）
                    // 对方接住走 OnFragmentCollected → 100ms 同接窗口超时后完成移除（M5 改服务器权威判定）
                    if (msg.data.playerId != NetworkSystem.Instance.LocalPlayerId
                        && DualEnigma.Fragment.FragmentSystem.HasInstance)
                    {
                        DualEnigma.Fragment.FragmentSystem.Instance.OnFragmentCollected(
                            msg.data.fragmentId, (byte)msg.data.playerId, false);
                    }
                    break;
                }

                case "S2C_HeartbeatAck":
                    break; // 心跳回复，无需处理

                default:
                    // 对局中的高阶消息（状态同步/建造/灾害等）后续接入
                    break;
            }
        }

        /// <summary>发布断线事件（主动 Disconnect 不发布）</summary>
        private void PublishDisconnected(string reason)
        {
            if (_disconnectPublished) return;
            _disconnectPublished = true;

            NetworkSystem.Instance.SetConnected(false);
            EventBus.Instance.Publish(new ServerDisconnectedEvent { reason = reason });
            Debug.LogWarning($"[GameServerClient] {reason}");
        }

        protected override void OnDestroy()
        {
            // 单例销毁（场景卸载）：静默释放
            _disconnectPublished = true;
            Cleanup();
            base.OnDestroy();
        }
    }
}
