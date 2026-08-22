/// ============================================================
/// 文件名: SocialNotifyService.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 社交通知服务。登录后全局轮询房间邀请与好友申请，
///       通过 EventBus 发布 SocialNotifyChangedEvent，
///       供全局邀请弹窗（任意界面可见）与好友面板订阅。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Framework.Core;

namespace DualEnigma.Network
{
    /// <summary>
    /// 社交通知服务（全局单例）。
    /// 与 UIFriendsCtrl 的好友列表轮询解耦：本服务只负责
    /// 邀请 + 好友申请的全局感知，登录态自动启停。
    /// </summary>
    public class SocialNotifyService : Singleton<SocialNotifyService>
    {
        /// <summary>轮询间隔（秒）</summary>
        private const float POLL_INTERVAL = 5f;

        private IFriendApiService _api;

        /// <summary>待处理邀请（最新一次轮询结果）</summary>
        public IReadOnlyList<InviteData> PendingInvites { get; private set; }
            = new List<InviteData>();

        /// <summary>待处理好友申请（最新一次轮询结果）</summary>
        public IReadOnlyList<FriendRequestData> PendingRequests { get; private set; }
            = new List<FriendRequestData>();

        private float _pollTimer;
        private bool _inviteBusy;
        private bool _requestBusy;
        private bool _wasLoggedIn;

        protected override void OnSingletonInitialized()
        {
            // FriendApiService 尚未创建时先触发其初始化（注册到 ServiceLocator）
            if (ServiceLocator.Get<IFriendApiService>() == null)
                _ = FriendApiService.Instance;
            _api = ServiceLocator.Get<IFriendApiService>();

            // 登录后立即开始第一轮轮询
            _pollTimer = POLL_INTERVAL;
            Debug.Log("[SocialNotifyService] 社交通知服务初始化完成");
        }

        /// <summary>立即触发一轮轮询（处理完申请/邀请后调用，加快列表与卡片刷新）</summary>
        public void ForcePoll()
        {
            _pollTimer = POLL_INTERVAL;
        }

        private void Update()
        {
            bool loggedIn = AuthService.HasInstance && AuthService.Instance.IsLoggedIn;

            // 登出 → 清空状态并广播空列表（弹窗卡片随之移除）
            if (_wasLoggedIn && !loggedIn)
            {
                PendingInvites = new List<InviteData>();
                PendingRequests = new List<FriendRequestData>();
                EventBus.Instance.Publish(new SocialNotifyChangedEvent());
                Debug.Log("[SocialNotifyService] 检测到登出，社交通知已清空");
            }
            _wasLoggedIn = loggedIn;

            if (!loggedIn) return;

            _pollTimer += Time.deltaTime;
            if (_pollTimer < POLL_INTERVAL) return;
            _pollTimer = 0f;

            PollInvites();
            PollRequests();
        }

        private void PollInvites()
        {
            if (_api == null || _inviteBusy) return;
            _inviteBusy = true;

            _api.GetInvites(
                invites =>
                {
                    _inviteBusy = false;
                    PendingInvites = invites ?? new List<InviteData>();
                    EventBus.Instance.Publish(new SocialNotifyChangedEvent
                    {
                        invites = invites,
                        requests = PendingRequests as List<FriendRequestData> ?? new List<FriendRequestData>(PendingRequests)
                    });
                },
                _ => { _inviteBusy = false; });
        }

        private void PollRequests()
        {
            if (_api == null || _requestBusy) return;
            _requestBusy = true;

            _api.GetFriendRequests(
                requests =>
                {
                    _requestBusy = false;
                    PendingRequests = requests ?? new List<FriendRequestData>();
                    EventBus.Instance.Publish(new SocialNotifyChangedEvent
                    {
                        invites = PendingInvites as List<InviteData> ?? new List<InviteData>(PendingInvites),
                        requests = requests
                    });
                },
                _ => { _requestBusy = false; });
        }
    }
}
