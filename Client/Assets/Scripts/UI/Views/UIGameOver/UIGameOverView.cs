/// ============================================================
/// 文件名: UIGameOverView.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: 对局结算面板视图。胜负标题 + 关卡进度 + 再来一局 + 返回主界面。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UIGameOverView : UIViewBase
    {
        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====
        [SerializeField] private Text m_TitleText;
        [SerializeField] private Text m_SubtitleText;
        [SerializeField] private Button m_RestartBtn;
        [SerializeField] private Button m_HomeBtn;
        // ===== Auto Bind End =====

        public Text TitleText => m_TitleText;
        public Text SubtitleText => m_SubtitleText;
        public Button RestartBtn => m_RestartBtn;
        public Button HomeBtn => m_HomeBtn;
    }
}
