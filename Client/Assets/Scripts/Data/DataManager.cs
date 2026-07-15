/// ============================================================
/// 文件名: DataManager.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 数据管理器。负责加载、缓存和提供所有配置数据。
///       支持 JSON 配置（导表产出）和 ScriptableObject 配置两种形式。
///       通过 ResMgr 加载资源，首次加载后缓存。
/// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;

namespace DualEnigma.Data
{
    /// <summary>
    /// 数据管理器。继承 Singleton&lt;T&gt;。
    /// 负责加载、缓存和提供所有配置数据。
    /// JSON 配置使用 JsonUtility 反序列化，ScriptableObject 配置通过 ResMgr 直接加载。
    /// </summary>
    public class DataManager : Singleton<DataManager>
    {
        /// <summary>已加载的 JSON 配置缓存，Key 为配置名</summary>
        private readonly Dictionary<string, object> _jsonCache = new Dictionary<string, object>();

        /// <summary>已加载的 ScriptableObject 配置缓存，Key 为配置类型</summary>
        private readonly Dictionary<Type, ScriptableObject> _soCache = new Dictionary<Type, ScriptableObject>();

        /// <summary>是否已初始化</summary>
        private bool _isInitialized;

        protected override void OnSingletonInitialized()
        {
            Debug.Log("[DataManager] 数据管理器初始化完成");
        }

        /// <summary>
        /// 初始化数据管理器（由 GameLaunch 调用）。
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[DataManager] 已初始化，跳过重复调用");
                return;
            }
            _isInitialized = true;

            Debug.Log("[DataManager] 数据管理器初始化完成");
        }

        /// <summary>
        /// 加载 JSON 配置。首次加载后缓存，后续直接返回缓存。
        /// </summary>
        /// <typeparam name="T">反序列化目标类型</typeparam>
        /// <param name="configName">配置名（如 "disaster"，对应 Data/disaster.json）</param>
        /// <returns>反序列化后的配置对象，加载失败返回 null</returns>
        public T LoadJson<T>(string configName) where T : class
        {
            if (string.IsNullOrEmpty(configName))
            {
                Debug.LogError("[DataManager] LoadJson 失败: configName 为空");
                return null;
            }

            // 检查缓存
            if (_jsonCache.TryGetValue(configName, out object cached))
            {
                return cached as T;
            }

            // 构建路径：Data/{configName}.json
            string path = $"Data/{configName}.json";

            TextAsset jsonAsset = ResMgr.Instance.Load<TextAsset>(path);
            if (jsonAsset == null)
            {
                Debug.LogError($"[DataManager] JSON 配置加载失败: {path}");
                return null;
            }

            T config = JsonUtility.FromJson<T>(jsonAsset.text);
            if (config == null)
            {
                Debug.LogError($"[DataManager] JSON 反序列化失败: {path}");
                return null;
            }

            _jsonCache[configName] = config;
            Debug.Log($"[DataManager] JSON 配置加载成功: {configName}");
            return config;
        }

        /// <summary>
        /// 加载 ScriptableObject 配置。首次加载后缓存，后续直接返回缓存。
        /// 文件名从类型名推断（如 RoundConfig → Data/RoundConfig.asset）。
        /// </summary>
        /// <typeparam name="T">配置类型（ScriptableObject 子类）</typeparam>
        /// <returns>配置实例，加载失败返回 null</returns>
        public T LoadConfig<T>() where T : ScriptableObject
        {
            Type type = typeof(T);

            // 检查缓存
            if (_soCache.TryGetValue(type, out ScriptableObject cached))
            {
                return cached as T;
            }

            // 构建路径：Data/{TypeName}.asset
            string configName = type.Name;
            string path = $"Data/{configName}.asset";

            T config = ResMgr.Instance.Load<T>(path);
            if (config == null)
            {
                Debug.LogError($"[DataManager] ScriptableObject 配置加载失败: {path}");
                return null;
            }

            _soCache[type] = config;
            Debug.Log($"[DataManager] ScriptableObject 配置加载成功: {configName}");
            return config;
        }

        /// <summary>
        /// 清除所有缓存的配置数据。
        /// </summary>
        public void ClearCache()
        {
            _jsonCache.Clear();
            _soCache.Clear();
            Debug.Log("[DataManager] 配置缓存已清空");
        }
    }
}
