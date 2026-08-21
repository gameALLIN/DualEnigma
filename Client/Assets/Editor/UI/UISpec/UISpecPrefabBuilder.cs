/// ============================================================
/// 文件名: UISpecPrefabBuilder.cs
/// 创建时间: 2026-08-20
/// 作者: DualEnigma
/// 描述: ui-spec 预制体构建器（《通用JSON预制体生成器》§五）。
///       递归解释节点树为 GameObject 树；全树完成后统一二次处理
///       复合组件接线（Button/InputField/ScrollRect/Slider/Toggle，
///       约定优于配置）；最后 View 字段绑定 + SaveAsPrefabAsset
///       原地覆盖保存（预制体 GUID 保持不变，场景/AB 引用不断）。
/// 引用：UISpecNode.cs, IComponentBuilder.cs, ComponentBuilders.cs,
///       UISpecValidator.cs, UISpecViewBinder.cs, UISpecBuildUtil.cs
/// ============================================================

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;
using Object = UnityEngine.Object;

namespace DualEnigma.UI.Editor
{
    /// <summary>ui-spec → 预制体 构建器</summary>
    public static class UISpecPrefabBuilder
    {
        /// <summary>预制体输出根目录（相对项目根）</summary>
        public const string PREFAB_ROOT = "Assets/AssetPackage/Prefabs/UI";

        // ================================================================
        //  全流程入口
        // ================================================================

        /// <summary>
        /// 完整生成流程：提取 → 校验（阻断错误即中止）→ 构建 → 接线 → 绑定 → 保存。
        /// </summary>
        /// <param name="htmlPath">设计稿 HTML 绝对路径</param>
        /// <param name="pageName">页面名（输出目录名，如 "UILogin"）</param>
        /// <returns>生成的预制体路径；失败返回 null</returns>
        public static string GenerateFromHtml(string htmlPath, string pageName)
        {
            UISpecNode spec = UISpecExtractor.ExtractFromFile(htmlPath);

            UISpecValidationResult validation = UISpecValidator.Validate(spec, pageName);
            validation.LogToConsole();
            if (validation.HasErrors)
            {
                Debug.LogError($"[UISpec] {pageName}: 校验存在 {validation.ErrorCount} 个阻断错误，已中止生成。");
                return null;
            }

            GameObject root = BuildTree(spec, pageName);
            return SavePrefab(root, pageName, spec.name);
        }

        // ================================================================
        //  构建（③ 递归构建 + ④ 复合接线 + ⑥ 字段绑定）
        // ================================================================

        /// <summary>
        /// 按 spec 构建完整 GameObject 树（含复合接线与 View 字段绑定），
        /// 调用方负责保存或销毁。
        /// </summary>
        public static GameObject BuildTree(UISpecNode spec, string pageName)
        {
            BuildContext ctx = new BuildContext
            {
                PageName = pageName,
                Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
            };

            GameObject root = BuildNode(spec, null, ctx);
            WireCompositeComponents(ctx);
            SetupAutoBinder(root);
            UISpecViewBinder.Bind(root);
            return root;
        }

        /// <summary>递归构建单个节点（§5.2）</summary>
        private static GameObject BuildNode(UISpecNode node, Transform parent, BuildContext ctx)
        {
            // v1.3：ref 嵌套预制体实例化（Common 公共组件，如 FriendItem 行模板）
            if (!string.IsNullOrEmpty(node.@ref))
                return InstantiateRefNode(node, parent, ctx);

            GameObject go = new GameObject(node.name);
            if (parent != null)
                go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            UISpecAnchors anchors = node.anchors ?? new UISpecAnchors();
            rt.anchorMin = anchors.Min;
            rt.anchorMax = anchors.Max;
            rt.pivot = node.Pivot;
            rt.anchoredPosition = node.Position;
            rt.sizeDelta = node.Size;
            rt.localScale = node.Scale;                                // v1.2，缺省 [1,1]
            rt.localRotation = Quaternion.Euler(0f, 0f, node.rotation); // v1.2，缺省 0

            if (node.components != null)
            {
                foreach (string comp in node.components)
                {
                    if (comp == "RectTransform") continue; // 核心统一处理
                    if (ComponentBuilderRegistry.IsScript(comp))
                    {
                        if (ScriptComponentBuilder.AddByClassName(go, comp) == null)
                            Debug.LogWarning($"[UISpec] 节点 {node.name}: 脚本类型 {comp} 解析失败，已跳过（应在校验期拦截）");
                        continue;
                    }
                    if (ComponentBuilderRegistry.TryGet(comp, out IComponentBuilder builder))
                        builder.Build(go, node, ctx);
                }
            }

            if (!node.active) go.SetActive(false);

            ctx.Register(node, go);

            if (node.children != null)
                foreach (UISpecNode child in node.children)
                    if (child != null)
                        BuildNode(child, go.transform, ctx);

            return go;
        }

