/// ============================================================
/// 文件名: UIFriendsModel.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 好友面板数据模型。缓存好友列表与搜索结果。
///       申请/邀请由 SocialNotifyService 全局管理，不入本模型。
/// ============================================================

using System.Collections.Generic;
using DualEnigma.Framework.UI;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    public class UIFriendsModel : UIModelBase
    {
        /// <summary>好友列表</summary>
        public List<FriendData> Friends { get; private set; } = new List<FriendData>();

        /// <summary>搜索结果</summary>
        public List<FriendData> SearchResults { get; private set; } = new List<FriendData>();

        public void SetFriends(List<FriendData> friends)
        {
            Friends = friends ?? new List<FriendData>();
        }

        public void SetSearchResults(List<FriendData> results)
        {
            SearchResults = results ?? new List<FriendData>();
        }
    }
}
