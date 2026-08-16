/// ============================================================
/// 文件名: UIHomeCtrl.cs
/// 创建时间: 2026-07-10 17:46:28
/// 作者: SLR
/// 描述: 主界面控制器。展示登录账号信息，提供开始游戏/退出登录入口。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.UI;
using DualEnigma.Core;
using DualEnigma.Network;

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
            if (_view == null) return;

            BindEvents();

            // 登录进入主界面后：启动全局社交通知轮询，并确保全局邀请弹窗常驻（均幂等）
            _ = SocialNotifyService.Instance;
            UIInvitePopupCtrl.Ensure();

            // 填充账号信息（从 AuthService 读取登录结果）
            if (AuthService.HasInstance)
            {
                _view.SetPlayerInfo(AuthService.Instance.DisplayName, AuthService.Instance.AccountId);
                if (AuthService.Instance.IsLoggedIn)
                    Debug.Log($"[UIHome] 进入主界面 — 欢迎 {AuthService.Instance.DisplayName}");
            }

            _view.SetVersion($"v{Application.version} · 本地开发版");
        }

        protected override void OnHide()
        {
            UnbindEvents();
        }

        private void BindEvents()
        {
            if (_view == null) return;

            if (_view.StartBtn != null)
                _view.StartBtn.onClick.AddListener(OnStartGameClicked);

            if (_view.RoomBtn != null)
                _view.RoomBtn.onClick.AddListener(OnRoomClicked);

            if (_view.FriendsBtn != null)
                _view.FriendsBtn.onClick.AddListener(OnFriendsClicked);

            if (_view.LogoutBtn != null)
                _view.LogoutBtn.onClick.AddListener(OnLogoutClicked);
        }

        private void UnbindEvents()
        {
            if (_view == null) return;

            if (_view.StartBtn != null)
                _view.StartBtn.onClick.RemoveListener(OnStartGameClicked);

            if (_view.RoomBtn != null)
                _view.RoomBtn.onClick.RemoveListener(OnRoomClicked);

            if (_view.FriendsBtn != null)
                _view.FriendsBtn.onClick.RemoveListener(OnFriendsClicked);

            if (_view.LogoutBtn != null)
                _view.LogoutBtn.onClick.RemoveListener(OnLogoutClicked);
        }

        /// <summary>开始游戏：隐藏栈内全部面板（保留栈结构，对局结束后恢复）并启动本地单局</summary>
        private void OnStartGameClicked()
        {
            UIManager.Instance.SetPanelsVisible(false);
            GameManager.Instance.StartGame();
        }

        /// <summary>联机开房：连接 game-server 创建房间（房主身份），进入房间等待面板</summary>
        private void OnRoomClicked()
        {
            UIRoomCtrl.Prepare("", true);
            UIManager.Instance.Push<UIRoomCtrl>(UIMode.FullScreen);
        }

        /// <summary>打开好友面板（好友列表/申请/房间邀请）</summary>
        private void OnFriendsClicked()
        {
            UIManager.Instance.Push<UIFriendsCtrl>(UIMode.FullScreen);
        }

        /// <summary>退出登录：清除令牌后返回登录面板</summary>
        private void OnLogoutClicked()
        {
            if (AuthService.HasInstance)
                AuthService.Instance.Logout();

            UIManager.Instance.Pop();   // 关闭 UIHome，恢复显示 UILogin
        }
    }
}
