/// ============================================================
/// 文件名: UISpecValidator.cs
/// 创建时间: 2026-08-20
/// 作者: DualEnigma
/// 描述: ui-spec 干跑校验（《通用JSON预制体生成器》§5.4）。
///       只解析与验证、不写资产：组件名合法性、约定子节点
///       （ScrollRect 缺 Viewport 等）、脚本类型可解析性、
///       绑定命名规范与字段可匹配性。错误列表输出到控制台。
/// 引用：UISpecNode.cs, IComponentBuilder.cs, ComponentBuilders.cs,
///       UISpecViewBinder.cs
/// ============================================================

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DualEnigma.UI.Editor
{
    /// <summary>校验问题严重级</summary>
    public enum UISpecIssueLevel
    {
        /// <summary>警告：不阻断生成（如绑定命名不规范、字段不可匹配）</summary>
        Warning,

        /// <summary>阻断错误：组件名未知 / 约定子节点缺失 / 脚本类型不可解析</summary>
        Error,
    }

    /// <summary>单条校验问题</summary>
    public struct UISpecIssue
    {
        public UISpecIssueLevel Level;
        public string NodePath;
        public string Message;

        public override string ToString() =>
            $"[{(Level == UISpecIssueLevel.Error ? "错误" : "警告")}] {NodePath}: {Message}";
    }

    /// <summary>校验结果集</summary>
    public sealed class UISpecValidationResult
    {
        public readonly List<UISpecIssue> Issues = new List<UISpecIssue>();

        public bool HasErrors
        {
            get
            {
                foreach (UISpecIssue i in Issues)
                    if (i.Level == UISpecIssueLevel.Error) return true;
                return false;
            }
        }

        public int ErrorCount
        {
            get
            {
                int n = 0;
                foreach (UISpecIssue i in Issues)
                    if (i.Level == UISpecIssueLevel.Error) n++;
                return n;
            }
        }

        public int WarningCount
        {
            get
            {
                int n = 0;
                foreach (UISpecIssue i in Issues)
                    if (i.Level == UISpecIssueLevel.Warning) n++;
                return n;
            }
        }

        public void Error(string path, string message) =>
            Issues.Add(new UISpecIssue { Level = UISpecIssueLevel.Error, NodePath = path, Message = message });

        public void Warning(string path, string message) =>
            Issues.Add(new UISpecIssue { Level = UISpecIssueLevel.Warning, NodePath = path, Message = message });

        /// <summary>输出到 Unity 控制台</summary>
        public void LogToConsole()
        {
            foreach (UISpecIssue issue in Issues)
            {
                if (issue.Level == UISpecIssueLevel.Error)
                    Debug.LogError("[UISpec] " + issue);
                else
                    Debug.LogWarning("[UISpec] " + issue);
            }
        }

        /// <summary>汇总文本（窗口显示用）</summary>
        public override string ToString()
        {
            if (Issues.Count == 0) return "校验通过，无问题。";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"共 {Issues.Count} 个问题（错误 {ErrorCount} / 警告 {WarningCount}）：");
            foreach (UISpecIssue issue in Issues)
                sb.AppendLine(issue.ToString());
            return sb.ToString();
        }
    }

    /// <summary>ui-spec 干跑校验器</summary>
    public static class UISpecValidator
    {
        /// <summary>
        /// 对整棵 spec 树执行全部校验规则。
        /// </summary>
        /// <param name="root">spec 根节点</param>
        /// <param name="pageName">页面名（错误信息用）</param>
        public static UISpecValidationResult Validate(UISpecNode root, string pageName)
        {
            UISpecValidationResult result = new UISpecValidationResult();
            if (root == null)
            {
                result.Error(pageName, "spec 根节点为空");
                return result;
            }
            ValidateNode(root, root.name, null, result);
            return result;
        }

        private static void ValidateNode(UISpecNode node, string path, Type parentViewType, UISpecValidationResult result)
        {
            // 所在子树内最近的视图脚本（绑定目标）决定字段可匹配性的检查对象
            Type viewType = FindNearestViewType(node) ?? parentViewType;

            // v1.3：ref 嵌套预制体节点单独校验，children/components 不参与递归构建
            if (!string.IsNullOrEmpty(node.@ref))
            {
                ValidateRef(node, path, result);
                ValidateBindingName(node, path, viewType, result);
                if (node.children != null && node.children.Length > 0)
                    result.Warning(path, "ref 节点的 children 将被忽略（结构由被引用预制体决定）");
                return;
            }

            ValidateComponents(node, path, result);
            ValidateConventions(node, path, result);
            ValidateBindingName(node, path, viewType, result);

            if (node.children != null)
                foreach (UISpecNode child in node.children)
                    if (child != null)
                        ValidateNode(child, path + "/" + child.name, viewType, result);
        }

        /// <summary>ref 节点校验：目标预制体必须存在（相对 PREFAB_ROOT，不含扩展名）</summary>
        private static void ValidateRef(UISpecNode node, string path, UISpecValidationResult result)
        {
            string prefabPath = UISpecPrefabBuilder.PREFAB_ROOT + "/" + node.@ref + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                result.Error(path,
                    $"ref 预制体不存在: {prefabPath}（请先单独生成被引用页面，如 Common）");
        }

        /// <summary>在节点脚本组件中查找最近的绑定目标类型（View/行视图等）</summary>
        private static Type FindNearestViewType(UISpecNode node)
        {
            if (node.components == null) return null;
            foreach (string comp in node.components)
            {
                if (!ComponentBuilderRegistry.IsScript(comp)) continue;
                Type t = ScriptComponentBuilder.Resolve(comp.Substring(0, comp.Length - ".cs".Length));
                if (UISpecViewBinder.IsBindTargetType(t)) return t;
            }
            return null;
        }

        // ==================== 组件名合法性 ====================

        private static void ValidateComponents(UISpecNode node, string path, UISpecValidationResult result)
        {
            if (node.components == null || node.components.Length == 0)
            {
                result.Warning(path, "components 为空（将只创建 RectTransform 容器）");
                return;
            }

            foreach (string comp in node.components)
            {
                if (string.IsNullOrEmpty(comp))
                {
                    result.Error(path, "存在空组件名");
                    continue;
                }
                if (ComponentBuilderRegistry.IsScript(comp))
                {
                    // 脚本类型可解析性（阻断）
                    string className = comp.Substring(0, comp.Length - ".cs".Length);
                    if (ScriptComponentBuilder.Resolve(className) == null)
                        result.Error(path, $"脚本类型 {comp} 无法解析（提示：先用 UIPanelGenerator 生成骨架）");
                    continue;
                }
                if (!ComponentBuilderRegistry.IsKnown(comp))
                {
                    result.Error(path,
                        $"未知组件名 \"{comp}\"；已注册: {string.Join(", ", ComponentBuilderRegistry.RegisteredNames)}（或以 .cs 结尾的脚本组件）");
                }
            }

            // LayoutGroup 节点缺 layout 参数时提示（按默认值构建）
            if ((node.HasComponent("HorizontalLayoutGroup") || node.HasComponent("VerticalLayoutGroup")) &&
                node.layout == null)
            {
                result.Warning(path, "LayoutGroup 节点缺少 layout 参数，将按默认（spacing=0/padding=0/UpperLeft）构建");
            }
        }

        // ==================== 约定子节点（§5.3） ====================

        private static void ValidateConventions(UISpecNode node, string path, UISpecValidationResult result)
        {
            if (node.HasComponent("InputField"))
            {
                RequireChild(node, path, "Text", result);
                RequireChild(node, path, "Placeholder", result);
            }

            if (node.HasComponent("ScrollRect"))
            {
                UISpecNode viewport = RequireChild(node, path, "Viewport", result);
                if (viewport != null && (viewport.children == null || viewport.children.Length == 0))
                    result.Error(path + "/Viewport", "ScrollRect 约定 Viewport 内含 Content（LayoutGroup 节点），但 Viewport 无子节点");
            }

            if (node.HasComponent("Slider"))
            {
                UISpecNode fillArea = RequireChild(node, path, "FillArea", result);
                UISpecNode handleArea = RequireChild(node, path, "HandleArea", result);
                // Fill/Handle 缺失时解释器会补建，仅提示
                if (fillArea != null && fillArea.FindChild("Fill") == null)
                    result.Warning(path + "/FillArea", "缺少 Fill 子节点，生成时将自动补建");
                if (handleArea != null && handleArea.FindChild("Handle") == null)
                    result.Warning(path + "/HandleArea", "缺少 Handle 子节点，生成时将自动补建");
            }

            if (node.HasComponent("Toggle"))
            {
                UISpecNode background = RequireChild(node, path, "Background", result);
                RequireChild(node, path, "Label", result);
                if (background != null && background.FindChild("Checkmark") == null)
                    result.Warning(path + "/Background", "缺少 Checkmark 子节点，生成时将自动补建");
            }

            // 文本节点缺字段提示
            if (node.HasComponent("Text") && !node.HasText)
                result.Warning(path, "Text 组件节点缺少 text 字段（将按空文本构建）");
        }

        private static UISpecNode RequireChild(UISpecNode node, string path, string childName, UISpecValidationResult result)
        {
            UISpecNode child = node.FindChild(childName);
            if (child == null)
                result.Error(path, $"缺少约定子节点 \"{childName}\"");
            return child;
        }

        // ==================== 绑定命名规范与字段可匹配性（§5.4 警告级） ====================

        private static void ValidateBindingName(UISpecNode node, string path, Type viewType, UISpecValidationResult result)
        {
            string name = node.name;
            if (string.IsNullOrEmpty(name)) return;

            string normalized = null; // 归一化为 m_Xxx 形式
            // m_/mi_ 前缀但后首字母小写 → 不会被绑定，警告
            if (name.StartsWith("m_", StringComparison.Ordinal) && name.Length > 2)
            {
                if (!IsUpper(name[2]))
                    result.Warning(path, $"节点名 {name} 以 m_ 开头但后首字母小写，不会参与 View 字段绑定");
                else
                    normalized = name;
            }
            else if (name.StartsWith("mi_", StringComparison.Ordinal) && name.Length > 3)
            {
                if (!IsUpper(name[3]))
                    result.Warning(path, $"节点名 {name} 以 mi_ 开头但后首字母小写，不会参与 View 字段绑定");
                else
                    normalized = "m_" + name.Substring(3);
            }

            // 绑定字段可匹配性：最近视图脚本无对应字段 → 警告（便于补字段）
            if (normalized != null && viewType != null &&
                !UISpecViewBinder.GetSerializableFieldNames(viewType).Contains(normalized))
            {
                result.Warning(path, $"视图 {viewType.Name} 中不存在字段 {normalized}，该节点不会被绑定");
            }
        }

        private static bool IsUpper(char c) => c >= 'A' && c <= 'Z';
    }
}
