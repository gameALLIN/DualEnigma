/// ============================================================
/// 文件名: SynthesisStation.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 合成台交互点。
/// ============================================================

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

        /// <summary>占用合成台</summary>
        public bool Occupy(byte playerId)
        {
            if (IsOccupied) return false;
            IsOccupied = true;
            CurrentUserId = playerId;
            return true;
        }

        /// <summary>释放合成台</summary>
        public void Release()
        {
            IsOccupied = false;
            CurrentUserId = 0;
        }
    }
}
