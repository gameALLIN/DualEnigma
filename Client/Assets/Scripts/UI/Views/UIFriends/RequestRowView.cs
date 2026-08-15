/// ============================================================
/// 文件名: RequestRowView.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 好友申请行视图（模板克隆）。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace DualEnigma.UI
{
    public class RequestRowView : MonoBehaviour
    {
        [SerializeField] private Text m_FromText;
        [SerializeField] private Button m_AcceptBtn;
        [SerializeField] private Button m_RejectBtn;

        public Text FromText => m_FromText;
        public Button AcceptBtn => m_AcceptBtn;
        public Button RejectBtn => m_RejectBtn;
    }
}
