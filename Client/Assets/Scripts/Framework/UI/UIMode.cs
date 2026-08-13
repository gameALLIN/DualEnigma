namespace DualEnigma.Framework.UI
{
    /// <summary>
    /// UI 面板模式，决定面板在栈中的显示行为。
    /// FullScreen 打开时会隐藏下方面板以独占显示；Popup / HUD 不影响下方面板。
    /// </summary>
    public enum UIMode
    {
        /// <summary>全屏面板，独占显示（打开时隐藏下方面板）</summary>
        FullScreen,

        /// <summary>弹窗，不独占（下方面板保持显示）</summary>
        Popup,

        /// <summary>常驻 HUD</summary>
        HUD
    }
}
