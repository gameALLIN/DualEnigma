/// ============================================================
/// 文件名: ResMgr.cs
/// 创建时间: 2026-07-10
/// 最后更新: 2026-07-11
/// 作者: DualEnigma
/// 描述: 资源管理器，对外统一接口层。
///       Editor 模式走 AssetDatabase 直接加载；
///       Runtime 模式走 AssetBundleMgr 通过 AB 加载。
///       屏蔽底层差异，业务代码无需感知当前模式。
/// ============================================================

#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Framework.Core
{
    /// <summary>
    /// 资源管理器对外接口层。
    /// 提供统一的资源加载 API，屏蔽 Editor（AssetDatabase）和 Runtime（AssetBundle）的差异。
    /// 调用方：所有业务代码（UIManager、游戏逻辑等）。
    /// </summary>
    public class ResMgr : Singleton<ResMgr>
    {
        /// <summary>
        /// 资源根路径（相对于 Assets 目录）
        /// </summary>
        private const string ASSET_ROOT = "AssetPackage/";

        /// <summary>
        /// 路径前缀 → Bundle 名称映射表
        /// </summary>
        private static readonly Dictionary<string, string> s_PathToBundle =
            new Dictionary<string, string>
            {
                { "Prefabs/UI",         "ui" },
                { "Prefabs/Characters", "character" },
                { "Prefabs/Effects",    "effect" },
                { "Atlases",            "atlas" },
                { "Audio",              "audio" },
                { "Data",               "data" },
            };

        /// <summary>是否已初始化</summary>
        private bool m_IsInitialized;

        // ============================================================
        // 生命周期
        // ============================================================

        protected override void OnSingletonInitialized()
        {
            Debug.Log("[ResMgr] 资源管理器初始化完成");
        }

        /// <summary>
        /// 初始化资源管理器。Runtime 模式下初始化 AssetBundleMgr，
        /// Editor 模式下无需额外操作。
        /// 由 GameLaunch.Awake() 调用。
        /// </summary>
        public void Init()
        {
            if (m_IsInitialized)
                return;
            m_IsInitialized = true;

#if !UNITY_EDITOR
            AssetBundleMgr.Instance.Init();
            Debug.Log("[ResMgr] Runtime 模式初始化完成，AssetBundleMgr 已就绪");
#else
            Debug.Log("[ResMgr] Editor 模式初始化完成，使用 AssetDatabase 直接加载");
#endif
        }

        // ============================================================
        // 路径映射
        // ============================================================

        /// <summary>
        /// 解析资源路径为 Bundle 名称和资产名。
        /// 路径前缀匹配映射表后确定 Bundle，资产名为路径最后一个组件（去除扩展名）。
        /// </summary>
        /// <param name="path">相对路径，如 "Prefabs/UI/UITest/UITest"</param>
        /// <param name="bundleName">输出的 Bundle 名称</param>
        /// <param name="assetName">输出的资产名</param>
        /// <returns>是否解析成功</returns>
        public static bool ResolvePath(string path, out string bundleName,
            out string assetName)
        {
            bundleName = null;
            assetName = null;

            // 查找匹配的前缀
            foreach (var mapping in s_PathToBundle)
            {
                if (!string.IsNullOrEmpty(mapping.Key) && path.StartsWith(mapping.Key))
                {
                    bundleName = mapping.Value;
                    break;
                }
            }

            if (bundleName == null)
            {
                Debug.LogError($"[ResMgr] 未找到路径映射: {path}");
                return false;
            }

            // 资产名为路径最后一个组件
            int lastSlash = path.LastIndexOf('/');
            assetName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;

            // 去除扩展名
            int dotIndex = assetName.LastIndexOf('.');
            if (dotIndex >= 0)
            {
                assetName = assetName.Substring(0, dotIndex);
            }

            return true;
        }

        // ============================================================
        // 同步加载
        // ============================================================

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型（GameObject、Sprite、AudioClip 等）</typeparam>
        /// <param name="path">相对路径，如 "Prefabs/UI/UITest/UITest"</param>
        /// <returns>资源对象，加载失败返回 null</returns>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            // Editor 模式：AssetDatabase 直接加载
            string fullPath = ASSET_ROOT + path;
            T asset = AssetDatabase.LoadAssetAtPath<T>("Assets/" + fullPath);
            if (asset == null)
            {
                AssetDatabase.Refresh();
                asset = AssetDatabase.LoadAssetAtPath<T>("Assets/" + fullPath);
            }
            if (asset == null)
            {
                Debug.LogError($"[ResMgr] 资源加载失败: Assets/{fullPath} (类型: {typeof(T).Name})");
            }
            return asset;
#else
            // Runtime 模式：通过 AssetBundleMgr 加载
            if (!ResolvePath(path, out string bundleName, out string assetName))
                return null;

            AssetBundleMgr.Instance.LoadBundle(bundleName);
            T asset = AssetBundleMgr.Instance.LoadAsset<T>(bundleName, assetName);
            if (asset == null)
            {
                Debug.LogError($"[ResMgr] 资源加载失败: Bundle={bundleName}, " +
                    $"Asset={assetName} (类型: {typeof(T).Name})");
            }
            return asset;
#endif
        }

        /// <summary>
        /// 加载 GameObject 预制体
        /// </summary>
        /// <param name="path">相对路径，如 "Prefabs/UI/UITest/UITest"</param>
        /// <returns>GameObject 预制体</returns>
        public GameObject LoadPrefab(string path)
        {
#if UNITY_EDITOR
            // Editor 模式：需要 .prefab 扩展名
            if (!path.EndsWith(".prefab"))
            {
                path += ".prefab";
            }
#else
            // Runtime 模式：不需要扩展名，ResolvePath 会自动去除
#endif
            return Load<GameObject>(path);
        }

        // ============================================================
        // 异步加载
        // ============================================================

        /// <summary>
        /// 异步加载资源。Editor 模式下内部同步执行，回调立即触发。
        /// Runtime 模式下通过 AssetBundleMgr 协程异步执行。
        /// </summary>
        public void LoadAsync<T>(string path, Action<T> onComplete)
            where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            // Editor 模式：同步执行，立即回调
            T asset = Load<T>(path);
            onComplete?.Invoke(asset);
#else
            // Runtime 模式：通过协程异步加载
            StartCoroutine(LoadAsyncCoroutine<T>(path, onComplete));
#endif
        }

