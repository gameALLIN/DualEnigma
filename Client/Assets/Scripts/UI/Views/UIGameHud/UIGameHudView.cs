/// ============================================================
/// 文件名: UIGameHudView.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: 局内 HUD 视图。双角色血条/能量条、阶段进度条、
///       关卡信息、碎片计数、设置按钮。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UIGameHudView : UIViewBase
    {
        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====
        [SerializeField] private Text m_LevelInfoText;
        [SerializeField] private Text m_PhaseNameText;
        [SerializeField] private Text m_PhaseTimerText;
        [SerializeField] private Image m_PhaseProgressFill;
        [SerializeField] private Text m_AquaTitleText;
        [SerializeField] private Image m_AquaHPFill;
        [SerializeField] private Text m_AquaHPText;
        [SerializeField] private Image m_AquaEnergyFill;
        [SerializeField] private Text m_AquaEnergyText;
        [SerializeField] private Text m_IgnisTitleText;
        [SerializeField] private Image m_IgnisHPFill;
        [SerializeField] private Text m_IgnisHPText;
        [SerializeField] private Image m_IgnisEnergyFill;
        [SerializeField] private Text m_IgnisEnergyText;
        [SerializeField] private Text m_FragmentCountText;
        [SerializeField] private Button m_SettingsBtn;
        // ===== Auto Bind End =====

        public Text LevelInfoText => m_LevelInfoText;
        public Text PhaseNameText => m_PhaseNameText;
        public Text PhaseTimerText => m_PhaseTimerText;
        public Image PhaseProgressFill => m_PhaseProgressFill;
        public Text AquaTitleText => m_AquaTitleText;
        public Image AquaHPFill => m_AquaHPFill;
        public Text AquaHPText => m_AquaHPText;
        public Image AquaEnergyFill => m_AquaEnergyFill;
        public Text AquaEnergyText => m_AquaEnergyText;
        public Text IgnisTitleText => m_IgnisTitleText;
        public Image IgnisHPFill => m_IgnisHPFill;
        public Text IgnisHPText => m_IgnisHPText;
        public Image IgnisEnergyFill => m_IgnisEnergyFill;
        public Text IgnisEnergyText => m_IgnisEnergyText;
        public Text FragmentCountText => m_FragmentCountText;
        public Button SettingsBtn => m_SettingsBtn;
    }
}
