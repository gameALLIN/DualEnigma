#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DualEnigma.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.UI.Editor
{
    /// <summary>
    /// UI 预制体创建工具。
    /// 自动创建测试面板 UITest 的预制体，包含 View、Ctrl、AutoBinder 组件和测试文本。
    /// 菜单路径: DualEnigma > UI > 创建测试预制体
    /// </summary>
    public static class UIPrefabCreator
    {
        /// <summary>预制体保存路径（相对于 Assets）</summary>
        private const string PREFAB_PATH = "Assets/AssetPackage/Prefabs/UI/UITest/UITest.prefab";

        [MenuItem("DualEnigma/UI/创建测试预制体")]
        public static void CreateUITestPrefab()
        {
            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. 创建根节点 UITest
            GameObject root = new GameObject("UITest");
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;

            // 2. 挂载 View、Ctrl、AutoBinder 三个组件
            root.AddComponent<UITestView>();
            root.AddComponent<UITestCtrl>();
            root.AddComponent<UIAutoBinder>();

            // 3. m_Background（深色半透明背景，拉伸铺满）
            GameObject bgObj = new GameObject("m_Background");
            bgObj.transform.SetParent(root.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            // 4. m_TitleText（标题，顶部居中）
            CreateText("m_TitleText", "UITest 测试面板", 36, root.transform,
                new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(400f, 60f), builtinFont);

            // 5. m_CountText（计数显示，居中偏上）
            CreateText("m_CountText", "计数: 0", 28, root.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(300f, 50f), builtinFont);

            // 6. 三个按钮
            CreateButton("m_BtnAdd", "计数 +1", new Color(0.2f, 0.6f, 1f), root.transform, new Vector2(0f, -20f), builtinFont);
            CreateButton("m_BtnReset", "重置", new Color(1f, 0.6f, 0.2f), root.transform, new Vector2(0f, -90f), builtinFont);
            CreateButton("m_BtnClose", "关闭面板", new Color(0.9f, 0.2f, 0.2f), root.transform, new Vector2(0f, -160f), builtinFont);

            // 7. 确保目录存在
            string absoluteDir = Path.Combine(Application.dataPath, "AssetPackage", "Prefabs", "UI", "UITest");
            Directory.CreateDirectory(absoluteDir);

            // 8. 保存为预制体
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);

            // 9. 销毁临时对象
            UnityEngine.Object.DestroyImmediate(root);

            // 10. 刷新资源数据库
            AssetDatabase.Refresh();

            // 11. 在 Project 窗口中选中并高亮预制体
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            Debug.Log($"[UIPrefabCreator] UITest 预制体已创建: {PREFAB_PATH}");
        }

        /// <summary>创建文本节点</summary>
        private static void CreateText(string name, string content, int fontSize,
            Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Font font)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Text text = obj.AddComponent<Text>();
            text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.raycastTarget = false;
            text.font = font;
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        /// <summary>创建按钮节点（Image 背景 + Button + Text 子节点）</summary>
        private static void CreateButton(string name, string label, Color bgColor,
            Transform parent, Vector2 pos, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = bgColor;

            Button button = btnObj.AddComponent<Button>();
            button.targetGraphic = btnImage;

            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0.5f);
            btnRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnRT.anchoredPosition = pos;
            btnRT.sizeDelta = new Vector2(200f, 50f);

            // Text 子节点
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            Text btnText = textObj.AddComponent<Text>();
            btnText.text = label;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.fontSize = 20;
            btnText.color = Color.white;
            btnText.raycastTarget = false;
            btnText.font = font;
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }
    }
}
#endif
