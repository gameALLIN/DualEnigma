/// ============================================================
/// 文件名: UIHomeCtrl.cs
/// 创建时间: 2026-07-10 17:46:28
/// 最后更新: 2026-08-21
/// 作者: SLR
/// 描述: 主界面控制器（主界面即大厅，唯一房间入口，UIRoom 已停用）。
///       开局按钮状态机：未连接=可点"开始游戏"（点击静默建房）；
///       建房中=灰"创建房间中..."；在房 1 人=灰"等待好友加入..."；
///       满员=房主(playerId=0)亮"开始对局"发 C2S_StartGame，
///       非房主保持灰"等待房主开始游戏"；
///       对手大厅离开(state=lobby)=回灰可再邀补位。
///       抽屉邀请未在房时静默建房，ConnectAck 后自动发出 REST 邀请。
///       GameStart 广播后隐藏全部面板进入对局。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;
using DualEnigma.Framework.Core;
using DualEnigma.Core;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    public class UIHomeCtrl : UICtrlBase
    {
        /// <summary>抽屉好友列表刷新间隔（秒）</summary>
        private const float FRIEND_REFRESH_INTERVAL = 5f;

        private UIHomeModel _model;
        private UIHomeView _view;
        private IFriendApiService _api;

        private readonly List<FriendData> _friends = new List<FriendData>();

        /// <summary>是否已在房间（ConnectAck 完成）</summary>
        private bool _roomReady;

        /// <summary>正在连接/建房（含静默建房）</summary>
        private bool _connecting;

        /// <summary>房间人数（含自己）</summary>
        private int _playerCount = 1;

        /// <summary>静默建房完成后待发送的邀请（未在房时点邀请）</summary>
        private FriendData _pendingInvite;

        private float _refreshTimer;
        private bool _refreshing;

        /// <summary>是否房主（playerId=0 创建房间者）</summary>
        private bool IsHost => NetworkSystem.HasInstance && NetworkSystem.Instance.LocalPlayerId == 0;

        protected override void OnCreate()
        {
            _model = new UIHomeModel();
            _view = GetComponent<UIHomeView>();
            _api = ServiceLocator.Get<IFriendApiService>();

            if (_api == null)
            {
                _ = FriendApiService.Instance;
                _api = ServiceLocator.Get<IFriendApiService>();
            }

            // 房间生命周期事件挂整个生命周期（对局结束返回主界面也不能错过）
            EventBus.Instance.Subscribe<RoomConnectedEvent>(OnRoomConnected);
            EventBus.Instance.Subscribe<PlayerJoinedRoomEvent>(OnPlayerJoined);
            EventBus.Instance.Subscribe<RoomGameStartEvent>(OnGameStart);
            EventBus.Instance.Subscribe<OpponentDisconnectEvent>(OnOpponentDisconnected);
            EventBus.Instance.Subscribe<ServerDisconnectedEvent>(OnServerDisconnected);
        }

        protected override void OnDestroy()
        {
            if (EventBus.HasInstance)
            {
                EventBus.Instance.Unsubscribe<RoomConnectedEvent>(OnRoomConnected);
                EventBus.Instance.Unsubscribe<PlayerJoinedRoomEvent>(OnPlayerJoined);
                EventBus.Instance.Unsubscribe<RoomGameStartEvent>(OnGameStart);
                EventBus.Instance.Unsubscribe<OpponentDisconnectEvent>(OnOpponentDisconnected);
                EventBus.Instance.Unsubscribe<ServerDisconnectedEvent>(OnServerDisconnected);
            }
            base.OnDestroy();
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

            // 已在房间（对局结束返回）→ 恢复大厅就绪态；否则回到初始态
            if (NetworkSystem.HasInstance && !string.IsNullOrEmpty(NetworkSystem.Instance.CurrentRoomCode))
            {
                _roomReady = true;
                _connecting = false;
                _view.SetRoomCode(NetworkSystem.Instance.CurrentRoomCode);
                UpdateStartButton();
            }
            else
            {
                ResetRoomUi();
            }
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

            if (_view.FriendsBtn != null)
                _view.FriendsBtn.onClick.AddListener(OnFriendsClicked);

            if (_view.MailBtn != null)
                _view.MailBtn.onClick.AddListener(OnMailClicked);

            if (_view.AchievementBtn != null)
                _view.AchievementBtn.onClick.AddListener(OnAchievementClicked);

            if (_view.SettingsBtn != null)
                _view.SettingsBtn.onClick.AddListener(OnSettingsClicked);

            if (_view.DrawerToggleBtn != null)
                _view.DrawerToggleBtn.onClick.AddListener(OnDrawerToggleClicked);
        }

        private void UnbindEvents()
        {
            if (_view == null) return;

            if (_view.StartBtn != null)
                _view.StartBtn.onClick.RemoveListener(OnStartGameClicked);

            if (_view.FriendsBtn != null)
                _view.FriendsBtn.onClick.RemoveListener(OnFriendsClicked);

            if (_view.MailBtn != null)
                _view.MailBtn.onClick.RemoveListener(OnMailClicked);

            if (_view.AchievementBtn != null)
                _view.AchievementBtn.onClick.RemoveListener(OnAchievementClicked);

            if (_view.SettingsBtn != null)
                _view.SettingsBtn.onClick.RemoveListener(OnSettingsClicked);

            if (_view.DrawerToggleBtn != null)
                _view.DrawerToggleBtn.onClick.RemoveListener(OnDrawerToggleClicked);
        }

        private void Update()
        {
            // 抽屉展开时定时轮询好友列表（在线四态刷新）
            if (_view == null || _view.DrawerPanel == null || !_view.DrawerPanel.activeSelf) return;

            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= FRIEND_REFRESH_INTERVAL)
            {
                _refreshTimer = 0f;
                RefreshFriends();
            }
        }

        // ============================================================
        //  房间流程（主界面即大厅，唯一房间入口）
        // ============================================================

        /// <summary>
        /// 开局按钮状态机（唯一收口，所有状态变化都经此刷新）：
        /// 未连接=亮"开始游戏"（点击静默建房）；建房中=灰"创建房间中..."；
        /// 在房 1 人=灰"等待好友加入..."；满员=房主亮"开始对局"/非房主灰"等待房主开始游戏"。
        /// </summary>
        private void UpdateStartButton()
        {
            if (_view == null || _view.StartBtn == null) return;

            if (!_roomReady)
            {
                _view.StartBtn.interactable = !_connecting;
                SetStartLabel(_connecting ? "创建房间中..." : "开始游戏");
                return;
            }

            if (_playerCount >= 2)
            {
                _view.StartBtn.interactable = IsHost;
                SetStartLabel(IsHost ? "开始对局" : "等待房主开始游戏");
            }
            else
            {
                _view.StartBtn.interactable = false;
                SetStartLabel(IsHost ? "等待好友加入..." : "等待房主开始游戏");
            }
        }

        /// <summary>点击开始：未在房→静默建房；房主满员→请求开局</summary>
        private void OnStartGameClicked()
        {
            if (!_roomReady)
            {
                if (_connecting) return;
                ConnectSilently(null);
                return;
            }

            // 防御：非房主按钮本应灰色，时序错乱也不允许发开局请求
            if (!IsHost)
            {
                UpdateStartButton();
                return;
            }

            if (_playerCount < 2)
            {
                _view.SetDrawerStatus("好友尚未加入，无法开始");
                return;
            }

            _view.SetDrawerStatus("正在开始对局...");
            GameServerClient.Instance.RequestStartGame();
        }

        /// <summary>静默连接 game-server（空房间码=自动建房）；完成后自动发出待发邀请</summary>
        private void ConnectSilently(FriendData pendingInvite)
        {
            _connecting = true;
            _pendingInvite = pendingInvite;
            UpdateStartButton();
            GameServerClient.Instance.Connect("");
        }

        private void OnRoomConnected(RoomConnectedEvent e)
        {
            _roomReady = true;
            _connecting = false;
            _playerCount = 1;

            _view.SetRoomCode(e.roomCode);
            _view.SetDrawerStatus(IsHost
                ? "房间已创建，从左侧列表邀请好友"
                : "已加入房间，等待房主开始游戏");
            UpdateStartButton();

            SetDrawerOpen(true); // 建房/进房成功自动展开邀请抽屉
            RefreshFriends();

            // 静默建房前挂起的邀请：拿到房间码后立即补发（流程 A.2）
            if (_pendingInvite != null)
            {
                FriendData friend = _pendingInvite;
                _pendingInvite = null;
                SendInvite(friend, e.roomCode);
            }
        }

        private void OnPlayerJoined(PlayerJoinedRoomEvent e)
        {
            _playerCount = Mathf.Max(1, e.playerCount);
            UpdateStartButton();
            if (_playerCount >= 2 && IsHost)
                _view.SetDrawerStatus("好友已就位，点击【开始对局】");
        }

        private void OnGameStart(RoomGameStartEvent e)
        {
            // 满员开局：隐藏栈内全部面板（保留栈结构，对局结束后恢复）并开始对局
            UIManager.Instance.SetPanelsVisible(false);
            if (GameManager.HasInstance)
                GameManager.Instance.StartGame();
        }

        private void OnOpponentDisconnected(OpponentDisconnectEvent e)
        {
            _playerCount = 1;

            if (e.state == "lobby")
            {
                // 大厅离开：回到等待好友，可再邀补位（验收 3）
                UpdateStartButton();
                _view.SetDrawerStatus("对方已离开房间");
            }
            else
            {
                // 对局中断线（waiting）：重连窗口属断线重连里程碑，这里只提示
                UpdateStartButton();
                _view.SetDrawerStatus("对方连接中断，等待重连...");
            }
        }

        private void OnServerDisconnected(ServerDisconnectedEvent e)
        {
            ResetRoomUi();
            _view.SetDrawerStatus(string.IsNullOrEmpty(e.reason) ? "与服务器断开连接" : e.reason);
        }

        private void ResetRoomUi()
        {
            _roomReady = false;
            _connecting = false;
            _pendingInvite = null;
            _playerCount = 1;
            _view.SetRoomCode("");
            _view.SetDrawerStatus("");
            UpdateStartButton();
        }

        private void SetStartLabel(string label)
        {
            if (_view.StartBtn == null) return;
            Text text = _view.StartBtn.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }

        // ============================================================
        //  邀请抽屉
        // ============================================================

        /// <summary>箭头开关：展开/收起抽屉，箭头方向随状态切换</summary>
        private void OnDrawerToggleClicked()
        {
            if (_view.DrawerPanel == null) return;
            SetDrawerOpen(!_view.DrawerPanel.activeSelf);
        }

        private void SetDrawerOpen(bool open)
        {
            if (_view.DrawerPanel == null) return;
            _view.DrawerPanel.SetActive(open);

            if (_view.DrawerToggleBtn != null)
            {
                Text arrow = _view.DrawerToggleBtn.GetComponentInChildren<Text>();
                if (arrow != null) arrow.text = open ? "◀" : "▶";
            }

            if (open)
            {
                _refreshTimer = FRIEND_REFRESH_INTERVAL; // 立即刷新一次
            }
        }

        private void RefreshFriends()
        {
            if (_api == null || _refreshing) return;
            _refreshing = true;

            _api.GetFriends(
                friends =>
                {
                    // 先复位刷新标记再渲染：渲染异常不再卡死后续轮询
                    _refreshing = false;
                    _friends.Clear();
                    if (friends != null) _friends.AddRange(friends);
                    RenderFriends();
                },
                error => { _view.SetDrawerStatus(error); _refreshing = false; });
        }

        private void ClearChildren(Transform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }

        private void RenderFriends()
        {
            if (_view.FriendListContent == null) return;
            ClearChildren(_view.FriendListContent);

            if (_friends.Count == 0)
            {
                _view.SetDrawerStatus("暂无好友：请先在【好友】中添加好友");
                return;
            }
            _view.SetDrawerStatus("");

            foreach (FriendData friend in _friends)
            {
                FriendData captured = friend;

                // 预制体模板克隆（嵌套 Common/FriendItem 实例），缺模板时退化为纯代码构建
                FriendItem row;
                if (_view.FriendRowTemplate != null)
                {
                    row = Instantiate(_view.FriendRowTemplate, _view.FriendListContent);
                    row.gameObject.SetActive(true);
                    row.name = "InviteRow_" + captured.accountId;
                    row.SetCompactLayout(296f);
                }
                else
                {
                    row = FriendItem.Create(_view.FriendListContent,
                        "InviteRow_" + captured.accountId, 296f);
                }

                row.SetMode(FriendItemMode.Invite);
                row.BindFriend(captured);

                // 主按钮=邀请（游戏中好友由 BindFriend 置灰）
                if (row.PrimaryBtn != null)
                    row.PrimaryBtn.onClick.AddListener(() => OnInviteFriendClicked(captured));
            }
        }

        /// <summary>邀请好友进房（流程 A）：未在房时先静默建房，ConnectAck 后自动发出</summary>
        private void OnInviteFriendClicked(FriendData friend)
        {
            if (_api == null) return;

            string roomCode = NetworkSystem.HasInstance ? NetworkSystem.Instance.CurrentRoomCode : "";
            if (_roomReady && !string.IsNullOrEmpty(roomCode))
            {
                SendInvite(friend, roomCode);
                return;
            }

            // 未在房：静默自动建房，拿到房间码后补发邀请（任务 A.1/A.2）
            if (_connecting || GameServerClient.Instance.IsConnected)
            {
                _view.SetDrawerStatus("房间正在建立，请稍候...");
                return;
            }
            _view.SetDrawerStatus("正在创建房间，完成后将自动邀请 " + friend.displayName);
            ConnectSilently(friend);
        }

        /// <summary>发出 REST 邀请（房间码取当前连接）</summary>
        private void SendInvite(FriendData friend, string roomCode)
        {
            if (_api == null || string.IsNullOrEmpty(roomCode))
            {
                _view.SetDrawerStatus("房间尚未就绪，请稍候再试");
                return;
            }

            _view.SetDrawerStatus($"已邀请 {friend.displayName}，等待对方接受...");
            _api.CreateInvite(friend.accountId, roomCode,
                _ => _view.SetDrawerStatus($"已邀请 {friend.displayName}，等待对方接受..."),
                error => _view.SetDrawerStatus(error));
        }

        // ============================================================
        //  功能入口
        // ============================================================

        /// <summary>打开好友面板（好友管理：搜索/申请）</summary>
        private void OnFriendsClicked()
        {
            UIManager.Instance.Push<UIFriendsCtrl>(UIMode.FullScreen);
        }

        /// <summary>邮箱入口（功能开发中，占位）</summary>
        private void OnMailClicked()
        {
            Debug.Log("[UIHome] 邮箱功能开发中");
        }

        /// <summary>成就入口（功能开发中，占位）</summary>
        private void OnAchievementClicked()
        {
            Debug.Log("[UIHome] 成就功能开发中");
        }

        /// <summary>打开设置弹窗（与局内 HUD 设置同一面板；退出登录入口在设置内）</summary>
        private void OnSettingsClicked()
        {
            UISettingsCtrl.Ensure();
            UISettingsCtrl.ShowPanel(allowLogout: true);
        }
    }
}
