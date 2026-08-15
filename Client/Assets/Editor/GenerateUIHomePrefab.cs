/// ============================================================
/// 文件名: GenerateUIHomePrefab.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: UIHome 预制体生成器 Editor 工具。自动创建主界面的完整 UGUI
///       层级结构（渐变背景 + 标题区 + 冰火双色装饰条 + 玩家信息卡 +
///       开始/退出按钮 + 版本号），挂载 UIHomeView、UIHomeCtrl、UIAutoBinder
///       组件，并通过 SerializedObject 自动绑定 View 字段。
///       菜单：DualEnigma/UI/生成 UIHome 预制体。
/// 引用：UIHomeView.cs, UIHomeCtrl.cs, UIAutoBinder.cs
/// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DualEnigma.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.Editor
{
    /// <summary>
    /// UIHome 预制体生成器。
    /// 层级结构：
    ///   UIHome (View + Ctrl + AutoBinder)
    ///   ├── Background (渐变背景 Image)
    ///   ├── TitleText "双生迷城"
    ///   ├── SubTitleText "DUAL ENIGMA · 双人协作生存"
    ///   ├── DecorBar (冰火双色装饰条)
    ///   │   ├── WaterBar (#4FC3F7)
    ///   │   └── FireBar (#FF6F00)
    ///   ├── PlayerCard (玩家信息卡)
    ///   │   ├── AvatarBg → AvatarText (昵称首字)
    ///   │   ├── DisplayNameText
    ///   │   └── AccountIdText
    ///   ├── StartBtn → Text
    ///   ├── LogoutBtn → Text
    ///   └── VersionText (右下角)
    /// </summary>
    public static class GenerateUIHomePrefab
    {
        // ===== 路径常量 =====

        /// <summary>预制体保存路径（相对于 Assets）</summary>
        private const string PREFAB_PATH = "Assets/AssetPackage/Prefabs/UI/UIHome/UIHome.prefab";

        /// <summary>预制体输出目录</summary>
        private const string PREFAB_DIR = "Assets/AssetPackage/Prefabs/UI/UIHome";

        /// <summary>渐变背景纹理输出目录</summary>
        private const string TEXTURE_DIR = "Assets/ArtResources/Textures/UI";

        // ===== 颜色常量（与 GenerateUILoginPrefab 保持同一套视觉规范）=====

        /// <summary>背景渐变顶部色 #1A237E</summary>
        private static readonly Color32 BG_TOP = new Color32(0x1A, 0x23, 0x7E, 0xFF);

        /// <summary>背景渐变底部色 #283593</summary>
        private static readonly Color32 BG_BOTTOM = new Color32(0x28, 0x35, 0x93, 0xFF);

        /// <summary>玩家信息卡背景色（深青灰半透明）</summary>
        private static readonly Color32 CARD_COLOR = new Color32(0x26, 0x32, 0x38, 0xD8);

        /// <summary>头像背景色（水蓝）</summary>
        private static readonly Color32 AVATAR_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);

        /// <summary>开始按钮色（水蓝）</summary>
        private static readonly Color32 START_BTN_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);

        /// <summary>退出按钮色（深灰蓝）</summary>
        private static readonly Color32 LOGOUT_BTN_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);

        /// <summary>冰元素装饰条（水蓝）</summary>
        private static readonly Color32 WATER_BAR_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);

        /// <summary>火元素装饰条（熔岩橙）</summary>
        private static readonly Color32 FIRE_BAR_COLOR = new Color32(0xFF, 0x6F, 0x00, 0xFF);

        /// <summary>副标题/信息文本色（浅灰蓝）</summary>
        private static readonly Color32 LABEL_COLOR = new Color32(0xB0, 0xBE, 0xC5, 0xFF);

        /// <summary>玩家信息卡宽度</summary>
        private const float CARD_WIDTH = 420f;

        /// <summary>玩家信息卡高度</summary>
        private const float CARD_HEIGHT = 90f;

        // ================================================================
        //  菜单入口
        // ================================================================

        /// <summary>
        /// 菜单入口：生成 UIHome 预制体。
        /// </summary>
        [MenuItem("DualEnigma/UI/生成 UIHome 预制体")]
        public static void Generate()
        {
            EnsureDirectory(PREFAB_DIR);
            EnsureDirectory(TEXTURE_DIR);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. 创建渐变背景 Sprite
            Sprite bgSprite = CreateGradientSprite("UIHome_BgGradient", BG_TOP, BG_BOTTOM);

            // 2. 构建 GameObject 层级
            GameObject root = BuildHierarchy(font, bgSprite);

            // 3. 通过 SerializedObject 绑定 UIHomeView 字段
            BindViewFields(root);

            // 4. 保存为预制体
            DeleteExistingAsset(PREFAB_PATH);
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);

            // 5. 清理临时对象
            Object.DestroyImmediate(root);

            // 6. 刷新资源数据库
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 7. 在 Project 窗口中选中并高亮预制体
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            Debug.Log("[GenerateUIHomePrefab] UIHome 预制体已生成: " + PREFAB_PATH);
        }

        // ================================================================
        //  层级构建
        // ================================================================

        /// <summary>
        /// 构建 UIHome 的完整 GameObject 层级结构。
        /// </summary>
        private static GameObject BuildHierarchy(Font font, Sprite bgSprite)
        {
            // ---- 根节点 UIHome ----
            GameObject root = new GameObject("UIHome");
            RectTransform rootRT = root.AddComponent<RectTransform>();
            SetStretch(rootRT);

            root.AddComponent<UIHomeView>();
            root.AddComponent<UIHomeCtrl>();
            UIAutoBinder autoBinder = root.AddComponent<UIAutoBinder>();
            autoBinder.ViewTypeName = nameof(UIHomeView);

            // ---- Background（全屏渐变背景）----
            GameObject bgObj = CreateImage("Background", root.transform, bgSprite, Color.white);
            SetStretch(bgObj.GetComponent<RectTransform>());

            // ---- 标题区（屏幕居中布局，以面板中心为原点）----

            // TitleText
            CreateText("TitleText", "双生迷城", 56, root.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 220f),
                new Vector2(600f, 70f), Color.white);

            // SubTitleText
            CreateText("SubTitleText", "DUAL ENIGMA · 双人协作生存", 18, root.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 172f),
                new Vector2(500f, 26f), LABEL_COLOR);

            // DecorBar（冰火双色装饰条，水蓝左 + 火橙右）
            GameObject waterBar = CreateImage("WaterBar", root.transform, null, WATER_BAR_COLOR);
            SetCenter(waterBar.GetComponent<RectTransform>(), new Vector2(150f, 6f), new Vector2(-80f, 146f));
            GameObject fireBar = CreateImage("FireBar", root.transform, null, FIRE_BAR_COLOR);
            SetCenter(fireBar.GetComponent<RectTransform>(), new Vector2(150f, 6f), new Vector2(80f, 146f));

            // ---- 玩家信息卡 ----
            GameObject card = CreateImage("PlayerCard", root.transform, null, CARD_COLOR);
            SetCenter(card.GetComponent<RectTransform>(), new Vector2(CARD_WIDTH, CARD_HEIGHT), new Vector2(0f, 30f));

            // AvatarBg + AvatarText（昵称首字头像占位）
            GameObject avatarBg = CreateImage("AvatarBg", card.transform, null, AVATAR_COLOR);
            SetCenter(avatarBg.GetComponent<RectTransform>(), new Vector2(64f, 64f), new Vector2(-158f, 0f));
            CreateText("AvatarText", "?", 32, avatarBg.transform, font,
                new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(64f, 64f), Color.white);

            // DisplayNameText（昵称，卡片左中）
            CreateText("DisplayNameText", "旅行者", 22, card.transform, font,
                new Vector2(0f, 0.5f), new Vector2(-105f, 12f),
                new Vector2(230f, 30f), Color.white).alignment = TextAnchor.MiddleLeft;

            // AccountIdText（账号 ID，昵称下方）
            CreateText("AccountIdText", "ID: 0", 14, card.transform, font,
                new Vector2(0f, 0.5f), new Vector2(-105f, -16f),
                new Vector2(230f, 22f), LABEL_COLOR).alignment = TextAnchor.MiddleLeft;

            // ---- 按钮区 ----

            // StartBtn（主按钮）
            CreateButton("StartBtn", "开始游戏", START_BTN_COLOR, root.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -90f),
                new Vector2(260f, 56f), 24, "Text");

            // FriendsBtn（好友入口）
            CreateButton("FriendsBtn", "好友", LOGOUT_BTN_COLOR, root.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -158f),
                new Vector2(260f, 44f), 17, "Text");

            // LogoutBtn（次按钮）
            CreateButton("LogoutBtn", "退出登录", LOGOUT_BTN_COLOR, root.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -212f),
                new Vector2(200f, 36f), 16, "Text");

            // ---- 版本号（右下角）----
            CreateText("VersionText", "v0.1", 14, root.transform, font,
                new Vector2(1f, 0f), new Vector2(-20f, 20f),
                new Vector2(300f, 24f), LABEL_COLOR).alignment = TextAnchor.MiddleRight;

            return root;
        }

        // ================================================================
        //  字段绑定
        // ================================================================

        /// <summary>
        /// 通过 SerializedObject 绑定 UIHomeView 的所有 [SerializeField] 字段。
        /// </summary>
        private static void BindViewFields(GameObject root)
        {
            UIHomeView view = root.GetComponent<UIHomeView>();
            SerializedObject so = new SerializedObject(view);

            // ---- 玩家信息卡 ----
            so.FindProperty("m_AvatarText").objectReferenceValue =
                FindDeepChild(root.transform, "AvatarText")?.GetComponent<Text>();
            so.FindProperty("m_DisplayNameText").objectReferenceValue =
                FindDeepChild(root.transform, "DisplayNameText")?.GetComponent<Text>();
            so.FindProperty("m_AccountIdText").objectReferenceValue =
                FindDeepChild(root.transform, "AccountIdText")?.GetComponent<Text>();

            // ---- 按钮 ----
            so.FindProperty("m_StartBtn").objectReferenceValue =
                FindDeepChild(root.transform, "StartBtn")?.GetComponent<Button>();
            so.FindProperty("m_FriendsBtn").objectReferenceValue =
                FindDeepChild(root.transform, "FriendsBtn")?.GetComponent<Button>();
            so.FindProperty("m_LogoutBtn").objectReferenceValue =
                FindDeepChild(root.transform, "LogoutBtn")?.GetComponent<Button>();

            // ---- 文本 ----
            so.FindProperty("m_VersionText").objectReferenceValue =
                FindDeepChild(root.transform, "VersionText")?.GetComponent<Text>();

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(view);
        }

        // ================================================================
        //  渐变纹理创建
        // ================================================================

        /// <summary>
        /// 创建垂直渐变 Texture2D 并保存为 Sprite 资产。
        /// </summary>
        private static Sprite CreateGradientSprite(string assetName, Color32 topColor, Color32 bottomColor)
        {
            const int width = 4;
            const int height = 256;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                Color32 rowColor = Color32.Lerp(bottomColor, topColor, t);
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = rowColor;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.name = assetName;

            string texPath = TEXTURE_DIR + "/" + assetName + ".asset";
            DeleteExistingAsset(texPath);
            AssetDatabase.CreateAsset(tex, texPath);

            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Sprite sprite = Sprite.Create(
                savedTex,
                new Rect(0, 0, savedTex.width, savedTex.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect
            );
            sprite.name = assetName;

            string spritePath = TEXTURE_DIR + "/" + assetName + "_Sprite.asset";
            DeleteExistingAsset(spritePath);
            AssetDatabase.CreateAsset(sprite, spritePath);

            return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        // ================================================================
        //  UI 组件创建辅助方法
        // ================================================================

        /// <summary>
        /// 创建带 RectTransform 的空 UI GameObject。
        /// </summary>
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        /// <summary>
        /// 创建带 Image 组件的 GameObject。
        /// </summary>
        private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = sprite != null;
            return go;
        }

        /// <summary>
        /// 创建带 Text 组件的 GameObject。
        /// </summary>
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

        /// <summary>
        /// 创建带 Image + Button 组件的 GameObject，并附带一个 Text 子节点。
        /// </summary>
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
            btnRT.anchorMin = anchor;
            btnRT.anchorMax = anchor;
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

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            SetStretch(textRT);

            return button;
        }

        // ================================================================
        //  RectTransform 辅助方法
        // ================================================================

        /// <summary>
        /// 将 RectTransform 设置为拉伸铺满父级。
        /// </summary>
        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 将 RectTransform 设置为居中锚点（anchorMin=anchorMax=0.5,0.5）。
        /// </summary>
        private static void SetCenter(RectTransform rt, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // ================================================================
        //  层级搜索辅助方法
        // ================================================================

        /// <summary>
        /// 递归查找指定名称的子节点 Transform。
        /// </summary>
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

        // ================================================================
        //  资产管理辅助方法
        // ================================================================

        /// <summary>
        /// 确保目录存在，不存在则逐级创建。
        /// </summary>
        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// <summary>
        /// 如果指定路径已存在资产，则删除（覆盖更新）。
        /// </summary>
        private static void DeleteExistingAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }
    }
}
