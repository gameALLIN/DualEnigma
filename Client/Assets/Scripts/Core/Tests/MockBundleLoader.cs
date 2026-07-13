/// ============================================================
/// 文件名: MockBundleLoader.cs
/// 创建时间: 2026-07-11
/// 作者: DualEnigma
/// 描述: Mock IBundleLoader 实现，用于 AssetBundleMgr 单元测试。
///        不依赖真实 AB 文件，返回 null，仅追踪加载的 AB 名称。
/// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Core.Tests
{
    /// <summary>
    /// Mock AB 加载器，用于测试 AssetBundleMgr 的管理逻辑（引用计数、延迟卸载等）。
    /// 不依赖真实 AB 文件，LoadFromFile 返回 null。
    /// </summary>
    public class MockBundleLoader : IBundleLoader
    {
        /// <summary>已加载的 AB 名称列表（记录哪些 AB 被"加载"过）</summary>
        public List<string> LoadedBundleNames = new List<string>();

        public AssetBundle LoadFromFile(string bundleName)
        {
            LoadedBundleNames.Add(bundleName);
            // 返回 null：测试环境中不创建真实 AB 实例
            return null;
        }

        public IEnumerator LoadFromFileAsync(string bundleName, Action<AssetBundle> callback)
        {
            LoadedBundleNames.Add(bundleName);
            callback?.Invoke(null);
            yield return null;
        }
    }
}