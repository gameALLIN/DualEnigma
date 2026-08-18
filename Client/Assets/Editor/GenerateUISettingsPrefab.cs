/// ============================================================
/// 文件名: GenerateUISettingsPrefab.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: UISettings 预制体生成器 Editor 工具。
///       层级：全屏 Dim / 中央面板（标题 + 音量滑条 + 继续游戏 + 退出对局）。
///       菜单：DualEnigma/UI/生成 UISettings 预制体。
/// 引用：UISettingsView.cs, UISettingsCtrl.cs
/// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DualEnigma.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.Editor
{
    public static class GenerateUISettingsPrefab
    {
        private const string PREFAB_PATH = "Assets/AssetPackage/Prefabs/UI/UISettings/UISettings.prefab";
        private const string PREFAB_DIR = "Assets/AssetPackage/Prefabs/UI/UISettings";

        // ===== 颜色（与登录/主界面/房间同一套规范）=====
        private static readonly Color32 PANEL_COLOR = new Color32(0x26, 0x32, 0x38, 0xFF);
        private static readonly Color32 CONTINUE_BTN_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 EXIT_BTN_COLOR = new Color32(0xBF, 0x36, 0x0C, 0xFF);
        private static readonly Color32 SLIDER_BG_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);
        private static readonly Color32 SLIDER_FILL_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 SLIDER_HANDLE_COLOR = new Color32(0xE1, 0xF5, 0xFE, 0xFF);

        private const float PANEL_W = 360f;
        private const float PANEL_H = 360f;

        [MenuItem("DualEnigma/UI/生成 UISettings 预制体")]
        public static void Generate()
        {
            EnsureDirectory(PREFAB_DIR);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject root = BuildHierarchy(font);
            BindViewFields(root);

            DeleteExistingAsset(PREFAB_PATH);
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            Debug.Log("[GenerateUISettingsPrefab] UISettings 预制体已生成: " + PREFAB_PATH);
        }

        private static GameObject BuildHierarchy(Font font)
        {
            GameObject root = new GameObject("UISettings");
            SetStretch(root.AddComponent<RectTransform>());
            root.AddComponent<UISettingsView>();
            root.AddComponent<UISettingsCtrl>();
            UIAutoBinder binder = root.AddComponent<UIAutoBinder>();
            binder.ViewTypeName = nameof(UISettingsView);

            // 全屏半透明底（设置弹窗期间阻挡游戏输入）
            GameObject dim = CreateImage("Dim", root.transform, new Color32(0, 0, 0, 0x88));
            dim.GetComponent<Image>().raycastTarget = true;
            SetStretch(dim.GetComponent<RectTransform>());

            // 主面板
            GameObject panel = CreateImage("Panel", root.transform, PANEL_COLOR);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(PANEL_W, PANEL_H);
            panel.GetComponent<Image>().raycastTarget = true;

            // 标题
            CreateText("TitleText", "设置", 24, panel.transform, font,
                new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(200f, 32f), Color.white);

            // ---- 音量行：标签 + 滑条 + 百分比 ----
            Text volumeLabel = CreateText("VolumeLabel", "音量", 16, panel.transform, font,
                new Vector2(0, 0.5f), new Vector2(24f, 40f), new Vector2(48f, 22f), Color.white);
            volumeLabel.alignment = TextAnchor.MiddleLeft;

            BuildVolumeSlider(panel.transform, font);

            CreateText("VolumeValueText", "80%", 14, panel.transform, font,
                new Vector2(1, 0.5f), new Vector2(-24f, 40f), new Vector2(48f, 22f), SLIDER_FILL_COLOR);

            // ---- 性能信息显示开关 ----
            BuildPerfToggle(panel.transform, font);

            // ---- 继续游戏 ----
            CreateButton("ContinueBtn", "继续游戏", CONTINUE_BTN_COLOR, panel.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -46f), new Vector2(260f, 40f), 16, "Text");

            // ---- 退出对局（仅局内显示，主界面打开时由 Ctrl 隐藏）----
            CreateButton("ExitBtn", "退出对局", EXIT_BTN_COLOR, panel.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(260f, 40f), 16, "Text");

            // ---- 退出登录（仅主界面打开时显示，局内隐藏）----
            CreateButton("LogoutBtn", "退出登录", EXIT_BTN_COLOR, panel.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -154f), new Vector2(260f, 40f), 16, "Text");

            return root;
        }

        /// <summary>构建性能信息显示开关（Background + Checkmark + Label）</summary>
        private static void BuildPerfToggle(Transform parent, Font font)
        {
            GameObject toggleObj = CreateUIObject("PerfToggle", parent);
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = true;

            RectTransform toggleRT = toggleObj.GetComponent<RectTransform>();
            toggleRT.anchorMin = toggleRT.anchorMax = new Vector2(0.5f, 0.5f);
            toggleRT.anchoredPosition = new Vector2(0f, 10f);
            toggleRT.sizeDelta = new Vector2(260f, 24f);

            // 勾选框背景
            GameObject bg = CreateImage("Background", toggleObj.transform, SLIDER_BG_COLOR);
            bg.GetComponent<Image>().raycastTarget = true;
            RectTransform bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = bgRT.anchorMax = new Vector2(0, 0.5f);
            bgRT.anchoredPosition = new Vector2(18f, 0f);
            bgRT.sizeDelta = new Vector2(20f, 20f);

            // 勾选标记
            GameObject check = CreateImage("Checkmark", bg.transform, SLIDER_FILL_COLOR);
            check.GetComponent<Image>().raycastTarget = false;
            RectTransform checkRT = check.GetComponent<RectTransform>();
            SetStretch(checkRT);
            checkRT.offsetMin = new Vector2(4f, 4f);
            checkRT.offsetMax = new Vector2(-4f, -4f);

            // 标签
            GameObject labelObj = CreateUIObject("Label", toggleObj.transform);
            Text label = labelObj.AddComponent<Text>();
            label.text = "显示性能信息（FPS / 延迟）";
            label.font = font;
            label.fontSize = 14;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            RectTransform labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = labelRT.anchorMax = new Vector2(0, 0.5f);
            labelRT.anchoredPosition = new Vector2(140f, 0f);
            labelRT.sizeDelta = new Vector2(220f, 22f);

            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
        }

        /// <summary>构建音量滑条（标准 Slider 层级：FillArea/Fill + HandleArea/Handle）</summary>
        private static void BuildVolumeSlider(Transform parent, Font font)
        {
            GameObject sliderObj = CreateUIObject("VolumeSlider", parent);
            Image bg = sliderObj.AddComponent<Image>();
            bg.color = SLIDER_BG_COLOR;
            bg.raycastTarget = true;
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
            sliderRT.anchorMin = sliderRT.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRT.anchoredPosition = new Vector2(-12f, 40f);
            sliderRT.sizeDelta = new Vector2(180f, 20f);

            // Fill Area + Fill
            GameObject fillArea = CreateUIObject("FillArea", sliderObj.transform);
            RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
            SetStretch(fillAreaRT);
            fillAreaRT.offsetMin = new Vector2(2f, 2f);
            fillAreaRT.offsetMax = new Vector2(-2f, -2f);

            GameObject fill = CreateImage("Fill", fillArea.transform, SLIDER_FILL_COLOR);
            fill.GetComponent<Image>().raycastTarget = true;
            RectTransform fillRT = fill.GetComponent<RectTransform>();
            SetStretch(fillRT);

            // Handle Area + Handle
            GameObject handleArea = CreateUIObject("HandleArea", sliderObj.transform);
            RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
            SetStretch(handleAreaRT);

            GameObject handle = CreateImage("Handle", handleArea.transform, SLIDER_HANDLE_COLOR);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.raycastTarget = true;
            RectTransform handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(14f, 22f);

            // 绑定 Slider 引用的 Rect（实例化字段，直接赋值即可，无需 SerializedObject）
            slider.fillRect = fillRT;
            slider.handleRect = handleRT;
            slider.targetGraphic = handleImage;
        }

        private static void BindViewFields(GameObject root)
        {
            UISettingsView view = root.GetComponent<UISettingsView>();
            SerializedObject so = new SerializedObject(view);

            so.FindProperty("m_VolumeSlider").objectReferenceValue =
                FindDeepChild(root.transform, "VolumeSlider")?.GetComponent<Slider>();
            so.FindProperty("m_VolumeValueText").objectReferenceValue =
                FindDeepChild(root.transform, "VolumeValueText")?.GetComponent<Text>();
            so.FindProperty("m_PerfToggle").objectReferenceValue =
                FindDeepChild(root.transform, "PerfToggle")?.GetComponent<Toggle>();
            so.FindProperty("m_ContinueBtn").objectReferenceValue =
                FindDeepChild(root.transform, "ContinueBtn")?.GetComponent<Button>();
            so.FindProperty("m_ExitBtn").objectReferenceValue =
                FindDeepChild(root.transform, "ExitBtn")?.GetComponent<Button>();
            so.FindProperty("m_LogoutBtn").objectReferenceValue =
                FindDeepChild(root.transform, "LogoutBtn")?.GetComponent<Button>();

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(view);
        }

        // ===== 通用辅助（与 GenerateUIRoomPrefab 保持一致）=====

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.sprite = null;
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private static Text CreateText(string name, string content, int fontSize,
            Transform parent, Font font, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Text text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return text;
        }

        private static Button CreateButton(string name, string label, Color bgColor,
            Transform parent, Font font, Vector2 anchor, Vector2 pos, Vector2 size,
            int fontSize, string textChildName = "Text")
        {
            GameObject btnObj = CreateUIObject(name, parent);
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = bgColor;
            btnImage.raycastTarget = true;
            Button button = btnObj.AddComponent<Button>();
            button.targetGraphic = btnImage;

            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = btnRT.anchorMax = anchor;
            btnRT.anchoredPosition = pos;
            btnRT.sizeDelta = size;

            GameObject textObj = CreateUIObject(textChildName, btnObj.transform);
            Text btnText = textObj.AddComponent<Text>();
            btnText.text = label;
            btnText.font = font;
            btnText.fontSize = fontSize;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.raycastTarget = false;
            SetStretch(textObj.GetComponent<RectTransform>());

            return button;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child;

                Transform result = FindDeepChild(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void DeleteExistingAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }
    }
}
