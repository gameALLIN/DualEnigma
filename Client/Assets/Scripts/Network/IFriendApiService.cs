/// ============================================================
/// 文件名: IFriendApiService.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 好友与房间邀请 API 接口，供 UI 层调用。
/// ============================================================

using System;
using System.Collections.Generic;

namespace DualEnigma.Network
{
    /// <summary>好友信息</summary>
    [Serializable]
    public class FriendData
    {
        public long accountId;
        public string username;
        public string displayName;
        public bool online;
    }

    /// <summary>好友申请信息</summary>
    [Serializable]
    public class FriendRequestData
    {
        public long requestId;
        public long fromAccountId;
        public string fromUsername;
        public string fromDisplayName;
        public string createdAt;
    }

    /// <summary>房间邀请信息</summary>
    [Serializable]
    public class InviteData
    {
        public long inviteId;
        public long fromAccountId;
        public string fromDisplayName;
        public string roomCode;
        public string createdAt;
    }

    /// <summary>JsonUtility 用的数组包装（服务端返回裸数组）</summary>
    [Serializable]
    public class FriendListWrapper { public List<FriendData> items; }

    [Serializable]
    public class FriendRequestListWrapper { public List<FriendRequestData> items; }

    [Serializable]
    public class InviteListWrapper { public List<InviteData> items; }

    /// <summary>
    /// 好友与房间邀请 API。
    /// 对应 account-server FriendController 的 11 个端点。
    /// </summary>
    public interface IFriendApiService
    {
        /// <summary>发送好友申请</summary>
        void SendFriendRequest(string username, Action<FriendRequestData> onSuccess, Action<string> onError);

        /// <summary>我收到的好友申请列表</summary>
        void GetFriendRequests(Action<List<FriendRequestData>> onSuccess, Action<string> onError);

        /// <summary>接受好友申请</summary>
        void AcceptFriendRequest(long requestId, Action onSuccess, Action<string> onError);

        /// <summary>拒绝好友申请</summary>
        void RejectFriendRequest(long requestId, Action onSuccess, Action<string> onError);

        /// <summary>好友列表</summary>
        void GetFriends(Action<List<FriendData>> onSuccess, Action<string> onError);

        /// <summary>删除好友</summary>
        void RemoveFriend(long friendId, Action onSuccess, Action<string> onError);

        /// <summary>搜索用户</summary>
        void SearchUsers(string keyword, Action<List<FriendData>> onSuccess, Action<string> onError);

        /// <summary>创建房间邀请</summary>
        void CreateInvite(long friendId, string roomCode, Action<InviteData> onSuccess, Action<string> onError);

        /// <summary>我收到的待处理邀请</summary>
        void GetInvites(Action<List<InviteData>> onSuccess, Action<string> onError);

        /// <summary>接受邀请 → 回调返回 roomCode</summary>
        void AcceptInvite(long inviteId, Action<string> onSuccess, Action<string> onError);

        /// <summary>拒绝邀请</summary>
        void DeclineInvite(long inviteId, Action onSuccess, Action<string> onError);
    }
}
