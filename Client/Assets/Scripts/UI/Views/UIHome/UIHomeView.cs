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
        [SerializeField] private Button m_LogoutBtn;

        [Header("文本")]
        [SerializeField] private Text m_VersionText;
        // ===== Auto Bind End =====

        public Text AvatarText => m_AvatarText;
        public Text DisplayNameText => m_DisplayNameText;
        public Text AccountIdText => m_AccountIdText;
        public Button StartBtn => m_StartBtn;
        public Button FriendsBtn => m_FriendsBtn;
        public Button LogoutBtn => m_LogoutBtn;
        public Text VersionText => m_VersionText;

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
