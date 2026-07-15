/// ============================================================
/// 文件名: ServiceLocator.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 服务定位器，提供接口注册与获取。
///       静态类，不需要 MonoBehaviour。
///       各系统 Manager 在初始化时注册自身接口，
///       其他系统通过接口获取服务。
/// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Core
{
    /// <summary>
    /// 服务定位器，提供接口注册与获取。
    /// 静态类，不需要 MonoBehaviour。
    /// 各系统 Manager 在初始化时注册自身接口，
    /// 其他系统通过接口获取服务。
    /// </summary>
    public static class ServiceLocator
    {
        /// <summary>服务实例表，Key 为接口类型，Value 为实现实例</summary>
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// 注册服务接口。
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="service">服务实例</param>
        public static void Register<T>(T service) where T : class
        {
            Type key = typeof(T);
            if (_services.ContainsKey(key))
            {
                Debug.LogWarning($"[ServiceLocator] 服务已注册，将被覆盖: {key.Name}");
            }
            _services[key] = service;
        }

        /// <summary>
        /// 获取服务接口，未注册时返回 null。
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <returns>服务实例，未注册时返回 null</returns>
        public static T Get<T>() where T : class
        {
            Type key = typeof(T);
            if (_services.TryGetValue(key, out object service))
            {
                return service as T;
            }
            return null;
        }

        /// <summary>
        /// 注销服务接口。
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        public static void Unregister<T>() where T : class
        {
            Type key = typeof(T);
            _services.Remove(key);
        }

        /// <summary>
        /// 判断指定服务是否已注册。
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 清除所有已注册的服务。用于场景切换或重置时。
        /// </summary>
        public static void ClearAll()
        {
            _services.Clear();
            Debug.Log("[ServiceLocator] 所有服务已清除");
        }
    }
}
