/// ============================================================
/// 文件名: IComponentBuilder.cs
/// 创建时间: 2026-08-20
/// 作者: DualEnigma
/// 描述: 组件构建器接口与注册表。每个 spec 组件名（Image/Text/Button/...）
///       对应一个 IComponentBuilder；未知组件名在校验期报错。
///       扩展机制：新组件类型 = 新增一个 Builder 类 + 注册一行，不改核心。
/// 引用：UISpecNode.cs, UISpecPrefabBuilder.cs
/// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.UI.Editor
{
    /// <summary>构建上下文：递归构建期间共享的查找与注册服务</summary>
    public sealed class BuildContext
    {
        /// <summary>当前页面名（如 "UILogin"），用于渐变资产命名等</summary>
        public string PageName;

        /// <summary>内置字体（LegacyRuntime.ttf）</summary>
        public Font Font;

        /// <summary>节点 → GameObject 注册表（按构建路径），供复合接线与绑定阶段使用</summary>
        public readonly Dictionary<UISpecNode, GameObject> NodeToGo =
            new Dictionary<UISpecNode, GameObject>();

        /// <summary>节点名 → GameObject（重名时保留先注册者），供按名查找</summary>
        public readonly Dictionary<string, GameObject> NameToGo =
            new Dictionary<string, GameObject>();

        /// <summary>注册节点构建产物</summary>
        public void Register(UISpecNode node, GameObject go)
        {
            NodeToGo[node] = go;
            if (!NameToGo.ContainsKey(node.name))
                NameToGo[node.name] = go;
        }

        /// <summary>按节点名查找构建产物</summary>
        public GameObject Find(string nodeName) =>
            NameToGo.TryGetValue(nodeName, out GameObject go) ? go : null;
    }

    /// <summary>
    /// 组件构建器：把 spec 中一个组件名解释为目标 GameObject 上的组件配置。
    /// 复合组件的子节点引用接线不在此处理（由 UISpecPrefabBuilder 统一二次处理）。
    /// </summary>
    public interface IComponentBuilder
    {
        /// <summary>在 go 上添加/配置本组件</summary>
        void Build(GameObject go, UISpecNode node, BuildContext ctx);
    }

    /// <summary>
    /// 组件构建器注册表。spec 组件名 → Builder 单例。
    /// RectTransform 由核心统一处理，不在注册表内。
    /// </summary>
    public static class ComponentBuilderRegistry
    {
        private static readonly Dictionary<string, IComponentBuilder> Builders = CreateTable();

        /// <summary>尝试获取组件构建器</summary>
        public static bool TryGet(string compName, out IComponentBuilder builder) =>
            Builders.TryGetValue(compName, out builder);

        /// <summary>已注册组件名列表（校验报错提示用）</summary>
        public static IEnumerable<string> RegisteredNames => Builders.Keys;

        /// <summary>判断组件名是否可解释（注册表内 or 脚本组件）</summary>
        public static bool IsKnown(string compName) =>
            compName == "RectTransform" || Builders.ContainsKey(compName) || IsScript(compName);

        /// <summary>是否为脚本组件（.cs 后缀）</summary>
        public static bool IsScript(string compName) =>
            !string.IsNullOrEmpty(compName) && compName.EndsWith(".cs", StringComparison.Ordinal);

        private static Dictionary<string, IComponentBuilder> CreateTable() =>
            new Dictionary<string, IComponentBuilder>
            {
                { "CanvasRenderer", new CanvasRendererBuilder() },
                { "Image", new ImageBuilder() },
                { "Text", new TextBuilder() },
                { "Button", new ButtonBuilder() },
                { "InputField", new InputFieldBuilder() },
                { "ScrollRect", new ScrollRectBuilder() },
                { "Mask", new MaskBuilder() },
                { "HorizontalLayoutGroup", new LayoutGroupBuilder(true) },
                { "VerticalLayoutGroup", new LayoutGroupBuilder(false) },
                { "LayoutElement", new LayoutElementBuilder() },
                { "Slider", new SliderBuilder() },
                { "Toggle", new ToggleBuilder() },
            };
    }
}