#if !UNITY_EDITOR
        private System.Collections.IEnumerator LoadAsyncCoroutine<T>(string path,
            Action<T> onComplete) where T : UnityEngine.Object
        {
            if (!ResolvePath(path, out string bundleName, out string assetName))
            {
                onComplete?.Invoke(null);
                yield break;
            }

            // 异步加载 AB
            AssetBundle bundle = null;
            bool bundleDone = false;
            AssetBundleMgr.Instance.LoadBundleAsync(bundleName, (b) =>
            {
                bundle = b;
                bundleDone = true;
            });
            yield return new WaitUntil(() => bundleDone);

            // 异步加载资产
            T asset = null;
            bool assetDone = false;
            AssetBundleMgr.Instance.LoadAssetAsync<T>(bundleName, assetName, (a) =>
            {
                asset = a;
                assetDone = true;
            });
            yield return new WaitUntil(() => assetDone);

            onComplete?.Invoke(asset);
        }
#endif

        /// <summary>
        /// 异步加载预制体。Editor 模式下同步执行，回调立即触发。
        /// </summary>
        public void LoadPrefabAsync(string path, Action<GameObject> onComplete)
        {
#if UNITY_EDITOR
            GameObject prefab = LoadPrefab(path);
            onComplete?.Invoke(prefab);
#else
            StartCoroutine(LoadAsyncCoroutine<GameObject>(path, onComplete));
#endif
        }

        // ============================================================
        // 引用管理（转发给 AssetBundleMgr）
        // ============================================================

        /// <summary>
        /// 增加 AB 引用计数，记录持有者。（转发给 AssetBundleMgr）
        /// Editor 模式下无操作。
        /// </summary>
        public void AddRef(string bundleName, UnityEngine.Object holder)
        {
#if !UNITY_EDITOR
            AssetBundleMgr.Instance.AddRef(bundleName, holder);
#endif
        }

        /// <summary>
        /// 减少 AB 引用计数，移除持有者。（转发给 AssetBundleMgr）
        /// Editor 模式下无操作。
        /// </summary>
        public void ReleaseRef(string bundleName, UnityEngine.Object holder)
        {
#if !UNITY_EDITOR
            AssetBundleMgr.Instance.ReleaseRef(bundleName, holder);
#endif
        }

        // ============================================================
        // 卸载（转发给 AssetBundleMgr）
        // ============================================================

        /// <summary>
        /// 立即卸载所有引用归零的 AB。（转发给 AssetBundleMgr）
        /// Editor 模式下无操作。
        /// </summary>
        public void UnloadUnused()
        {
#if !UNITY_EDITOR
            AssetBundleMgr.Instance.UnloadUnused();
#endif
        }

        /// <summary>
        /// 标记 AB 为常驻，不参与卸载。（转发给 AssetBundleMgr）
        /// Editor 模式下无操作。
        /// </summary>
        public void SetPersistentBundle(string name)
        {
#if !UNITY_EDITOR
            AssetBundleMgr.Instance.SetPersistent(name);
#endif
        }
    }
}