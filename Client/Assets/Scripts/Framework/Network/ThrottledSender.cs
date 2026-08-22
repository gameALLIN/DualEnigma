/// ============================================================
/// 文件名: ThrottledSender.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 限频发送器。抽取 NetworkSystem.SendHighFrequencyState 的节流逻辑：
///       累积 delta、间隔到点清零放行（与旧实现行为一致）。
/// 引用：无（纯逻辑，可单测）
/// ============================================================

namespace DualEnigma.Framework.Network
{
    /// <summary>限频器：按每秒次数放行</summary>
    public class ThrottledSender
    {
        private readonly float _interval;

        private float _accumulator;

        /// <summary>ratePerSecond 必须大于 0（否则视为不限频，每次放行）</summary>
        public ThrottledSender(float ratePerSecond)
        {
            _interval = ratePerSecond > 0f ? 1f / ratePerSecond : 0f;
        }

        /// <summary>到达发送时机返回 true（调用方执行发送）。累积 delta、到点清零放行。</summary>
        public bool Tick(float deltaTime)
        {
            if (_interval <= 0f) return true; // 不限频

            _accumulator += deltaTime;
            if (_accumulator < _interval)
                return false;

            _accumulator = 0f;
            return true;
        }

        /// <summary>清零累积（重连/复用时复位）</summary>
        public void Reset()
        {
            _accumulator = 0f;
        }
    }
}