        /// <summary>
        /// v1.3：ref 节点 → 实例化嵌套预制体（相对 PREFAB_ROOT 的路径，如 "Common/FriendItem"）。
        /// spec 的布局字段（anchors/pivot/position/size/active）覆盖实例默认值；
        /// children 与 components 被忽略（结构与组件由被引用预制体决定）。
        /// </summary>
        private static GameObject InstantiateRefNode(UISpecNode node, Transform parent, BuildContext ctx)
        {
            string prefabPath = PREFAB_ROOT + "/" + node.@ref + ".prefab";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (source == null)
            {
                Debug.LogError($"[UISpec] 节点 {node.name}: ref 预制体未找到 {prefabPath}（需先单独生成被引用页面，如 Common）");
                // 降级为空容器，保证后续节点不受阻断
                GameObject fallback = new GameObject(node.name);
                if (parent != null) fallback.transform.SetParent(parent, false);
                fallback.AddComponent<RectTransform>();
                if (!node.active) fallback.SetActive(false);
                ctx.Register(node, fallback);
                return fallback;
            }

            GameObject go = parent != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(source, parent)
                : (GameObject)PrefabUtility.InstantiatePrefab(source);
            go.name = node.name;

            // 布局覆盖：模板的摆放位置由宿主设计稿决定
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                UISpecAnchors anchors = node.anchors ?? new UISpecAnchors();
                rt.anchorMin = anchors.Min;
                rt.anchorMax = anchors.Max;
                rt.pivot = node.Pivot;
                rt.anchoredPosition = node.Position;
                rt.sizeDelta = node.Size;
            }

            if (!node.active) go.SetActive(false);

            ctx.Register(node, go);
            return go;
        }

        // ================================================================
        //  ④ 复合接线（§5.3 约定优于配置）
        // ================================================================

        private static void WireCompositeComponents(BuildContext ctx)
        {
            foreach (KeyValuePair<UISpecNode, GameObject> pair in ctx.NodeToGo)
            {
                GameObject go = pair.Value;

                Button button = go.GetComponent<Button>();
                if (button != null && button.targetGraphic == null)
                    button.targetGraphic = go.GetComponent<Image>();

                InputField input = go.GetComponent<InputField>();
                if (input != null) WireInputField(go.transform, input);

                ScrollRect scroll = go.GetComponent<ScrollRect>();
                if (scroll != null) WireScrollRect(go.transform, scroll);

                Slider slider = go.GetComponent<Slider>();
                if (slider != null) WireSlider(go.transform, slider);

                Toggle toggle = go.GetComponent<Toggle>();
                if (toggle != null) WireToggle(go.transform, toggle);
            }
        }

        /// <summary>InputField：约定子节点 Text（输入内容）+ Placeholder（占位文案）</summary>
        private static void WireInputField(Transform host, InputField input)
        {
            Transform textT = host.Find("Text");
            Transform placeholderT = host.Find("Placeholder");
            if (textT != null)
            {
                Text text = textT.GetComponent<Text>();
                if (text != null)
                {
                    text.supportRichText = false; // 与手写生成器一致
                    input.textComponent = text;
                }
            }
            if (placeholderT != null)
            {
                Text placeholder = placeholderT.GetComponent<Text>();
                if (placeholder != null)
                {
                    placeholder.fontStyle = FontStyle.Italic; // Placeholder 隐式约定：斜体
                    input.placeholder = placeholder;
                }
            }
            input.text = "";
        }

