/// ============================================================
/// 文件名: NetMessageRegistry.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 消息路由注册表：type 字符串 → 反序列化目标类型 + 处理器。
///       替换巨型 switch 的分发职责；handler 异常隔离（单条抛错不断流）；
///       未知类型静默忽略并 log。支持 (body) 与 (envelope, body) 两种
///       handler 签名——需要信封 timestamp 的消息（如时钟差值法）用双参版。
/// 引用：INetMessage.cs, NetEnvelope.cs, NetJson.cs
/// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Framework.Network
{
    /// <summary>消息注册表：注册 / 派发 / 注销</summary>
    public class NetMessageRegistry
    {
        /// <summary>handler 包装：按需提供信封</summary>
        private abstract class Entry
        {
            public abstract void Invoke(NetEnvelope envelope, string json);
        }

        private sealed class Entry<T> : Entry where T : class, INetMessage
        {
            public readonly Action<T> BodyHandler;
            public readonly Action<NetEnvelope, T> EnvelopeHandler;

            public Entry(Action<T> bodyHandler, Action<NetEnvelope, T> envelopeHandler)
            {
                BodyHandler = bodyHandler;
                EnvelopeHandler = envelopeHandler;
            }

            public override void Invoke(NetEnvelope envelope, string json)
            {
                T body = NetJson.FromJson<T>(json);
                if (body == null) return;

                if (EnvelopeHandler != null)
                    EnvelopeHandler(envelope, body);
                else
                    BodyHandler?.Invoke(body);
            }
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        /// <summary>已注册消息类型数（测试与调试用）</summary>
        public int Count => _entries.Count;

        /// <summary>注册：消息类型字符串 → 处理器（不需要信封）</summary>
        public void Register<T>(string messageType, Action<T> handler) where T : class, INetMessage
        {
            if (handler == null) return;
            RegisterInternal<T>(messageType, handler, null);
        }

        /// <summary>注册：消息类型字符串 → 处理器（需要信封 timestamp/playerId）</summary>
        public void Register<T>(string messageType, Action<NetEnvelope, T> handler) where T : class, INetMessage
        {
            if (handler == null) return;
            RegisterInternal<T>(messageType, null, handler);
        }

        private void RegisterInternal<T>(string messageType, Action<T> bodyHandler, Action<NetEnvelope, T> envelopeHandler)
            where T : class, INetMessage
        {
            if (string.IsNullOrEmpty(messageType))
            {
                Debug.LogWarning("[NetMessageRegistry] 注册失败：messageType 为空");
                return;
            }

            _entries[messageType] = new Entry<T>(bodyHandler, envelopeHandler);
        }

        /// <summary>注销指定处理器（同类型重复注册时整体移除）</summary>
        public void Unregister<T>(string messageType, Action<T> handler) where T : class, INetMessage
        {
            if (handler == null) return;
            RemoveIfType<T>(messageType);
        }

        /// <summary>注销（信封签名重载）</summary>
        public void Unregister<T>(string messageType, Action<NetEnvelope, T> handler) where T : class, INetMessage
        {
            if (handler == null) return;
            RemoveIfType<T>(messageType);
        }

        private void RemoveIfType<T>(string messageType) where T : class, INetMessage
        {
            if (_entries.TryGetValue(messageType, out Entry entry) && entry is Entry<T>)
                _entries.Remove(messageType);
        }

        /// <summary>
        /// 分发一条原始 JSON：信封解析取 type → 查表 → 完整 JSON 反序列化 → 派发。
        /// 未知类型静默忽略并 log；handler 异常隔离（吞掉并记录，不影响后续消息）。
        /// </summary>
        public void Dispatch(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            NetEnvelope envelope = NetJson.ParseEnvelope(json);
            string type = envelope?.type;
            if (string.IsNullOrEmpty(type))
            {
                Debug.LogWarning("[NetMessageRegistry] 丢弃无 type 消息");
                return;
            }

            if (!_entries.TryGetValue(type, out Entry entry))
            {
                Debug.Log($"[NetMessageRegistry] 未知消息类型，忽略: {type}");
                return;
            }

            try
            {
                entry.Invoke(envelope, json);
            }
            catch (Exception e)
            {
                // 单 handler 抛错不断流：记录后继续
                Debug.LogError($"[NetMessageRegistry] 处理 {type} 异常: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
