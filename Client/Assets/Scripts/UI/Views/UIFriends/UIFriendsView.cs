/// ============================================================
/// 文件名: UIFriendsView.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 好友面板视图。邀请区/申请区/好友列表/搜索区 + 关闭按钮。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    /// <summary>好友列表行（模板克隆）</summary>
    public class FriendRowView : MonoBehaviour
    {
        [SerializeField] private Text m_NameText;
        [SerializeField] private Text m_IdText;
        [SerializeField] private Button m_InviteBtn;
        [SerializeField] private Button m_DeleteBtn;

        public Text NameText => m_NameText;
        public Text IdText => m_IdText;
        public Button InviteBtn => m_InviteBtn;
        public Button DeleteBtn => m_DeleteBtn;
    }

    /// <summary>好友申请行（模板克隆）</summary>
    public class RequestRowView : MonoBehaviour
    {
        [SerializeField] private Text m_FromText;
        [SerializeField] private Button m_AcceptBtn;
        [SerializeField] private Button m_RejectBtn;

        public Text FromText => m_FromText;
        public Button AcceptBtn => m_AcceptBtn;
        public Button RejectBtn => m_RejectBtn;
    }

    /// <summary>房间邀请行（模板克隆）</summary>
    public class InviteRowView : MonoBehaviour
    {
        [SerializeField] private Text m_FromText;
        [SerializeField] private Button m_AcceptBtn;
        [SerializeField] private Button m_RejectBtn;

        public Text FromText => m_FromText;
        public Button AcceptBtn => m_AcceptBtn;
        public Button RejectBtn => m_RejectBtn;
    }

    /// <summary>
    /// 好友面板视图。
    /// </summary>
    public class UIFriendsView : UIViewBase
    {
        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====
        [Header("容器")]
        [SerializeField] private GameObject m_InviteSection;
        [SerializeField] private GameObject m_RequestSection;
        [SerializeField] private Transform m_InviteListContent;
        [SerializeField] private Transform m_RequestListContent;
        [SerializeField] private Transform m_FriendListContent;

        [Header("行模板（默认隐藏，运行时克隆）")]
        [SerializeField] private FriendRowView m_FriendRowTemplate;
        [SerializeField] private RequestRowView m_RequestRowTemplate;
        [SerializeField] private InviteRowView m_InviteRowTemplate;

        [Header("搜索区")]
        [SerializeField] private InputField m_SearchInput;
        [SerializeField] private Button m_SearchBtn;

        [Header("其他")]
        [SerializeField] private Text m_StatusText;
        [SerializeField] private Button m_CloseBtn;
        // ===== Auto Bind End =====

        public GameObject InviteSection => m_InviteSection;
        public GameObject RequestSection => m_RequestSection;
        public Transform InviteListContent => m_InviteListContent;
        public Transform RequestListContent => m_RequestListContent;
        public Transform FriendListContent => m_FriendListContent;
        public FriendRowView FriendRowTemplate => m_FriendRowTemplate;
        public RequestRowView RequestRowTemplate => m_RequestRowTemplate;
        public InviteRowView InviteRowTemplate => m_InviteRowTemplate;
        public InputField SearchInput => m_SearchInput;
        public Button SearchBtn => m_SearchBtn;
        public Text StatusText => m_StatusText;
        public Button CloseBtn => m_CloseBtn;

        /// <summary>显示状态提示（搜索结果/操作反馈），空串隐藏</summary>
        public void SetStatus(string message)
        {
            if (m_StatusText != null)
            {
                m_StatusText.text = message ?? "";
                m_StatusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }
    }
}
