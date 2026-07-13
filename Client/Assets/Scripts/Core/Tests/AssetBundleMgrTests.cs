/// ============================================================
/// 文件名: AssetBundleMgrTests.cs
/// 创建时间: 2026-07-11
/// 作者: DualEnigma
/// 描述: AssetBundleMgr 单元测试 — 使用 MockBundleLoader 和 MockManifest
///        测试引用计数、延迟卸载、常驻AB、无效持有者清理、查询方法
/// ============================================================

using NUnit.Framework;
using UnityEngine;
using DualEnigma.Core;

namespace DualEnigma.Core.Tests
{
    [TestFixture]
    public class AssetBundleMgrTests
    {
        private AssetBundleMgr m_Mgr;
        private MockBundleLoader m_Loader;
        private MockManifest m_Manifest;

        [SetUp]
        public void SetUp()
        {
            // 确保单例存在
            m_Mgr = AssetBundleMgr.Instance;

            // 注入 Mock 加载器和依赖解析器
            m_Loader = new MockBundleLoader();
            m_Manifest = new MockManifest();
            m_Mgr.SetLoaderForTesting(m_Loader);
            m_Mgr.SetManifestForTesting(m_Manifest);
            m_Mgr.SetDelayUnloadSecondsForTesting(2f); // 重置默认延迟时间

            // 清空之前测试可能遗留的状态
            m_Mgr.UnloadAll();
        }

        [TearDown]
        public void TearDown()
        {
            // 清理状态
            m_Mgr.UnloadAll();

            // 销毁单例 GameObject
            if (AssetBundleMgr.HasInstance)
            {
                Object.DestroyImmediate(AssetBundleMgr.Instance.gameObject);
            }
        }

        // ================================================================
        // 1. 引用计数增减
        // ================================================================

        [Test]
        public void LoadBundle_FirstLoad_RefCount_Equals_One()
        {
            AssetBundle bundle = m_Mgr.LoadBundle("ui");
            ABRefItem item = m_Mgr.GetRefItem("ui");

            Assert.IsNotNull(item, "AB 应已加载");
            Assert.AreEqual(1, item.RefCount, "首次加载后 RefCount 应为 1");
            Assert.AreEqual("ui", item.BundleName);
        }

        [Test]
        public void LoadBundle_AlreadyLoaded_RefCount_Increments()
        {
            m_Mgr.LoadBundle("ui");       // RefCount: 0 → 1
            m_Mgr.LoadBundle("ui");       // RefCount: 1 → 2

            ABRefItem item = m_Mgr.GetRefItem("ui");
            Assert.AreEqual(2, item.RefCount,
                "已加载的 AB 再次 LoadBundle 应递增 RefCount");
        }

        [Test]
        public void AddRef_Increments_RefCount()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1

            var holder = new GameObject("TestHolder");
            m_Mgr.AddRef("ui", holder);                     // RefCount: 1 → 2

            ABRefItem item = m_Mgr.GetRefItem("ui");
            Assert.AreEqual(2, item.RefCount,
                "AddRef 应递增 RefCount");
            Assert.AreEqual(1, item.Holders.Count,
                "AddRef 应记录持有者");

            Object.DestroyImmediate(holder);
        }

        [Test]
        public void AddRef_Records_Holder()
        {
            m_Mgr.LoadBundle("character");

            var holderA = new GameObject("HolderA");
            var holderB = new GameObject("HolderB");
            m_Mgr.AddRef("character", holderA);
            m_Mgr.AddRef("character", holderB);

            ABRefItem item = m_Mgr.GetRefItem("character");
            Assert.AreEqual(3, item.RefCount);   // 1 (LoadBundle) + 2 (AddRef)
            Assert.AreEqual(2, item.Holders.Count);

            Object.DestroyImmediate(holderA);
            Object.DestroyImmediate(holderB);
        }

        [Test]
        public void ReleaseRef_Decrements_RefCount()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1
            var holder = new GameObject("TestHolder");
            m_Mgr.AddRef("ui", holder);                     // RefCount = 2
            m_Mgr.ReleaseRef("ui", holder);                 // RefCount: 2 → 1

            ABRefItem item = m_Mgr.GetRefItem("ui");
            Assert.AreEqual(1, item.RefCount,
                "ReleaseRef 应递减 RefCount");
            Assert.AreEqual(0, item.Holders.Count,
                "ReleaseRef 应移除持有者");

