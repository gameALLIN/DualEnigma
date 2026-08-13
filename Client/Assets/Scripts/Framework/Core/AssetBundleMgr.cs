/// ============================================================
/// 文件名: AssetBundleMgr.cs
/// 创建时间: 2026-07-11
/// 作者: DualEnigma
/// 描述: AssetBundle 管理器的内部实现，负责 AB 加载/卸载、依赖解析、引用计数、
///        延迟卸载、多平台适配。由 ResMgr 调用，业务代码不直接使用。
/// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Framework.Core
{
    // ============================================================
    // IBundleManifest — 依赖解析接口（可测试性）
    // ============================================================

    /// <summary>
    /// 依赖解析接口。用于解耦 AssetBundleManifest，方便测试时注入 Mock。
    /// </summary>
    public interface IBundleManifest
    {
        /// <summary>
        /// 获取指定 AB 的所有依赖（包含传递依赖）
        /// </summary>
        string[] GetAllDependencies(string bundleName);
    }

    // ============================================================
    // AssetBundleManifestWrapper — 真实 Manifest 的适配器
    // ============================================================

    /// <summary>
    /// 包装 Unity 原生的 AssetBundleManifest，实现 IBundleManifest 接口。
    /// </summary>
    public class AssetBundleManifestWrapper : IBundleManifest
    {
        private AssetBundleManifest m_Manifest;

        public AssetBundleManifestWrapper(AssetBundleManifest manifest)
        {
            m_Manifest = manifest;
        }

        public string[] GetAllDependencies(string bundleName)
        {
            if (m_Manifest == null)
                return new string[0];
            return m_Manifest.GetAllDependencies(bundleName);
        }
    }

    // ============================================================
    // IBundleLoader — 多平台加载接口
    // ============================================================

    /// <summary>
    /// AB 加载接口。各平台实现不同，支持 Mock 注入用于测试。
    /// </summary>
    public interface IBundleLoader
    {
        /// <summary>同步加载 AB</summary>
        AssetBundle LoadFromFile(string bundleName);

        /// <summary>异步加载 AB（协程）</summary>
        IEnumerator LoadFromFileAsync(string bundleName, Action<AssetBundle> callback);
    }

    // ============================================================
    // StandaloneBundleLoader — Standalone 平台（Windows/macOS）
    // ============================================================

    /// <summary>
    /// Standalone（Windows/macOS）平台 AB 加载器。从本地 StreamingAssets 同步读取，
    /// 延迟可忽略，推荐同步加载。
    /// </summary>
    public class StandaloneBundleLoader : IBundleLoader
    {
        private readonly string m_RootPath;

        public StandaloneBundleLoader(string rootPath)
        {
            m_RootPath = rootPath;
        }

        public AssetBundle LoadFromFile(string bundleName)
        {
            string path = System.IO.Path.Combine(m_RootPath, bundleName + ".bundle");
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"[StandaloneBundleLoader] AB 文件不存在: {path}");
                return null;
            }
            return AssetBundle.LoadFromFile(path);
        }

        public IEnumerator LoadFromFileAsync(string bundleName, Action<AssetBundle> callback)
        {
            string path = System.IO.Path.Combine(m_RootPath, bundleName + ".bundle");
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(path);
            yield return request;
            callback?.Invoke(request.assetBundle);
        }
    }

    // ============================================================
    // AndroidBundleLoader — Android 平台
    // ============================================================

    /// <summary>
    /// Android 平台 AB 加载器。从 StreamingAssets 或 OBB 读取，
    /// 使用 Unity Caching 缓存。
    /// </summary>
    public class AndroidBundleLoader : IBundleLoader
    {
        private readonly string m_RootPath;

        public AndroidBundleLoader(string rootPath)
        {
            m_RootPath = rootPath;
        }

        public AssetBundle LoadFromFile(string bundleName)
        {
            string path = System.IO.Path.Combine(m_RootPath, bundleName + ".bundle");
            return AssetBundle.LoadFromFile(path);
        }

        public IEnumerator LoadFromFileAsync(string bundleName, Action<AssetBundle> callback)
        {
            string path = System.IO.Path.Combine(m_RootPath, bundleName + ".bundle");
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(path);
            yield return request;
            callback?.Invoke(request.assetBundle);
        }
    }

    // ============================================================
    // IOSBundleLoader — iOS 平台
    // ============================================================

    /// <summary>
    /// iOS 平台 AB 加载器。从 StreamingAssets 读取，使用 Unity Caching 缓存。
    /// </summary>
    public class IOSBundleLoader : IBundleLoader
    {
        private readonly string m_RootPath;

        public IOSBundleLoader(string rootPath)
        {
            m_RootPath = rootPath;
        }

        public AssetBundle LoadFromFile(string bundleName)
        {
            string path = System.IO.Path.Combine(m_RootPath, bundleName + ".bundle");
            return AssetBundle.LoadFromFile(path);
        }

        public IEnumerator LoadFromFileAsync(string bundleName, Action<AssetBundle> callback)
        {
            string path = System.IO.Path.Combine(m_RootPath, bundleName + ".bundle");
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(path);
            yield return request;
            callback?.Invoke(request.assetBundle);
        }
    }

    // ============================================================
    // WebGLBundleLoader — WebGL 平台
    // ============================================================

    /// <summary>
    /// WebGL 平台 AB 加载器。使用 UnityWebRequestAssetBundle 从远程服务器下载，
    /// 不支持同步加载，WebGL 2022+ 禁用 Unity Caching，使用浏览器原生缓存。
    /// </summary>
    public class WebGLBundleLoader : IBundleLoader
    {
        private readonly string m_RootPath;

        public WebGLBundleLoader(string rootPath)
        {
            m_RootPath = rootPath;
        }

        public AssetBundle LoadFromFile(string bundleName)
        {
            Debug.LogError("[WebGLBundleLoader] WebGL 不支持同步加载 AB，请使用 LoadBundleAsync");
            return null;
        }

        public IEnumerator LoadFromFileAsync(string bundleName, Action<AssetBundle> callback)
        {
            string path = System.IO.Path.Combine(m_RootPath, bundleName + ".bundle");
            using (UnityEngine.Networking.UnityWebRequest request =
                UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(path))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    AssetBundle bundle = UnityEngine.Networking.DownloadHandlerAssetBundle.GetContent(request);
                    callback?.Invoke(bundle);
                }
                else
                {
                    Debug.LogError($"[WebGLBundleLoader] AB 加载失败: {path}\n错误: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }
    }

    // ============================================================
    // AssetBundleMgr — AB 管理器单例
    // ============================================================

    /// <summary>
    /// AssetBundle 管理器内部实现。负责 AB 的加载/卸载、依赖解析、引用计数、
    /// 延迟卸载（避免抖动）、多平台适配和常驻 AB 管理。
    /// 由 ResMgr 在 Runtime 模式下调用，业务代码不直接接触此类。
    /// </summary>
    public class AssetBundleMgr : Singleton<AssetBundleMgr>
    {
        // ===== 内部容器 =====

        /// <summary>已加载的 AB 清单，Key 为 AB 名称</summary>
        private Dictionary<string, ABRefItem> m_LoadedBundles
            = new Dictionary<string, ABRefItem>();

        /// <summary>延迟卸载队列，RefCount 归零后加入，Time.time 到期后执行卸载</summary>
        private List<DelayUnloadItem> m_DelayUnloadList
            = new List<DelayUnloadItem>();

        /// <summary>常驻 AB 名称集合，不参与卸载（仅在 UnloadAll 时释放）</summary>
        private HashSet<string> m_PersistentBundles
            = new HashSet<string>();

        /// <summary>依赖解析接口（真实 Manifest 或 Mock）</summary>
        private IBundleManifest m_Manifest;

        /// <summary>平台加载器</summary>
        private IBundleLoader m_Loader;

        // ===== 平台与路径 =====

        /// <summary>当前平台名称（如 "Windows", "Android"）</summary>
        private string m_PlatformName;

        /// <summary>AB 根路径</summary>
        private string m_ABRootPath;

        // ===== 定时器 =====

        /// <summary>延迟卸载秒数（默认 2 秒，测试时可设为 0）</summary>
        private float m_DelayUnloadSeconds = 2f;

        /// <summary>无效持有者检查间隔（秒）</summary>
        private const float NULL_CHECK_INTERVAL = 1f;

        /// <summary>上次无效持有者检查时间</summary>
        private float m_LastNullCheckTime;

        /// <summary>是否已初始化</summary>
        private bool m_IsInitialized;

        // ============================================================
        // 生命周期
        // ============================================================

        protected override void OnSingletonInitialized()
        {
            Debug.Log("[AssetBundleMgr] 单例创建完成");
        }

        /// <summary>
        /// 初始化 AB 管理器：确定平台路径、创建对应 IBundleLoader、加载 AssetBundleManifest。
        /// 由 ResMgr.Init() 在 Runtime 模式下调用。
        /// </summary>
        public void Init()
        {
            if (m_IsInitialized)
            {
                Debug.LogWarning("[AssetBundleMgr] 已初始化，跳过重复调用");
                return;
            }
            m_IsInitialized = true;

            m_PlatformName = GetPlatformName();
            m_ABRootPath = System.IO.Path.Combine(Application.streamingAssetsPath,
                "AssetBundles", m_PlatformName);
            m_Loader = CreateLoader();
            LoadManifest();

            Debug.Log($"[AssetBundleMgr] 初始化完成 - 平台: {m_PlatformName}, AB 根路径: {m_ABRootPath}");
        }

        /// <summary>根据编译宏确定平台名称</summary>
        private static string GetPlatformName()
        {
#if UNITY_STANDALONE_WIN
            return "Windows";
#elif UNITY_STANDALONE_OSX
            return "OSX";
#elif UNITY_ANDROID
            return "Android";
#elif UNITY_IOS
            return "iOS";
#elif UNITY_WEBGL
            return "WebGL";
#else
            return "Windows"; // 默认
#endif
        }

        /// <summary>根据平台创建对应的 IBundleLoader 实现</summary>
        private IBundleLoader CreateLoader()
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            return new StandaloneBundleLoader(m_ABRootPath);
#elif UNITY_ANDROID
            return new AndroidBundleLoader(m_ABRootPath);
#elif UNITY_IOS
            return new IOSBundleLoader(m_ABRootPath);
#elif UNITY_WEBGL
            return new WebGLBundleLoader(m_ABRootPath);
#else
            return new StandaloneBundleLoader(m_ABRootPath);
#endif
        }

        /// <summary>
        /// 加载平台根目录的 Manifest 总包，从中获取 AssetBundleManifest。
        /// Manifest 文件不缓存，每次启动重新加载。
        /// </summary>
        private void LoadManifest()
        {
            if (m_Loader == null)
            {
                Debug.LogWarning("[AssetBundleMgr] 加载器未就绪，Manifest 加载失败");
                return;
            }

            AssetBundle manifestBundle = m_Loader.LoadFromFile(m_PlatformName);
            if (manifestBundle != null)
            {
                AssetBundleManifest manifest =
                    manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                if (manifest != null)
                {
                    m_Manifest = new AssetBundleManifestWrapper(manifest);
                    Debug.Log("[AssetBundleMgr] Manifest 加载成功");
                }
                manifestBundle.Unload(false); // 卸载 Manifest 总包但保留 Manifest 对象
            }
            else
            {
                Debug.LogWarning($"[AssetBundleMgr] Manifest 总包加载失败: {m_PlatformName}.bundle");
            }
        }

        // ============================================================
        // Update — 延迟卸载 + 无效持有者清理
        // ============================================================

        private void Update()
        {
            ProcessDelayedUnload();

            if (Time.time - m_LastNullCheckTime >= NULL_CHECK_INTERVAL)
            {
                m_LastNullCheckTime = Time.time;
                CleanNullHolders();
            }
        }

        // ============================================================
        // Bundle 加载
        // ============================================================

        /// <summary>
        /// 同步加载 AB 及其所有依赖，引用计数 +1。
        /// 如果 AB 已加载则直接返回并增加引用计数。
        /// </summary>
        public AssetBundle LoadBundle(string bundleName,
            DownloadSettings settings = DownloadSettings.Default)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                Debug.LogError("[AssetBundleMgr] LoadBundle 失败: bundleName 为空");
                return null;
            }

            // 已加载 → 恢复引用（如果 RefCount = 0 则从延迟队列中移除）
            if (m_LoadedBundles.TryGetValue(bundleName, out ABRefItem item))
            {
                if (item.RefCount <= 0)
                {
                    RemoveFromDelayUnload(bundleName);
                    IncrementDependencies(item);
                }
                item.RefCount++;
                return item.Bundle;
            }

            // 首加载：先加载所有依赖
            string[] deps = m_Manifest != null
                ? m_Manifest.GetAllDependencies(bundleName) : null;

            if (deps != null && deps.Length > 0)
            {
                foreach (string dep in deps)
                {
                    LoadDependency(dep, settings);
                }
            }

            // 加载目标 AB
            AssetBundle bundle = m_Loader != null
                ? m_Loader.LoadFromFile(bundleName) : null;

            ABRefItem newItem = new ABRefItem
            {
                BundleName = bundleName,
                Bundle = bundle,
                RefCount = 1,
                Dependencies = deps,
                Settings = settings,
                IsPersistent = m_PersistentBundles.Contains(bundleName),
            };
            m_LoadedBundles[bundleName] = newItem;

            Debug.Log($"[AssetBundleMgr] AB 加载完成: {bundleName}");
            return bundle;
        }

        /// <summary>加载单个依赖 AB（复用 LoadDependency 辅助方法）</summary>
        private void LoadDependency(string depBundleName, DownloadSettings settings)
        {
            if (m_LoadedBundles.TryGetValue(depBundleName, out ABRefItem depItem))
            {
                if (depItem.RefCount <= 0)
                {
                    RemoveFromDelayUnload(depBundleName);
                }
                depItem.RefCount++;
            }
            else
            {
                AssetBundle depBundle = m_Loader != null
                    ? m_Loader.LoadFromFile(depBundleName) : null;

                ABRefItem newDepItem = new ABRefItem
                {
                    BundleName = depBundleName,
                    Bundle = depBundle,
                    RefCount = 1,
                    Dependencies = m_Manifest != null
                        ? m_Manifest.GetAllDependencies(depBundleName) : null,
                    Settings = settings,
                    IsPersistent = m_PersistentBundles.Contains(depBundleName),
                };
                m_LoadedBundles[depBundleName] = newDepItem;
            }
        }

        /// <summary>
        /// 异步加载 AB 及其依赖（协程）。加载完成后回调，适用于 WebGL 和大 AB。
        /// </summary>
        public void LoadBundleAsync(string bundleName, Action<AssetBundle> callback,
            DownloadSettings settings = DownloadSettings.Default)
        {
            StartCoroutine(LoadBundleAsyncCoroutine(bundleName, callback, settings));
        }

        private IEnumerator LoadBundleAsyncCoroutine(string bundleName,
            Action<AssetBundle> callback, DownloadSettings settings)
        {
            // 已加载
            if (m_LoadedBundles.TryGetValue(bundleName, out ABRefItem item))
            {
                if (item.RefCount <= 0)
                {
                    RemoveFromDelayUnload(bundleName);
                    IncrementDependencies(item);
                }
                item.RefCount++;
                callback?.Invoke(item.Bundle);
                yield break;
            }

            // 加载依赖
            string[] deps = m_Manifest != null
                ? m_Manifest.GetAllDependencies(bundleName) : null;

            if (deps != null && deps.Length > 0)
            {
                foreach (string dep in deps)
                {
                    if (m_LoadedBundles.TryGetValue(dep, out ABRefItem depItem))
                    {
                        if (depItem.RefCount <= 0)
                            RemoveFromDelayUnload(dep);
                        depItem.RefCount++;
                    }
                    else
                    {
                        AssetBundle depBundle = null;
                        if (m_Loader != null)
                        {
                            yield return m_Loader.LoadFromFileAsync(dep, (b) => depBundle = b);
                        }

                        ABRefItem newDepItem = new ABRefItem
                        {
                            BundleName = dep,
                            Bundle = depBundle,
                            RefCount = 1,
                            Dependencies = m_Manifest != null
                                ? m_Manifest.GetAllDependencies(dep) : null,
                            Settings = settings,
                            IsPersistent = m_PersistentBundles.Contains(dep),
                        };
                        m_LoadedBundles[dep] = newDepItem;
                    }
                }
            }

            // 加载目标 AB
            AssetBundle bundle = null;
            if (m_Loader != null)
            {
                yield return m_Loader.LoadFromFileAsync(bundleName, (b) => bundle = b);
            }

            ABRefItem newItem = new ABRefItem
            {
                BundleName = bundleName,
                Bundle = bundle,
                RefCount = 1,
                Dependencies = deps,
                Settings = settings,
                IsPersistent = m_PersistentBundles.Contains(bundleName),
            };
            m_LoadedBundles[bundleName] = newItem;

            Debug.Log($"[AssetBundleMgr] AB 异步加载完成: {bundleName}");
            callback?.Invoke(bundle);
        }

        // ============================================================
        // 资产加载
        // ============================================================

        /// <summary>从已加载的 AB 中同步加载资产</summary>
        public T LoadAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
        {
            if (!m_LoadedBundles.TryGetValue(bundleName, out ABRefItem item))
            {
                Debug.LogError($"[AssetBundleMgr] LoadAsset 失败: AB 未加载 - {bundleName}");
                return null;
            }

            if (item.Bundle == null)
            {
                Debug.LogWarning($"[AssetBundleMgr] AB 实例为空（可能是测试环境）: {bundleName}");
                return null;
            }

            T asset = item.Bundle.LoadAsset<T>(assetName);
            if (asset == null)
            {
                Debug.LogWarning($"[AssetBundleMgr] 资产加载失败: Bundle={bundleName}, Asset={assetName}, Type={typeof(T).Name}");
            }
            return asset;
        }

        /// <summary>从已加载的 AB 中异步加载资产（协程）</summary>
        public void LoadAssetAsync<T>(string bundleName, string assetName,
            Action<T> callback) where T : UnityEngine.Object
        {
            StartCoroutine(LoadAssetAsyncCoroutine<T>(bundleName, assetName, callback));
        }

        private IEnumerator LoadAssetAsyncCoroutine<T>(string bundleName, string assetName,
            Action<T> callback) where T : UnityEngine.Object
        {
            if (!m_LoadedBundles.TryGetValue(bundleName, out ABRefItem item)
                || item.Bundle == null)
            {
                Debug.LogWarning($"[AssetBundleMgr] LoadAssetAsync 失败: AB 未就绪 - {bundleName}");
                callback?.Invoke(null);
                yield break;
            }

            AssetBundleRequest request = item.Bundle.LoadAssetAsync<T>(assetName);
            yield return request;

            T asset = request.asset as T;
            if (asset == null)
            {
                Debug.LogWarning($"[AssetBundleMgr] 异步资产加载失败: Bundle={bundleName}, Asset={assetName}");
            }
            callback?.Invoke(asset);
        }

        // ============================================================
        // 引用计数
        // ============================================================

        /// <summary>
        /// 增加 AB 引用计数，并记录持有者。
        /// 业务通过 ResMgr 调用此方法以声明"我正在使用这个 AB"。
        /// </summary>
        public void AddRef(string bundleName, UnityEngine.Object holder)
        {
            if (!m_LoadedBundles.TryGetValue(bundleName, out ABRefItem item))
            {
                Debug.LogWarning($"[AssetBundleMgr] AddRef 失败: AB 未加载 - {bundleName}");
                return;
            }

            if (item.RefCount <= 0)
            {
                RemoveFromDelayUnload(bundleName);
            }

            item.RefCount++;
            if (holder != null)
            {
                item.Holders.Add(holder);
            }
        }

        /// <summary>
        /// 减少 AB 引用计数，移除持有者。
        /// RefCount 归零且非持久 AB 时加入延迟卸载队列。
        /// </summary>
        public void ReleaseRef(string bundleName, UnityEngine.Object holder)
        {
            if (!m_LoadedBundles.TryGetValue(bundleName, out ABRefItem item))
            {
                Debug.LogWarning($"[AssetBundleMgr] ReleaseRef 失败: AB 未加载 - {bundleName}");
                return;
            }

            if (holder != null)
            {
                item.Holders.Remove(holder);
            }

            item.RefCount--;

            if (item.RefCount <= 0 && !item.IsPersistent)
            {
                item.RefCount = 0;
                AddToDelayUnload(bundleName);
                DecrementDependencies(item);
            }
        }

        // ============================================================
        // 依赖引用计数辅助方法
        // ============================================================

        /// <summary>
        /// 当 AB 的 RefCount 从 0 恢复到 >0 时，恢复其依赖的引用计数
        /// </summary>
        private void IncrementDependencies(ABRefItem item)
        {
            if (item.Dependencies == null)
                return;
            foreach (string dep in item.Dependencies)
            {
                if (m_LoadedBundles.TryGetValue(dep, out ABRefItem depItem))
                {
                    if (depItem.RefCount <= 0)
                        RemoveFromDelayUnload(dep);
                    depItem.RefCount++;
                }
            }
        }

        /// <summary>
        /// 当 AB 的 RefCount 归零时，减少其依赖的引用计数
        /// </summary>
        private void DecrementDependencies(ABRefItem item)
        {
            if (item.Dependencies == null)
                return;
            foreach (string dep in item.Dependencies)
            {
                if (m_LoadedBundles.TryGetValue(dep, out ABRefItem depItem))
                {
                    depItem.RefCount--;
                    if (depItem.RefCount <= 0 && !depItem.IsPersistent)
                    {
                        depItem.RefCount = 0;
                        AddToDelayUnload(dep);
                    }
                }
            }
        }

        // ============================================================
        // 卸载
        // ============================================================

        /// <summary>
        /// 标记 AB 为常驻。即使 RefCount 归零也不卸载，仅在游戏退出时 UnloadAll 释放。
        /// </summary>
        public void SetPersistent(string bundleName)
        {
            m_PersistentBundles.Add(bundleName);

            if (m_LoadedBundles.TryGetValue(bundleName, out ABRefItem item))
            {
                item.IsPersistent = true;
                RemoveFromDelayUnload(bundleName);
            }
        }

        /// <summary>
        /// 立即卸载所有引用计数归零且非常驻的 AB，同时清空延迟卸载队列。
        /// </summary>
        public void UnloadUnused()
        {
            List<string> toRemove = new List<string>();
            foreach (var kvp in m_LoadedBundles)
            {
                if (kvp.Value.RefCount <= 0 && !kvp.Value.IsPersistent)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (string name in toRemove)
            {
                if (m_LoadedBundles.TryGetValue(name, out ABRefItem item))
                {
                    if (item.Bundle != null)
                    {
                        item.Bundle.Unload(true);
                    }
                    m_LoadedBundles.Remove(name);
                }
            }

            m_DelayUnloadList.Clear();

            if (toRemove.Count > 0)
            {
                Debug.Log($"[AssetBundleMgr] UnloadUnused 完成: 卸载 {toRemove.Count} 个 AB");
            }
        }

        /// <summary>
        /// 卸载所有 AB（包括常驻 AB）。游戏退出时调用。
        /// </summary>
        public void UnloadAll()
        {
            foreach (var kvp in m_LoadedBundles)
            {
                if (kvp.Value.Bundle != null)
                {
                    kvp.Value.Bundle.Unload(true);
                }
            }

            int count = m_LoadedBundles.Count;
            m_LoadedBundles.Clear();
            m_DelayUnloadList.Clear();
            m_PersistentBundles.Clear();

            Debug.Log($"[AssetBundleMgr] UnloadAll 完成: 卸载 {count} 个 AB");
        }

        // ============================================================
        // 查询
        // ============================================================

        /// <summary>判断 AB 是否已加载</summary>
        public bool IsLoaded(string bundleName)
        {
            return m_LoadedBundles.ContainsKey(bundleName);
        }

        /// <summary>检查目标 AB 及其所有依赖是否已加载</summary>
        public bool IsAllDependenciesLoaded(string bundleName)
        {
            if (!IsLoaded(bundleName))
                return false;

            string[] deps = m_Manifest != null
                ? m_Manifest.GetAllDependencies(bundleName) : null;

            if (deps == null)
                return true;

            foreach (string dep in deps)
            {
                if (!IsLoaded(dep))
                    return false;
            }
            return true;
        }

        /// <summary>获取所有已加载的 AB 名称列表</summary>
        public List<string> GetLoadedBundleNames()
        {
            return new List<string>(m_LoadedBundles.Keys);
        }

        // ============================================================
        // 延迟卸载处理
        // ============================================================

        /// <summary>
        /// 将 AB 加入延迟卸载队列。RefCount 归零时调用。
        /// </summary>
        private void AddToDelayUnload(string bundleName)
        {
            // 检查是否已在队列中
            foreach (var item in m_DelayUnloadList)
            {
                if (item.BundleName == bundleName)
                    return;
            }

            m_DelayUnloadList.Add(new DelayUnloadItem
            {
                BundleName = bundleName,
                UnloadTime = Time.time + m_DelayUnloadSeconds,
            });
        }

        /// <summary>
        /// 从延迟卸载队列中移除指定 AB（被重新引用时调用）。
        /// </summary>
        private void RemoveFromDelayUnload(string bundleName)
        {
            for (int i = m_DelayUnloadList.Count - 1; i >= 0; i--)
            {
                if (m_DelayUnloadList[i].BundleName == bundleName)
                {
                    m_DelayUnloadList.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 遍历延迟卸载队列，卸载到期的 AB。
        /// 在 Update() 中每帧调用，也可通过测试手动触发。
        /// </summary>
        public void ProcessDelayedUnload()
        {
            if (m_DelayUnloadList.Count == 0)
                return;

            float currentTime = Time.time;
            for (int i = m_DelayUnloadList.Count - 1; i >= 0; i--)
            {
                DelayUnloadItem delayItem = m_DelayUnloadList[i];
                if (currentTime >= delayItem.UnloadTime)
                {
                    // 确认 RefCount 仍为 0 且非常驻
                    if (m_LoadedBundles.TryGetValue(delayItem.BundleName,
                        out ABRefItem item))
                    {
                        if (item.RefCount <= 0 && !item.IsPersistent)
                        {
                            if (item.Bundle != null)
                            {
                                item.Bundle.Unload(true);
                            }
                            m_LoadedBundles.Remove(delayItem.BundleName);
                        }
                    }
                    m_DelayUnloadList.RemoveAt(i);
                }
            }
        }

        // ============================================================
        // 无效持有者清理
        // ============================================================

        /// <summary>
        /// 遍历已加载 AB 的 Holders 列表，自动移除已销毁的持有者（Unity null），
        /// 并减少引用计数。RefCount 归零时加入延迟卸载。
        /// 用途：防止开发者忘记手动 ReleaseRef 导致 AB 泄漏。
        /// </summary>
        public void CleanNullHolders()
        {
            List<string> toDelayUnload = new List<string>();

            foreach (var kvp in m_LoadedBundles)
            {
                ABRefItem item = kvp.Value;
                if (item.RefCount <= 0 || item.Holders.Count == 0)
                    continue;

                int nullCount = 0;
                for (int i = item.Holders.Count - 1; i >= 0; i--)
                {
                    if (item.Holders[i] == null)
                    {
                        item.Holders.RemoveAt(i);
                        nullCount++;
                    }
                }

                if (nullCount > 0)
                {
                    item.RefCount -= nullCount;
                    if (item.RefCount <= 0 && !item.IsPersistent)
                    {
                        item.RefCount = 0;
                        toDelayUnload.Add(kvp.Key);
                    }
                }
            }

            foreach (string name in toDelayUnload)
            {
                AddToDelayUnload(name);
            }
        }

        // ============================================================
        // 测试辅助方法
        // ============================================================

        /// <summary>设置平台加载器（用于测试注入 MockBundleLoader）</summary>
        public void SetLoaderForTesting(IBundleLoader loader)
        {
            m_Loader = loader;
        }

        /// <summary>设置依赖解析接口（用于测试注入 MockManifest）</summary>
        public void SetManifestForTesting(IBundleManifest manifest)
        {
            m_Manifest = manifest;
        }

        /// <summary>获取指定 AB 的引用追踪项（用于测试验证）</summary>
        public ABRefItem GetRefItem(string bundleName)
        {
            m_LoadedBundles.TryGetValue(bundleName, out ABRefItem item);
            return item;
        }

        /// <summary>获取延迟卸载队列中的项目数量（用于测试验证）</summary>
        public int DelayUnloadCount => m_DelayUnloadList.Count;

        /// <summary>设置延迟卸载秒数（测试时可设为 0 以立即过期）</summary>
        public void SetDelayUnloadSecondsForTesting(float seconds)
        {
            m_DelayUnloadSeconds = seconds;
        }

        /// <summary>添加常驻 AB（绕过 manifest 检查，用于测试设置持久 AB）</summary>
        public void AddPersistentBundleForTesting(string bundleName)
        {
            m_PersistentBundles.Add(bundleName);
        }
    }
}