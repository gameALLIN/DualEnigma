/// ============================================================
/// 文件名: NetworkGameSync.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: 对局内关键事件网络同步入口。当前职责：碎片拾取上报。
///       本地接住（收集完成）→ C2S_FragmentCaught → 服务器转发双方。
/// 引用：状态同步实施计划.md M4
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.Core;

namespace DualEnigma.Network
{
    public class NetworkGameSync : Singleton<NetworkGameSync>
    {
        protected override void OnSingletonInitialized()
        {
            // FragmentCollectedEvent 在收集"完成"时发布（同接判定结束/超时后），此时本地碎片已移除
            EventBus.Instance.Subscribe<DualEnigma.Core.FragmentCollectedEvent>(OnFragmentCollected);
            Debug.Log("[NetworkGameSync] 对局同步初始化完成");
        }

        private void OnFragmentCollected(DualEnigma.Core.FragmentCollectedEvent e)
        {
            // 只上报自己接住的；对方接住的由 S2C_FragmentResult 驱动本地移除（防止回环）
            if (!RoomSession.HasInstance || !RoomSession.Instance.IsConnected) return;
            if (e.playerId != RoomSession.Instance.LocalPlayerId) return;

            GameConnection.Instance.SendFragmentCaught(e.fragmentId, e.posX, e.posY);
        }

        protected override void OnDestroy()
        {
            if (EventBus.HasInstance)
                EventBus.Instance.Unsubscribe<DualEnigma.Core.FragmentCollectedEvent>(OnFragmentCollected);
            base.OnDestroy();
        }
    }
}
