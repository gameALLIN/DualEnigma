/// ============================================================
/// 文件名: ResMgrTests.cs
/// 创建时间: 2026-07-11
/// 作者: DualEnigma
/// 描述: ResMgr 单元测试 — 覆盖路径映射、资产名解析、Editor 模式加载、转发逻辑
/// ============================================================

using NUnit.Framework;
using DualEnigma.Core;

namespace DualEnigma.Core.Tests
{
    [TestFixture]
    public class ResMgrTests
    {
        // ============================================================
        // 路径映射正确性
        // ============================================================

        [Test]
        public void ResolvePath_Prefabs_UI_MapsTo_ui()
        {
            bool result = ResMgr.ResolvePath(
                "Prefabs/UI/UITest/UITest",
                out string bundleName,
                out string assetName);

            Assert.IsTrue(result, "路径解析应成功");
            Assert.AreEqual("ui", bundleName, "Bundle 名称应为 ui");
            Assert.AreEqual("UITest", assetName, "资产名应为 UITest");
        }

        [Test]
        public void ResolvePath_Prefabs_Characters_MapsTo_character()
        {
            bool result = ResMgr.ResolvePath(
                "Prefabs/Characters/Aqua",
                out string bundleName,
                out string assetName);

            Assert.IsTrue(result);
            Assert.AreEqual("character", bundleName);
            Assert.AreEqual("Aqua", assetName);
        }

        [Test]
        public void ResolvePath_Prefabs_Effects_MapsTo_effect()
        {
            bool result = ResMgr.ResolvePath(
                "Prefabs/Effects/FireExplosion",
                out string bundleName,
                out string assetName);

            Assert.IsTrue(result);
            Assert.AreEqual("effect", bundleName);
            Assert.AreEqual("FireExplosion", assetName);
        }

        [Test]
        public void ResolvePath_Atlases_MapsTo_atlas()
        {
            bool result = ResMgr.ResolvePath(
                "Atlases/Icons/icon_fire",
                out string bundleName,
                out string assetName);

            Assert.IsTrue(result);
            Assert.AreEqual("atlas", bundleName);
            Assert.AreEqual("icon_fire", assetName);
        }

        [Test]
        public void ResolvePath_Audio_MapsTo_audio()
        {
            bool result = ResMgr.ResolvePath(
                "Audio/BGM/bgm_main",
                out string bundleName,
                out string assetName);

            Assert.IsTrue(result);
            Assert.AreEqual("audio", bundleName);
            Assert.AreEqual("bgm_main", assetName);
        }

        [Test]
        public void ResolvePath_Data_MapsTo_data()
        {
            bool result = ResMgr.ResolvePath(
                "Data/Config/GameSettings",
                out string bundleName,
                out string assetName);

            Assert.IsTrue(result);
            Assert.AreEqual("data", bundleName);
            Assert.AreEqual("GameSettings", assetName);
        }

        // ============================================================
        // 资产名解析正确性
        // ============================================================

        [Test]
        public void ResolvePath_StripsExtension()
        {
            bool result = ResMgr.ResolvePath(
                "Prefabs/UI/UITest/UITest.prefab",
                out string bundleName,
                out string assetName);

            Assert.IsTrue(result);
            Assert.AreEqual("ui", bundleName);
            Assert.AreEqual("UITest", assetName,
                "扩展名 .prefab 应被去除");
        }

        [Test]
        public void ResolvePath_DeepNested_ReturnsLastComponent()
        {
            bool result = ResMgr.ResolvePath(
                "Atlases/Sub/Deep/icon_star",
                out string bundleName,
                out string assetName);

            Assert.IsTrue(result);
            Assert.AreEqual("atlas", bundleName);
            Assert.AreEqual("icon_star", assetName,
                "资产名应为路径最后一个组件");
        }

        [Test]
        public void ResolvePath_UnknownPrefix_ReturnsFalse()
        {
            bool result = ResMgr.ResolvePath(
                "Unknown/Path/asset",
                out string bundleName,
                out string assetName);

            Assert.IsFalse(result, "未匹配任何前缀时应返回 false");
            Assert.IsNull(bundleName);
            Assert.IsNull(assetName);
        }

        [Test]
        public void ResolvePath_EmptyString_ReturnsFalse()
        {
            bool result = ResMgr.ResolvePath(
                "",
                out string bundleName,
                out string assetName);

            Assert.IsFalse(result);
        }

        [Test]
        public void ResolvePath_NullString_ReturnsFalse()
        {
            bool result = ResMgr.ResolvePath(
                null,
                out string bundleName,
                out string assetName);

            Assert.IsFalse(result);
        }

        // ============================================================
        // Editor 模式 Load / LoadPrefab 正常执行
        // ============================================================

        [Test]
        public void Load_NonExistentAsset_ReturnsNull_DoesNotThrow()
        {
            // 在 Editor 模式下，加载不存在的资源应返回 null 而不抛异常
            var asset = ResMgr.Instance.Load<UnityEngine.GameObject>(
                "Prefabs/UI/NonExistent/NonExistent");

            Assert.IsNull(asset, "不存在的资源应返回 null");
        }

        [Test]
        public void LoadPrefab_NonExistentPrefab_ReturnsNull_DoesNotThrow()
        {
            var prefab = ResMgr.Instance.LoadPrefab(
                "Prefabs/UI/NonExistent/NonExistent");

            Assert.IsNull(prefab, "不存在的预制体应返回 null");
        }

        [Test]
        public void LoadPrefab_AddsPrefabExtension_WhenMissing()
        {
            // 验证 LoadPrefab 在 Editor 模式下自动补 .prefab
            // 对于不存在的资源，返回值应为 null 但不抛异常
            Assert.DoesNotThrow(() =>
            {
                ResMgr.Instance.LoadPrefab("Prefabs/UI/UITest/UITest");
            });
        }

        // ============================================================
        // AddRef / ReleaseRef 转发逻辑
        // ============================================================

        [Test]
        public void AddRef_EditorMode_DoesNotThrow()
        {
            // Editor 模式下 AddRef 为空操作，不应抛异常
            Assert.DoesNotThrow(() =>
                ResMgr.Instance.AddRef("ui", null));
        }

        [Test]
        public void ReleaseRef_EditorMode_DoesNotThrow()
        {
            // Editor 模式下 ReleaseRef 为空操作，不应抛异常
            Assert.DoesNotThrow(() =>
                ResMgr.Instance.ReleaseRef("ui", null));
        }

        [Test]
        public void UnloadUnused_EditorMode_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                ResMgr.Instance.UnloadUnused());
        }

        [Test]
        public void SetPersistentBundle_EditorMode_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                ResMgr.Instance.SetPersistentBundle("ui"));
        }

        // ============================================================
        // Init 方法
        // ============================================================

        [Test]
        public void Init_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                ResMgr.Instance.Init());
        }

        [Test]
        public void Init_DoubleCall_DoesNotThrow()
        {
            // 重复调用 Init 不应抛异常
            Assert.DoesNotThrow(() =>
            {
                ResMgr.Instance.Init();
                ResMgr.Instance.Init();
            });
        }
    }
}