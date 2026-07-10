/// ============================================================
/// 文件名: ResMgr.cs
/// 创建时间: 2026-07-10
/// 作者: DualEnigma
/// 描述: 资源管理器，基于 AssetDatabase 加载，后续可替换为 AssetGraph 打包方案
/// ============================================================

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using DualEnigma.Core;

namespace DualEnigma.Core
{
    /// <summary>
    /// 资源管理器。当前阶段使用 AssetDatabase 直接加载，
    /// 后续接入 AssetGraph 打包后只需替换内部实现，外部接口不变。
    /// </summary>
    public class ResMgr : Singleton<ResMgr>
    {
        /// <summary>
        /// 资源根路径（相对于 Assets 目录）
        /// </summary>
        private const string ASSET_ROOT = "AssetPackage/";

        protected override void OnSingletonInitialized()
        {
            Debug.Log("[ResMgr] 资源管理器初始化完成");
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型（GameObject、Sprite、AudioClip 等）</typeparam>
        /// <param name="path">相对路径，如 "Prefabs/UI/UITest/UITest"</param>
        /// <returns>资源对象，加载失败返回 null</returns>
        public T Load<T>(string path) where T : Object
        {
            string fullPath = ASSET_ROOT + path;

#if UNITY_EDITOR
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
            Debug.LogError("[ResMgr] 当前为运行时环境，AssetDatabase 不可用，请接入 AssetGraph 打包方案");
            return null;
#endif
        }

        /// <summary>
        /// 加载 GameObject 预制体
        /// </summary>
        /// <param name="path">相对路径，如 "Prefabs/UI/UITest/UITest"</param>
        /// <returns>GameObject 预制体</returns>
        public GameObject LoadPrefab(string path)
        {
            if (!path.EndsWith(".prefab"))
            {
                path += ".prefab";
            }
            return Load<GameObject>(path);
        }

        /// <summary>
        /// 异步加载资源（当前阶段内部仍为同步，接口预留）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">相对路径</param>
        /// <param name="onComplete">加载完成回调</param>
        public void LoadAsync<T>(string path, System.Action<T> onComplete) where T : Object
        {
            T asset = Load<T>(path);
            onComplete?.Invoke(asset);
        }

        /// <summary>
        /// 异步加载预制体（当前阶段内部仍为同步，接口预留）
        /// </summary>
        /// <param name="path">相对路径</param>
        /// <param name="onComplete">加载完成回调</param>
        public void LoadPrefabAsync(string path, System.Action<GameObject> onComplete)
        {
            GameObject prefab = LoadPrefab(path);
            onComplete?.Invoke(prefab);
        }
    }
}
