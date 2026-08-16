/// ============================================================
/// 文件名: UIFriendsCtrl.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 好友面板控制器。拉取/渲染好友、搜索添加好友；
///       好友申请列表读取 SocialNotifyService（全局轮询）；
///       房间邀请已移交全局弹窗 UIInvitePopup 处理。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;
using DualEnigma.Framework.Core;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    public class UIFriendsCtrl : UICtrlBase
    {
        /// <summary>数据刷新间隔（秒）</summary>
        private const float REFRESH_INTERVAL = 5f;

        private UIFriendsModel _model;
        private UIFriendsView _view;
        private IFriendApiService _api;

        private float _refreshTimer;
        private bool _refreshing;

        protected override void OnCreate()
        {
            _model = new UIFriendsModel();
            _view = GetComponent<UIFriendsView>();
            _api = ServiceLocator.Get<IFriendApiService>();

            if (_api == null)
            {
                _ = FriendApiService.Instance;
                _api = ServiceLocator.Get<IFriendApiService>();
            }
        }

        protected override void OnShow()
        {
            if (_view == null) return;

            BindEvents();
            EventBus.Instance.Subscribe<SocialNotifyChangedEvent>(OnSocialNotifyChanged);
            _refreshTimer = REFRESH_INTERVAL; // 立即触发首次刷新
            RefreshAll();
            RenderRequests();
            RefreshLayout(); // 初始收拢（无申请时好友列表顶置）
        }

        protected override void OnHide()
        {
            UnbindEvents();
            // 场景卸载时 EventBus 单例可能已先被销毁，避免 NRE
            if (EventBus.HasInstance)
                EventBus.Instance.Unsubscribe<SocialNotifyChangedEvent>(OnSocialNotifyChanged);
        }

        private void BindEvents()
        {
            if (_view.CloseBtn != null)
                _view.CloseBtn.onClick.AddListener(OnCloseClicked);

            if (_view.SearchBtn != null)
                _view.SearchBtn.onClick.AddListener(OnSearchClicked);
        }

        private void UnbindEvents()
        {
            if (_view.CloseBtn != null)
                _view.CloseBtn.onClick.RemoveListener(OnCloseClicked);

            if (_view.SearchBtn != null)
                _view.SearchBtn.onClick.RemoveListener(OnSearchClicked);
        }

        private void Update()
        {
            // 定时轮询好友/申请/邀请（OnShow 时也立即刷一次）
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= REFRESH_INTERVAL)
            {
                _refreshTimer = 0f;
                RefreshAll();
            }
        }

        // ============================================================
        //  数据刷新
        // ============================================================

        private void RefreshAll()
        {
            if (_api == null || _refreshing) return;
            _refreshing = true;

            _api.GetFriends(
                friends => { _model.SetFriends(friends); RenderFriends(); _refreshing = false; },
                error => { _view.SetStatus(error); _refreshing = false; });

            // 申请/邀请由 SocialNotifyService 全局轮询，本面板通过事件刷新
        }

        /// <summary>全局社交通知变化（申请被处理/新增）→ 刷新申请区</summary>
        private void OnSocialNotifyChanged(SocialNotifyChangedEvent e)
        {
            RenderRequests();
        }

        // ============================================================
        //  渲染
        // ============================================================

        private void ClearChildren(Transform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                // 隐藏的子物体是行模板（挂在 Content 下），跳过避免模板被销毁导致后续 Instantiate null
                if (!content.GetChild(i).gameObject.activeSelf)
                    continue;
                Destroy(content.GetChild(i).gameObject);
            }
        }

        private void RenderFriends()
        {
            if (_view.FriendListContent == null) return;
            ClearChildren(_view.FriendListContent);

            foreach (FriendData friend in _model.Friends)
            {
                FriendData captured = friend;
                FriendRowView row = Instantiate(_view.FriendRowTemplate, _view.FriendListContent);
                row.gameObject.SetActive(true);
                row.name = "FriendRow_" + captured.accountId;

                if (row.NameText != null)
                    row.NameText.text = $"{captured.displayName} ({captured.username})";
                if (row.IdText != null)
                    row.IdText.text = "ID: " + captured.accountId;

                ApplyStatus(row, captured.status);

                if (row.InviteBtn != null)
                {
                    // 游戏中无法接受邀请，置灰
                    row.InviteBtn.interactable = captured.status != "ingame";
                    row.InviteBtn.onClick.AddListener(() => OnInviteFriendClicked(captured));
                }

                if (row.DeleteBtn != null)
                    row.DeleteBtn.onClick.AddListener(() => OnDeleteFriendClicked(captured));
            }
        }

        private void RenderRequests()
        {
            if (_view.RequestListContent == null || _view.RequestSection == null) return;
            ClearChildren(_view.RequestListContent);

            // 申请数据来自全局轮询服务（单一数据源）
            IReadOnlyList<FriendRequestData> requests = SocialNotifyService.HasInstance
                ? SocialNotifyService.Instance.PendingRequests
                : null;
            _view.RequestSection.SetActive(requests != null && requests.Count > 0);
            if (requests == null) { RefreshLayout(); return; }

            foreach (FriendRequestData request in requests)
            {
                FriendRequestData captured = request;
                RequestRowView row = Instantiate(_view.RequestRowTemplate, _view.RequestListContent);
                row.gameObject.SetActive(true);
                row.name = "RequestRow_" + captured.requestId;

                if (row.FromText != null)
                    row.FromText.text = $"{captured.fromDisplayName} 请求加你为好友";

                if (row.AcceptBtn != null)
                    row.AcceptBtn.onClick.AddListener(() => OnAcceptRequestClicked(captured));

                if (row.RejectBtn != null)
                    row.RejectBtn.onClick.AddListener(() => OnRejectRequestClicked(captured));
            }

            RefreshLayout();
        }

        // ============================================================
        //  布局联动（申请区与好友列表固定分区，彻底消除遮挡）
        //  坐标语义：anchor top 的 anchoredPosition.y 为矩形中心到面板顶的距离（负值），
        //  T 值 = 距顶距离（0..640），矩形占位 [T_center - h/2, T_center + h/2]。
        // ============================================================

        /// <summary>申请区固定中心：T103（占 T68..138，紧跟标题栏）</summary>
        private const float REQUEST_CENTER_Y = -103f;

        /// <summary>列表标题中心：有申请 T160（占 T150..170）/ 无申请 T76（占 T66..86）</summary>
        private const float TITLE_WITH_REQUEST_Y = -160f;
        private const float TITLE_NO_REQUEST_Y = -76f;

        /// <summary>滚动区顶边 T：有申请 180 / 无申请 96</summary>
        private const float SCROLL_TOP_WITH_REQUEST = 180f;
        private const float SCROLL_TOP_NO_REQUEST = 96f;

        /// <summary>滚动区底边 T：固定 564（搜索区 T578 上方留 14px）</summary>
        private const float SCROLL_BOTTOM = 564f;

        /// <summary>
        /// 按申请区显隐联动好友列表：申请显示时列表整体下移并压缩高度，
        /// 隐藏时列表扩展占满（高度动态变化，顶/底分区永不相交）。
        /// </summary>
        private void RefreshLayout()
        {
            bool requestVisible = _view.RequestSection != null && _view.RequestSection.activeSelf;

            // 申请区：固定顶部（不依赖其他区块；邀请区已移交全局弹窗 UIInvitePopup，恒隐藏）
            if (_view.RequestSection != null)
            {
                RectTransform rt = _view.RequestSection.transform as RectTransform;
                if (rt != null)
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, REQUEST_CENTER_Y);
            }

            // 列表标题跟随
            if (_view.FriendSectionTitle != null)
                _view.FriendSectionTitle.anchoredPosition = new Vector2(
                    _view.FriendSectionTitle.anchoredPosition.x,
                    requestVisible ? TITLE_WITH_REQUEST_Y : TITLE_NO_REQUEST_Y);

            // 滚动区：顶边跟随、底边固定、高度动态
            if (_view.FriendScroll != null)
            {
                float top = requestVisible ? SCROLL_TOP_WITH_REQUEST : SCROLL_TOP_NO_REQUEST;
                _view.FriendScroll.sizeDelta = new Vector2(_view.FriendScroll.sizeDelta.x, SCROLL_BOTTOM - top);
                _view.FriendScroll.anchoredPosition = new Vector2(
                    _view.FriendScroll.anchoredPosition.x,
                    -(top + SCROLL_BOTTOM) / 2f);
            }
        }

        // ============================================================
        //  交互
        // ============================================================

        private void OnCloseClicked()
        {
            UIManager.Instance.Pop();
        }

        private void OnSearchClicked()
        {
            string keyword = _view.SearchInput != null ? _view.SearchInput.text.Trim() : "";
            if (string.IsNullOrEmpty(keyword) || _api == null) return;

            _view.SetStatus("搜索中...");
            _api.SearchUsers(keyword,
                results =>
                {
                    _model.SetSearchResults(results);
                    _view.SetStatus(results.Count == 0
                        ? "未找到用户"
                        : $"找到 {results.Count} 个用户，点击结果行添加");
                    RenderSearchResults();
                },
                error => _view.SetStatus(error));
        }

        /// <summary>渲染好友在线状态（四态，颜色区分）</summary>
        private void ApplyStatus(FriendRowView row, string status)
        {
            if (row.StatusText == null) return;

            switch (status)
            {
                case "online":
                    row.StatusText.text = "在线";
                    row.StatusText.color = new Color32(0x66, 0xBB, 0x6A, 0xFF);
                    break;
                case "teaming":
                    row.StatusText.text = "组队中";
                    row.StatusText.color = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
                    break;
                case "ingame":
                    row.StatusText.text = "游戏中";
                    row.StatusText.color = new Color32(0xFF, 0x6F, 0x00, 0xFF);
                    break;
                default:
                    row.StatusText.text = "离线";
                    row.StatusText.color = new Color32(0x78, 0x90, 0x9C, 0xFF);
                    break;
            }
        }

        /// <summary>搜索结果渲染为可点击的添加行（复用好友行模板：邀请/删除按钮语义映射为 添加/忽略）</summary>
        private void RenderSearchResults()
        {
            if (_view.FriendListContent == null) return;
            ClearChildren(_view.FriendListContent);

            foreach (FriendData user in _model.SearchResults)
            {
                FriendData captured = user;
                FriendRowView row = Instantiate(_view.FriendRowTemplate, _view.FriendListContent);
                row.gameObject.SetActive(true);
                row.name = "SearchRow_" + captured.accountId;

                if (row.NameText != null)
                    row.NameText.text = $"{captured.displayName} ({captured.username})";
                if (row.IdText != null)
                    row.IdText.text = "ID: " + captured.accountId;

                // 搜索结果无状态信息，隐藏状态列
                if (row.StatusText != null)
                    row.StatusText.gameObject.SetActive(false);

                if (row.InviteBtn != null)
                {
                    // 搜索模式下复用第一个按钮作为"添加好友"
                    Text btnText = row.InviteBtn.GetComponentInChildren<Text>();
                    if (btnText != null) btnText.text = "添加";
                    row.InviteBtn.onClick.AddListener(() => OnAddFriendClicked(captured));
                }

                if (row.DeleteBtn != null)
                    row.DeleteBtn.gameObject.SetActive(false);
            }
        }

        private void OnAddFriendClicked(FriendData user)
        {
            if (_api == null) return;
            _view.SetStatus($"已向 {user.displayName} 发送好友申请");
            _api.SendFriendRequest(user.username, _ => RefreshAll(), error => _view.SetStatus(error));
        }

        private void OnAcceptRequestClicked(FriendRequestData request)
        {
            _api?.AcceptFriendRequest(request.requestId,
                () => SocialNotifyService.Instance.ForcePoll(),
                error => _view.SetStatus(error));
        }

        private void OnRejectRequestClicked(FriendRequestData request)
        {
            _api?.RejectFriendRequest(request.requestId,
                () => SocialNotifyService.Instance.ForcePoll(),
                error => _view.SetStatus(error));
        }

        private void OnInviteFriendClicked(FriendData friend)
        {
            if (_api == null) return;
            string roomCode = NetworkSystem.Instance.CurrentRoomCode;

            if (string.IsNullOrEmpty(roomCode))
            {
                _view.SetStatus("尚未进入房间：请先回到主界面点击【联机开房】，看到房间码后再来邀请好友");
                return;
            }

            _view.SetStatus($"已邀请 {friend.displayName}，等待对方接受...");
            _api.CreateInvite(friend.accountId, roomCode,
                _ => _view.SetStatus($"已邀请 {friend.displayName}，等待对方接受..."),
                error => _view.SetStatus(error));
        }

        private void OnDeleteFriendClicked(FriendData friend)
        {
            _api?.RemoveFriend(friend.accountId, RefreshAll, error => _view.SetStatus(error));
        }
    }
}
