/// ============================================================
/// 文件名: GenerateUIGameHudPrefab.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: UIGameHud 预制体生成器 Editor 工具。
///       层级：关卡信息 / 阶段进度条+阶段名+倒计时 / 双角色血条+能量条 /
///       碎片计数 / 设置按钮。全部锚点贴边，不遮挡游戏画面。
///       菜单：DualEnigma/UI/生成 UIGameHUD 预制体。
/// 引用：UIGameHudView.cs, UIGameHudCtrl.cs
/// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DualEnigma.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.Editor
{
    public static class GenerateUIGameHudPrefab
    {
        private const string PREFAB_PATH = "Assets/AssetPackage/Prefabs/UI/UIGameHud/UIGameHud.prefab";
        private const string PREFAB_DIR = "Assets/AssetPackage/Prefabs/UI/UIGameHud";

        // ===== 颜色（与登录/主界面/房间同一套规范）=====
        private static readonly Color32 BAR_BG_COLOR = new Color32(0x26, 0x32, 0x38, 0xCC);
        private static readonly Color32 PHASE_FILL_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 AQUA_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 IGNIS_COLOR = new Color32(0xFF, 0x6F, 0x00, 0xFF);
        private static readonly Color32 ENERGY_COLOR = new Color32(0x26, 0xA6, 0x9A, 0xFF);
        private static readonly Color32 SETTINGS_BTN_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);
        private static readonly Color32 LABEL_COLOR = new Color32(0xB0, 0xBE, 0xC5, 0xFF);

        [MenuItem("DualEnigma/UI/生成 UIGameHUD 预制体")]
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

            Debug.Log("[GenerateUIGameHudPrefab] UIGameHud 预制体已生成: " + PREFAB_PATH);
        }

        private static GameObject BuildHierarchy(Font font)
        {
            GameObject root = new GameObject("UIGameHud");
            SetStretch(root.AddComponent<RectTransform>());
            root.AddComponent<UIGameHudView>();
            root.AddComponent<UIGameHudCtrl>();
            UIAutoBinder binder = root.AddComponent<UIAutoBinder>();
            binder.ViewTypeName = nameof(UIGameHudView);

            // ---- 顶部左侧：关卡信息 ----
            Text levelInfo = CreateText("LevelInfoText", "第1章 1-1 · 第1轮", 16, root.transform, font,
                new Vector2(0, 1), new Vector2(16, -14), new Vector2(260, 22), Color.white);
            levelInfo.alignment = TextAnchor.MiddleLeft;

            // ---- 顶部中央：阶段进度条 + 阶段名 + 倒计时 ----
            GameObject phaseBarBg = CreateImage("PhaseBarBg", root.transform, BAR_BG_COLOR);
            RectTransform barRT = phaseBarBg.GetComponent<RectTransform>();
            barRT.anchorMin = barRT.anchorMax = new Vector2(0.5f, 1f);
            barRT.anchoredPosition = new Vector2(0f, -18f);
            barRT.sizeDelta = new Vector2(320f, 22f);

            GameObject phaseFill = CreateImage("PhaseProgressFill", phaseBarBg.transform, PHASE_FILL_COLOR);
            Image fillImage = phaseFill.GetComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            RectTransform fillRT = phaseFill.GetComponent<RectTransform>();
            SetStretch(fillRT);
            fillRT.offsetMin = new Vector2(2f, 2f);
            fillRT.offsetMax = new Vector2(-2f, -2f);

            CreateText("PhaseNameText", "预告", 16, root.transform, font,
                new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(200f, 22f), Color.white);

            CreateText("PhaseTimerText", "5.0s", 14, root.transform, font,
                new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(120f, 20f), PHASE_FILL_COLOR);

            // ---- 左上：水人血条/能量条（关卡信息下方）----
            BuildVitalsPanel("AquaPanel", root.transform, font,
                new Vector2(0, 1), new Vector2(110, -60), "Aqua", "水人", AQUA_COLOR);

            // ---- 右上：火人血条/能量条 ----
            BuildVitalsPanel("IgnisPanel", root.transform, font,
                new Vector2(1, 1), new Vector2(-110, -60), "Ignis", "火人", IGNIS_COLOR);

            // ---- 顶部右侧：设置按钮 ----
            CreateButton("SettingsBtn", "设置", SETTINGS_BTN_COLOR, root.transform, font,
                new Vector2(1, 1), new Vector2(-16, -14), new Vector2(56f, 30f), 14, "Text");

            // ---- 左下：碎片计数 ----
            Text fragmentText = CreateText("FragmentCountText", "冰×0  熔岩×0  岩石×0", 14, root.transform, font,
                new Vector2(0, 0), new Vector2(16, 16), new Vector2(320f, 22f), LABEL_COLOR);
            fragmentText.alignment = TextAnchor.MiddleLeft;

            return root;
        }

        /// <summary>构建单个角色的状态面板：标题 + HP条 + 能量条（prefix 为 Aqua/Ignis）</summary>
        private static void BuildVitalsPanel(string panelName, Transform parent, Font font,
            Vector2 anchor, Vector2 pos, string prefix, string title, Color32 hpColor)
        {
            GameObject panel = CreateUIObject(panelName, parent);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = panelRT.anchorMax = anchor;
            panelRT.anchoredPosition = pos;
            panelRT.sizeDelta = new Vector2(190f, 62f);

            // 标题（面板左上）
            Text titleText = CreateText(prefix + "TitleText", title, 14, panel.transform, font,
                new Vector2(0, 1), new Vector2(2, -8), new Vector2(80f, 18f), hpColor);
            titleText.alignment = TextAnchor.MiddleLeft;

            // HP 条（面板中央）：背景 + 填充 + 数值
            GameObject hpBg = CreateImage(prefix + "HPBarBg", panel.transform, BAR_BG_COLOR);
            RectTransform hpBgRT = hpBg.GetComponent<RectTransform>();
            hpBgRT.anchorMin = hpBgRT.anchorMax = new Vector2(0.5f, 0.5f);
            hpBgRT.anchoredPosition = new Vector2(0f, 6f);
            hpBgRT.sizeDelta = new Vector2(160f, 16f);

            GameObject hpFill = CreateImage(prefix + "HPFill", hpBg.transform, hpColor);
            Image hpFillImage = hpFill.GetComponent<Image>();
            hpFillImage.type = Image.Type.Filled;
            hpFillImage.fillMethod = Image.FillMethod.Horizontal;
            hpFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpFillImage.fillAmount = 1f;
            RectTransform hpFillRT = hpFill.GetComponent<RectTransform>();
            SetStretch(hpFillRT);
            hpFillRT.offsetMin = new Vector2(2f, 2f);
            hpFillRT.offsetMax = new Vector2(-2f, -2f);

            CreateText(prefix + "HPText", "100/100", 12, hpBg.transform, font,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 14f), Color.white);

            // 能量条（HP 条下方）
            GameObject energyBg = CreateImage(prefix + "EnergyBarBg", panel.transform, BAR_BG_COLOR);
            RectTransform energyBgRT = energyBg.GetComponent<RectTransform>();
            energyBgRT.anchorMin = energyBgRT.anchorMax = new Vector2(0.5f, 0.5f);
            energyBgRT.anchoredPosition = new Vector2(0f, -14f);
            energyBgRT.sizeDelta = new Vector2(160f, 12f);

            GameObject energyFill = CreateImage(prefix + "EnergyFill", energyBg.transform, ENERGY_COLOR);
            Image energyFillImage = energyFill.GetComponent<Image>();
            energyFillImage.type = Image.Type.Filled;
            energyFillImage.fillMethod = Image.FillMethod.Horizontal;
            energyFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            energyFillImage.fillAmount = 1f;
            RectTransform energyFillRT = energyFill.GetComponent<RectTransform>();
            SetStretch(energyFillRT);
            energyFillRT.offsetMin = new Vector2(2f, 2f);
            energyFillRT.offsetMax = new Vector2(-2f, -2f);

            CreateText(prefix + "EnergyText", "100", 11, energyBg.transform, font,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 12f), Color.white);
        }

        private static void BindViewFields(GameObject root)
        {
            UIGameHudView view = root.GetComponent<UIGameHudView>();
            SerializedObject so = new SerializedObject(view);

            BindText(so, root, "m_LevelInfoText", "LevelInfoText");
            BindText(so, root, "m_PhaseNameText", "PhaseNameText");
            BindText(so, root, "m_PhaseTimerText", "PhaseTimerText");
            BindImage(so, root, "m_PhaseProgressFill", "PhaseProgressFill");
            BindText(so, root, "m_AquaTitleText", "AquaTitleText");
            BindImage(so, root, "m_AquaHPFill", "AquaHPFill");
            BindText(so, root, "m_AquaHPText", "AquaHPText");
            BindImage(so, root, "m_AquaEnergyFill", "AquaEnergyFill");
            BindText(so, root, "m_AquaEnergyText", "AquaEnergyText");
            BindText(so, root, "m_IgnisTitleText", "IgnisTitleText");
            BindImage(so, root, "m_IgnisHPFill", "IgnisHPFill");
            BindText(so, root, "m_IgnisHPText", "IgnisHPText");
            BindImage(so, root, "m_IgnisEnergyFill", "IgnisEnergyFill");
            BindText(so, root, "m_IgnisEnergyText", "IgnisEnergyText");
            BindText(so, root, "m_FragmentCountText", "FragmentCountText");
            so.FindProperty("m_SettingsBtn").objectReferenceValue =
                FindDeepChild(root.transform, "SettingsBtn")?.GetComponent<Button>();

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(view);
        }

        private static void BindText(SerializedObject so, GameObject root, string fieldName, string childName)
        {
            so.FindProperty(fieldName).objectReferenceValue =
                FindDeepChild(root.transform, childName)?.GetComponent<Text>();
        }

        private static void BindImage(SerializedObject so, GameObject root, string fieldName, string childName)
        {
            so.FindProperty(fieldName).objectReferenceValue =
                FindDeepChild(root.transform, childName)?.GetComponent<Image>();
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
