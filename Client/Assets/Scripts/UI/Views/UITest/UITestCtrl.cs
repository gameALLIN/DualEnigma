/// ============================================================
/// 文件名: UITestCtrl.cs
/// 创建时间: 2026-07-10 11:15:36
/// 作者: SLR
/// 描述: 一个面板测试
/// ============================================================

using UnityEngine;

namespace DualEnigma.UI
{
    public class UITestCtrl : UICtrlBase
    {
        private UITestView m_TestView;

        private int m_Count;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TestView = GetView<UITestView>();
        }

        protected override void OnShow()
        {
            m_TestView.SetTitle("UITest 测试面板");
            m_TestView.SetCount(m_Count);

            m_TestView.RegisterAddBtn(OnAddBtnClick);
            m_TestView.RegisterResetBtn(OnResetBtnClick);
            m_TestView.RegisterCloseBtn(OnCloseBtnClick);
        }

        protected override void OnHide()
        {
            m_TestView.UnregisterAllBtns();
        }

        private void OnAddBtnClick()
        {
            m_Count++;
            m_TestView.SetCount(m_Count);
        }

        private void OnResetBtnClick()
        {
            m_Count = 0;
            m_TestView.SetCount(m_Count);
        }

        private void OnCloseBtnClick()
        {
            UIManager.Instance.Pop();
        }
    }
}
