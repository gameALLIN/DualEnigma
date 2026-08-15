/// ============================================================
/// 文件名: UIRoomModel.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 房间等待面板数据模型。
/// ============================================================

using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UIRoomModel : UIModelBase
    {
        /// <summary>当前房间码</summary>
        public string RoomCode { get; set; } = "";

        /// <summary>房间人数（1=等待中，2=满员开局）</summary>
        public int PlayerCount { get; set; } = 1;

        /// <summary>是否房主（房主=等待好友加入，非房主=已应邀加入）</summary>
        public bool IsHost { get; set; } = true;
    }
}
