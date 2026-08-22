/// ============================================================
/// 文件名: UIRoomCtrl.cs
/// 创建时间: 2026-08-15
/// 最后更新: 2026-08-22
/// 作者: DualEnigma
/// 描述: [已停用] 房间等待面板控制器。联机开局流程改造后主界面（UIHome）
///       为唯一房间入口，本面板不再被 Push。开局/阶段逻辑已移除（防复用
///       时与 UIHomeCtrl 双订阅同事件导致 StartGame 双触发）；文件保留
///       仅为兼容旧场景引用，勿在新流程中使用。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.UI;
using DualEnigma.Framework.Core;
using DualEnigma.Core;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    public class UIRoomCtrl : UICtrlBase
    {
        /// <summary>Push 前暂存的房间码（UIManager.Push 无法携带参数）</summary>
        private static string s_PendingRoomCode = "";

        private static bool s_PendingIsHost;

        private UIRoomModel _model;
        private UIRoomView _view;

        /// <summary>打开面板前调用：UIRoomCtrl.Prepare(roomCode); UIManager.Push&lt;UIRoomCtrl&gt;();</summary>
        public static void Prepare(string roomCode, bool isHost = false)
        {
            s_PendingRoomCode = roomCode ?? "";
            s_PendingIsHost = isHost;
        }

        protected override void OnCreate()
        {
            _model = new UIRoomModel();
            _view = GetComponent<UIRoomView>();

            // [已停用] 不再订阅任何网络/阶段事件：开局逻辑由 UIHomeCtrl + GameplayDriver 承担。
            // 若误被 Push，仅作静态展示，不参与流程（防止 StartGame 双触发）。
        }

        protected override void OnDestroy()
        {
            // [已停用] 无订阅需要注销（OnCreate 不再订阅）
            base.OnDestroy();
        }

        protected override void OnShow()
        {
            if (_view == null) return;

            _model.RoomCode = s_PendingRoomCode;
            _model.IsHost = s_PendingIsHost;

            if (_view.LeaveBtn != null)
                _view.LeaveBtn.onClick.AddListener(OnLeaveClicked);

            if (_view.InviteBtn != null)
                _view.InviteBtn.onClick.AddListener(OnInviteClicked);

            if (_view.StartBtn != null)
                _view.StartBtn.onClick.AddListener(OnStartClicked);

            RefreshDisplay();
            ConnectToServer();
        }

        protected override void OnHide()
        {
            if (_view != null && _view.LeaveBtn != null)
                _view.LeaveBtn.onClick.RemoveListener(OnLeaveClicked);

            if (_view != null && _view.InviteBtn != null)
                _view.InviteBtn.onClick.RemoveListener(OnInviteClicked);

            if (_view != null && _view.StartBtn != null)
                _view.StartBtn.onClick.RemoveListener(OnStartClicked);
        }

        // ============================================================
        //  连接流程
        // ============================================================

        private void ConnectToServer()
        {
            if (RoomSession.HasInstance && RoomSession.Instance.IsConnected)
            {
                // 已在房间（如从全局邀请弹窗再次进入）→ 直接展示现有状态
                _model.RoomCode = RoomSession.Instance.CurrentRoomCode;
                RefreshDisplay();
                return;
            }

            SetStatus(_model.IsHost
                ? "正在创建房间..."
                : $"正在加入房间 {_model.RoomCode}...");

            // 空 roomCode = 创建房间（服务端自动分配房间码）
            GameConnection.Instance.ConnectToRoom(_model.RoomCode);
        }

        private void OnRoomConnected(RoomConnectedEvent e)
        {
            _model.RoomCode = e.roomCode;
            RefreshDisplay();
        }

        private void OnPlayerJoined(PlayerJoinedRoomEvent e)
        {
            _model.PlayerCount = Mathf.Max(1, e.playerCount);
            RefreshDisplay();
        }

        // ============================================================
        //  显示
        // ============================================================

        private void RefreshDisplay()
        {
            if (_view.RoomCodeText != null)
                _view.RoomCodeText.text = string.IsNullOrEmpty(_model.RoomCode)
                    ? "----"
                    : _model.RoomCode;

            bool full = _model.PlayerCount >= 2;

            if (_view.StatusText != null)
                _view.StatusText.text = _model.IsHost
                    ? (full ? "好友已就位，可以开始对局" : "等待好友加入...")
                    : "已加入房间，等待房主开始游戏";

            if (_view.TipText != null)
                _view.TipText.text = _model.IsHost
                    ? (full ? "点击【开始对局】进入游戏" : "点击【邀请好友】发送房间邀请，或把房间码告诉对方")
                    : "等待房主开始游戏";

            // 开始对局：仅房主可见，好友就位后才可点
            if (_view.StartBtn != null)
            {
                _view.StartBtn.gameObject.SetActive(_model.IsHost);
                _view.StartBtn.interactable = full;
            }
        }

        private void SetStatus(string message)
        {
            if (_view.StatusText != null)
                _view.StatusText.text = message;
        }

        /// <summary>邀请好友：好友管理面板叠加打开（受邀方视角；房主邀请走主界面抽屉），关闭后回到房间面板</summary>
        private void OnInviteClicked()
        {
            if (UIManager.Instance.GetTopPanel() is UIFriendsCtrl) return;
            UIManager.Instance.Push<UIFriendsCtrl>(UIMode.FullScreen);
        }

        /// <summary>房主开始对局：服务端校验通过后广播 GameStart，双方进入对局</summary>
        private void OnStartClicked()
        {
            if (_model.PlayerCount < 2)
            {
                SetStatus("好友尚未加入，无法开始");
                return;
            }
            SetStatus("正在开始对局...");
            GameConnection.Instance.RequestStartGame();
        }

        /// <summary>退出房间：断开连接并返回主界面（会话状态经统一出口清零）</summary>
        private void OnLeaveClicked()
        {
            GameConnection.Instance.Disconnect();
            UIManager.Instance.Pop();
        }
    }
}