            Object.DestroyImmediate(holder);
        }

        [Test]
        public void ReleaseRef_Removes_Holder()
        {
            m_Mgr.LoadBundle("effect");
            var holder = new GameObject("EffectHolder");
            m_Mgr.AddRef("effect", holder);

            m_Mgr.ReleaseRef("effect", holder);

            ABRefItem item = m_Mgr.GetRefItem("effect");
            Assert.AreEqual(0, item.Holders.Count,
                "ReleaseRef 后 Holders 列表应不再包含该持有者");

            Object.DestroyImmediate(holder);
        }

        // ================================================================
        // 2. 延迟卸载机制
        // ================================================================

        [Test]
        public void ReleaseRef_ZeroRefCount_Adds_To_DelayUnload()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1
            m_Mgr.ReleaseRef("ui", null);                   // RefCount: 1 → 0

            ABRefItem item = m_Mgr.GetRefItem("ui");
            Assert.AreEqual(0, item.RefCount,
                "RefCount 归零后应为 0");
            Assert.IsTrue(m_Mgr.IsLoaded("ui"),
                "RefCount 归零后 AB 仍应在加载列表（延迟队列中）");
            Assert.Greater(m_Mgr.DelayUnloadCount, 0,
                "RefCount 归零应加入延迟卸载队列");
        }

        [Test]
        public void DelayedUnload_StaysLoaded_BeforeExpiry()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1
            m_Mgr.ReleaseRef("ui", null);                   // RefCount = 0 → 延迟队列
                                                              // UnloadTime = Time.time + 2
                                                              // Time.time = 0 → UnloadTime = 2

            Assert.IsTrue(m_Mgr.IsLoaded("ui"),
                "延迟时间未到时 AB 应仍在加载列表中");
            Assert.Greater(m_Mgr.DelayUnloadCount, 0);
        }

        [Test]
        public void DelayedUnload_Unloads_After_ProcessDelayedUnload_WithZeroDelay()
        {
            m_Mgr.SetDelayUnloadSecondsForTesting(0f);

            m_Mgr.LoadBundle("ui");                          // RefCount = 1
            m_Mgr.ReleaseRef("ui", null);                   // RefCount = 0
                                                              // UnloadTime = 0 + 0 = 0

            // 手动触发延迟卸载处理
            m_Mgr.ProcessDelayedUnload();

            Assert.IsFalse(m_Mgr.IsLoaded("ui"),
                "延迟到期后 AB 应从加载列表移除");
            Assert.AreEqual(0, m_Mgr.DelayUnloadCount,
                "队列应为空");
        }

        [Test]
        public void DelayedUnload_ReloadBeforeExpiry_CancelsUnload()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1
            m_Mgr.ReleaseRef("ui", null);                   // RefCount = 0 → 延迟队列

            // 延迟到期前重新加载
            m_Mgr.LoadBundle("ui");                          // RefCount: 0 → 1（从延迟队列恢复）

            ABRefItem item = m_Mgr.GetRefItem("ui");
            Assert.AreEqual(1, item.RefCount,
                "重新加载后 RefCount 应为 1");
            Assert.AreEqual(0, m_Mgr.DelayUnloadCount,
                "重新加载后延迟队列应移除该 AB");

            // 在 delay=0 模式下处理
            m_Mgr.SetDelayUnloadSecondsForTesting(0f);
            m_Mgr.ProcessDelayedUnload();

            Assert.IsTrue(m_Mgr.IsLoaded("ui"),
                "重新加载后 AB 应仍在加载列表中");
        }

        [Test]
        public void UnloadUnused_ImmediatelyUnloads_ZeroRefBundles()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1
            m_Mgr.LoadBundle("character");                   // RefCount = 1
            m_Mgr.ReleaseRef("ui", null);                   // RefCount = 0
            m_Mgr.ReleaseRef("character", null);            // RefCount = 0

            // 两者都在延迟队列中
            Assert.Greater(m_Mgr.DelayUnloadCount, 0);

            // 立即卸载
            m_Mgr.UnloadUnused();

            Assert.IsFalse(m_Mgr.IsLoaded("ui"),
                "UnloadUnused 应立即卸载 RefCount=0 的 AB");
            Assert.IsFalse(m_Mgr.IsLoaded("character"));
            Assert.AreEqual(0, m_Mgr.DelayUnloadCount);
        }

        // ================================================================
        // 3. 常驻 AB 不被卸载
        // ================================================================

        [Test]
        public void SetPersistent_Prevents_Unload()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1
            m_Mgr.SetPersistent("ui");
            m_Mgr.ReleaseRef("ui", null);                   // RefCount = 0

            // 即使 RefCount 为 0，常驻 AB 也不应加入延迟队列
            Assert.IsTrue(m_Mgr.IsLoaded("ui"),
                "常驻 AB 的 RefCount 归零后仍应在加载列表");
            Assert.AreEqual(0, m_Mgr.DelayUnloadCount,
                "常驻 AB 不应加入延迟卸载队列");
        }

        [Test]
        public void SetPersistent_AfterRelease_RemovesFromDelayQueue()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1
            m_Mgr.ReleaseRef("ui", null);                   // RefCount = 0 → 延迟队列

            Assert.Greater(m_Mgr.DelayUnloadCount, 0);

            m_Mgr.SetPersistent("ui");                      // 标记常驻

            Assert.AreEqual(0, m_Mgr.DelayUnloadCount,
                "标记常驻后应从延迟队列中移除");

            // UnloadUnused 也不应影响常驻 AB
            m_Mgr.UnloadUnused();
            Assert.IsTrue(m_Mgr.IsLoaded("ui"));
        }

        [Test]
        public void UnloadUnused_DoesNotAffect_PersistentBundle()
        {
            m_Mgr.LoadBundle("ui");
            m_Mgr.LoadBundle("character");
            m_Mgr.SetPersistent("ui");

            m_Mgr.ReleaseRef("ui", null);                    // RefCount = 0, 但常驻
            m_Mgr.ReleaseRef("character", null);             // RefCount = 0, 非常驻

            m_Mgr.UnloadUnused();

            Assert.IsTrue(m_Mgr.IsLoaded("ui"),
                "常驻 AB 不应被 UnloadUnused 卸载");
            Assert.IsFalse(m_Mgr.IsLoaded("character"),
                "非常驻 AB 应被 UnloadUnused 卸载");
        }

        [Test]
        public void UnloadAll_Unloads_PersistentBundles()
        {
            m_Mgr.LoadBundle("ui");
            m_Mgr.SetPersistent("ui");

            m_Mgr.UnloadAll();

            Assert.IsFalse(m_Mgr.IsLoaded("ui"),
                "UnloadAll 应卸载包括常驻在内的所有 AB");
        }

        // ================================================================
        // 4. 无效持有者自动清理
        // ================================================================

        [Test]
        public void CleanNullHolders_Removes_DestroyedHolders()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1

            var holder = new GameObject("TestHolder");
            m_Mgr.AddRef("ui", holder);                     // RefCount = 2

            ABRefItem item = m_Mgr.GetRefItem("ui");
            Assert.AreEqual(2, item.RefCount);
            Assert.AreEqual(1, item.Holders.Count);

            // 销毁持有者
            Object.DestroyImmediate(holder);

            // Unity null 检查：销毁后 holder == null 为 true
            Assert.IsTrue(holder == null,
                "销毁后 Unity Object 应为 null");

            // 清理无效持有者
            m_Mgr.CleanNullHolders();

            item = m_Mgr.GetRefItem("ui");
            Assert.AreEqual(1, item.RefCount,
                "清理后应从 RefCount 中减去无效持有者");
            Assert.AreEqual(0, item.Holders.Count,
                "清理后 Holders 列表应不再包含 null 对象");
        }

        [Test]
        public void CleanNullHolders_ZeroRefCount_AddsToDelayUnload()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1

            var holder = new GameObject("TestHolder");
            m_Mgr.AddRef("ui", holder);                     // RefCount = 2

            // 销毁持有者
            Object.DestroyImmediate(holder);

            // 同时手动 ReleaseRef → RefCount = 1
            m_Mgr.ReleaseRef("ui", null);

            ABRefItem item = m_Mgr.GetRefItem("ui");
            Assert.AreEqual(1, item.RefCount,
                "手动 ReleaseRef + 未清理的无效持有者");

            // 清理无效持有者 → RefCount 从 1 降到 0
            m_Mgr.CleanNullHolders();

            item = m_Mgr.GetRefItem("ui");
            Assert.AreEqual(0, item.RefCount,
                "清理无效持有者后 RefCount 应为 0");

            // 应加入延迟卸载队列
            Assert.Greater(m_Mgr.DelayUnloadCount, 0,
                "RefCount 归零后应加入延迟卸载队列");
        }

        [Test]
        public void CleanNullHolders_MultipleNulls_DecrementsCorrectly()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1

            var holder1 = new GameObject("H1");
            var holder2 = new GameObject("H2");
            var holder3 = new GameObject("H3");
            m_Mgr.AddRef("ui", holder1);                    // RefCount = 2
            m_Mgr.AddRef("ui", holder2);                    // RefCount = 3
            m_Mgr.AddRef("ui", holder3);                    // RefCount = 4

            // 销毁全部持有者
            Object.DestroyImmediate(holder1);
            Object.DestroyImmediate(holder2);
            Object.DestroyImmediate(holder3);

            m_Mgr.CleanNullHolders();

            ABRefItem item = m_Mgr.GetRefItem("ui");
            Assert.AreEqual(1, item.RefCount,
                "清理 3 个无效持有者后 RefCount 应减至 1（仅剩 LoadBundle 的引用）");
            Assert.AreEqual(0, item.Holders.Count);
        }

        // ================================================================
        // 5. IsLoaded / IsAllDependenciesLoaded 查询
        // ================================================================

        [Test]
        public void IsLoaded_ReturnsFalse_ForNotLoadedBundle()
        {
            Assert.IsFalse(m_Mgr.IsLoaded("nonexistent"),
                "未加载的 AB 应返回 false");
        }

        [Test]
        public void IsLoaded_ReturnsTrue_ForLoadedBundle()
        {
            m_Mgr.LoadBundle("ui");
            Assert.IsTrue(m_Mgr.IsLoaded("ui"));
        }

        [Test]
        public void IsAllDependenciesLoaded_AllLoaded_ReturnsTrue()
        {
            // 配置依赖：character → [atlas, effect]
            m_Manifest.SetDependencies("character",
                new[] { "atlas", "effect" });

            m_Mgr.LoadBundle("atlas");
            m_Mgr.LoadBundle("effect");
            m_Mgr.LoadBundle("character"); // character 依赖 atlas, effect

            Assert.IsTrue(m_Mgr.IsAllDependenciesLoaded("character"),
                "所有 AB（含依赖）均已加载时应返回 true");
        }

        [Test]
        public void IsAllDependenciesLoaded_MissingDependency_ReturnsFalse()
        {
            // 配置依赖
            m_Manifest.SetDependencies("character",
                new[] { "atlas", "effect" });

            m_Mgr.LoadBundle("atlas");
            m_Mgr.LoadBundle("character"); // effect 未加载

            Assert.IsFalse(m_Mgr.IsAllDependenciesLoaded("character"),
                "依赖 AB 未完全加载时应返回 false");
        }

        [Test]
        public void IsAllDependenciesLoaded_NoDependencies_ReturnsTrue()
        {
            // ui 没有配置依赖
            m_Mgr.LoadBundle("ui");

            Assert.IsTrue(m_Mgr.IsAllDependenciesLoaded("ui"),
                "无依赖的 AB 加载后应返回 true");
        }

        // ================================================================
        // 6. GetLoadedBundleNames
        // ================================================================

        [Test]
        public void GetLoadedBundleNames_Returns_CorrectList()
        {
            Assert.AreEqual(0, m_Mgr.GetLoadedBundleNames().Count,
                "初始时应为空");

            m_Mgr.LoadBundle("ui");
            m_Mgr.LoadBundle("character");
            m_Mgr.LoadBundle("effect");

            var names = m_Mgr.GetLoadedBundleNames();
            Assert.AreEqual(3, names.Count);
            Assert.Contains("ui", names);
            Assert.Contains("character", names);
            Assert.Contains("effect", names);
        }

        [Test]
        public void GetLoadedBundleNames_AfterUnloadUnused_RemovesZeroRef()
        {
            m_Mgr.LoadBundle("ui");
            m_Mgr.LoadBundle("character");
            m_Mgr.ReleaseRef("character", null);            // character → 延迟卸载

            m_Mgr.UnloadUnused();

            var names = m_Mgr.GetLoadedBundleNames();
            Assert.AreEqual(1, names.Count,
                "UnloadUnused 后应只剩 RefCount>0 的 AB");

            Assert.Contains("ui", names);
            Assert.IsFalse(names.Contains("character"),
                "character 已被卸载，不应在列表中");
        }

        // ================================================================
        // 7. 边界情况
        // ================================================================

        [Test]
        public void LoadBundle_NullBundleName_ReturnsNull()
        {
            AssetBundle bundle = m_Mgr.LoadBundle(null);
            Assert.IsNull(bundle, "空名称应返回 null");
        }

        [Test]
        public void LoadBundle_EmptyBundleName_ReturnsNull()
        {
            AssetBundle bundle = m_Mgr.LoadBundle("");
            Assert.IsNull(bundle, "空字符串名称应返回 null");
        }

        [Test]
        public void ReleaseRef_NegativeRefCount_ClampedToZero()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1
            m_Mgr.ReleaseRef("ui", null);                   // RefCount = 0
            m_Mgr.ReleaseRef("ui", null);                   // RefCount 保持在 0

            ABRefItem item = m_Mgr.GetRefItem("ui");
            Assert.AreEqual(0, item.RefCount,
                "RefCount 应保持在 0，不会变为负数");
        }

        [Test]
        public void AddRef_WhileInDelayQueue_RemovesFromQueue()
        {
            m_Mgr.LoadBundle("ui");                          // RefCount = 1
            m_Mgr.ReleaseRef("ui", null);                   // RefCount = 0 → 延迟队列

            Assert.Greater(m_Mgr.DelayUnloadCount, 0);

            // 延迟卸载前重新 AddRef
            m_Mgr.AddRef("ui", null);                       // RefCount: 0 → 1

            Assert.AreEqual(0, m_Mgr.DelayUnloadCount,
                "重新 AddRef 后应从延迟队列移除");
        }

        [Test]
        public void DependencyRefCount_Incremented_OnLoadBundle()
        {
            m_Manifest.SetDependencies("character",
                new[] { "atlas", "effect" });

            m_Mgr.LoadBundle("character");  // character → 加载 atlas + effect 依赖

            // character 依赖 atlas, effect
            ABRefItem atlasItem = m_Mgr.GetRefItem("atlas");
            ABRefItem effectItem = m_Mgr.GetRefItem("effect");

            Assert.IsNotNull(atlasItem, "atlas 应被加载");
            Assert.IsNotNull(effectItem, "effect 应被加载");
            Assert.AreEqual(1, atlasItem.RefCount,
                "依赖 AB 的 RefCount 应为 1（被 character 引用）");
            Assert.AreEqual(1, effectItem.RefCount);
        }

        [Test]
        public void DependencyRefCount_Decremented_OnReleaseRef()
        {
            m_Manifest.SetDependencies("character",
                new[] { "atlas" });

            m_Mgr.LoadBundle("character");                  // atlas.RefCount = 1
            m_Mgr.ReleaseRef("character", null);            // character.RefCount = 0
                                                              // atlas.RefCount: 1 → 0

            ABRefItem atlasItem = m_Mgr.GetRefItem("atlas");
            Assert.AreEqual(0, atlasItem.RefCount,
                "依赖 AB 的 RefCount 应随主 AB 归零而递减");
        }

        [Test]
        public void SharedDependency_RefCount_TracksMultipleConsumers()
        {
            m_Manifest.SetDependencies("character",
                new[] { "atlas" });
            m_Manifest.SetDependencies("effect",
                new[] { "atlas" });

            m_Mgr.LoadBundle("character");                  // atlas.RefCount = 1
            m_Mgr.LoadBundle("effect");                     // atlas.RefCount: 1 → 2

            ABRefItem atlasItem = m_Mgr.GetRefItem("atlas");
            Assert.AreEqual(2, atlasItem.RefCount,
                "两个消费者共享一个依赖，RefCount 应为 2");

            m_Mgr.ReleaseRef("character", null);            // atlas.RefCount: 2 → 1

            atlasItem = m_Mgr.GetRefItem("atlas");
            Assert.AreEqual(1, atlasItem.RefCount,
                "释放一个消费者后 RefCount 应为 1");
        }

        [Test]
        public void DependencyRefCount_Restored_OnReloadFromDelayQueue()
        {
            m_Manifest.SetDependencies("character",
                new[] { "atlas" });

            m_Mgr.LoadBundle("character");                  // atlas.RefCount = 1
            m_Mgr.ReleaseRef("character", null);            // character.RefCount = 0
                                                              // atlas.RefCount: 1 → 0

            // 两者都在延迟队列中

            // 重新加载 character
            m_Mgr.LoadBundle("character");                  // character.RefCount: 0 → 1
                                                              // atlas.RefCount 应恢复

            ABRefItem atlasItem = m_Mgr.GetRefItem("atlas");
            Assert.AreEqual(1, atlasItem.RefCount,
                "重新加载主 AB 后依赖的 RefCount 应恢复到 1");
        }
    }
}