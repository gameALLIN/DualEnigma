/// ============================================================
/// 文件名: EventBus.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 事件总线，模块间松耦合通信。基于 Dictionary<Type, Delegate>
///       存储事件订阅，支持泛型事件结构体，避免 GC 开销。
/// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Framework.Core
{
    /// <summary>
    /// 事件数据标记接口，所有事件结构体实现此接口。
    /// </summary>
    public interface IEventData { }

    /// <summary>
    /// 事件总线，模块间松耦合通信。
    /// MonoBehaviour 单例，继承 Singleton&lt;T&gt;。
    /// 使用 Dictionary&lt;Type, Delegate&gt; 存储事件订阅，
    /// 同类型多个订阅者通过 Delegate.Combine 组合为委托链。
    /// </summary>
    public class EventBus : Singleton<EventBus>
    {
        /// <summary>事件订阅表，Key 为事件类型，Value 为委托链</summary>
        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        protected override void OnSingletonInitialized()
        {
            Debug.Log("[EventBus] 事件总线初始化完成");
        }

        /// <summary>
        /// 订阅指定类型的事件。
        /// </summary>
        /// <typeparam name="T">事件类型（struct, IEventData）</typeparam>
        /// <param name="handler">事件处理委托</param>
        public void Subscribe<T>(Action<T> handler) where T : struct, IEventData
        {
            if (handler == null)
            {
                Debug.LogWarning("[EventBus] Subscribe 失败: handler 为空");
                return;
            }

            Type key = typeof(T);
            if (_handlers.TryGetValue(key, out Delegate existing))
            {
                _handlers[key] = Delegate.Combine(existing, handler);
            }
            else
            {
                _handlers[key] = handler;
            }
        }

        /// <summary>
        /// 取消订阅指定类型的事件。
        /// </summary>
        /// <typeparam name="T">事件类型（struct, IEventData）</typeparam>
        /// <param name="handler">要移除的事件处理委托</param>
        public void Unsubscribe<T>(Action<T> handler) where T : struct, IEventData
        {
            if (handler == null)
                return;

            Type key = typeof(T);
            if (!_handlers.TryGetValue(key, out Delegate existing))
                return;

            Delegate newDelegate = Delegate.Remove(existing, handler);
            if (newDelegate == null)
            {
                _handlers.Remove(key);
            }
            else
            {
                _handlers[key] = newDelegate;
            }
        }

        /// <summary>
        /// 发布事件，触发所有订阅者。
        /// 逐个调用订阅者并隔离异常：单个订阅者抛错只记录日志，不中断其余订阅者。
        /// </summary>
        /// <typeparam name="T">事件类型（struct, IEventData）</typeparam>
        /// <param name="eventData">事件数据</param>
        public void Publish<T>(T eventData) where T : struct, IEventData
        {
            Type key = typeof(T);
            if (!_handlers.TryGetValue(key, out Delegate existing))
                return;

            if (existing is not Action<T> handler)
                return;

            foreach (Delegate subscriber in handler.GetInvocationList())
            {
                try
                {
                    ((Action<T>)subscriber).Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] {key.Name} 事件订阅者抛出异常: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 清除所有事件订阅。用于场景切换或重置时。
        /// </summary>
        public void ClearAll()
        {
            _handlers.Clear();
            Debug.Log("[EventBus] 所有事件订阅已清除");
        }

        /// <summary>
        /// 获取指定类型事件的订阅者数量。
        /// </summary>
        public int GetSubscriberCount<T>() where T : struct, IEventData
        {
            Type key = typeof(T);
            if (!_handlers.TryGetValue(key, out Delegate existing))
                return 0;
            return existing.GetInvocationList().Length;
        }
    }
}
