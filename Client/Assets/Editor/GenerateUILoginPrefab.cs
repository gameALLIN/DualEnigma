/// ============================================================
/// 文件名: GenerateUILoginPrefab.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: UILogin 预制体生成器 Editor 工具。自动创建登录/注册面板的
///       完整 UGUI 层级结构，挂载 UILoginView、UILoginCtrl、UIAutoBinder
///       组件，并通过 SerializedObject 自动绑定 View 的所有 [SerializeField]
///       字段。菜单：DualEnigma/UI/生成 UILogin 预制体。
/// 引用：UILoginView.cs, UILoginCtrl.cs, UIAutoBinder.cs, UIPrefabCreator.cs
/// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DualEnigma.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.Editor
{
    /// <summary>
    /// UILogin 预制体生成器。
    /// 自动构建登录/注册面板的 UGUI 层级，绑定 View 字段，保存为预制体。
    /// 层级结构：
    ///   UILogin (View + Ctrl + AutoBinder)
    ///   ├── Background (渐变背景 Image)
    ///   └── Panel (面板背景 Image)
    ///       ├── TitleText
    ///       ├── UsernameGroup → UsernameLabel + UsernameInput
    ///       ├── PasswordGroup → PasswordLabel + PasswordInput
    ///       ├── DisplayNameGroup → DisplayNameLabel + DisplayNameInput
    ///       ├── ErrorText
    ///       ├── SubmitBtn → SubmitBtnText
    ///       ├── ToggleModeBtn → ToggleModeBtnText
    ///       └── LoadingGroup → LoadingText
    /// </summary>
    public static class GenerateUILoginPrefab
    {
        // ===== 路径常量 =====

        /// <summary>预制体保存路径（相对于 Assets）</summary>
        private const string PREFAB_PATH = "Assets/AssetPackage/Prefabs/UI/UILogin/UILogin.prefab";

        /// <summary>预制体输出目录</summary>
        private const string PREFAB_DIR = "Assets/AssetPackage/Prefabs/UI/UILogin";

        /// <summary>渐变背景纹理输出目录</summary>
        private const string TEXTURE_DIR = "Assets/ArtResources/Textures/UI";

        // ===== 颜色常量 =====

        /// <summary>背景渐变顶部色 #1A237E</summary>
        private static readonly Color32 BG_TOP = new Color32(0x1A, 0x23, 0x7E, 0xFF);

        /// <summary>背景渐变底部色 #283593</summary>
        private static readonly Color32 BG_BOTTOM = new Color32(0x28, 0x35, 0x93, 0xFF);

        /// <summary>面板背景色 #263238</summary>
        private static readonly Color32 PANEL_COLOR = new Color32(0x26, 0x32, 0x38, 0xFF);

        /// <summary>输入框背景色 #37474F</summary>
        private static readonly Color32 INPUT_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);

        /// <summary>按钮色 #4FC3F7</summary>
        private static readonly Color32 BTN_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);

        /// <summary>切换模式按钮色（深灰蓝）</summary>
        private static readonly Color32 TOGGLE_BTN_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF);

        /// <summary>标签文本色（浅灰蓝）</summary>
        private static readonly Color32 LABEL_COLOR = new Color32(0xB0, 0xBE, 0xC5, 0xFF);

        /// <summary>错误文本色（红色）</summary>
        private static readonly Color32 ERROR_COLOR = new Color32(0xEF, 0x53, 0x50, 0xFF);

        /// <summary>占位符文本色（半透明灰）</summary>
        private static readonly Color32 PLACEHOLDER_COLOR = new Color32(0x90, 0xA4, 0xAE, 0x80);

        // ===== 布局常量 =====

        /// <summary>面板宽度</summary>
        private const float PANEL_WIDTH = 400f;

        /// <summary>面板高度</summary>
        private const float PANEL_HEIGHT = 560f;

        // ================================================================
        //  菜单入口
        // ================================================================

        /// <summary>
        /// 菜单入口：生成 UILogin 预制体。
        /// </summary>
        [MenuItem("DualEnigma/UI/生成 UILogin 预制体")]
        public static void Generate()
        {
            EnsureDirectory(PREFAB_DIR);
            EnsureDirectory(TEXTURE_DIR);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. 创建渐变背景 Sprite
            Sprite bgSprite = CreateGradientSprite("UILogin_BgGradient", BG_TOP, BG_BOTTOM);

            // 2. 构建 GameObject 层级
            GameObject root = BuildHierarchy(font, bgSprite);

            // 3. 通过 SerializedObject 绑定 UILoginView 字段
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

            Debug.Log("[GenerateUILoginPrefab] UILogin 预制体已生成: " + PREFAB_PATH);
        }

        // ================================================================
        //  层级构建
        // ================================================================

        /// <summary>
        /// 构建 UILogin 的完整 GameObject 层级结构。
        /// </summary>
        /// <param name="font">内置字体</param>
        /// <param name="bgSprite">渐变背景 Sprite</param>
        /// <returns>根节点 GameObject</returns>
        private static GameObject BuildHierarchy(Font font, Sprite bgSprite)
        {
            // ---- 根节点 UILogin ----
            GameObject root = new GameObject("UILogin");
            RectTransform rootRT = root.AddComponent<RectTransform>();
            SetStretch(rootRT);

            // 挂载 View、Ctrl、AutoBinder
            root.AddComponent<UILoginView>();
            root.AddComponent<UILoginCtrl>();
            UIAutoBinder autoBinder = root.AddComponent<UIAutoBinder>();
            autoBinder.ViewTypeName = nameof(UILoginView);

            // ---- Background（全屏渐变背景）----
            GameObject bgObj = CreateImage("Background", root.transform, bgSprite, Color.white);
            SetStretch(bgObj.GetComponent<RectTransform>());

            // ---- Panel（居中面板）----
            GameObject panel = CreateImage("Panel", root.transform, null, PANEL_COLOR);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);

            // ---- Panel 子节点 ----

            // TitleText（顶部居中）
            CreateText("TitleText", "登录", 32, panel.transform, font,
                new Vector2(0.5f, 1f), new Vector2(0f, -40f),
                new Vector2(360f, 40f), Color.white);

            // UsernameGroup
            GameObject usernameGroup = CreateUIObject("UsernameGroup", panel.transform);
            SetTopCenter(usernameGroup.GetComponent<RectTransform>(),
                new Vector2(360f, 40f), new Vector2(0f, -100f));
            CreateText("UsernameLabel", "用户名", 18, usernameGroup.transform, font,
                new Vector2(0f, 0.5f), new Vector2(40f, 0f),
                new Vector2(80f, 36f), LABEL_COLOR);
            CreateInputField("UsernameInput", usernameGroup.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-40f, 0f),
                new Vector2(240f, 36f), "请输入用户名");

            // PasswordGroup
            GameObject passwordGroup = CreateUIObject("PasswordGroup", panel.transform);
            SetTopCenter(passwordGroup.GetComponent<RectTransform>(),
                new Vector2(360f, 40f), new Vector2(0f, -150f));
            CreateText("PasswordLabel", "密码", 18, passwordGroup.transform, font,
                new Vector2(0f, 0.5f), new Vector2(40f, 0f),
                new Vector2(80f, 36f), LABEL_COLOR);
            InputField passwordInput = CreateInputField("PasswordInput",
                passwordGroup.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-40f, 0f),
                new Vector2(240f, 36f), "请输入密码");
            passwordInput.contentType = InputField.ContentType.Password;

            // DisplayNameGroup（默认隐藏，注册模式下显示）
            GameObject displayNameGroup = CreateUIObject("DisplayNameGroup", panel.transform);
            SetTopCenter(displayNameGroup.GetComponent<RectTransform>(),
                new Vector2(360f, 40f), new Vector2(0f, -200f));
            displayNameGroup.SetActive(false);
            CreateText("DisplayNameLabel", "昵称", 18, displayNameGroup.transform, font,
                new Vector2(0f, 0.5f), new Vector2(40f, 0f),
                new Vector2(80f, 36f), LABEL_COLOR);
            CreateInputField("DisplayNameInput", displayNameGroup.transform, font,
                new Vector2(1f, 0.5f), new Vector2(-40f, 0f),
                new Vector2(240f, 36f), "请输入昵称");

            // ErrorText（默认隐藏）
            Text errorText = CreateText("ErrorText", "", 16, panel.transform, font,
                new Vector2(0.5f, 1f), new Vector2(0f, -250f),
                new Vector2(360f, 30f), ERROR_COLOR);
            errorText.gameObject.SetActive(false);

            // SubmitBtn
            CreateButton("SubmitBtn", "登录", BTN_COLOR, panel.transform, font,
                new Vector2(0.5f, 1f), new Vector2(0f, -300f),
                new Vector2(200f, 45f), "SubmitBtnText");

            // ToggleModeBtn
            CreateButton("ToggleModeBtn", "没有账号？去注册",
                TOGGLE_BTN_COLOR, panel.transform, font,
                new Vector2(0.5f, 1f), new Vector2(0f, -360f),
                new Vector2(300f, 35f), "ToggleModeBtnText");

            // LoadingGroup（默认隐藏）
            GameObject loadingGroup = CreateUIObject("LoadingGroup", panel.transform);
            SetTopCenter(loadingGroup.GetComponent<RectTransform>(),
                new Vector2(360f, 30f), new Vector2(0f, -410f));
            loadingGroup.SetActive(false);
            CreateText("LoadingText", "请求中...", 18, loadingGroup.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 0f),
                new Vector2(360f, 30f), Color.white);

            return root;
        }

        // ================================================================
        //  字段绑定
        // ================================================================

        /// <summary>
        /// 通过 SerializedObject 绑定 UILoginView 的所有 [SerializeField] 字段。
        /// 按字段名查找 SerializedProperty，设置对应组件/GameObject 引用。
        /// </summary>
        /// <param name="root">预制体根节点</param>
        private static void BindViewFields(GameObject root)
        {
            UILoginView view = root.GetComponent<UILoginView>();
            SerializedObject so = new SerializedObject(view);

            // ---- 输入框 ----
            so.FindProperty("m_UsernameInput").objectReferenceValue =
                FindDeepChild(root.transform, "UsernameInput")?.GetComponent<InputField>();
            so.FindProperty("m_PasswordInput").objectReferenceValue =
                FindDeepChild(root.transform, "PasswordInput")?.GetComponent<InputField>();
            so.FindProperty("m_DisplayNameInput").objectReferenceValue =
                FindDeepChild(root.transform, "DisplayNameInput")?.GetComponent<InputField>();

            // ---- 按钮 ----
            so.FindProperty("m_SubmitBtn").objectReferenceValue =
                FindDeepChild(root.transform, "SubmitBtn")?.GetComponent<Button>();
            so.FindProperty("m_ToggleModeBtn").objectReferenceValue =
                FindDeepChild(root.transform, "ToggleModeBtn")?.GetComponent<Button>();

            // ---- 文本 ----
            so.FindProperty("m_TitleText").objectReferenceValue =
                FindDeepChild(root.transform, "TitleText")?.GetComponent<Text>();
            so.FindProperty("m_SubmitBtnText").objectReferenceValue =
                FindDeepChild(root.transform, "SubmitBtnText")?.GetComponent<Text>();
            so.FindProperty("m_ToggleModeBtnText").objectReferenceValue =
                FindDeepChild(root.transform, "ToggleModeBtnText")?.GetComponent<Text>();
            so.FindProperty("m_ErrorText").objectReferenceValue =
                FindDeepChild(root.transform, "ErrorText")?.GetComponent<Text>();

            // ---- 容器（GameObject 引用）----
            so.FindProperty("m_DisplayNameGroup").objectReferenceValue =
                FindDeepChild(root.transform, "DisplayNameGroup")?.gameObject;
            so.FindProperty("m_LoadingGroup").objectReferenceValue =
                FindDeepChild(root.transform, "LoadingGroup")?.gameObject;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(view);
        }

        // ================================================================
        //  渐变纹理创建
        // ================================================================

        /// <summary>
        /// 创建垂直渐变 Texture2D 并保存为 Sprite 资产。
        /// 将 Texture2D 保存为 .asset，从持久化纹理重建 Sprite 并保存为单独 .asset。
        /// </summary>
        /// <param name="assetName">资产名称（不含扩展名）</param>
        /// <param name="topColor">渐变顶部颜色</param>
        /// <param name="bottomColor">渐变底部颜色</param>
        /// <returns>持久化的 Sprite 引用</returns>
        private static Sprite CreateGradientSprite(string assetName, Color32 topColor, Color32 bottomColor)
        {
            const int width = 4;
            const int height = 256;

            // 1. 生成渐变 Texture2D（Unity 纹理原点在左下角）
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

            // 2. 保存 Texture2D 为 .asset
            string texPath = TEXTURE_DIR + "/" + assetName + ".asset";
            DeleteExistingAsset(texPath);
            AssetDatabase.CreateAsset(tex, texPath);

            // 3. 从持久化纹理创建 Sprite 并保存
            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Sprite sprite = Sprite.Create(
                savedTex,
                new Rect(0, 0, savedTex.width, savedTex.height),
                new Vector2(0.5f, 0.5f),
                100f, // pixelsPerUnit = 100
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
        /// <param name="name">节点名称</param>
        /// <param name="parent">父级 Transform</param>
        /// <param name="sprite">Image 的 Sprite（null 则不设置）</param>
        /// <param name="color">Image 颜色</param>
        /// <returns>创建的 GameObject</returns>
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
        /// <param name="name">节点名称</param>
        /// <param name="content">文本内容</param>
        /// <param name="fontSize">字号</param>
        /// <param name="parent">父级 Transform</param>
        /// <param name="font">字体</param>
        /// <param name="anchor">锚点</param>
        /// <param name="pos">锚定位置</param>
        /// <param name="size">尺寸</param>
        /// <param name="color">文本颜色</param>
        /// <returns>创建的 Text 组件</returns>
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
        /// <param name="name">按钮节点名称</param>
        /// <param name="label">按钮文本</param>
        /// <param name="bgColor">按钮背景色</param>
        /// <param name="parent">父级 Transform</param>
        /// <param name="font">字体</param>
        /// <param name="anchor">锚点</param>
        /// <param name="pos">锚定位置</param>
        /// <param name="size">按钮尺寸</param>
        /// <param name="textChildName">Text 子节点名称（用于绑定）</param>
        /// <returns>创建的 Button 组件</returns>
        private static Button CreateButton(string name, string label, Color bgColor,
            Transform parent, Font font, Vector2 anchor, Vector2 pos, Vector2 size,
            string textChildName = "Text")
        {
            // 按钮容器
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

            // Text 子节点
            GameObject textObj = CreateUIObject(textChildName, btnObj.transform);
            Text btnText = textObj.AddComponent<Text>();
            btnText.text = label;
            btnText.font = font;
            btnText.fontSize = 20;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.raycastTarget = false;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            SetStretch(textRT);

            return button;
        }

        /// <summary>
        /// 创建完整的 InputField 组件（Image 背景 + Text 子节点 + Placeholder 子节点）。
        /// </summary>
        /// <param name="name">输入框节点名称</param>
        /// <param name="parent">父级 Transform</param>
        /// <param name="font">字体</param>
        /// <param name="anchor">锚点</param>
        /// <param name="pos">锚定位置</param>
        /// <param name="size">输入框尺寸</param>
        /// <param name="placeholder">占位符文本</param>
        /// <returns>创建的 InputField 组件</returns>
        private static InputField CreateInputField(string name, Transform parent, Font font,
            Vector2 anchor, Vector2 pos, Vector2 size, string placeholder)
        {
            // 输入框容器（Image 背景）
            GameObject inputObj = CreateUIObject(name, parent);

            Image inputImage = inputObj.AddComponent<Image>();
            inputImage.color = INPUT_COLOR;

            InputField inputField = inputObj.AddComponent<InputField>();

            RectTransform inputRT = inputObj.GetComponent<RectTransform>();
            inputRT.anchorMin = anchor;
            inputRT.anchorMax = anchor;
            inputRT.anchoredPosition = pos;
            inputRT.sizeDelta = size;

            // Text 子节点（显示输入文本）
            GameObject textObj = CreateUIObject("Text", inputObj.transform);
            Text inputText = textObj.AddComponent<Text>();
            inputText.text = "";
            inputText.font = font;
            inputText.fontSize = 18;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.raycastTarget = false;
            inputText.supportRichText = false;
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10f, 2f);
            textRT.offsetMax = new Vector2(-10f, -2f);

            // Placeholder 子节点
            GameObject placeholderObj = CreateUIObject("Placeholder", inputObj.transform);
            Text placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.font = font;
            placeholderText.fontSize = 18;
            placeholderText.color = PLACEHOLDER_COLOR;
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.raycastTarget = false;
            RectTransform placeholderRT = placeholderObj.GetComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = new Vector2(10f, 2f);
            placeholderRT.offsetMax = new Vector2(-10f, -2f);

            // 关联 InputField 引用
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.text = "";

            return inputField;
        }

        // ================================================================
        //  RectTransform 辅助方法
        // ================================================================

        /// <summary>
        /// 将 RectTransform 设置为拉伸铺满父级（anchorMin=0,0 anchorMax=1,1 offset=0,0）。
        /// </summary>
        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 将 RectTransform 设置为顶部居中锚点。
        /// </summary>
        /// <param name="rt">目标 RectTransform</param>
        /// <param name="size">尺寸</param>
        /// <param name="pos">相对锚点的偏移位置</param>
        private static void SetTopCenter(RectTransform rt, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // ================================================================
        //  层级搜索辅助方法
        // ================================================================

        /// <summary>
        /// 递归查找指定名称的子节点 Transform。
        /// </summary>
        /// <param name="parent">搜索起始 Transform</param>
        /// <param name="name">目标节点名称</param>
        /// <returns>找到的 Transform，未找到返回 null</returns>
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
        /// <param name="path">相对于项目根的完整路径，如 "Assets/AssetPackage/Prefabs/UI"</param>
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
        /// <param name="path">资产路径</param>
        private static void DeleteExistingAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }
    }
}
