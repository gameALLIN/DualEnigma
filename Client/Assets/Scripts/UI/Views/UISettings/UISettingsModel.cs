/// ============================================================
/// 文件名: UISettingsModel.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: 局内设置弹窗数据模型。
/// ============================================================

using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UISettingsModel : UIModelBase
    {
        /// <summary>音量（0-1，PlayerPrefs 持久化）</summary>
        public float Volume { get; set; } = 0.8f;
    }
}
