/// ============================================================
/// 文件名: UITestView.cs
/// 创建时间: 2026-07-10 11:15:36
/// 作者: SLR
/// 描述: 一个面板测试
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UITestView : UIViewBase
    {
        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====
        [SerializeField] private Image  m_Background;
        [SerializeField] private Text   m_TitleText;
        [SerializeField] private Text   m_CountText;
        [SerializeField] private Button m_BtnAdd;
        [SerializeField] private Button m_BtnReset;
        [SerializeField] private Button m_BtnClose;
        // ===== Auto Bind End =====

        public void SetTitle(string title)
        {
            if (m_TitleText != null)
                m_TitleText.text = title;
        }

        public void SetCount(int count)
        {
            if (m_CountText != null)
                m_CountText.text = $"计数: {count}";
        }

        public void RegisterAddBtn(UnityEngine.Events.UnityAction action)
        {
            if (m_BtnAdd != null)
                m_BtnAdd.onClick.AddListener(action);
        }

        public void RegisterResetBtn(UnityEngine.Events.UnityAction action)
        {
            if (m_BtnReset != null)
                m_BtnReset.onClick.AddListener(action);
        }

        public void RegisterCloseBtn(UnityEngine.Events.UnityAction action)
        {
            if (m_BtnClose != null)
                m_BtnClose.onClick.AddListener(action);
        }

        public void UnregisterAllBtns()
        {
            if (m_BtnAdd != null)
                m_BtnAdd.onClick.RemoveAllListeners();
            if (m_BtnReset != null)
                m_BtnReset.onClick.RemoveAllListeners();
            if (m_BtnClose != null)
                m_BtnClose.onClick.RemoveAllListeners();
        }
    }
}
