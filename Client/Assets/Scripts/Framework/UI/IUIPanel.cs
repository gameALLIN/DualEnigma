namespace DualEnigma.Framework.UI
{
    /// <summary>
    /// 面板生命周期接口，由 UICtrlBase 实现。
    /// UIManager 通过此接口驱动面板的创建、显示、隐藏、销毁。
    /// </summary>
    public interface IUIPanel
    {
        /// <summary>面板实例化时调用，绑定组件引用</summary>
        void OnCreate();

        /// <summary>面板显示时调用，注册事件监听</summary>
        void OnShow();

        /// <summary>面板隐藏时调用，注销事件监听</summary>
        void OnHide();

        /// <summary>面板销毁时调用，清理资源</summary>
        void OnDestroy();
    }
}
