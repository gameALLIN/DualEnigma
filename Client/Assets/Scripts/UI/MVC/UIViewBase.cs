using UnityEngine;

namespace DualEnigma.UI
{
    /// <summary>
    /// View 基类，持有 UGUI 组件引用并负责显示刷新。
    /// 不含判断逻辑，仅被动刷新表现。
    /// </summary>
    public class UIViewBase : MonoBehaviour
    {
        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====

        // ===== Auto Bind End =====

        /// <summary>刷新视图显示，子类重写实现具体刷新逻辑</summary>
        public virtual void Refresh() { }
    }
}
