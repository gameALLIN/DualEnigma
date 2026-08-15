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
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using DualEnigma.Framework.Core;
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
                case "S2C_ConnectAck":
                {
                    ConnectAckMessage msg = JsonUtility.FromJson<ConnectAckMessage>(json);
                    string roomCode = msg.data != null ? msg.data.roomCode : "";
                    int playerId = msg.data != null ? msg.data.playerId : 0;

                    NetworkSystem.Instance.SetRoomCode(roomCode);
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

                case "S2C_OpponentDisconnect":
                {
                    OpponentDisconnectMessage msg = JsonUtility.FromJson<OpponentDisconnectMessage>(json);
                    EventBus.Instance.Publish(new OpponentDisconnectEvent { playerId = msg.playerId });
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
