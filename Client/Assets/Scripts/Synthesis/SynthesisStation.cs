/// ============================================================
/// 文件名: SynthesisStation.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 合成台交互点。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Synthesis
{
    /// <summary>
    /// 合成台交互点。挂在合成台 GameObject 上。
    /// 引用：合成系统.md §3.2
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SynthesisStation : MonoBehaviour
    {
        /// <summary>合成台位置标识</summary>
        public int StationId { get; set; }
        /// <summary>当前是否有人正在使用</summary>
        public bool IsOccupied { get; private set; }
        /// <summary>当前使用者ID</summary>
        public byte CurrentUserId { get; private set; }
        /// <summary>当前等待队列中的玩家数量</summary>
        public int QueueCount => _waitQueue.Count;

        /// <summary>等待队列（两人同时使用需排队）</summary>
        private readonly Queue<byte> _waitQueue = new Queue<byte>();

        /// <summary>
        /// 占用合成台。
        /// 引用：合成系统.md §4.1 队列规则
        /// </summary>
        /// <param name="playerId">请求占用的玩家ID</param>
        /// <returns>true=成功占用；false=已占用，玩家已加入等待队列</returns>
        public bool Occupy(byte playerId)
        {
            if (IsOccupied)
            {
                // 避免同一玩家重复入队
                foreach (byte queuedId in _waitQueue)
                {
                    if (queuedId == playerId)
                    {
                        Debug.Log($"[SynthesisStation] 玩家{playerId}已在合成台{StationId}的等待队列中");
                        return false;
                    }
                }

                _waitQueue.Enqueue(playerId);
                Debug.Log($"[SynthesisStation] 合成台{StationId}已占用，玩家{playerId}加入等待队列（当前队列{_waitQueue.Count}人）");
                return false;
            }

            IsOccupied = true;
            CurrentUserId = playerId;
            return true;
        }

        /// <summary>
        /// 释放合成台。若等待队列中有玩家，自动分配给下一个玩家。
        /// 引用：合成系统.md §4.1 队列规则
        /// </summary>
        public void Release()
        {
            IsOccupied = false;
            CurrentUserId = 0;

            if (_waitQueue.Count > 0)
            {
                byte nextPlayerId = _waitQueue.Dequeue();
                IsOccupied = true;
                CurrentUserId = nextPlayerId;
                Debug.Log($"[SynthesisStation] 合成台{StationId}释放，自动分配给等待中的玩家{nextPlayerId}");

                // 通知合成系统，合成台已可用
                SynthesisSystem.Instance.OnStationAvailable(this, nextPlayerId);
            }
            else
            {
                Debug.Log($"[SynthesisStation] 合成台{StationId}释放，无等待者");
            }
        }

        /// <summary>
        /// 从等待队列中移除指定玩家（取消等待）。
        /// </summary>
        /// <param name="playerId">要移除的玩家ID</param>
        public void CancelWait(byte playerId)
        {
            if (_waitQueue.Count == 0) return;

            // Queue 不支持随机移除，需要重建队列
            int originalCount = _waitQueue.Count;
            byte[] remaining = new byte[originalCount];
            int writeIndex = 0;

            while (_waitQueue.Count > 0)
            {
                byte id = _waitQueue.Dequeue();
                if (id != playerId)
                    remaining[writeIndex++] = id;
            }

            for (int i = 0; i < writeIndex; i++)
                _waitQueue.Enqueue(remaining[i]);

            if (writeIndex < originalCount)
                Debug.Log($"[SynthesisStation] 玩家{playerId}已取消合成台{StationId}的等待");
        }
    }
}
