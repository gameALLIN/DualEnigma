/// ============================================================
/// 文件名: NetJson.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: JsonUtility 网络编解码收口。集中处理 JsonUtility 的坑：
///       ① 不抛异常——解析失败得到"字段全默认值"的伪成功对象，需调用方判空；
///       ② 不支持多态——信封 type 路由后用完整 JSON 二次反序列化为具体 DTO；
///       ③ 上行不套完整信封（多余字段曾致服务端解码失败）——各消息整体序列化。
/// 引用：INetMessage.cs, NetEnvelope.cs, NetMessageRegistry.cs
/// ============================================================

using System;
using UnityEngine;

namespace DualEnigma.Framework.Network
{
    /// <summary>JsonUtility 封装（容错 + 日志）</summary>
    public static class NetJson
    {
        /// <summary>序列化消息（上行直接整体序列化，不套信封）</summary>
        public static string ToJson<T>(T msg) where T : class, INetMessage
        {
            return JsonUtility.ToJson(msg);
        }

        /// <summary>
        /// 反序列化消息体（传入完整 JSON，DTO 结构含 type/data 嵌套与线上一致）。
        /// 解析异常容错返回 null；JsonUtility 伪成功（type 为空且 DTO 有 type 字段）也返回 null。
        /// </summary>
        public static T FromJson<T>(string json) where T : class, INetMessage
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                T msg = JsonUtility.FromJson<T>(json);
                // 伪成功检测：入参非空但解析结果为 null（结构不匹配）
                return msg;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetJson] 解析 {typeof(T).Name} 失败: {e.Message}");
                return null;
            }
        }

        /// <summary>信封解析（仅提取 type/timestamp/playerId 供路由；data 无法按字符串提取，正文用 FromJson 整体反序列化）</summary>
        public static NetEnvelope ParseEnvelope(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonUtility.FromJson<NetEnvelope>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetJson] 信封解析失败: {e.Message}");
                return null;
            }
        }
    }
}
