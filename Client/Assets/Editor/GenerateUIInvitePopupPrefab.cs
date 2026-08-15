/// ============================================================
/// 文件名: GenerateUIInvitePopupPrefab.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: UIInvitePopup 预制体生成器 Editor 工具。
///       全局邀请弹窗：根节点无 Graphic（不遮挡点击），
///       顶部卡片容器 + 邀请卡片/好友申请卡片两个隐藏模板。
///       菜单：DualEnigma/UI/生成 UIInvitePopup 预制体。
/// 引用：UIInvitePopupView.cs, UIInvitePopupCtrl.cs,
///       InviteCardView.cs, RequestCardView.cs
/// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DualEnigma.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.Editor
{
    public static class GenerateUIInvitePopupPrefab
    {
        private const string PREFAB_PATH = "Assets/AssetPackage/Prefabs/UI/UIInvitePopup/UIInvitePopup.prefab";
        private const string PREFAB_DIR = "Assets/AssetPackage/Prefabs/UI/UIInvitePopup";

        // ===== 颜色（与好友面板同一套规范）=====
        private static readonly Color32 CARD_COLOR = new Color32(0x26, 0x32, 0x38, 0xF0);
        private static readonly Color32 BTN_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 WARN_BTN_COLOR = new Color32(0xEF, 0x53, 0x50, 0xFF);
        private static readonly Color32 TOGGLE_BTN_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);
        private static readonly Color32 LABEL_COLOR = new Color32(0xB0, 0xBE, 0xC5, 0xFF);
        private static readonly Color32 ROOM_CODE_COLOR = new Color32(0xFF, 0xB7, 0x4D, 0xFF);

        private const float CARD_W = 440f;

        [MenuItem("DualEnigma/UI/生成 UIInvitePopup 预制体")]
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

            Debug.Log("[GenerateUIInvitePopupPrefab] UIInvitePopup 预制体已生成: " + PREFAB_PATH);
        }

        private static GameObject BuildHierarchy(Font font)
        {
            // 根节点：全屏拉伸，无 Graphic → 不遮挡任何点击
            GameObject root = new GameObject("UIInvitePopup");
            SetStretch(root.AddComponent<RectTransform>());
            root.AddComponent<UIInvitePopupView>();
            root.AddComponent<UIInvitePopupCtrl>();

            // 卡片容器：顶部居中，自上而下排列
            GameObject container = CreateUIObject("CardContainer", root.transform);
            RectTransform containerRT = container.GetComponent<RectTransform>();
            containerRT.anchorMin = containerRT.anchorMax = new Vector2(0.5f, 1f);
            containerRT.pivot = new Vector2(0.5f, 1f);
            containerRT.anchoredPosition = new Vector2(0f, -12f);
            containerRT.sizeDelta = new Vector2(CARD_W, 0f);

            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 8f;

            // ---- 邀请卡片模板（隐藏）----
            GameObject inviteCard = CreateImage("InviteCardTemplate", container.transform, null, CARD_COLOR);
            SetSize(inviteCard.GetComponent<RectTransform>(), CARD_W, 64f);
            inviteCard.SetActive(false);
            inviteCard.AddComponent<InviteCardView>();
            CreateText("FromText", "XX 邀请你进入房间", 14, inviteCard.transform, font,
                new Vector2(0f, 0.5f), new Vector2(12f, 6f), new Vector2(215f, 22f), Color.white).alignment = TextAnchor.MiddleLeft;
            CreateText("RoomText", "A1B2C3", 18, inviteCard.transform, font,
                new Vector2(0f, 0.5f), new Vector2(232f, 4f), new Vector2(76f, 26f), ROOM_CODE_COLOR).fontStyle = FontStyle.Bold;
            CreateButton("RejectBtn", "拒绝", TOGGLE_BTN_COLOR, inviteCard.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-72f, 0f), new Vector2(56f, 28f), 13, "Text");
            CreateButton("AcceptBtn", "接受", BTN_COLOR, inviteCard.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(56f, 28f), 13, "Text");

            // ---- 好友申请卡片模板（隐藏）----
            GameObject requestCard = CreateImage("RequestCardTemplate", container.transform, null, CARD_COLOR);
            SetSize(requestCard.GetComponent<RectTransform>(), CARD_W, 48f);
            requestCard.SetActive(false);
            requestCard.AddComponent<RequestCardView>();
            CreateText("FromText", "XX 请求加你为好友", 14, requestCard.transform, font,
                new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(300f, 20f), Color.white).alignment = TextAnchor.MiddleLeft;
            CreateButton("ViewBtn", "去处理", WARN_BTN_COLOR, requestCard.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(72f, 28f), 13, "Text");

            return root;
        }

        private static void BindViewFields(GameObject root)
        {
            UIInvitePopupView view = root.GetComponent<UIInvitePopupView>();
            SerializedObject so = new SerializedObject(view);

            so.FindProperty("m_CardContainer").objectReferenceValue =
                FindDeepChild(root.transform, "CardContainer");
            so.FindProperty("m_InviteCardTemplate").objectReferenceValue =
                FindDeepChild(root.transform, "InviteCardTemplate")?.GetComponent<InviteCardView>();
            so.FindProperty("m_RequestCardTemplate").objectReferenceValue =
                FindDeepChild(root.transform, "RequestCardTemplate")?.GetComponent<RequestCardView>();
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(view);

            // 卡片组件字段
            Transform inviteCard = FindDeepChild(root.transform, "InviteCardTemplate");
            if (inviteCard != null)
            {
                SerializedObject cardSo = new SerializedObject(inviteCard.GetComponent<InviteCardView>());
                cardSo.FindProperty("m_FromText").objectReferenceValue =
                    FindDeepChild(inviteCard, "FromText")?.GetComponent<Text>();
                cardSo.FindProperty("m_RoomText").objectReferenceValue =
                    FindDeepChild(inviteCard, "RoomText")?.GetComponent<Text>();
                cardSo.FindProperty("m_AcceptBtn").objectReferenceValue =
                    FindDeepChild(inviteCard, "AcceptBtn")?.GetComponent<Button>();
                cardSo.FindProperty("m_RejectBtn").objectReferenceValue =
                    FindDeepChild(inviteCard, "RejectBtn")?.GetComponent<Button>();
                cardSo.ApplyModifiedProperties();
            }

            Transform requestCard = FindDeepChild(root.transform, "RequestCardTemplate");
            if (requestCard != null)
            {
                SerializedObject cardSo = new SerializedObject(requestCard.GetComponent<RequestCardView>());
                cardSo.FindProperty("m_FromText").objectReferenceValue =
                    FindDeepChild(requestCard, "FromText")?.GetComponent<Text>();
                cardSo.FindProperty("m_ViewBtn").objectReferenceValue =
                    FindDeepChild(requestCard, "ViewBtn")?.GetComponent<Button>();
                cardSo.ApplyModifiedProperties();
            }
        }

        // ===== 通用辅助（与 GenerateUIFriendsPrefab 相同约定）=====

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
