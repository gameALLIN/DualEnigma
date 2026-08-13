namespace DualEnigma.Framework.UI
{
    /// <summary>
    /// Canvas 层级定义。数值越大渲染越靠上，
    /// 用于运行时自动创建的多层 Canvas 隔离不同类型 UI 的渲染顺序。
    /// </summary>
    public enum UILayer
    {
        /// <summary>常驻底层，如 HUD</summary>
        Bottom = 0,

        /// <summary>普通面板</summary>
        Normal = 1,

        /// <summary>弹窗</summary>
        Top = 2,

        /// <summary>加载/过渡</summary>
        Loading = 3
    }
}