        /// <summary>ScrollRect：约定子节点 Viewport（其下第一个 LayoutGroup 节点为 Content）</summary>
        private static void WireScrollRect(Transform host, ScrollRect scroll)
        {
            Transform viewport = host.Find("Viewport");
            if (viewport == null) return;

            scroll.viewport = viewport as RectTransform;
            Transform content = FindLayoutGroupChild(viewport);
            if (content != null)
                scroll.content = content as RectTransform;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 20f;
        }

        /// <summary>在 viewport 子树中查找第一个挂 LayoutGroup 的节点（Content 约定）</summary>
        private static Transform FindLayoutGroupChild(Transform viewport)
        {
            for (int i = 0; i < viewport.childCount; i++)
            {
                Transform child = viewport.GetChild(i);
                if (child.GetComponent<HorizontalOrVerticalLayoutGroup>() != null)
                    return child;
            }
            // 无 LayoutGroup 时退化为第一个直接子节点
            return viewport.childCount > 0 ? viewport.GetChild(0) : null;
        }

        /// <summary>Slider：约定子节点 FillArea/Fill + HandleArea/Handle（缺失图形由解释器补建）</summary>
        private static void WireSlider(Transform host, Slider slider)
        {
            Transform fill = host.Find("FillArea/Fill");
            Transform handle = host.Find("HandleArea/Handle");

            if (fill == null) fill = EnsureGraphic(host, "FillArea", "Fill");
            if (handle == null) handle = EnsureGraphic(host, "HandleArea", "Handle");

            if (fill != null)
            {
                slider.fillRect = fill as RectTransform;
                Image fillImg = fill.GetComponent<Image>();
                if (fillImg != null) fillImg.raycastTarget = true;
            }
            if (handle != null)
            {
                slider.handleRect = handle as RectTransform;
                Image handleImg = handle.GetComponent<Image>();
                if (handleImg != null)
                {
                    handleImg.raycastTarget = true;
                    slider.targetGraphic = handleImg;
                }
            }
        }

        /// <summary>Toggle：约定子节点 Background/Checkmark（缺失由解释器补建）+ Label</summary>
        private static void WireToggle(Transform host, Toggle toggle)
        {
            Transform background = host.Find("Background");
            if (background == null) return;

            Transform checkmark = background.Find("Checkmark");
            if (checkmark == null) checkmark = EnsureGraphic(host, "Background", "Checkmark");

            if (checkmark != null)
                toggle.graphic = checkmark.GetComponent<Graphic>();
            Image bgImage = background.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.raycastTarget = true;
                toggle.targetGraphic = bgImage;
            }
        }

        /// <summary>补建复合组件缺失的约定图形子节点（含 Image）</summary>
        private static Transform EnsureGraphic(Transform host, string areaName, string graphicName)
        {
            Transform area = host.Find(areaName);
            if (area == null) return null;
            GameObject go = new GameObject(graphicName);
            go.transform.SetParent(area, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.AddComponent<CanvasRenderer>();
            go.AddComponent<Image>();
            return rt;
        }

        // ================================================================
        //  ⑤ UIAutoBinder.ViewTypeName 设置
        // ================================================================

        private static void SetupAutoBinder(GameObject root)
        {
            UIAutoBinder binder = root.GetComponent<UIAutoBinder>();
            if (binder == null || !string.IsNullOrEmpty(binder.ViewTypeName)) return;
            UIViewBase view = root.GetComponent<UIViewBase>();
            if (view != null)
                binder.ViewTypeName = view.GetType().Name;
        }

        // ================================================================
        //  ⑦ 保存（§5.7 SaveAsPrefabAsset 原地覆盖，GUID 稳定）
        // ================================================================

        /// <summary>
        /// 把构建好的根节点保存为预制体并清理临时对象。
        /// 输出路径: Assets/AssetPackage/Prefabs/UI/&lt;页面名&gt;/&lt;根节点名&gt;.prefab
        /// </summary>
        public static string SavePrefab(GameObject root, string pageName, string rootName)
        {
            string dir = PREFAB_ROOT + "/" + pageName;
            string path = dir + "/" + rootName + ".prefab";
            UISpecBuildUtil.EnsureDirectory(dir);

            // 原地覆盖：不先删资产，预制体 GUID 保持不变
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            Debug.Log("[UISpec] 预制体已生成: " + path);
            return path;
        }
    }
}
