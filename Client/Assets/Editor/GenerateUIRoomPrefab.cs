/// ============================================================
/// 文件名: GenerateUIRoomPrefab.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: UIRoom 预制体生成器 Editor 工具。
///       层级：标题 / 房间码大字 / 状态文案 / 提示文案 / 邀请+退出按钮。
///       菜单：DualEnigma/UI/生成 UIRoom 预制体。
/// 引用：UIRoomView.cs, UIRoomCtrl.cs
/// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DualEnigma.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.Editor
{
    public static class GenerateUIRoomPrefab
    {
        private const string PREFAB_PATH = "Assets/AssetPackage/Prefabs/UI/UIRoom/UIRoom.prefab";
        private const string PREFAB_DIR = "Assets/AssetPackage/Prefabs/UI/UIRoom";

        // ===== 颜色（与登录/主界面同一套规范）=====
        private static readonly Color32 PANEL_COLOR = new Color32(0x26, 0x32, 0x38, 0xFF);
        private static readonly Color32 CODE_BG_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);
        private static readonly Color32 BTN_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 LEAVE_BTN_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);
        private static readonly Color32 INVITE_BTN_COLOR = new Color32(0xFF, 0x6F, 0x00, 0xFF);
        private static readonly Color32 LABEL_COLOR = new Color32(0xB0, 0xBE, 0xC5, 0xFF);
        private static readonly Color32 CODE_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);

        private const float PANEL_W = 480f;
        private const float PANEL_H = 360f;

        [MenuItem("DualEnigma/UI/生成 UIRoom 预制体")]
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

            Debug.Log("[GenerateUIRoomPrefab] UIRoom 预制体已生成: " + PREFAB_PATH);
        }

        private static GameObject BuildHierarchy(Font font)
        {
            GameObject root = new GameObject("UIRoom");
            SetStretch(root.AddComponent<RectTransform>());
            root.AddComponent<UIRoomView>();
            root.AddComponent<UIRoomCtrl>();
            UIAutoBinder binder = root.AddComponent<UIAutoBinder>();
            binder.ViewTypeName = nameof(UIRoomView);

            // 全屏半透明底
            GameObject dim = CreateImage("Dim", root.transform, null, new Color32(0, 0, 0, 0x88));
            SetStretch(dim.GetComponent<RectTransform>());

            // 主面板
            GameObject panel = CreateImage("Panel", root.transform, null, PANEL_COLOR);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(PANEL_W, PANEL_H);

            // 标题
            CreateText("TitleText", "房间", 30, panel.transform, font,
                new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(300f, 40f), Color.white);

            // 房间码背景块 + 大字
            GameObject codeBg = CreateImage("CodeBg", panel.transform, null, CODE_BG_COLOR);
            RectTransform codeRT = codeBg.GetComponent<RectTransform>();
            codeRT.anchorMin = codeRT.anchorMax = new Vector2(0.5f, 0.5f);
            codeRT.anchoredPosition = new Vector2(0f, 40f);
            codeRT.sizeDelta = new Vector2(320f, 90f);
            CreateText("RoomCodeText", "------", 52, codeBg.transform, font,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 70f), CODE_COLOR);

            // 状态文案
            CreateText("StatusText", "等待好友加入...", 20, panel.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(400f, 30f), Color.white);

            // 提示文案
            CreateText("TipText", "点击【邀请好友】发送房间邀请，或把房间码告诉对方", 14, panel.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(440f, 24f), LABEL_COLOR);

            // 邀请好友（主操作，叠加打开好友面板，不断开连接）
            CreateButton("InviteBtn", "邀请好友", INVITE_BTN_COLOR, panel.transform, font,
                new Vector2(0.5f, 0f), new Vector2(-100f, 42f), new Vector2(180f, 40f), 16, "Text");

            // 退出按钮
            CreateButton("LeaveBtn", "退出房间", LEAVE_BTN_COLOR, panel.transform, font,
                new Vector2(0.5f, 0f), new Vector2(100f, 42f), new Vector2(180f, 40f), 16, "Text");

            return root;
        }

        private static void BindViewFields(GameObject root)
        {
            UIRoomView view = root.GetComponent<UIRoomView>();
            SerializedObject so = new SerializedObject(view);

            so.FindProperty("m_RoomCodeText").objectReferenceValue =
                FindDeepChild(root.transform, "RoomCodeText")?.GetComponent<Text>();
            so.FindProperty("m_StatusText").objectReferenceValue =
                FindDeepChild(root.transform, "StatusText")?.GetComponent<Text>();
            so.FindProperty("m_TipText").objectReferenceValue =
                FindDeepChild(root.transform, "TipText")?.GetComponent<Text>();
            so.FindProperty("m_InviteBtn").objectReferenceValue =
                FindDeepChild(root.transform, "InviteBtn")?.GetComponent<Button>();
            so.FindProperty("m_LeaveBtn").objectReferenceValue =
                FindDeepChild(root.transform, "LeaveBtn")?.GetComponent<Button>();

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(view);
        }

        // ===== 通用辅助 =====

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = sprite != null;
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
