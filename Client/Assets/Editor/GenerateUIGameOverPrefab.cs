/// ============================================================
/// 文件名: GenerateUIGameOverPrefab.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: UIGameOver 预制体生成器 Editor 工具。
///       层级：全屏 Dim / 中央面板（胜负大标题 + 进度副标题 + 再来一局 + 返回主界面）。
///       菜单：DualEnigma/UI/生成 UIGameOver 预制体。
/// 引用：UIGameOverView.cs, UIGameOverCtrl.cs
/// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DualEnigma.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.Editor
{
    public static class GenerateUIGameOverPrefab
    {
        private const string PREFAB_PATH = "Assets/AssetPackage/Prefabs/UI/UIGameOver/UIGameOver.prefab";
        private const string PREFAB_DIR = "Assets/AssetPackage/Prefabs/UI/UIGameOver";

        // ===== 颜色（与登录/主界面/房间同一套规范）=====
        private static readonly Color32 PANEL_COLOR = new Color32(0x26, 0x32, 0x38, 0xFF);
        private static readonly Color32 RESTART_BTN_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 HOME_BTN_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);

        private const float PANEL_W = 400f;
        private const float PANEL_H = 320f;

        [MenuItem("DualEnigma/UI/生成 UIGameOver 预制体")]
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

            Debug.Log("[GenerateUIGameOverPrefab] UIGameOver 预制体已生成: " + PREFAB_PATH);
        }

        private static GameObject BuildHierarchy(Font font)
        {
            GameObject root = new GameObject("UIGameOver");
            SetStretch(root.AddComponent<RectTransform>());
            root.AddComponent<UIGameOverView>();
            root.AddComponent<UIGameOverCtrl>();
            UIAutoBinder binder = root.AddComponent<UIAutoBinder>();
            binder.ViewTypeName = nameof(UIGameOverView);

            // 全屏半透明底（结算期间阻挡游戏输入）
            GameObject dim = CreateImage("Dim", root.transform, new Color32(0, 0, 0, 0x99));
            dim.GetComponent<Image>().raycastTarget = true;
            SetStretch(dim.GetComponent<RectTransform>());

            // 主面板
            GameObject panel = CreateImage("Panel", root.transform, PANEL_COLOR);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(PANEL_W, PANEL_H);
            panel.GetComponent<Image>().raycastTarget = true;

            // 胜负大标题（颜色由 Ctrl 按胜负设置）
            CreateText("TitleText", "胜  利", 36, panel.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 84f), new Vector2(320f, 52f), Color.white);

            // 进度副标题
            CreateText("SubtitleText", "止步于 第1章 1-1 · 第1轮", 15, panel.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(320f, 22f),
                new Color32(0xB0, 0xBE, 0xC5, 0xFF));

            // 再来一局（仅单机显示，联机隐藏）
            CreateButton("RestartBtn", "再来一局", RESTART_BTN_COLOR, panel.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(260f, 40f), 16, "Text");

            // 返回主界面
            CreateButton("HomeBtn", "返回主界面", HOME_BTN_COLOR, panel.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(260f, 40f), 16, "Text");

            return root;
        }

        private static void BindViewFields(GameObject root)
        {
            UIGameOverView view = root.GetComponent<UIGameOverView>();
            SerializedObject so = new SerializedObject(view);

            so.FindProperty("m_TitleText").objectReferenceValue =
                FindDeepChild(root.transform, "TitleText")?.GetComponent<Text>();
            so.FindProperty("m_SubtitleText").objectReferenceValue =
                FindDeepChild(root.transform, "SubtitleText")?.GetComponent<Text>();
            so.FindProperty("m_RestartBtn").objectReferenceValue =
                FindDeepChild(root.transform, "RestartBtn")?.GetComponent<Button>();
            so.FindProperty("m_HomeBtn").objectReferenceValue =
                FindDeepChild(root.transform, "HomeBtn")?.GetComponent<Button>();

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
