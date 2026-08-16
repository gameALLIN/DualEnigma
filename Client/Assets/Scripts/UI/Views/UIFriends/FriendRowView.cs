/// ============================================================
/// 文件名: FriendRowView.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 好友列表行视图（模板克隆），搜索结果行复用此模板。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace DualEnigma.UI
{
    public class FriendRowView : MonoBehaviour
    {
        [SerializeField] private Text m_NameText;
        [SerializeField] private Text m_IdText;
        [SerializeField] private Text m_StatusText;
        [SerializeField] private Button m_InviteBtn;
        [SerializeField] private Button m_DeleteBtn;

        public Text NameText => m_NameText;
        public Text IdText => m_IdText;
        public Text StatusText => m_StatusText;
        public Button InviteBtn => m_InviteBtn;
        public Button DeleteBtn => m_DeleteBtn;
    }
}
