/// ============================================================
/// 文件名: GenerateFriendItemPrefab.cs
/// 创建时间: 2026-08-18
/// 最后更新: 2026-08-18
/// 作者: DualEnigma
/// 描述: FriendItem 通用好友条目预制体生成器 Editor 工具。
///       行内子控件由 HorizontalLayoutGroup + LayoutElement 排布：
///       昵称列弹性占满、状态/ID/按钮按首选宽度右聚；
///       与 FriendItem.BuildChildren 结构一致（656×34 行）。
///       菜单：DualEnigma/UI/生成 FriendItem 预制体（Common）。
/// 引用：FriendItem.cs
/// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DualEnigma.UI;

namespace DualEnigma.Editor
{
    public static class GenerateFriendItemPrefab
    {
        private const string PREFAB_PATH = "Assets/AssetPackage/Prefabs/UI/Common/FriendItem.prefab";
        private const string PREFAB_DIR = "Assets/AssetPackage/Prefabs/UI/Common";

        // ===== 配色与 FriendItem 常量保持一致 =====
        private static readonly Color32 PRIMARY_BTN_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 DANGER_BTN_COLOR = new Color32(0xEF, 0x53, 0x50, 0xFF);
        private static readonly Color32 FRIEND_ROW_BG = new Color32(0x37, 0x47, 0x4F, 0xFF);
        private static readonly Color32 LABEL_COLOR = new Color32(0xB0, 0xBE, 0xC5, 0xFF);

        [MenuItem("DualEnigma/UI/生成 FriendItem 预制体（Common）")]
        public static void Generate()
        {
            EnsureDirectory(PREFAB_DIR);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject root = BuildHierarchy(font);
            BindItemFields(root);

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

            Debug.Log("[GenerateFriendItemPrefab] FriendItem 预制体已生成: " + PREFAB_PATH);
        }

        private static GameObject BuildHierarchy(Font font)
        {
            GameObject root = new GameObject("FriendItem");
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(FriendItem.ROW_WIDTH, FriendItem.ROW_HEIGHT);

            // 行底色 Image（raycastTarget 保持 true，与运行时构建一致）
            Image bg = root.AddComponent<Image>();
            bg.color = FRIEND_ROW_BG;

            root.AddComponent<FriendItem>();

            // 水平列表容器：昵称弹性占满，其余列按首选宽度右聚
            HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateText("NameText", "昵称 (用户名)", 15, new Vector2(240f, 26f), Color.white,
                TextAnchor.MiddleLeft, font, root.transform, flexibleWidth: 1f);
            CreateText("StatusText", "离线", 13, new Vector2(120f, 24f), LABEL_COLOR,
                TextAnchor.MiddleLeft, font, root.transform);
            CreateText("IdText", "ID: 0", 13, new Vector2(120f, 24f), LABEL_COLOR,
                TextAnchor.MiddleRight, font, root.transform);

            CreateButton("PrimaryBtn", "邀请", PRIMARY_BTN_COLOR, new Vector2(64f, 26f), 13, font, root.transform);
            CreateButton("SecondaryBtn", "删除", DANGER_BTN_COLOR, new Vector2(48f, 26f), 13, font, root.transform);

            return root;
        }

        private static void BindItemFields(GameObject root)
        {
            FriendItem item = root.GetComponent<FriendItem>();
            SerializedObject so = new SerializedObject(item);

            so.FindProperty("m_Bg").objectReferenceValue = root.GetComponent<Image>();
            so.FindProperty("m_NameText").objectReferenceValue =
                FindDeepChild(root.transform, "NameText")?.GetComponent<Text>();
            so.FindProperty("m_StatusText").objectReferenceValue =
                FindDeepChild(root.transform, "StatusText")?.GetComponent<Text>();
            so.FindProperty("m_IdText").objectReferenceValue =
                FindDeepChild(root.transform, "IdText")?.GetComponent<Text>();
            so.FindProperty("m_PrimaryBtn").objectReferenceValue =
                FindDeepChild(root.transform, "PrimaryBtn")?.GetComponent<Button>();
            so.FindProperty("m_SecondaryBtn").objectReferenceValue =
                FindDeepChild(root.transform, "SecondaryBtn")?.GetComponent<Button>();

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(item);
        }

        // ===== 通用辅助（水平列表容器版） =====

        private static Text CreateText(string name, string content, int fontSize, Vector2 size, Color color,
            TextAnchor alignment, Font font, Transform parent, float flexibleWidth = 0f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            Text text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
            le.flexibleWidth = flexibleWidth;
            return text;
        }

        private static Button CreateButton(string name, string label, Color32 bgColor, Vector2 size,
            int fontSize, Font font, Transform parent)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.AddComponent<RectTransform>();
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = bgColor;
            btnImage.raycastTarget = true;
            Button button = btnObj.AddComponent<Button>();
            button.targetGraphic = btnImage;

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            textObj.AddComponent<RectTransform>();
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
