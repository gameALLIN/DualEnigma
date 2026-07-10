#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DualEnigma.UI;

namespace DualEnigma.UI.Editor
{
    /// <summary>
    /// UI 组件自动绑定工具。
    /// 作为 UIAutoBinder 的 CustomEditor，在 Inspector 上提供「Auto Bind」按钮。
    /// 点击后递归扫描预制体下所有符合命名规范（m_Xxx / mi_Xxx）的节点，
    /// 自动在对应 View 脚本中生成/更新 [SerializeField] 字段，并绑定组件引用。
    /// </summary>
    [CustomEditor(typeof(UIAutoBinder))]
    public class UIBindingGenerator : UnityEditor.Editor
    {
        // ==================== 常量 ====================

        /// <summary>自动绑定区域起始标记</summary>
        private const string BEGIN_MARKER = "// ===== Auto Bind Fields（自动绑定，请勿手动修改）=====";

        /// <summary>自动绑定区域结束标记</summary>
        private const string END_MARKER = "// ===== Auto Bind End =====";

        /// <summary>字段声明缩进（8 空格 = 2 级 × 4 空格）</summary>
        private const string INDENT = "        ";

        // ==================== 数据结构 ====================

        /// <summary>扫描结果条目</summary>
        private struct BindEntry
        {
            /// <summary>字段名（mi_ 前缀已转为 m_）</summary>
            public string FieldName;

            /// <summary>原始节点名</summary>
            public string OriginalName;

            /// <summary>组件类型名（如 Button、Text、Transform）</summary>
            public string TypeName;

            /// <summary>组件引用</summary>
            public Component Component;
        }

        // ==================== Inspector ====================

        public override void OnInspectorGUI()
        {
            // 绘制默认 Inspector（显示 m_ViewTypeName 字段）
            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            // Auto Bind 按钮
            if (GUILayout.Button("Auto Bind", GUILayout.Height(32)))
            {
                ExecuteBinding();
            }
        }

        // ==================== 主流程 ====================

        /// <summary>
        /// 执行自动绑定：扫描节点 → 更新 View 脚本 → 绑定引用
        /// </summary>
        private void ExecuteBinding()
        {
            UIAutoBinder binder = (UIAutoBinder)target;
            GameObject root = binder.gameObject;

            // 1. 查找 View 组件
            UIViewBase view = FindViewComponent(root);
            if (view == null)
            {
                Debug.LogError("[UIBindingGenerator] 未在预制体根节点上找到 UIViewBase 子类组件，无法执行绑定。");
                return;
            }

            // 自动检测 View 类型名（如果未手动填写）
            if (string.IsNullOrEmpty(binder.ViewTypeName))
            {
                SerializedObject binderSO = new SerializedObject(binder);
                binderSO.FindProperty("m_ViewTypeName").stringValue = view.GetType().Name;
                binderSO.ApplyModifiedProperties();
            }

            // 2. 递归扫描子节点
            List<BindEntry> entries = ScanNodes(root);
            Debug.Log($"[UIBindingGenerator] 扫描完成：共找到 {entries.Count} 个符合命名规范的节点。");

            // 3. 更新 View 脚本文件
            bool scriptModified = UpdateViewScript(view, entries);

            // 4. 绑定预制体引用
            BindReferences(view, entries);

            // 5. 提示
            if (scriptModified)
            {
                Debug.Log("[UIBindingGenerator] View 脚本已更新，请等待编译完成后再次点击 Auto Bind 以绑定新增字段的引用。");
            }
            else
            {
                Debug.Log("[UIBindingGenerator] 自动绑定完成。");
            }
        }

        // ==================== View 组件查找 ====================

        /// <summary>
        /// 在预制体根节点上查找 UIViewBase 子类组件。
        /// 如果 UIAutoBinder 上指定了 ViewTypeName，优先按类型名查找。
        /// </summary>
        private UIViewBase FindViewComponent(GameObject root)
        {
            UIAutoBinder binder = (UIAutoBinder)target;

            // 优先按指定的类型名查找
            if (!string.IsNullOrEmpty(binder.ViewTypeName))
            {
                Type viewType = FindTypeByName(binder.ViewTypeName);
                if (viewType != null)
                {
                    Component comp = root.GetComponent(viewType);
                    if (comp is UIViewBase view)
                        return view;
                }
                Debug.LogWarning($"[UIBindingGenerator] 未找到类型 {binder.ViewTypeName}，尝试自动检测。");
            }

            // 自动检测：查找 UIViewBase 子类
            return root.GetComponent<UIViewBase>();
        }

