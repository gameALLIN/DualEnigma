using System;

namespace DualEnigma.UI
{
    /// <summary>
    /// Model 基类，持有 UI 显示数据并通过事件通知 View 刷新。
    /// 纯 C# 类不继承 MonoBehaviour，保证数据层可独立测试且不引用任何 UGUI 类型。
    /// </summary>
    public class UIModelBase
    {
        /// <summary>数据变更事件，View 监听此事件触发刷新</summary>
        public event Action OnDataChanged;

        /// <summary>通知数据已变更，触发所有监听者刷新</summary>
        protected void NotifyDataChanged()
        {
            OnDataChanged?.Invoke();
        }
    }
}
