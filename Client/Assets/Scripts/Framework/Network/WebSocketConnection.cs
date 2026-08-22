/// ============================================================
/// 文件名: WebSocketConnection.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: WebSocket 传输层（框架化，零业务知识）：连接/字节收发/分片拼包/
///       心跳定时/RTT/主线程分发泵/主动与异常断开区分。
///       从 GameServerClient 逐行搬运泛化：不发送任何业务 JSON（进房等
///       由上层在 OnOpen 后自行发送）；心跳包内容由 payloadFactory 注入，
///       Ack 识别由上层注册表调用 NotifyHeartbeatAck()。
///       使用方式：由上层（如 GameConnection）动态 AddComponent，非自身单例。
/// ============================================================

using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DualEnigma.Framework.Network
{
    /// <summary>WebSocket 连接（MonoBehaviour，挂上层单例 GameObject 下）</summary>
    public class WebSocketConnection : MonoBehaviour
    {
        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        /// <summary>接收线程 → 主线程 的消息队列（null = 断线标记）</summary>
        private readonly ConcurrentQueue<byte[]> _receiveQueue = new ConcurrentQueue<byte[]>();

        /// <summary>主动关闭标记：断线路径不触发异常事件</summary>
        private bool _manualClose;

        private float _heartbeatTimer;
        private float _heartbeatInterval = -1f;
        private Func<byte[]> _heartbeatPayloadFactory;

        /// <summary>心跳发送时刻（Time.realtimeSinceStartup，Ack 时求差得 RTT）</summary>
        private float _lastHeartbeatSendTime = -1f;

        /// <summary>是否已连接（握手完成）</summary>
        public bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;

        /// <summary>最近一次心跳往返延迟（毫秒）；-1 = 未知（尚无样本）</summary>
        public float RttMs { get; private set; } = -1f;

        /// <summary>收到完整消息（主线程，已拆包的二进制帧）</summary>
        public event Action<byte[]> OnMessageReceived;

        /// <summary>异常断开（主线程，reason；主动 CloseAsync 不触发）</summary>
        public event Action<string> OnAbnormalDisconnected;

        // ============================================================
        //  连接
        // ============================================================

        /// <summary>
        /// 连接服务器（握手超时默认 5 秒；重复连接防抖）。
        /// 仅建立传输层连接，不发送任何业务消息；成功后启动接收循环。
        /// 握手失败向上抛异常，由上层决定文案与后续处理。
        /// </summary>
        public async Task ConnectAsync(Uri url, float connectTimeoutSec = 5f)
        {
            if (IsConnected || (_socket != null && _socket.State == WebSocketState.Connecting))
            {
                Debug.Log("[WebSocketConnection] 已连接或正在连接，忽略重复请求");
                return;
            }

            _manualClose = false;
            _cts = new CancellationTokenSource();
            _socket = new ClientWebSocket();

            try
            {
                using (var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                {
                    connectTimeout.CancelAfter(TimeSpan.FromSeconds(connectTimeoutSec));
                    await _socket.ConnectAsync(url, connectTimeout.Token);
                }
                _ = ReceiveLoopAsync(_cts.Token);
            }
            catch (Exception)
            {
                Cleanup();
                throw; // 上层 catch 后决定文案（ServerUnreachable / ConnectTimeout 等）
            }
        }

        /// <summary>主动关闭（不触发 OnAbnormalDisconnected）</summary>
        public async Task CloseAsync()
        {
            _manualClose = true;
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
                Debug.LogWarning($"[WebSocketConnection] 关闭连接异常: {e.Message}");
            }
            finally
            {
                Cleanup();
            }
        }

        private void Cleanup()
        {
            RttMs = -1f;
            _lastHeartbeatSendTime = -1f;
            _heartbeatInterval = -1f;
            _heartbeatPayloadFactory = null;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _socket?.Dispose();
            _socket = null;
        }

        // ============================================================
        //  发送
        // ============================================================

        /// <summary>发送二进制帧（线程安全，同一时刻仅一个 SendAsync）</summary>
        public async Task SendAsync(byte[] payload)
        {
            ClientWebSocket socket = _socket;
            CancellationTokenSource cts = _cts;
            if (socket == null || cts == null || socket.State != WebSocketState.Open) return;

            await _sendLock.WaitAsync();
            try
            {
                await socket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Binary, true, cts.Token);
            }
            catch (Exception e)
            {
                // 发送失败多数由断开引起，接收循环会统一走断线流程
                Debug.LogWarning($"[WebSocketConnection] 发送失败: {e.Message}");
            }
            finally
            {
                _sendLock.Release();
            }
        }

        // ============================================================
        //  心跳
        // ============================================================

        /// <summary>启动应用层心跳（interval 秒一次，帧内容由 payloadFactory 提供）</summary>
        public void StartHeartbeat(float intervalSec, Func<byte[]> payloadFactory)
        {
            _heartbeatInterval = intervalSec;
            _heartbeatPayloadFactory = payloadFactory;
            _heartbeatTimer = 0f;
        }

        /// <summary>心跳 Ack 通知（由上层 Ack 处理器调用），刷新 RTT</summary>
        public void NotifyHeartbeatAck()
        {
            if (_lastHeartbeatSendTime > 0f)
            {
                RttMs = (Time.realtimeSinceStartup - _lastHeartbeatSendTime) * 1000f;
                _lastHeartbeatSendTime = -1f;
            }
        }

        // ============================================================
        //  接收循环（后台线程）
        // ============================================================

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[8192];

            try
            {
                while (!token.IsCancellationRequested && _socket.State == WebSocketState.Open)
                {
                    // 二进制协议：整帧缓冲（Envelope 小帧 <MTU，整帧到达一次收完）
                    using (var ms = new System.IO.MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                Debug.Log("[WebSocketConnection] 服务端关闭连接");
                                _receiveQueue.Enqueue(null); // 断线标记
                                return;
                            }
                            if (result.MessageType == WebSocketMessageType.Text)
                            {
                                // 协议已切换为二进制帧——文本帧视为协议错误
                                Debug.LogWarning("[WebSocketConnection] 收到意外文本帧，丢弃");
                                break;
                            }
                            ms.Write(buffer, 0, result.Count);
                        } while (!result.EndOfMessage);

                        if (ms.Length > 0)
                            _receiveQueue.Enqueue(ms.ToArray());
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 主动断开（CloseAsync 触发 Cancel），正常退出
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WebSocketConnection] 接收异常: {e.Message}");
                _receiveQueue.Enqueue(null);
            }
        }

        // ============================================================
        //  主线程分发泵
        // ============================================================

        private void Update()
        {
            // null = 连接断开标记
            while (_receiveQueue.TryDequeue(out byte[] payload))
            {
                if (payload == null)
                {
                    HandleDisconnected("连接已断开");
                    continue;
                }
                OnMessageReceived?.Invoke(payload);
            }

            // 应用层心跳
            if (IsConnected && _heartbeatInterval > 0f && _heartbeatPayloadFactory != null)
            {
                _heartbeatTimer += Time.unscaledDeltaTime;
                if (_heartbeatTimer >= _heartbeatInterval)
                {
                    _heartbeatTimer = 0f;
                    _lastHeartbeatSendTime = Time.realtimeSinceStartup;
                    _ = SendAsync(_heartbeatPayloadFactory());
                }
            }
        }

        private void HandleDisconnected(string reason)
        {
            bool manual = _manualClose;
            Cleanup();
            if (!manual)
                OnAbnormalDisconnected?.Invoke(reason);
        }

        protected virtual void OnDestroy()
        {
            _manualClose = true; // 场景卸载销毁不算异常断线
            Cleanup();
        }
    }
}
