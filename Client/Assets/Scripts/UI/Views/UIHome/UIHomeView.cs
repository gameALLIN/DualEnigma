/// ============================================================
/// 文件名: UIHomeView.cs
/// 创建时间: 2026-07-10 17:46:28
/// 作者: SLR
/// 描述: 主界面视图。标题区 + 玩家信息卡 + 开始/退出按钮 + 版本号。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UIHomeView : UIViewBase
    {
        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====
        [Header("玩家信息卡")]
        [SerializeField] private Text m_AvatarText;
        [SerializeField] private Text m_DisplayNameText;
        [SerializeField] private Text m_AccountIdText;

        [Header("按钮")]
        [SerializeField] private Button m_StartBtn;
        [SerializeField] private Button m_FriendsBtn;
        [SerializeField] private Button m_MailBtn;
        [SerializeField] private Button m_AchievementBtn;
        [SerializeField] private Button m_SettingsBtn;

        [Header("文本")]
        [SerializeField] private Text m_VersionText;

        [Header("邀请抽屉")]
        [SerializeField] private Button m_DrawerToggleBtn;
        [SerializeField] private GameObject m_DrawerPanel;
        [SerializeField] private Text m_RoomCodeText;
        [SerializeField] private Transform m_FriendListContent;
        [SerializeField] private Text m_StatusText;
        [SerializeField] private FriendItem m_FriendRowTemplate;
        // ===== Auto Bind End =====

        public Text AvatarText => m_AvatarText;
        public Text DisplayNameText => m_DisplayNameText;
        public Text AccountIdText => m_AccountIdText;
        public Button StartBtn => m_StartBtn;
        public Button FriendsBtn => m_FriendsBtn;
        public Button MailBtn => m_MailBtn;
        public Button AchievementBtn => m_AchievementBtn;
        public Button SettingsBtn => m_SettingsBtn;
        public Text VersionText => m_VersionText;
        public Button DrawerToggleBtn => m_DrawerToggleBtn;
        public GameObject DrawerPanel => m_DrawerPanel;
        public Text RoomCodeText => m_RoomCodeText;
        public Transform FriendListContent => m_FriendListContent;
        public Text StatusText => m_StatusText;
        public FriendItem FriendRowTemplate => m_FriendRowTemplate;

        /// <summary>显示抽屉状态提示（空串隐藏）</summary>
        public void SetDrawerStatus(string message)
        {
            if (m_StatusText == null) return;
            m_StatusText.text = message ?? "";
            m_StatusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        /// <summary>设置抽屉房间码文本</summary>
        public void SetRoomCode(string roomCode)
        {
            if (m_RoomCodeText != null)
                m_RoomCodeText.text = string.IsNullOrEmpty(roomCode) ? "房间码: ----" : $"房间码: {roomCode}";
        }

        /// <summary>填充玩家信息卡：头像取昵称首字，ID 显示账号编号</summary>
        public void SetPlayerInfo(string displayName, long accountId)
        {
            if (m_AvatarText != null)
                m_AvatarText.text = string.IsNullOrEmpty(displayName) ? "?" : displayName.Substring(0, 1);

            if (m_DisplayNameText != null)
                m_DisplayNameText.text = string.IsNullOrEmpty(displayName) ? "旅行者" : displayName;

            if (m_AccountIdText != null)
                m_AccountIdText.text = $"ID: {accountId}";
        }

        /// <summary>设置右下角版本号</summary>
        public void SetVersion(string version)
        {
            if (m_VersionText != null)
                m_VersionText.text = version;
        }
    }
}
