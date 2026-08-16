/// ============================================================
/// 文件名: UIRoomCtrl.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 房间等待面板控制器。打开时连接 game-server
///       （空 roomCode = 创建房间，非空 = 加入好友房间），
///       ConnectAck 后显示真实房间码，此时好友面板可发起邀请；
///       满员收到 GameStart 关闭全部 UI 并开始对局。
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

            // 网络事件挂在整个生命周期：好友面板叠在上方时（OnHide）也不能错过开局消息
            EventBus.Instance.Subscribe<RoomConnectedEvent>(OnRoomConnected);
            EventBus.Instance.Subscribe<RoomGameStartEvent>(OnGameStart);
            EventBus.Instance.Subscribe<PlayerJoinedRoomEvent>(OnPlayerJoined);
            EventBus.Instance.Subscribe<OpponentDisconnectEvent>(OnOpponentDisconnected);
            EventBus.Instance.Subscribe<ServerDisconnectedEvent>(OnServerDisconnected);
        }

        protected override void OnDestroy()
        {
            // 场景卸载时 EventBus 单例可能已先被销毁
            if (EventBus.HasInstance)
            {
                EventBus.Instance.Unsubscribe<RoomConnectedEvent>(OnRoomConnected);
                EventBus.Instance.Unsubscribe<RoomGameStartEvent>(OnGameStart);
                EventBus.Instance.Unsubscribe<PlayerJoinedRoomEvent>(OnPlayerJoined);
                EventBus.Instance.Unsubscribe<OpponentDisconnectEvent>(OnOpponentDisconnected);
                EventBus.Instance.Unsubscribe<ServerDisconnectedEvent>(OnServerDisconnected);
            }
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
            GameServerClient client = GameServerClient.Instance;
            if (client.IsConnected)
            {
                // 已在房间（如从全局邀请弹窗再次进入）→ 直接展示现有状态
                _model.RoomCode = NetworkSystem.Instance.CurrentRoomCode;
                RefreshDisplay();
                return;
            }

            SetStatus(_model.IsHost
                ? "正在创建房间..."
                : $"正在加入房间 {_model.RoomCode}...");

            // 空 roomCode = 创建房间（服务端自动分配房间码）
            client.Connect(_model.RoomCode);
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

        private void OnGameStart(RoomGameStartEvent e)
        {
            // 两人满员 → 隐藏栈内全部面板（保留栈结构，对局结束后恢复）并开始对局
            UIManager.Instance.SetPanelsVisible(false);
            GameManager.Instance.StartGame();
        }

        private void OnOpponentDisconnected(OpponentDisconnectEvent e)
        {
            SetStatus("对方连接中断，等待重连...");
        }

        private void OnServerDisconnected(ServerDisconnectedEvent e)
        {
            SetStatus(string.IsNullOrEmpty(e.reason) ? "与服务器断开连接" : e.reason);
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

        /// <summary>邀请好友：好友面板叠加打开（连接与房间保持不变），关闭后回到房间面板</summary>
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
            GameServerClient.Instance.RequestStartGame();
        }

        /// <summary>退出房间：断开连接并返回主界面</summary>
        private void OnLeaveClicked()
        {
            GameServerClient.Instance.Disconnect();
            NetworkSystem.Instance.SetRoomCode("");
            UIManager.Instance.Pop();
        }
    }
}
