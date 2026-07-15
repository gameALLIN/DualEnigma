/// ============================================================
/// 文件名: FragmentController.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 碎片控制器，管理存续倒计时和状态。
/// ============================================================

using UnityEngine;
using DualEnigma.Core;

namespace DualEnigma.Fragment
{
    /// <summary>
    /// 碎片控制器，挂载在碎片 GameObject 上。
    /// 使用 Trigger2D 检测角色碰撞，管理存续倒计时。
    /// 引用：碎片系统.md §3.2
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FragmentController : MonoBehaviour
    {
        /// <summary>碎片唯一ID</summary>
        public int FragmentId { get; private set; }

        /// <summary>碎片类型</summary>
        public FragmentType Type { get; private set; }

        /// <summary>当前状态</summary>
        public FragmentState State { get; private set; }

        /// <summary>存续剩余时间（秒）</summary>
        public float RemainingTime { get; private set; }

        private bool _isInitialized;

        /// <summary>
        /// 初始化碎片。
        /// </summary>
        public void Initialize(FragmentDropPlan plan, float lifetime)
        {
            FragmentId = plan.FragmentId;
            Type = plan.Type;
            RemainingTime = lifetime;
            State = FragmentState.Falling;
            _isInitialized = true;

            transform.position = plan.Position;
        }

        /// <summary>
        /// 设置碎片状态。
        /// </summary>
        public void SetState(FragmentState state)
        {
            State = state;
        }

        private void Update()
        {
            if (!_isInitialized || State != FragmentState.Falling)
                return;

            RemainingTime -= Time.deltaTime;

            if (RemainingTime <= 0f)
            {
                SetState(FragmentState.Despawned);
                FragmentSystem.Instance.OnFragmentDespawned(FragmentId);
            }
        }
    }
}
