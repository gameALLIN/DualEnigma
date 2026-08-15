/// ============================================================
/// 文件名: InviteCardView.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 全局邀请弹窗的邀请卡片视图（模板克隆）。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace DualEnigma.UI
{
    public class InviteCardView : MonoBehaviour
    {
        [SerializeField] private Text m_FromText;
        [SerializeField] private Text m_RoomText;
        [SerializeField] private Button m_AcceptBtn;
        [SerializeField] private Button m_RejectBtn;

        public Text FromText => m_FromText;
        public Text RoomText => m_RoomText;
        public Button AcceptBtn => m_AcceptBtn;
        public Button RejectBtn => m_RejectBtn;
    }
}
