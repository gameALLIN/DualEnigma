/// ============================================================
/// 文件名: UIRoomCtrl.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 房间等待面板控制器。展示房间码与等待状态；
///       WebSocket 接通后由 ConnectAck/GameStart 事件驱动状态更新。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.UI;
using DualEnigma.Core;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    public class UIRoomCtrl : UICtrlBase
    {
        /// <summary>Push 前暂存的房间码（UIManager.Push 无法携带参数）</summary>
        private static string s_PendingRoomCode = "";

        private UIRoomModel _model;
        private UIRoomView _view;

        /// <summary>打开面板前调用：UIRoomCtrl.Prepare(roomCode); UIManager.Push&lt;UIRoomCtrl&gt;();</summary>
        public static void Prepare(string roomCode, bool isHost = false)
        {
            s_PendingRoomCode = roomCode ?? "";
            s_PendingIsHost = isHost;
        }

        private static bool s_PendingIsHost;

        protected override void OnCreate()
        {
            _model = new UIRoomModel();
            _view = GetComponent<UIRoomView>();
        }

        protected override void OnShow()
        {
            if (_view == null) return;

            _model.RoomCode = s_PendingRoomCode;
            _model.IsHost = s_PendingIsHost;

            if (_view.LeaveBtn != null)
                _view.LeaveBtn.onClick.AddListener(OnLeaveClicked);

            RefreshDisplay();

            // TODO(WebSocket): 连接 game-server（roomCode 非空则加入指定房间，否则创建），
            // 收到 S2C_ConnectAck 后调用 NetworkSystem.Instance.SetRoomCode(code) 并刷新显示；
            // 两人满员收到 S2C_GameStart 后关闭全部 UI 面板并开始对局。
        }

        protected override void OnHide()
        {
            if (_view != null && _view.LeaveBtn != null)
                _view.LeaveBtn.onClick.RemoveListener(OnLeaveClicked);
        }

        private void RefreshDisplay()
        {
            if (_view.RoomCodeText != null)
                _view.RoomCodeText.text = string.IsNullOrEmpty(_model.RoomCode)
                    ? "----"
                    : _model.RoomCode;

            if (_view.StatusText != null)
                _view.StatusText.text = _model.PlayerCount >= 2
                    ? "好友已就位，即将开始"
                    : (_model.IsHost ? "等待好友加入..." : "已加入房间");

            if (_view.TipText != null)
                _view.TipText.text = _model.IsHost
                    ? "把房间码告诉好友，或打开好友列表直接邀请"
                    : "等待房主开始游戏";
        }

        /// <summary>退出房间：断开连接（WS 接通后补充）并返回主界面</summary>
        private void OnLeaveClicked()
        {
            // TODO(WebSocket): NetworkSystem.Instance.Disconnect();
            NetworkSystem.Instance.SetRoomCode("");
            UIManager.Instance.Pop();
        }
    }
}
