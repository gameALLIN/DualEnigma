/// ============================================================
/// 文件名: UIHomeCtrl.cs
/// 创建时间: 2026-07-10 17:46:28
/// 作者: SLR
/// 描述: 是的是的
/// ============================================================

using UnityEngine;

namespace DualEnigma.UI
{
    public class UIHomeCtrl : UICtrlBase
    {
        private UIHomeModel _model;
        private UIHomeView _view;

        protected override void OnCreate()
        {
            _model = new UIHomeModel();
            _view = GetComponent<UIHomeView>();
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }
    }
}
