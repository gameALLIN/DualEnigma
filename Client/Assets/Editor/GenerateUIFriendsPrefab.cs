/// ============================================================
/// 文件名: GenerateUIFriendsPrefab.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: UIFriends 预制体生成器 Editor 工具。
///       层级：标题 / 房间邀请区 / 好友申请区 / 好友列表(滚动) /
///       搜索区 / 状态提示 / 关闭按钮，含三种行模板。
///       菜单：DualEnigma/UI/生成 UIFriends 预制体。
/// 引用：UIFriendsView.cs, UIFriendsCtrl.cs
/// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DualEnigma.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.Editor
{
    public static class GenerateUIFriendsPrefab
    {
        private const string PREFAB_PATH = "Assets/AssetPackage/Prefabs/UI/UIFriends/UIFriends.prefab";
        private const string PREFAB_DIR = "Assets/AssetPackage/Prefabs/UI/UIFriends";

        // ===== 颜色（与登录/主界面同一套规范）=====
        private static readonly Color32 PANEL_COLOR = new Color32(0x26, 0x32, 0x38, 0xFF);
        private static readonly Color32 SECTION_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);
        private static readonly Color32 ROW_COLOR = new Color32(0x2E, 0x3D, 0x45, 0xFF);
        private static readonly Color32 INPUT_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);
        private static readonly Color32 BTN_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 WARN_BTN_COLOR = new Color32(0xEF, 0x53, 0x50, 0xFF);
        private static readonly Color32 TOGGLE_BTN_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);
        private static readonly Color32 LABEL_COLOR = new Color32(0xB0, 0xBE, 0xC5, 0xFF);
        private static readonly Color32 INVITE_SECTION_COLOR = new Color32(0x37, 0x2A, 0x18, 0xFF);

        // 面板 720x640
        private const float PANEL_W = 720f;
        private const float PANEL_H = 640f;

        [MenuItem("DualEnigma/UI/生成 UIFriends 预制体")]
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

            Debug.Log("[GenerateUIFriendsPrefab] UIFriends 预制体已生成: " + PREFAB_PATH);
        }

        private static GameObject BuildHierarchy(Font font)
        {
            GameObject root = new GameObject("UIFriends");
            SetStretch(root.AddComponent<RectTransform>());
            root.AddComponent<UIFriendsView>();
            root.AddComponent<UIFriendsCtrl>();
            UIAutoBinder binder = root.AddComponent<UIAutoBinder>();
            binder.ViewTypeName = nameof(UIFriendsView);

            // 全屏半透明底（点不到后面）
            GameObject dim = CreateImage("Dim", root.transform, null, new Color32(0, 0, 0, 0x88));
            SetStretch(dim.GetComponent<RectTransform>());

            // 主面板
            GameObject panel = CreateImage("Panel", root.transform, null, PANEL_COLOR);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(PANEL_W, PANEL_H);

            // 标题 + 关闭
            CreateText("TitleText", "好友", 30, panel.transform, font,
                new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(300f, 40f), Color.white);
            CreateButton("CloseBtn", "✕", TOGGLE_BTN_COLOR, panel.transform, font,
                new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(48f, 48f), 20, "Text");

            // ---- 房间邀请区（默认隐藏）----
            GameObject inviteSection = CreateImage("InviteSection", panel.transform, null, INVITE_SECTION_COLOR);
            SetTop(inviteSection.GetComponent<RectTransform>(), new Vector2(680f, 70f), new Vector2(0f, -64f));
            inviteSection.SetActive(false);
            CreateText("InviteSectionTitle", "房间邀请", 16, inviteSection.transform, font,
                new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(200f, 20f), new Color32(0xFF, 0xB7, 0x4D, 0xFF))
                .alignment = TextAnchor.MiddleLeft;
            GameObject inviteList = CreateUIObject("InviteListContent", inviteSection.transform);
            SetStretchOffset(inviteList.GetComponent<RectTransform>(), 8f, 6f, 8f, 26f);
            AttachVerticalLayout(inviteList);
            // 邀请行模板（隐藏）
            GameObject inviteRow = CreateImage("InviteRowTemplate", inviteList.transform, null, ROW_COLOR);
            SetSize(inviteRow.GetComponent<RectTransform>(), 656f, 30f);
            inviteRow.SetActive(false);
            inviteRow.AddComponent<InviteRowView>();
            CreateText("FromText", "XX 邀请你进入房间 XXXX", 14, inviteRow.transform, font,
                new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(420f, 24f), Color.white).alignment = TextAnchor.MiddleLeft;
            CreateButton("AcceptBtn", "接受", BTN_COLOR, inviteRow.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-130f, 0f), new Vector2(56f, 24f), 13, "Text");
            CreateButton("RejectBtn", "拒绝", TOGGLE_BTN_COLOR, inviteRow.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-66f, 0f), new Vector2(56f, 24f), 13, "Text");

            // ---- 好友申请区（默认隐藏）----
            GameObject requestSection = CreateImage("RequestSection", panel.transform, null, SECTION_COLOR);
            SetTop(requestSection.GetComponent<RectTransform>(), new Vector2(680f, 70f), new Vector2(0f, -142f));
            requestSection.SetActive(false);
            CreateText("RequestSectionTitle", "好友申请", 16, requestSection.transform, font,
                new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(200f, 20f), LABEL_COLOR).alignment = TextAnchor.MiddleLeft;
            GameObject requestList = CreateUIObject("RequestListContent", requestSection.transform);
            SetStretchOffset(requestList.GetComponent<RectTransform>(), 8f, 6f, 8f, 26f);
            AttachVerticalLayout(requestList);
            // 申请行模板（隐藏）
            GameObject requestRow = CreateImage("RequestRowTemplate", requestList.transform, null, ROW_COLOR);
            SetSize(requestRow.GetComponent<RectTransform>(), 656f, 30f);
            requestRow.SetActive(false);
            requestRow.AddComponent<RequestRowView>();
            CreateText("FromText", "XX 请求加你为好友", 14, requestRow.transform, font,
                new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(420f, 24f), Color.white).alignment = TextAnchor.MiddleLeft;
            CreateButton("AcceptBtn", "接受", BTN_COLOR, requestRow.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-130f, 0f), new Vector2(56f, 24f), 13, "Text");
            CreateButton("RejectBtn", "拒绝", TOGGLE_BTN_COLOR, requestRow.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-66f, 0f), new Vector2(56f, 24f), 13, "Text");

            // ---- 好友列表（滚动）----
            CreateText("FriendSectionTitle", "好友列表", 16, panel.transform, font,
                new Vector2(0f, 1f), new Vector2(24f, -228f), new Vector2(200f, 20f), LABEL_COLOR).alignment = TextAnchor.MiddleLeft;

            GameObject scroll = CreateUIObject("FriendScroll", panel.transform);
            RectTransform scrollRT = scroll.GetComponent<RectTransform>();
            SetTop(scrollRT, new Vector2(672f, 260f), new Vector2(0f, -246f));

            Image scrollBg = scroll.AddComponent<Image>();
            scrollBg.color = ROW_COLOR;
            ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();

            GameObject viewport = CreateUIObject("Viewport", scroll.transform);
            SetStretch(viewport.GetComponent<RectTransform>());
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            Image viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = Color.white;

            GameObject content = CreateUIObject("FriendListContent", viewport.transform);
            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 300f);
            AttachVerticalLayout(content);

            scrollRect.content = contentRT;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.scrollSensitivity = 20f;

            // 好友行模板（隐藏）
            GameObject friendRow = CreateImage("FriendRowTemplate", content.transform, null, SECTION_COLOR);
            SetSize(friendRow.GetComponent<RectTransform>(), 648f, 34f);
            friendRow.SetActive(false);
            friendRow.AddComponent<FriendRowView>();
            CreateText("NameText", "昵称 (用户名)", 15, friendRow.transform, font,
                new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(320f, 26f), Color.white).alignment = TextAnchor.MiddleLeft;
            CreateText("IdText", "ID: 0", 13, friendRow.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-232f, 0f), new Vector2(130f, 24f), LABEL_COLOR).alignment = TextAnchor.MiddleRight;
            CreateButton("InviteBtn", "邀请", BTN_COLOR, friendRow.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-80f, 0f), new Vector2(64f, 26f), 13, "Text");
            CreateButton("DeleteBtn", "删除", WARN_BTN_COLOR, friendRow.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(48f, 26f), 13, "Text");

            // ---- 搜索区（底部）----
            CreateInputField("SearchInput", panel.transform, font,
                new Vector2(0f, 0.5f), new Vector2(196f, -276f), new Vector2(340f, 36f), "输入用户名/昵称搜索");
            CreateButton("SearchBtn", "搜索", BTN_COLOR, panel.transform, font,
                new Vector2(0f, 0.5f), new Vector2(384f, -276f), new Vector2(90f, 36f), 15, "Text");

            // ---- 状态提示 ----
            CreateText("StatusText", "", 14, panel.transform, font,
                new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(600f, 26f), LABEL_COLOR).gameObject.SetActive(false);

            return root;
        }

        private static void BindViewFields(GameObject root)
        {
            UIFriendsView view = root.GetComponent<UIFriendsView>();
            SerializedObject so = new SerializedObject(view);

            so.FindProperty("m_InviteSection").objectReferenceValue =
                FindDeepChild(root.transform, "InviteSection")?.gameObject;
            so.FindProperty("m_RequestSection").objectReferenceValue =
                FindDeepChild(root.transform, "RequestSection")?.gameObject;
            so.FindProperty("m_InviteListContent").objectReferenceValue =
                FindDeepChild(root.transform, "InviteListContent");
            so.FindProperty("m_RequestListContent").objectReferenceValue =
                FindDeepChild(root.transform, "RequestListContent");
            so.FindProperty("m_FriendListContent").objectReferenceValue =
                FindDeepChild(root.transform, "FriendListContent");
            so.FindProperty("m_FriendRowTemplate").objectReferenceValue =
                FindDeepChild(root.transform, "FriendRowTemplate")?.GetComponent<FriendRowView>();
            so.FindProperty("m_RequestRowTemplate").objectReferenceValue =
                FindDeepChild(root.transform, "RequestRowTemplate")?.GetComponent<RequestRowView>();
            so.FindProperty("m_InviteRowTemplate").objectReferenceValue =
                FindDeepChild(root.transform, "InviteRowTemplate")?.GetComponent<InviteRowView>();
            so.FindProperty("m_SearchInput").objectReferenceValue =
                FindDeepChild(root.transform, "SearchInput")?.GetComponent<InputField>();
            so.FindProperty("m_SearchBtn").objectReferenceValue =
                FindDeepChild(root.transform, "SearchBtn")?.GetComponent<Button>();
            so.FindProperty("m_StatusText").objectReferenceValue =
                FindDeepChild(root.transform, "StatusText")?.GetComponent<Text>();
            so.FindProperty("m_CloseBtn").objectReferenceValue =
                FindDeepChild(root.transform, "CloseBtn")?.GetComponent<Button>();

            // 行组件字段
            BindRowView<InviteRowView>(root, "InviteRowTemplate");
            BindRowView<RequestRowView>(root, "RequestRowTemplate");
            BindFriendRow(root);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(view);
        }

        private static void BindRowView<T>(GameObject root, string rowName) where T : MonoBehaviour
        {
            Transform row = FindDeepChild(root.transform, rowName);
            if (row == null) return;

            SerializedObject so = new SerializedObject(row.GetComponent<T>());
            so.FindProperty("m_FromText").objectReferenceValue =
                FindDeepChild(row, "FromText")?.GetComponent<Text>();
            so.FindProperty("m_AcceptBtn").objectReferenceValue =
                FindDeepChild(row, "AcceptBtn")?.GetComponent<Button>();
            so.FindProperty("m_RejectBtn").objectReferenceValue =
                FindDeepChild(row, "RejectBtn")?.GetComponent<Button>();
            so.ApplyModifiedProperties();
        }

        private static void BindFriendRow(GameObject root)
        {
            Transform row = FindDeepChild(root.transform, "FriendRowTemplate");
            if (row == null) return;

            SerializedObject so = new SerializedObject(row.GetComponent<FriendRowView>());
            so.FindProperty("m_NameText").objectReferenceValue =
                FindDeepChild(row, "NameText")?.GetComponent<Text>();
            so.FindProperty("m_IdText").objectReferenceValue =
                FindDeepChild(row, "IdText")?.GetComponent<Text>();
            so.FindProperty("m_InviteBtn").objectReferenceValue =
                FindDeepChild(row, "InviteBtn")?.GetComponent<Button>();
            so.FindProperty("m_DeleteBtn").objectReferenceValue =
                FindDeepChild(row, "DeleteBtn")?.GetComponent<Button>();
            so.ApplyModifiedProperties();
        }

        private static void AttachVerticalLayout(GameObject obj)
        {
            VerticalLayoutGroup layout = obj.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 4f;
        }

        // ===== 通用辅助（与 GenerateUILoginPrefab 相同约定）=====

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

        private static InputField CreateInputField(string name, Transform parent, Font font,
            Vector2 anchor, Vector2 pos, Vector2 size, string placeholder)
        {
            GameObject inputObj = CreateUIObject(name, parent);
            Image inputImage = inputObj.AddComponent<Image>();
            inputImage.color = INPUT_COLOR;
            InputField inputField = inputObj.AddComponent<InputField>();

            RectTransform inputRT = inputObj.GetComponent<RectTransform>();
            inputRT.anchorMin = inputRT.anchorMax = anchor;
            inputRT.anchoredPosition = pos;
            inputRT.sizeDelta = size;

            GameObject textObj = CreateUIObject("Text", inputObj.transform);
            Text inputText = textObj.AddComponent<Text>();
            inputText.text = "";
            inputText.font = font;
            inputText.fontSize = 15;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.raycastTarget = false;
            inputText.supportRichText = false;
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10f, 2f);
            textRT.offsetMax = new Vector2(-10f, -2f);

            GameObject placeholderObj = CreateUIObject("Placeholder", inputObj.transform);
            Text placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.font = font;
            placeholderText.fontSize = 15;
            placeholderText.color = new Color32(0x90, 0xA4, 0xAE, 0x80);
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.raycastTarget = false;
            RectTransform placeholderRT = placeholderObj.GetComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = new Vector2(10f, 2f);
            placeholderRT.offsetMax = new Vector2(-10f, -2f);

            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.text = "";
            return inputField;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetStretchOffset(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTop(RectTransform rt, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void SetSize(RectTransform rt, float w, float h)
        {
            rt.sizeDelta = new Vector2(w, h);
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
