/// ============================================================
/// 文件名: UIInvitePopupView.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 全局邀请弹窗视图。根节点无 Graphic（不遮挡点击），
///       顶部卡片区由运行时克隆填充。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    /// <summary>
    /// 全局邀请弹窗视图。常驻 UILayer.Top，不属于面板栈。
    /// </summary>
    public class UIInvitePopupView : UIViewBase
    {
        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====
        [Header("卡片区")]
        [SerializeField] private Transform m_CardContainer;
        [SerializeField] private InviteCardView m_InviteCardTemplate;
        [SerializeField] private RequestCardView m_RequestCardTemplate;
        // ===== Auto Bind End =====

        public Transform CardContainer => m_CardContainer;
        public InviteCardView InviteCardTemplate => m_InviteCardTemplate;
        public RequestCardView RequestCardTemplate => m_RequestCardTemplate;
    }
}
