/// ============================================================
/// 文件名: MockManifest.cs
/// 创建时间: 2026-07-11
/// 作者: DualEnigma
/// 描述: Mock IBundleManifest 实现，用于 AssetBundleMgr 单元测试。
///        可预配置依赖关系，不依赖真实 AssetBundleManifest 文件。
/// ============================================================

using System.Collections.Generic;

namespace DualEnigma.Core.Tests
{
    /// <summary>
    /// Mock 依赖解析器，用于测试 AssetBundleMgr 的依赖管理逻辑。
    /// 通过 SetDependencies 预配置 AB 之间的依赖关系。
    /// </summary>
    public class MockManifest : IBundleManifest
    {
        private Dictionary<string, string[]> m_Dependencies
            = new Dictionary<string, string[]>();

        /// <summary>
        /// 设置指定 AB 的依赖列表
        /// </summary>
        public void SetDependencies(string bundleName, string[] deps)
        {
            m_Dependencies[bundleName] = deps;
        }

        /// <summary>
        /// 获取指定 AB 的所有依赖。无配置时返回空数组。
        /// </summary>
        public string[] GetAllDependencies(string bundleName)
        {
            if (m_Dependencies.TryGetValue(bundleName, out string[] deps))
                return deps;
            return new string[0];
        }
    }
}