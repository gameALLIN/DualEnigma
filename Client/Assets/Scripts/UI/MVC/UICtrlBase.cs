using UnityEngine;

namespace DualEnigma.UI
{
    /// <summary>
    /// Controller 基类，连接 Model 和 View，处理交互逻辑并实现面板生命周期。
    /// 生命周期方法通过显式接口实现暴露给 UIManager，子类以 protected override 重写。
    /// </summary>
    public abstract class UICtrlBase : MonoBehaviour, IUIPanel
    {
        protected UIModelBase m_Model;
        protected UIViewBase m_View;

        // 显式接口实现：UIManager 通过 IUIPanel 调用，子类通过 protected override 重写
        void IUIPanel.OnCreate() => OnCreate();
        void IUIPanel.OnShow() => OnShow();
        void IUIPanel.OnHide() => OnHide();
        void IUIPanel.OnDestroy() => OnDestroy();

        /// <summary>面板实例化时调用，绑定组件引用</summary>
        protected virtual void OnCreate()
        {
            m_View = GetComponent<UIViewBase>();
        }

        /// <summary>面板显示时调用，注册事件监听</summary>
        protected virtual void OnShow() { }

        /// <summary>面板隐藏时调用，注销事件监听</summary>
        protected virtual void OnHide() { }

        /// <summary>面板销毁时调用，清理资源（由 Unity 生命周期自动调用）</summary>
        protected virtual void OnDestroy() { }

        /// <summary>获取 View 组件的强类型引用</summary>
        public T GetView<T>() where T : UIViewBase
        {
            return m_View as T;
        }

        /// <summary>获取 Model 对象的强类型引用</summary>
        public T GetModel<T>() where T : UIModelBase
        {
            return m_Model as T;
        }
    }
}
