/// ============================================================
/// 文件名: ObjectPool.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 通用对象池，复用 Component 类型的 GameObject。
///       使用 Stack&lt;T&gt; 存储 inactive 对象，减少 GC 开销。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Framework.Core
{
    /// <summary>
    /// 通用对象池，复用 Component 类型的 GameObject。
    /// 使用 Stack&lt;T&gt; 存储 inactive 对象。
    /// </summary>
    /// <typeparam name="T">Component 类型</typeparam>
    public class ObjectPool<T> where T : Component
    {
        /// <summary>预制体引用</summary>
        private readonly T _prefab;

        /// <summary>父节点</summary>
        private readonly Transform _parent;

        /// <summary>inactive 对象栈</summary>
        private readonly Stack<T> _pool = new Stack<T>();

        /// <summary>已创建的对象总数（含 active + inactive）</summary>
        private int _totalCreated;

        /// <summary>已创建的对象总数（含 active + inactive）</summary>
        public int TotalCreated => _totalCreated;

        /// <summary>当前池中可用（inactive）对象数</summary>
        public int PoolCount => _pool.Count;

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="initialSize">预加载数量</param>
        /// <param name="parent">父节点</param>
        public ObjectPool(T prefab, int initialSize, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;

            if (initialSize > 0)
            {
                Prewarm(initialSize);
            }
        }

        /// <summary>
        /// 从池中获取一个实例。如果池为空则实例化新对象。
        /// </summary>
        /// <returns>激活的 Component 实例</returns>
        public T Get()
        {
            T item;
            if (_pool.Count > 0)
            {
                item = _pool.Pop();
            }
            else
            {
                item = CreateInstance();
            }

            item.gameObject.SetActive(true);
            return item;
        }

        /// <summary>
        /// 将实例归还到池中，设为 inactive。
        /// </summary>
        /// <param name="item">要归还的实例</param>
        public void Release(T item)
        {
            if (item == null)
                return;

            item.gameObject.SetActive(false);
            item.transform.SetParent(_parent, false);
            _pool.Push(item);
        }

        /// <summary>
        /// 预加载指定数量的实例（实例化后立即设为 inactive 入池）。
        /// </summary>
        /// <param name="count">预加载数量</param>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T item = CreateInstance();
                item.gameObject.SetActive(false);
                _pool.Push(item);
            }
        }

        /// <summary>
        /// 清空对象池，销毁所有池中对象。
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T item = _pool.Pop();
                if (item != null)
                {
                    Object.Destroy(item.gameObject);
                }
            }
            _totalCreated = 0;
        }

        /// <summary>
        /// 实例化新对象并设置父节点。
        /// </summary>
        private T CreateInstance()
        {
            T item = Object.Instantiate(_prefab, _parent);
            item.name = _prefab.name; // 去掉 (Clone) 后缀
            _totalCreated++;
            return item;
        }
    }
}
