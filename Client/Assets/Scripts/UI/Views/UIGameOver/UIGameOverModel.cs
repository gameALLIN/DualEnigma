/// ============================================================
/// 文件名: UIGameOverModel.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: 对局结算面板数据模型。
/// ============================================================

using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UIGameOverModel : UIModelBase
    {
        /// <summary>是否胜利</summary>
        public bool IsVictory { get; set; }

        /// <summary>是否联机对局（联机不提供"再来一局"，需回房间重开）</summary>
        public bool IsNetworked { get; set; }
    }
}
