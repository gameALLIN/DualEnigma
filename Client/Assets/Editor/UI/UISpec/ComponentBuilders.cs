/// ============================================================
/// 文件名: ComponentBuilders.cs
/// 创建时间: 2026-08-20
/// 作者: DualEnigma
/// 描述: 内置组件构建器实现（《通用JSON预制体生成器》§4.2 全覆盖）：
///       CanvasRenderer/Image/Text/Button/InputField/ScrollRect/Mask/
///       Horizontal·VerticalLayoutGroup/Slider/Toggle + 脚本组件类型解析。
///       复合组件的子节点接线由 UISpecPrefabBuilder 统一二次处理。
/// 引用：IComponentBuilder.cs, UISpecBuildUtil.cs
/// ============================================================

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DualEnigma.UI.Editor
{
    /// <summary>CanvasRenderer：有可见图形的节点需要</summary>
    public sealed class CanvasRendererBuilder : IComponentBuilder
    {
        public void Build(GameObject go, UISpecNode node, BuildContext ctx)
        {
            if (go.GetComponent<CanvasRenderer>() == null)
                go.AddComponent<CanvasRenderer>();
        }
    }

    /// <summary>
    /// Image：background → 颜色；linear-gradient → 生成渐变 Sprite；
    /// sprite 字段 → 引用程序化 Sprite 资产（v1.2）。
    /// 纯装饰图 raycastTarget=false；交互宿主（Button/InputField/Slider/Toggle/ScrollRect/Mask）为 true。
    /// </summary>
    public sealed class ImageBuilder : IComponentBuilder
    {
        public void Build(GameObject go, UISpecNode node, BuildContext ctx)
        {
            Image image = go.GetComponent<Image>() ?? go.AddComponent<Image>();

            Sprite sprite = null;
            if (!string.IsNullOrEmpty(node.sprite))
            {
                // v1.2 扩展：显式引用 Sprite 资产（相对 Assets/，不含扩展名）
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/" + node.sprite + ".asset");
                if (sprite == null)
                    Debug.LogWarning($"[UISpec] 节点 {node.name}: sprite 资产未找到 Assets/{node.sprite}.asset");
            }
            else if (UISpecBuildUtil.IsGradient(node.background))
            {
                sprite = UISpecBuildUtil.CreateGradientSprite(
                    node.background, $"{ctx.PageName}_{node.name}_Gradient");
                if (sprite == null)
                    Debug.LogWarning($"[UISpec] 节点 {node.name}: 渐变解析失败，回退为纯色 — {node.background}");
            }

            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.color = UISpecBuildUtil.ParseColor(node.background) ?? Color.white;
            }

            image.raycastTarget = sprite != null || IsInteractiveHost(node);
        }

        /// <summary>节点是否承载需要射线目标的交互组件</summary>
        private static bool IsInteractiveHost(UISpecNode node) =>
            node.HasComponent("Button") || node.HasComponent("InputField") ||
            node.HasComponent("Slider") || node.HasComponent("Toggle") ||
            node.HasComponent("ScrollRect") || node.HasComponent("Mask");
    }

    /// <summary>
    /// Text：text/fontSize/align/color → UGUI Text；内置字体 LegacyRuntime.ttf；
    /// raycastTarget=false；horizontalOverflow=Overflow；fontStyle 支持 italic/bold（v1.2）。
    /// </summary>
    public sealed class TextBuilder : IComponentBuilder
    {
        public void Build(GameObject go, UISpecNode node, BuildContext ctx)
        {
            Text text = go.GetComponent<Text>() ?? go.AddComponent<Text>();
            text.text = node.text ?? "";
            text.font = ctx.Font;
            if (node.fontSize > 0) text.fontSize = node.fontSize;
            text.alignment = ParseTextAnchor(node.align, TextAnchor.MiddleCenter);
            text.color = UISpecBuildUtil.ParseColor(node.color) ?? Color.white;
            text.fontStyle = ParseFontStyle(node.fontStyle);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        internal static TextAnchor ParseTextAnchor(string align, TextAnchor fallback)
        {
            if (!string.IsNullOrEmpty(align) &&
                Enum.TryParse(align, out TextAnchor anchor))
                return anchor;
            return fallback;
        }

        internal static FontStyle ParseFontStyle(string style)
        {
            switch ((style ?? "").ToLowerInvariant())
            {
                case "italic": return FontStyle.Italic;
                case "bold": return FontStyle.Bold;
                case "bolditalic":
                case "boldanditalic": return FontStyle.BoldAndItalic;
                default: return FontStyle.Normal;
            }
        }
    }

    /// <summary>Button：targetGraphic=自身 Image（接线阶段完成）</summary>
    public sealed class ButtonBuilder : IComponentBuilder
    {
        public void Build(GameObject go, UISpecNode node, BuildContext ctx)
        {
            go.AddComponent<Button>();
        }
    }

    /// <summary>
    /// InputField：约定子节点 Text/Placeholder 的接线在二次处理阶段完成。
    /// 命名含 "Password" 的输入框按密码框处理（与手写生成器约定一致）。
    /// </summary>
    public sealed class InputFieldBuilder : IComponentBuilder
    {
        public void Build(GameObject go, UISpecNode node, BuildContext ctx)
        {
            InputField input = go.AddComponent<InputField>();
            if (node.name.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0)
                input.contentType = InputField.ContentType.Password;
        }
    }

    /// <summary>ScrollRect：约定子节点 Viewport/Content 的接线在二次处理阶段完成</summary>
    public sealed class ScrollRectBuilder : IComponentBuilder
    {
        public void Build(GameObject go, UISpecNode node, BuildContext ctx)
        {
            go.AddComponent<ScrollRect>();
        }
    }

    /// <summary>Mask：showMaskGraphic=false（白色底由 Image 提供）</summary>
    public sealed class MaskBuilder : IComponentBuilder
    {
        public void Build(GameObject go, UISpecNode node, BuildContext ctx)
        {
            Mask mask = go.AddComponent<Mask>();
            mask.showMaskGraphic = false;
        }
    }

    /// <summary>
    /// LayoutGroup：layout 参数 → spacing/padding/childAlignment；
    /// childControl/childForceExpand 全 false（与手写生成器一致）。
    /// </summary>
    public sealed class LayoutGroupBuilder : IComponentBuilder
    {
        private readonly bool _horizontal;

        public LayoutGroupBuilder(bool horizontal) { _horizontal = horizontal; }

        public void Build(GameObject go, UISpecNode node, BuildContext ctx)
        {
            HorizontalOrVerticalLayoutGroup layout = _horizontal
                ? go.AddComponent<HorizontalLayoutGroup>()
                : (HorizontalOrVerticalLayoutGroup)go.AddComponent<VerticalLayoutGroup>();

            UISpecLayout spec = node.layout;
            layout.spacing = spec?.spacing ?? 0f;
            layout.padding = spec?.ToRectOffset() ?? new RectOffset();
            layout.childAlignment = TextBuilder.ParseTextAnchor(spec?.align, TextAnchor.UpperLeft);
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
    }

    /// <summary>
    /// LayoutElement（v1.3）：preferredWidth/Height 取节点 size（布局容器内即首选尺寸），
    /// flexibleWidth 可选（&gt;0 时该列弹性占满剩余空间，如好友行昵称列）。
    /// 供运行时窄容器回流（FriendItem.SetCompactLayout 依赖 preferredWidth）。
    /// </summary>
    public sealed class LayoutElementBuilder : IComponentBuilder
    {
        public void Build(GameObject go, UISpecNode node, BuildContext ctx)
        {
            LayoutElement le = go.AddComponent<LayoutElement>();
            Vector2 size = node.Size;
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
            le.flexibleWidth = node.flexibleWidth;
        }
    }

    /// <summary>Slider：Fill/Handle 图形接线在二次处理阶段完成</summary>
    public sealed class SliderBuilder : IComponentBuilder
    {
        public void Build(GameObject go, UISpecNode node, BuildContext ctx)
        {
            Slider slider = go.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }
    }

    /// <summary>Toggle：Checkmark/Label 接线在二次处理阶段完成；默认勾选（与手写生成器一致）</summary>
    public sealed class ToggleBuilder : IComponentBuilder
    {
        public void Build(GameObject go, UISpecNode node, BuildContext ctx)
        {
            Toggle toggle = go.AddComponent<Toggle>();
            toggle.isOn = true;
        }
    }

    /// <summary>
    /// 脚本组件类型解析（§5.5）：类名在固定命名空间列表中解析，
    /// 兜底全程序集按简名扫描；解析不到即校验失败。
    /// </summary>
    public static class ScriptComponentBuilder
    {
        /// <summary>固定命名空间列表（与项目 UI 代码分布一致）</summary>
        private static readonly string[] Namespaces =
        {
            "DualEnigma.UI",
            "DualEnigma.Framework.UI",
            "DualEnigma.UI.Components",
        };

        /// <summary>把 "XxxView.cs" 解析为类型并 AddComponent；失败返回 null</summary>
        public static Component AddByClassName(GameObject go, string specComp)
        {
            Type type = Resolve(specComp.Substring(0, specComp.Length - ".cs".Length));
            return type != null ? go.AddComponent(type) : null;
        }

        /// <summary>按类名解析类型（固定命名空间 + 全程序集简名兜底）</summary>
        public static Type Resolve(string className)
        {
            foreach (string ns in Namespaces)
            {
                Type t = FindInAssemblies(ns + "." + className);
                if (t != null) return t;
            }
            // 兜底：全程序集按简名匹配（与 UIBindingGenerator.FindTypeByName 同一策略）
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (System.Reflection.ReflectionTypeLoadException) { continue; }
                foreach (Type t in types)
                    if (t.Name == className) return t;
            }
            return null;
        }

        private static Type FindInAssemblies(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = assembly.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