        /// <summary>在所有已加载程序集中按全名或简名查找类型</summary>
        private static Type FindTypeByName(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 先按简名查找
                Type type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            // 简名未命中，遍历所有类型按 Name 匹配
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException)
                {
                    // 部分程序集可能无法加载所有类型，跳过
                    continue;
                }

                foreach (var t in types)
                {
                    if (t.Name == typeName)
                        return t;
                }
            }

            return null;
        }

        // ==================== 节点扫描 ====================

        /// <summary>
        /// 递归扫描预制体下所有子节点，匹配命名规范并检测组件类型。
        /// </summary>
        private List<BindEntry> ScanNodes(GameObject root)
        {
            List<BindEntry> entries = new List<BindEntry>();
            HashSet<string> fieldNames = new HashSet<string>();

            // 获取所有子节点（包含不激活的节点）
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                // 跳过根节点自身
                if (t == root.transform)
                    continue;

                string nodeName = t.name;

                // 解析字段名
                if (!TryGetFieldName(nodeName, out string fieldName))
                    continue;

                // 检查字段名重复
                if (fieldNames.Contains(fieldName))
                {
                    Debug.LogWarning($"[UIBindingGenerator] 字段名重复: {fieldName}（节点: {nodeName}），已跳过。");
                    continue;
                }

                // 检测组件类型
                string typeName = DetectComponentType(t.gameObject, out Component component);

                entries.Add(new BindEntry
                {
                    FieldName = fieldName,
                    OriginalName = nodeName,
                    TypeName = typeName,
                    Component = component
                });

                fieldNames.Add(fieldName);
            }

            return entries;
        }

        /// <summary>
        /// 解析节点名为字段名。
        /// m_Xxx → m_Xxx（保持原名）
        /// mi_Xxx → m_Xxx（mi_ 前缀统一转为 m_）
        /// 不符合规范返回 false。
        /// </summary>
        private static bool TryGetFieldName(string nodeName, out string fieldName)
        {
            fieldName = null;

            // m_ 前缀：m_ 后第一个字符必须大写
            if (nodeName.StartsWith("m_") && nodeName.Length > 2 && IsUpperCase(nodeName[2]))
            {
                fieldName = nodeName;
                return true;
            }

            // mi_ 前缀：mi_ 后第一个字符必须大写
            if (nodeName.StartsWith("mi_") && nodeName.Length > 3 && IsUpperCase(nodeName[3]))
            {
                fieldName = "m_" + nodeName.Substring(3);
                return true;
            }

            return false;
        }

        /// <summary>判断字符是否为大写字母 A-Z</summary>
        private static bool IsUpperCase(char c)
        {
            return c >= 'A' && c <= 'Z';
        }

        // ==================== 组件类型检测 ====================

        /// <summary>
        /// 按优先级检测 GameObject 上的组件类型。
        /// 优先级: Button > Text > TMP_Text > Image > RawImage > Slider > Toggle > InputField > TMP_InputField > ScrollRect > Transform
        /// </summary>
        private static string DetectComponentType(GameObject go, out Component component)
        {
            component = null;

            // 1. Button
            component = go.GetComponent<Button>();
            if (component != null) return "Button";

            // 2. Text
            component = go.GetComponent<Text>();
            if (component != null) return "Text";

            // 3. TMP_Text（通过反射，避免硬依赖 TextMeshPro）
            Type tmpTextType = FindTypeByName("TMPro.TMP_Text");
            if (tmpTextType != null)
            {
                component = go.GetComponent(tmpTextType);
                if (component != null) return "TMP_Text";
            }

            // 4. Image
            component = go.GetComponent<Image>();
            if (component != null) return "Image";

            // 5. RawImage
            component = go.GetComponent<RawImage>();
            if (component != null) return "RawImage";

            // 6. Slider
            component = go.GetComponent<Slider>();
            if (component != null) return "Slider";

            // 7. Toggle
            component = go.GetComponent<Toggle>();
            if (component != null) return "Toggle";

            // 8. InputField
            component = go.GetComponent<InputField>();
            if (component != null) return "InputField";

            // 9. TMP_InputField（通过反射）
            Type tmpInputType = FindTypeByName("TMPro.TMP_InputField");
            if (tmpInputType != null)
            {
                component = go.GetComponent(tmpInputType);
                if (component != null) return "TMP_InputField";
            }

            // 10. ScrollRect
            component = go.GetComponent<ScrollRect>();
            if (component != null) return "ScrollRect";

            // 11. Transform（兜底，所有节点都有 Transform）
            component = go.GetComponent<Transform>();
            return "Transform";
        }

        // ==================== View 脚本更新 ====================

        /// <summary>
        /// 读取 View 脚本文件，在自动绑定区域内更新字段声明。
        /// 返回 true 表示脚本内容有变更。
        /// </summary>
        private bool UpdateViewScript(UIViewBase view, List<BindEntry> entries)
        {
            // 获取脚本文件路径
            MonoScript monoScript = MonoScript.FromMonoBehaviour(view);
            if (monoScript == null)
            {
                Debug.LogError("[UIBindingGenerator] 无法获取 View 脚本的 MonoScript 资源。");
                return false;
            }

            string scriptAssetPath = AssetDatabase.GetAssetPath(monoScript);
            if (string.IsNullOrEmpty(scriptAssetPath))
            {
                Debug.LogError("[UIBindingGenerator] 无法获取 View 脚本的文件路径。");
                return false;
            }

            string fullPath = Path.GetFullPath(scriptAssetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError("[UIBindingGenerator] View 脚本文件不存在: " + fullPath);
                return false;
            }

            string content = File.ReadAllText(fullPath);
            string originalContent = content;

            // 生成字段声明
            string fieldDeclarations = GenerateFieldDeclarations(entries);

            // 查找自动绑定区域标记
            int beginPos = content.IndexOf(BEGIN_MARKER);
            int endPos = content.IndexOf(END_MARKER);

            if (beginPos >= 0 && endPos >= 0 && beginPos < endPos)
            {
                // 区域标记已存在，替换区域内容
                content = ReplaceAutoBindRegion(content, beginPos, endPos, fieldDeclarations);
            }
            else
            {
                // 区域标记不存在，在类体开头插入
                string className = view.GetType().Name;
                int classBodyStart = FindClassBodyOpenBrace(content, className);
                if (classBodyStart < 0)
                {
                    Debug.LogError($"[UIBindingGenerator] 无法在脚本中找到类 {className} 的类体起始位置。");
                    return false;
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("\n").Append(INDENT).Append(BEGIN_MARKER).Append("\n");
                if (!string.IsNullOrEmpty(fieldDeclarations))
                {
                    sb.Append(fieldDeclarations).Append("\n");
                }
                sb.Append(INDENT).Append(END_MARKER).Append("\n");

                content = content.Substring(0, classBodyStart + 1)
                        + sb.ToString()
                        + content.Substring(classBodyStart + 1);
            }

            // 确保必要的 using 指令
            content = EnsureUsingDirectives(content, entries);

            // 有变更才写回
            if (content != originalContent)
            {
                try
                {
                    File.WriteAllText(fullPath, content, new UTF8Encoding(false));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UIBindingGenerator] 写入 View 脚本失败: {fullPath}\n{e.Message}");
                    return false;
                }
                AssetDatabase.ImportAsset(scriptAssetPath);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 生成对齐后的字段声明文本。
        /// 类型名按最长类型名右填充空格对齐。
        /// </summary>
        private static string GenerateFieldDeclarations(List<BindEntry> entries)
        {
            if (entries.Count == 0)
                return string.Empty;

            // 计算最长类型名长度
            int maxTypeLength = 0;
            foreach (var entry in entries)
            {
                if (entry.TypeName.Length > maxTypeLength)
                    maxTypeLength = entry.TypeName.Length;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                BindEntry entry = entries[i];
                // 类型名右填充空格到最长长度，再加 1 个空格分隔
                string paddedType = entry.TypeName.PadRight(maxTypeLength);
                sb.Append(INDENT)
                  .Append("[SerializeField] private ")
                  .Append(paddedType).Append(' ')
                  .Append(entry.FieldName)
                  .Append(';');

                if (i < entries.Count - 1)
                    sb.Append('\n');
            }

            return sb.ToString();
        }

        /// <summary>
        /// 替换自动绑定区域内的内容（保留标记本身）。
        /// </summary>
        private static string ReplaceAutoBindRegion(string content, int beginPos, int endPos, string fieldDeclarations)
        {
            // 找到 begin 标记所在行的行首
            int lineStart = content.LastIndexOf('\n', beginPos);
            lineStart = (lineStart < 0) ? 0 : lineStart + 1;

            // 找到 end 标记所在行的行尾（含换行符）
            int lineEnd = content.IndexOf('\n', endPos + END_MARKER.Length);
            lineEnd = (lineEnd < 0) ? content.Length : lineEnd + 1;

            StringBuilder sb = new StringBuilder();
            sb.Append(INDENT).Append(BEGIN_MARKER).Append('\n');
            if (!string.IsNullOrEmpty(fieldDeclarations))
            {
                sb.Append(fieldDeclarations).Append('\n');
            }
            sb.Append(INDENT).Append(END_MARKER).Append('\n');

            return content.Substring(0, lineStart) + sb.ToString() + content.Substring(lineEnd);
        }

        /// <summary>
        /// 在脚本内容中查找指定类名对应的类体起始花括号位置。
        /// 返回 '{' 字符的索引，未找到返回 -1。
        /// </summary>
        private static int FindClassBodyOpenBrace(string content, string className)
        {
            string searchPattern = "class " + className;
            int classIndex = content.IndexOf(searchPattern);
            if (classIndex < 0)
                return -1;

            // 从类名后开始搜索第一个 '{'
            for (int i = classIndex + searchPattern.Length; i < content.Length; i++)
            {
                if (content[i] == '{')
                    return i;
                // 遇到分号说明可能是前向声明或接口，放弃
                if (content[i] == ';')
                    return -1;
            }
            return -1;
        }

        // ==================== Using 指令管理 ====================

        /// <summary>
        /// 根据扫描到的组件类型，确保 View 脚本包含必要的 using 指令。
        /// </summary>
        private static string EnsureUsingDirectives(string content, List<BindEntry> entries)
        {
            // UnityEngine.UI 命名空间下的类型
            HashSet<string> unityUITypes = new HashSet<string>
            {
                "Button", "Text", "Image", "RawImage",
                "Slider", "Toggle", "InputField", "ScrollRect"
            };

            // TMPro 命名空间下的类型
            HashSet<string> tmpTypes = new HashSet<string> { "TMP_Text", "TMP_InputField" };

            bool needsUnityEngineUI = false;
            bool needsTMPro = false;

            foreach (var entry in entries)
            {
                if (unityUITypes.Contains(entry.TypeName))
                    needsUnityEngineUI = true;
                if (tmpTypes.Contains(entry.TypeName))
                    needsTMPro = true;
            }

            if (needsUnityEngineUI)
                content = EnsureUsingDirective(content, "using UnityEngine.UI;");
            if (needsTMPro)
                content = EnsureUsingDirective(content, "using TMPro;");

            return content;
        }

        /// <summary>
        /// 确保脚本包含指定的 using 指令。已存在则跳过。
        /// 插入位置为最后一个 using 指令之后。
        /// </summary>
        private static string EnsureUsingDirective(string content, string usingLine)
        {
            if (content.Contains(usingLine))
                return content;

            // 按行分割，找到最后一个 using 指令的位置
            string[] lines = content.Split('\n');
            int lastUsingIndex = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("using "))
                {
                    lastUsingIndex = i;
                }
                else if (trimmed.StartsWith("namespace ") ||
                         trimmed.StartsWith("public ") ||
                         trimmed.StartsWith("internal ") ||
                         trimmed.StartsWith("["))
                {
                    // 遇到命名空间或类声明就停止搜索
                    break;
                }
            }

            if (lastUsingIndex >= 0)
            {
                var newLines = new List<string>(lines);
                newLines.Insert(lastUsingIndex + 1, usingLine);
                return string.Join("\n", newLines);
            }

            // 没有 using 指令，添加到文件开头
            return usingLine + "\n" + content;
        }

        // ==================== 预制体引用绑定 ====================

        /// <summary>
        /// 使用 SerializedObject 将组件引用写入 View 脚本的 [SerializeField] 字段。
        /// 如果字段尚未编译（新增字段），该字段会被跳过，提示用户再次执行。
        /// </summary>
        private void BindReferences(UIViewBase view, List<BindEntry> entries)
        {
            SerializedObject serializedObject = new SerializedObject(view);
            int boundCount = 0;
            int missingCount = 0;

            foreach (var entry in entries)
            {
                SerializedProperty prop = serializedObject.FindProperty(entry.FieldName);
                if (prop != null)
                {
                    prop.objectReferenceValue = entry.Component;
                    boundCount++;
                }
                else
                {
                    missingCount++;
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(view);
            AssetDatabase.SaveAssets();

            if (boundCount > 0)
                Debug.Log($"[UIBindingGenerator] 引用绑定完成: {boundCount} 个字段已绑定。");

            if (missingCount > 0)
                Debug.LogWarning($"[UIBindingGenerator] {missingCount} 个字段尚未编译，请等待编译完成后再次点击 Auto Bind。");
        }
    }
}
#endif
