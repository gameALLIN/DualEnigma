/// ============================================================
/// 文件名: RequestCardView.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 全局邀请弹窗的好友申请卡片视图（模板克隆）。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace DualEnigma.UI
{
    public class RequestCardView : MonoBehaviour
    {
        [SerializeField] private Text m_FromText;
        [SerializeField] private Button m_ViewBtn;

        public Text FromText => m_FromText;
        public Button ViewBtn => m_ViewBtn;
    }
}
