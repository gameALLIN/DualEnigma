/// ============================================================
/// 文件名: UIInvitePopupCtrl.cs
/// 创建时间: 2026-08-15
/// 最后更新: 2026-08-21
/// 作者: DualEnigma
/// 描述: 全局邀请弹窗控制器。常驻 UILayer.Top（不进面板栈，
///       任何界面下均可见）。订阅 SocialNotifyChangedEvent，
///       以列表为准全量对账卡片：邀请卡（接受/拒绝）、
///       好友申请卡（跳转好友面板处理）。
///       接受邀请 = 直连进房（UIRoom 已停用，停留主界面）。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Framework.UI;
using DualEnigma.Framework.Core;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    public class UIInvitePopupCtrl : UICtrlBase
    {
        /// <summary>预制体路径（相对于 AssetPackage/，与 UIManager 约定一致）</summary>
        private const string PREFAB_PATH = "Prefabs/UI/UIInvitePopup/UIInvitePopup";

        private static UIInvitePopupCtrl s_Instance;

        private UIInvitePopupView _view;
        private IFriendApiService _api;

        /// <summary>已创建的邀请卡片（inviteId → 卡片）</summary>
        private readonly Dictionary<long, InviteCardView> _inviteCards = new Dictionary<long, InviteCardView>();

        /// <summary>已创建的申请卡片（requestId → 卡片）</summary>
        private readonly Dictionary<long, RequestCardView> _requestCards = new Dictionary<long, RequestCardView>();

        /// <summary>确保全局弹窗存在（登录后调用一次，幂等）</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;

            GameObject prefab = ResMgr.Instance.LoadPrefab(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[UIInvitePopup] 预制体加载失败: {PREFAB_PATH}（请先运行菜单 DualEnigma/UI/生成 UIInvitePopup 预制体）");
                return;
            }

            RectTransform layerRoot = UIManager.Instance.GetLayerRoot(UILayer.Top);
            if (layerRoot == null)
            {
                Debug.LogError("[UIInvitePopup] 未获取到 Top 层级根节点");
                return;
            }

            GameObject popupObj = Instantiate(prefab, layerRoot, false);
            popupObj.name = "UIInvitePopup";
            popupObj.transform.SetAsLastSibling();

            s_Instance = popupObj.GetComponent<UIInvitePopupCtrl>();
            if (s_Instance == null)
            {
                Debug.LogError("[UIInvitePopup] 预制体上未找到 UIInvitePopupCtrl 组件");
                Destroy(popupObj);
                return;
            }

            ((IUIPanel)s_Instance).OnCreate();
        }

        protected override void OnCreate()
        {
            _view = GetComponent<UIInvitePopupView>();
            _api = ServiceLocator.Get<IFriendApiService>();

            if (_api == null)
            {
                _ = FriendApiService.Instance;
                _api = ServiceLocator.Get<IFriendApiService>();
            }

            // 常驻面板：订阅挂在 OnCreate，销毁时注销
            EventBus.Instance.Subscribe<SocialNotifyChangedEvent>(OnSocialNotifyChanged);
        }

        protected override void OnDestroy()
        {
            // 场景卸载时 EventBus 单例可能已先被销毁，避免 NRE
            if (EventBus.HasInstance)
                EventBus.Instance.Unsubscribe<SocialNotifyChangedEvent>(OnSocialNotifyChanged);
            if (s_Instance == this) s_Instance = null;
            base.OnDestroy();
        }

        // ============================================================
        //  卡片对账（以服务端最新列表为准）
        // ============================================================

        private void OnSocialNotifyChanged(SocialNotifyChangedEvent e)
        {
            if (_view == null) return;
            ReconcileInvites(e.invites);
            ReconcileRequests(e.requests);
        }

        private void ReconcileInvites(List<InviteData> invites)
        {
            var keep = new HashSet<long>();
            if (invites != null)
            {
                foreach (InviteData invite in invites)
                {
                    keep.Add(invite.inviteId);
                    if (_inviteCards.ContainsKey(invite.inviteId)) continue;
                    CreateInviteCard(invite);
                }
            }

            RemoveStale(_inviteCards, keep);
        }

        private void ReconcileRequests(List<FriendRequestData> requests)
        {
            var keep = new HashSet<long>();
            if (requests != null)
            {
                foreach (FriendRequestData request in requests)
                {
                    keep.Add(request.requestId);
                    if (_requestCards.ContainsKey(request.requestId)) continue;
                    CreateRequestCard(request);
                }
            }

            RemoveStale(_requestCards, keep);
        }

        private void CreateInviteCard(InviteData invite)
        {
            if (_view.CardContainer == null || _view.InviteCardTemplate == null) return;

            InviteData captured = invite;
            InviteCardView card = Instantiate(_view.InviteCardTemplate, _view.CardContainer);
            card.gameObject.SetActive(true);
            card.name = "InviteCard_" + captured.inviteId;

            if (card.FromText != null)
                card.FromText.text = $"{captured.fromDisplayName} 邀请你进入房间";
            if (card.RoomText != null)
                card.RoomText.text = captured.roomCode;

            if (card.AcceptBtn != null)
                card.AcceptBtn.onClick.AddListener(() => OnAcceptClicked(captured));
            if (card.RejectBtn != null)
                card.RejectBtn.onClick.AddListener(() => OnRejectClicked(captured));

            _inviteCards[captured.inviteId] = card;
        }

        private void CreateRequestCard(FriendRequestData request)
        {
            if (_view.CardContainer == null || _view.RequestCardTemplate == null) return;

            FriendRequestData captured = request;
            RequestCardView card = Instantiate(_view.RequestCardTemplate, _view.CardContainer);
            card.gameObject.SetActive(true);
            card.name = "RequestCard_" + captured.requestId;

            if (card.FromText != null)
                card.FromText.text = $"{captured.fromDisplayName} 请求加你为好友";
            if (card.ViewBtn != null)
                card.ViewBtn.onClick.AddListener(OnViewRequestsClicked);

            _requestCards[captured.requestId] = card;
        }

        private static void RemoveStale<T>(Dictionary<long, T> cards, HashSet<long> keep) where T : Component
        {
            List<long> stale = null;
            foreach (KeyValuePair<long, T> kv in cards)
            {
                if (keep.Contains(kv.Key)) continue;
                stale ??= new List<long>();
                stale.Add(kv.Key);
            }

            if (stale == null) return;
            foreach (long id in stale)
            {
                if (cards.TryGetValue(id, out T card) && card != null)
                    Destroy(card.gameObject);
                cards.Remove(id);
            }
        }

        // ============================================================
        //  交互
        // ============================================================

        private void OnAcceptClicked(InviteData invite)
        {
            if (_api == null) return;
            _api.AcceptInvite(invite.inviteId,
                roomCode =>
                {
                    // 流程 B：直连进房（playerId=1），停留主界面等待房主开始（UIRoom 已停用）
                    GameServerClient.Instance.Connect(roomCode);
                },
                error => Debug.LogError($"[UIInvitePopup] 接受邀请失败: {error}"));
            // 卡片移除交给下一次轮询对账（服务端列表中该邀请已消失）
        }

        private void OnRejectClicked(InviteData invite)
        {
            _api?.DeclineInvite(invite.inviteId, null, error => Debug.LogError($"[UIInvitePopup] 拒绝邀请失败: {error}"));
        }

        private void OnViewRequestsClicked()
        {
            // 好友面板已在栈顶时不重复打开
            if (UIManager.Instance.GetTopPanel() is UIFriendsCtrl) return;
            UIManager.Instance.Push<UIFriendsCtrl>(UIMode.FullScreen);
        }
    }
}
