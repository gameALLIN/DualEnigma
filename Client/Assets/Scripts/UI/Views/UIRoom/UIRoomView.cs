/// ============================================================
/// 文件名: UIRoomView.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 房间等待面板视图。房间码大字 + 等待状态 + 退出按钮。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UIRoomView : UIViewBase
    {
        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====
        [SerializeField] private Text m_RoomCodeText;
        [SerializeField] private Text m_StatusText;
        [SerializeField] private Text m_TipText;
        [SerializeField] private Button m_InviteBtn;
        [SerializeField] private Button m_LeaveBtn;
        // ===== Auto Bind End =====

        public Text RoomCodeText => m_RoomCodeText;
        public Text StatusText => m_StatusText;
        public Text TipText => m_TipText;
        public Button InviteBtn => m_InviteBtn;
        public Button LeaveBtn => m_LeaveBtn;
    }
}
