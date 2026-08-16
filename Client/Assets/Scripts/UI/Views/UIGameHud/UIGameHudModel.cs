/// ============================================================
/// 文件名: UIGameHudModel.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: 局内 HUD 数据模型。阶段进度/显示值缓存。
/// ============================================================

using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UIGameHudModel : UIModelBase
    {
        /// <summary>当前阶段总时长（秒），阶段切换瞬间剩余时长即总时长</summary>
        public float PhaseTotalSeconds { get; set; } = 5f;

        /// <summary>HUD 是否处于对局中（对局开始到结束之间）</summary>
        public bool IsInGame { get; set; }
    }
}
