/// ============================================================
/// 文件名: UISettingsView.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: 局内设置弹窗视图。音量滑条 + 继续游戏 + 退出对局。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UISettingsView : UIViewBase
    {
        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====
        [SerializeField] private Slider m_VolumeSlider;
        [SerializeField] private Text m_VolumeValueText;
        [SerializeField] private Button m_ContinueBtn;
        [SerializeField] private Button m_ExitBtn;
        // ===== Auto Bind End =====

        public Slider VolumeSlider => m_VolumeSlider;
        public Text VolumeValueText => m_VolumeValueText;
        public Button ContinueBtn => m_ContinueBtn;
        public Button ExitBtn => m_ExitBtn;
    }
}
