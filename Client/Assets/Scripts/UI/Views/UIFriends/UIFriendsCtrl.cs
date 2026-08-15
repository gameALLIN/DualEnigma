/// ============================================================
/// 文件名: UIFriendsCtrl.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 好友面板控制器。拉取/渲染好友、申请、邀请；
///       搜索添加好友；接受邀请 → 打开 UIRoom（携带 roomCode）。
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
            _refreshTimer = REFRESH_INTERVAL; // 立即触发首次刷新
            RefreshAll();
        }

        protected override void OnHide()
        {
            UnbindEvents();
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

            _api.GetFriendRequests(
                requests => { _model.SetRequests(requests); RenderRequests(); },
                _ => { });

            _api.GetInvites(
                invites => { _model.SetInvites(invites); RenderInvites(); },
                _ => { });
        }

        // ============================================================
        //  渲染
        // ============================================================

        private void ClearChildren(Transform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
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

                if (row.InviteBtn != null)
                    row.InviteBtn.onClick.AddListener(() => OnInviteFriendClicked(captured));

                if (row.DeleteBtn != null)
                    row.DeleteBtn.onClick.AddListener(() => OnDeleteFriendClicked(captured));
            }
        }

        private void RenderRequests()
        {
            if (_view.RequestListContent == null || _view.RequestSection == null) return;
            ClearChildren(_view.RequestListContent);
            _view.RequestSection.SetActive(_model.Requests.Count > 0);

            foreach (FriendRequestData request in _model.Requests)
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
        }

        private void RenderInvites()
        {
            if (_view.InviteListContent == null || _view.InviteSection == null) return;
            ClearChildren(_view.InviteListContent);
            _view.InviteSection.SetActive(_model.Invites.Count > 0);

            foreach (InviteData invite in _model.Invites)
            {
                InviteData captured = invite;
                InviteRowView row = Instantiate(_view.InviteRowTemplate, _view.InviteListContent);
                row.gameObject.SetActive(true);
                row.name = "InviteRow_" + captured.inviteId;

                if (row.FromText != null)
                    row.FromText.text = $"{captured.fromDisplayName} 邀请你进入房间 {captured.roomCode}";

                if (row.AcceptBtn != null)
                    row.AcceptBtn.onClick.AddListener(() => OnAcceptInviteClicked(captured));

                if (row.RejectBtn != null)
                    row.RejectBtn.onClick.AddListener(() => OnRejectInviteClicked(captured));
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
            _api?.AcceptFriendRequest(request.requestId, RefreshAll, error => _view.SetStatus(error));
        }

        private void OnRejectRequestClicked(FriendRequestData request)
        {
            _api?.RejectFriendRequest(request.requestId, RefreshAll, error => _view.SetStatus(error));
        }

        private void OnInviteFriendClicked(FriendData friend)
        {
            if (_api == null) return;
            string roomCode = NetworkSystem.Instance.CurrentRoomCode;

            if (string.IsNullOrEmpty(roomCode))
            {
                _view.SetStatus("尚未创建房间（需要先进入联机大厅，WebSocket 通道开发中）");
                return;
            }

            _view.SetStatus($"已邀请 {friend.displayName}，等待对方接受...");
            _api.CreateInvite(friend.accountId, roomCode,
                _ => _view.SetStatus($"已邀请 {friend.displayName}，等待对方接受..."),
                error => _view.SetStatus(error));
        }

        private void OnAcceptInviteClicked(InviteData invite)
        {
            _api?.AcceptInvite(invite.inviteId,
                roomCode =>
                {
                    // 接受邀请 → 关闭好友面板，进入房间等待界面（携带 roomCode）
                    UIManager.Instance.Pop();
                    UIRoomCtrl.Prepare(roomCode);
                    UIManager.Instance.Push<UIRoomCtrl>(UIMode.FullScreen);
                },
                error => _view.SetStatus(error));
        }

        private void OnRejectInviteClicked(InviteData invite)
        {
            _api?.DeclineInvite(invite.inviteId, RefreshAll, error => _view.SetStatus(error));
        }

        private void OnDeleteFriendClicked(FriendData friend)
        {
            _api?.RemoveFriend(friend.accountId, RefreshAll, error => _view.SetStatus(error));
        }
    }
}
