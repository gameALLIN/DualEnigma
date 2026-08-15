/// ============================================================
/// 文件名: BuildTool.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: Windows 客户端打包工具。
///       一键完成: 打 AssetBundle → 复制场景 → 打包 exe。
///       AB 按路径前缀分组（与 ResMgr.s_PathToBundle 映射一致），
///       输出到 StreamingAssets/AssetBundles/Windows，随 exe 一起发布。
///       菜单：DualEnigma/Build/打包 Windows 客户端
///       命令行：Unity.exe -batchmode -quit -executeMethod
///               DualEnigma.Editor.BuildTool.BuildWindowsCI
/// 引用：ResMgr.cs（路径映射）, AssetBundleMgr.cs（运行时加载约定）
/// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DualEnigma.Editor
{
    /// <summary>
    /// Windows 打包工具。
    /// 打包顺序必须为: AB → Player。
    /// 原因: Runtime 模式下 ResMgr 走 AssetBundleMgr 从
    /// StreamingAssets/AssetBundles/Windows 加载资源，
    /// 若 AB 缺失则 UI 预制体、配置全部加载失败（白屏）。
    /// </summary>
    public static class BuildTool
    {
        // ===== 路径常量 =====

        /// <summary>AssetPackage 资源根目录</summary>
        private const string ASSET_PACKAGE_ROOT = "Assets/AssetPackage";

        /// <summary>AB 输出目录（必须在 StreamingAssets 内，随包发布）</summary>
        private const string AB_OUTPUT_DIR = "Assets/StreamingAssets/AssetBundles/Windows";

        /// <summary>exe 输出目录（相对工程根，已被 .gitignore 忽略）</summary>
        private const string PLAYER_OUTPUT_DIR = "Builds/Windows";

        /// <summary>exe 文件名</summary>
        private const string EXE_NAME = "DualEnigma.exe";

        /// <summary>主场景路径</summary>
        private const string MAIN_SCENE = "Assets/Scenes/Main.unity";

        /// <summary>
        /// 路径前缀 → Bundle 名映射。
        /// 必须与 ResMgr.s_PathToBundle 保持一致，否则运行时找不到资源。
        /// </summary>
        private static readonly (string prefix, string bundle)[] s_BundleMapping =
        {
            ("Prefabs/UI",         "ui"),
            ("Prefabs/Characters", "character"),
            ("Prefabs/Effects",    "effect"),
            ("Atlases",            "atlas"),
            ("Audio",              "audio"),
            ("Data",               "data"),
        };

        // ================================================================
        //  菜单入口
        // ================================================================

        /// <summary>
        /// 菜单入口：打包 Windows 客户端（AB + exe）。
        /// </summary>
        [MenuItem("DualEnigma/Build/打包 Windows 客户端")]
        public static void BuildWindows()
        {
            bool ok = RunFullBuild(out string message);
            if (ok)
            {
                Debug.Log($"[BuildTool] 打包成功: {message}");
                EditorUtility.RevealInFinder(message);
            }
            else
            {
                Debug.LogError($"[BuildTool] 打包失败: {message}");
                EditorUtility.DisplayDialog("打包失败", message, "确定");
            }
        }

        /// <summary>
        /// 命令行入口（-executeMethod 用）。成功返回码 0，失败返回码 1。
        /// </summary>
        public static void BuildWindowsCI()
        {
            bool ok = RunFullBuild(out string message);
            Debug.Log(ok ? $"[BuildTool] CI 打包成功: {message}" : $"[BuildTool] CI 打包失败: {message}");
            EditorApplication.Exit(ok ? 0 : 1);
        }

        // ================================================================
        //  构建流程
        // ================================================================

        /// <summary>
        /// 完整打包流程：AB 构建 → 场景设置 → Player 构建。
        /// </summary>
        /// <param name="message">成功时为 exe 路径，失败时为错误信息</param>
        private static bool RunFullBuild(out string message)
        {
            DateTime start = DateTime.Now;

            try
            {
                // 1. 构建 AssetBundle（Runtime 资源加载的唯一来源）
                if (!BuildAssetBundles(out string abError))
                {
                    message = $"AB 构建失败: {abError}";
                    return false;
                }

                // 2. 确保主场景在 Build Settings 中
                if (!EnsureSceneInBuildSettings())
                {
                    message = $"主场景不存在: {MAIN_SCENE}";
                    return false;
                }

                // 3. 构建 Player
                string exePath = Path.Combine(PLAYER_OUTPUT_DIR, EXE_NAME);
                var options = new BuildPlayerOptions
                {
                    scenes = GetEnabledScenes(),
                    locationPathName = exePath,
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None,
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);

                if (report.summary.result != BuildResult.Succeeded)
                {
                    message = $"Player 构建结果: {report.summary.result}, 错误数: {report.summary.totalErrors}";
                    return false;
                }

                string fullPath = Path.GetFullPath(exePath);
                double sizeMB = report.summary.totalSize / 1024.0 / 1024.0;
                double seconds = (DateTime.Now - start).TotalSeconds;
                message = $"{fullPath} ({sizeMB:F1} MB, 耗时 {seconds:F0}s)";
                return true;
            }
            catch (Exception e)
            {
                message = $"异常: {e.Message}\n{e.StackTrace}";
                return false;
            }
        }

        // ================================================================
        //  AssetBundle 构建
        // ================================================================

        /// <summary>
        /// 按路径前缀分组收集 AssetPackage 资源并构建 AB。
        /// 输出目录名 Windows 即 Manifest 总包名（运行时按平台名加载）。
        /// </summary>
        private static bool BuildAssetBundles(out string error)
        {
            error = null;

            if (!Directory.Exists(AB_OUTPUT_DIR))
            {
                Directory.CreateDirectory(AB_OUTPUT_DIR);
            }

            var builds = new List<AssetBundleBuild>();

            foreach (var (prefix, bundleName) in s_BundleMapping)
            {
                string folder = $"{ASSET_PACKAGE_ROOT}/{prefix}";

                if (!Directory.Exists(folder))
                {
                    Debug.Log($"[BuildTool] 跳过不存在的目录: {folder}");
                    continue;
                }

                // 收集目录下所有资产（排除文件夹本身）
                string[] guids = AssetDatabase.FindAssets("", new[] { folder });
                var assetPaths = new List<string>();

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(DefaultAsset))
                        continue; // 文件夹或无类型资产
                    assetPaths.Add(path);
                }

                if (assetPaths.Count == 0)
                {
                    Debug.Log($"[BuildTool] 跳过空目录: {folder}");
                    continue;
                }

                builds.Add(new AssetBundleBuild
                {
                    assetBundleName = bundleName,
                    assetNames = assetPaths.ToArray(),
                });

                Debug.Log($"[BuildTool] Bundle[{bundleName}] 收集 {assetPaths.Count} 个资产 ({prefix})");
            }

            if (builds.Count == 0)
            {
                error = "未收集到任何资产，请检查 AssetPackage 目录结构";
                return false;
            }

            var manifest = BuildPipeline.BuildAssetBundles(
                AB_OUTPUT_DIR,
                builds.ToArray(),
                BuildAssetBundleOptions.ChunkBasedCompression, // LZ4：加载快
                BuildTarget.StandaloneWindows64);

            if (manifest == null)
            {
                error = "BuildAssetBundles 返回 null，详见 Console 错误";
                return false;
            }

            // 运行时 StandaloneBundleLoader 按 {bundleName}.bundle 查找文件，
            // 而 BuildAssetBundles 产物无扩展名，统一重命名补齐（含以目录名命名的 Manifest 总包）
            List<string> bundleFileNames = builds
                .Select(b => b.assetBundleName)
                .ToList();
            bundleFileNames.Add(Path.GetFileName(AB_OUTPUT_DIR.TrimEnd('/'))); // "Windows"

            foreach (string name in bundleFileNames)
            {
                string src = Path.Combine(AB_OUTPUT_DIR, name);
                string dst = src + ".bundle";
                if (File.Exists(src))
                {
                    if (File.Exists(dst))
                        File.Delete(dst);
                    File.Move(src, dst);
                }
            }

            // 清理编辑器用的 *.manifest 文本（运行时不需要，减小包体）
            foreach (string manifestFile in Directory.GetFiles(AB_OUTPUT_DIR, "*.manifest"))
                File.Delete(manifestFile);

            AssetDatabase.Refresh();
            Debug.Log($"[BuildTool] AB 构建完成: {builds.Count} 个 Bundle → {AB_OUTPUT_DIR}");
            return true;
        }

        // ================================================================
        //  场景管理
        // ================================================================

        /// <summary>
        /// 确保主场景在 Build Settings 且启用。不存在返回 false。
        /// </summary>
        private static bool EnsureSceneInBuildSettings()
        {
            if (File.Exists(MAIN_SCENE) == false)
                return false;

            bool already = EditorBuildSettings.scenes
                .Any(s => s.path == MAIN_SCENE && s.enabled);

            if (!already)
            {
                var scenes = EditorBuildSettings.scenes
                    .Where(s => !string.IsNullOrEmpty(s.path))
                    .ToList();
                scenes.Insert(0, new EditorBuildSettingsScene(MAIN_SCENE, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log($"[BuildTool] 已将主场景加入 Build Settings: {MAIN_SCENE}");
            }

            return true;
        }

        /// <summary>获取 Build Settings 中启用的场景路径数组</summary>
        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToArray();
        }
    }
}
