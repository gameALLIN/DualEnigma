/// ============================================================
/// 文件名: UISpecGenerateWindow.cs
/// 创建时间: 2026-08-20
/// 作者: DualEnigma
/// 描述: ui-spec 生成器编辑器窗口。扫描 TechnicalDocs/Client/UIPrefab/*.html
///       列出可选页面，支持多选「生成预制体」与「干跑校验」（只解析验证、
///       不写资产）。菜单：DualEnigma > UI > 从设计稿生成预制体 /
///       校验设计稿。
/// 引用：UISpecExtractor.cs, UISpecValidator.cs, UISpecPrefabBuilder.cs
/// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DualEnigma.UI.Editor
{
    /// <summary>ui-spec 生成器窗口</summary>
    public sealed class UISpecGenerateWindow : EditorWindow
    {
        /// <summary>设计稿页面目录（相对仓库根的仓库内路径）</summary>
        private const string SPEC_DIR_RELATIVE = "TechnicalDocs/Client/UIPrefab/pages";

        private sealed class PageEntry
        {
            public string Name;     // 页面名（文件名去扩展名）
            public string HtmlPath; // 绝对路径
            public bool Selected = true;
        }

        private List<PageEntry> _pages;
        private Vector2 _pageScroll;
        private Vector2 _reportScroll;
        private string _report = "";

        // ================================================================
        //  菜单入口
        // ================================================================

        [MenuItem("DualEnigma/UI/从设计稿生成预制体")]
        public static void OpenWindow()
        {
            UISpecGenerateWindow window = GetWindow<UISpecGenerateWindow>("ui-spec 生成器");
            window.minSize = new Vector2(420, 360);
            window.RefreshPageList();
            window.Show();
        }

        [MenuItem("DualEnigma/UI/校验设计稿")]
        public static void ValidateAllMenu()
        {
            UISpecGenerateWindow window = GetWindow<UISpecGenerateWindow>("ui-spec 生成器");
            window.minSize = new Vector2(420, 360);
            window.RefreshPageList();
            window.ValidateAll();
            window.Show();
        }

        // ================================================================
        //  页面扫描
        // ================================================================

        private static string SpecDir =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", SPEC_DIR_RELATIVE));

        private void RefreshPageList()
        {
            _pages = new List<PageEntry>();
            string dir = SpecDir;
            if (!Directory.Exists(dir))
            {
                _report = "设计稿目录不存在: " + dir;
                return;
            }

            foreach (string file in Directory.GetFiles(dir, "*.html"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                // 只收含 ui-spec 的设计稿（pages/ 目录已无索引/编辑器页，保留防御性排除）
                if (name == "index" || name == "editor") continue;
                // 只收含 ui-spec 的设计稿
                string html = File.ReadAllText(file);
                if (!html.Contains("id=\"ui-spec\"")) continue;
                _pages.Add(new PageEntry { Name = name, HtmlPath = file });
            }
            _pages.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        // ================================================================
        //  GUI
        // ================================================================

        private void OnGUI()
        {
            if (_pages == null) RefreshPageList();

            EditorGUILayout.LabelField("设计稿页面（TechnicalDocs/Client/UIPrefab/pages）", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // 全选/反选
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(60))) SetAllSelected(true);
            if (GUILayout.Button("全不选", GUILayout.Width(60))) SetAllSelected(false);
            if (GUILayout.Button("刷新列表", GUILayout.Width(80))) RefreshPageList();
            EditorGUILayout.EndHorizontal();

            _pageScroll = EditorGUILayout.BeginScrollView(_pageScroll, GUILayout.Height(160));
            foreach (PageEntry page in _pages)
                page.Selected = EditorGUILayout.ToggleLeft("  " + page.Name, page.Selected);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("校验选中（干跑，不写资产）", GUILayout.Height(26)))
                ValidateSelected();
            if (GUILayout.Button("生成选中预制体", GUILayout.Height(26)))
                GenerateSelected();
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("校验全部页面", GUILayout.Height(22)))
                ValidateAll();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("结果", EditorStyles.boldLabel);
            _reportScroll = EditorGUILayout.BeginScrollView(_reportScroll);
            EditorGUILayout.TextArea(_report, EditorStyles.wordWrappedMiniLabel, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void SetAllSelected(bool selected)
        {
            foreach (PageEntry page in _pages) page.Selected = selected;
        }

        // ================================================================
        //  动作
        // ================================================================

        private void ValidateSelected() => RunOnSelected(validateOnly: true);
        private void GenerateSelected() => RunOnSelected(validateOnly: false);

        private void ValidateAll()
        {
            foreach (PageEntry page in _pages) page.Selected = true;
            RunOnSelected(validateOnly: true);
        }

        private void RunOnSelected(bool validateOnly)
        {
            List<string> lines = new List<string>();
            int ok = 0, failed = 0;

            foreach (PageEntry page in _pages)
            {
                if (!page.Selected) continue;
                try
                {
                    UISpecNode spec = UISpecExtractor.ExtractFromFile(page.HtmlPath);
                    UISpecValidationResult validation = UISpecValidator.Validate(spec, page.Name);

                    if (validateOnly)
                    {
                        validation.LogToConsole();
                        lines.Add($"{page.Name}: {(validation.HasErrors ? "✕ " + validation.ErrorCount + " 错误" : "✓ 通过")}" +
                                  (validation.WarningCount > 0 ? $"（{validation.WarningCount} 警告）" : ""));
                        if (validation.HasErrors) failed++; else ok++;
                    }
                    else
                    {
                        if (validation.HasErrors)
                        {
                            validation.LogToConsole();
                            lines.Add($"{page.Name}: ✕ 校验 {validation.ErrorCount} 个阻断错误，未生成");
                            failed++;
                            continue;
                        }
                        GameObject root = UISpecPrefabBuilder.BuildTree(spec, page.Name);
                        string path = UISpecPrefabBuilder.SavePrefab(root, page.Name, spec.name);
                        lines.Add($"{page.Name}: ✓ 已生成 {path}" +
                                  (validation.WarningCount > 0 ? $"（{validation.WarningCount} 警告）" : ""));
                        ok++;
                    }
                }
                catch (UISpecException e)
                {
                    Debug.LogError("[UISpec] " + e.Message);
                    lines.Add($"{page.Name}: ✕ {e.Message}");
                    failed++;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    lines.Add($"{page.Name}: ✕ 异常 — {e.Message}");
                    failed++;
                }
            }

            lines.Add("-----");
            lines.Add($"完成：成功 {ok} / 失败 {failed}（详细问题见 Console）");
            _report = string.Join("\n", lines);
            Repaint();
        }
    }
}
