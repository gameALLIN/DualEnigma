/// ============================================================
/// 文件名: RequestTracker.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 轻量请求跟踪器：reqId 登记 → 超时扫描 → S2C_Resp 回执派发。
///       保证每个请求必有一次性结局：服务器回执 code / 本地超时(-1)。
///       高频流（C2S_HighFreqState）豁免不登记；心跳以 S2C_HeartbeatAck 为专属回执。
/// 引用：GameConnection.cs, NetErrorCode, NetworkEvents.cs
/// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Network
{
    /// <summary>请求跟踪器（纯逻辑，可单测）</summary>
    public class RequestTracker
    {
        private sealed class Entry
        {
            public string SourceType;
            public float Deadline;                 // Time.realtimeSinceStartup 时刻
            public Action<int, string> OnResp;
        }

        private readonly Dictionary<int, Entry> _pending = new Dictionary<int, Entry>();
        private int _nextReqId = 1;                // 每连接自增，从 1 开始（0=旧客户端语义）

        /// <summary>待决请求数（调试/测试用）</summary>
        public int PendingCount => _pending.Count;

        /// <summary>
        /// 发送前登记。返回分配的 reqId（调用方填入 C2S 消息体）。
        /// onResp 必被调用一次：服务器回执 code，或本地超时 -1（此时 message 为超时文案）。
        /// </summary>
        public int Register(string sourceType, float timeoutSec, Action<int, string> onResp)
        {
            int reqId = _nextReqId++;
            _pending[reqId] = new Entry
            {
                SourceType = sourceType,
                Deadline = Time.realtimeSinceStartup + timeoutSec,
                OnResp = onResp,
            };
            return reqId;
        }

        /// <summary>收到 S2C_Resp 派发（未知 reqId 静默忽略——旧请求已超时清理等场景）</summary>
        public void OnResp(int reqId, int code, string message)
        {
            if (!_pending.TryGetValue(reqId, out Entry entry)) return;
            _pending.Remove(reqId);
            entry.OnResp?.Invoke(code, string.IsNullOrEmpty(message) ? "" : message);
        }

        /// <summary>超时扫描（主线程 Update 调用）：到期的请求以 code=-1 结束</summary>
        public void Tick(float deltaTime)
        {
            if (_pending.Count == 0) return;

            List<int> expired = null;
            float now = Time.realtimeSinceStartup;
            foreach (var kvp in _pending)
            {
                if (now >= kvp.Value.Deadline)
                {
                    expired ??= new List<int>();
                    expired.Add(kvp.Key);
                }
            }

            if (expired == null) return;
            foreach (int reqId in expired)
            {
                if (_pending.TryGetValue(reqId, out Entry entry))
                {
                    _pending.Remove(reqId);
                    entry.OnResp?.Invoke((int)NetErrorCode.LocalTimeout, "操作无响应，请检查网络");
                }
            }
        }

        /// <summary>清空全部待决（断线/重连时调用；不触发回调——连接层事件已覆盖）</summary>
        public void Clear()
        {
            _pending.Clear();
        }
    }
}
