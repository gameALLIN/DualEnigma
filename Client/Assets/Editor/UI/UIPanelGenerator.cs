#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DualEnigma.UI.Editor
{
    /// <summary>
    /// UI 面板自动生成工具。
    /// 通过菜单输入面板名称、作者和描述，自动生成 MVC 三件套代码文件和预制体目录结构。
    /// 菜单路径: DualEnigma > UI > 生成面板
    /// </summary>
    public class UIPanelGenerator : EditorWindow
    {
        // ===== 输入字段 =====

        // 面板名称（必须以 "UI" 开头）
        private string m_PanelName = "";
        // 作者名称
        private string m_AuthorName = "";
        // 面板描述（可选）
        private string m_Description = "";
        // 是否生成 Common 子目录（可选，默认不勾选）
        private bool m_GenerateCommon = false;

        // ===== 常量 =====

        // 标签宽度
        private const float LABEL_WIDTH = 80f;

        // 代码生成根路径（相对于 Assets）
        private const string CODE_ROOT = "Assets/Scripts/UI/Views";
        // 预制体生成根路径（相对于 Assets）
        private const string PREFAB_ROOT = "Assets/AssetPackage/Prefabs/UI";

        [MenuItem("DualEnigma/UI/生成面板")]
        public static void Open()
        {
            // 打开工具窗口
            UIPanelGenerator window = GetWindow<UIPanelGenerator>("UI 面板生成器");
            window.minSize = new Vector2(400, 240);
        }

        private void OnGUI()
        {
            GUILayout.Label("UI 面板生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 面板名称输入
            m_PanelName = EditorGUILayout.TextField("面板名称", m_PanelName);

            // 校验面板名称必须以 "UI" 开头
            if (!string.IsNullOrEmpty(m_PanelName) && !m_PanelName.StartsWith("UI"))
            {
                EditorGUILayout.HelpBox(
                    "面板名称必须以 \"UI\" 开头，例如: UIHome",
                    MessageType.Error);
            }

            // 作者名称输入
            m_AuthorName = EditorGUILayout.TextField("作者", m_AuthorName);

            // 描述输入（可选）
            EditorGUILayout.LabelField("描述（可选）:");
            m_Description = EditorGUILayout.TextArea(
                m_Description,
                GUILayout.MinHeight(60),
                GUILayout.MaxHeight(120));

                // 是否生成 Common 子目录（可选）
                m_GenerateCommon = EditorGUILayout.Toggle("生成 Common 子目录", m_GenerateCommon);

                EditorGUILayout.Space();

            // 生成按钮：面板名必须以 "UI" 开头且作者名不能为空
            bool canGenerate = !string.IsNullOrWhiteSpace(m_PanelName)
                && m_PanelName.StartsWith("UI")
                && !string.IsNullOrWhiteSpace(m_AuthorName);

            EditorGUI.BeginDisabledGroup(!canGenerate);
            {
                if (GUILayout.Button("生成", GUILayout.Height(30)))
                {
                    GeneratePanel();
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        // ===== 生成逻辑 =====

        /// <summary>
        /// 执行面板生成，创建 MVC 三件套代码和预制体目录结构。
        /// </summary>
        private void GeneratePanel()
        {
            string panelName = m_PanelName.Trim();
            string author = m_AuthorName.Trim();
            string description = m_Description.Trim();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 计算绝对路径
            string codeDir = Path.Combine(Application.dataPath, "Scripts/UI/Views", panelName);
            string prefabDir = Path.Combine(Application.dataPath, "AssetPackage/Prefabs/UI", panelName);

            // 代码文件路径
            string ctrlPath = Path.Combine(codeDir, panelName + "Ctrl.cs");
            string modelPath = Path.Combine(codeDir, panelName + "Model.cs");
            string viewPath = Path.Combine(codeDir, panelName + "View.cs");

            // 检查文件是否已存在，提示覆盖
            if (File.Exists(ctrlPath) || File.Exists(modelPath) || File.Exists(viewPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "文件已存在",
                    "面板 \"" + panelName + "\" 的部分代码文件已存在，是否覆盖？",
                    "覆盖",
                    "取消");

                if (!overwrite)
                {
                    return;
                }
            }

            try
            {
                // 创建代码目录
                Directory.CreateDirectory(codeDir);

                // 生成三个代码文件
                File.WriteAllText(ctrlPath, GenerateCtrlCode(panelName, author, description, timestamp));
                File.WriteAllText(modelPath, GenerateModelCode(panelName, author, description, timestamp));
                File.WriteAllText(viewPath, GenerateViewCode(panelName, author, description, timestamp));

                // 创建预制体目录
                Directory.CreateDirectory(prefabDir);

                // 可选：生成 Common 子目录
                if (m_GenerateCommon)
                {
                    string commonDir = Path.Combine(prefabDir, "Common");
                    Directory.CreateDirectory(commonDir);
                    string gitKeepPath = Path.Combine(commonDir, ".gitkeep");
                    if (!File.Exists(gitKeepPath))
                    {
                        File.WriteAllText(gitKeepPath, "");
                    }
                }

                // 刷新资源数据库，使新文件被 Unity 识别
                AssetDatabase.Refresh();

                // 在 Project 窗口中选中生成的文件
                SelectGeneratedFiles(panelName);

                // 输出成功日志
                Debug.Log("[UIPanelGenerator] 面板 \"" + panelName + "\" 生成完成！\n" +
                    "代码目录: " + CODE_ROOT + "/" + panelName + "/\n" +
                    "预制体目录: " + PREFAB_ROOT + "/" + panelName + "/");
            }
            catch (Exception e)
            {
                Debug.LogError("[UIPanelGenerator] 生成失败: " + e.Message);
                EditorUtility.DisplayDialog("生成失败", "生成过程中发生错误:\n" + e.Message, "确定");
            }
        }

        /// <summary>
        /// 在 Project 窗口中选中生成的代码文件。
        /// </summary>
        private void SelectGeneratedFiles(string panelName)
        {
            string ctrlAssetPath = CODE_ROOT + "/" + panelName + "/" + panelName + "Ctrl.cs";
            string modelAssetPath = CODE_ROOT + "/" + panelName + "/" + panelName + "Model.cs";
            string viewAssetPath = CODE_ROOT + "/" + panelName + "/" + panelName + "View.cs";

            MonoScript ctrlScript = AssetDatabase.LoadAssetAtPath<MonoScript>(ctrlAssetPath);
            MonoScript modelScript = AssetDatabase.LoadAssetAtPath<MonoScript>(modelAssetPath);
            MonoScript viewScript = AssetDatabase.LoadAssetAtPath<MonoScript>(viewAssetPath);

            List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
            if (ctrlScript != null) objects.Add(ctrlScript);
            if (modelScript != null) objects.Add(modelScript);
            if (viewScript != null) objects.Add(viewScript);

            if (objects.Count > 0)
            {
                Selection.objects = objects.ToArray();
                EditorGUIUtility.PingObject(objects[0]);
            }
        }

        // ===== 代码模板生成 =====

        /// <summary>
        /// 生成文件头注释。
        /// </summary>
        private static string GenerateFileHeader(string fileName, string author, string description, string timestamp)
        {
            return "/// ============================================================\n" +
                   "/// 文件名: " + fileName + "\n" +
                   "/// 创建时间: " + timestamp + "\n" +
                   "/// 作者: " + author + "\n" +
                   "/// 描述: " + description + "\n" +
                   "/// ============================================================\n\n";
        }

        /// <summary>
        /// 获取描述文本。如果用户未输入描述，则根据角色生成默认描述。
        /// </summary>
        private static string GetDescription(string panelName, string role, string userInput)
        {
            if (!string.IsNullOrWhiteSpace(userInput))
            {
                return userInput;
            }

            switch (role)
            {
                case "Ctrl":
                    return panelName + " 面板控制器，处理用户交互逻辑";
                case "Model":
                    return panelName + " 面板数据层，持有显示数据";
                case "View":
                    return panelName + " 面板视图层，持有 UGUI 组件引用";
                default:
                    return panelName;
            }
        }

        /// <summary>
        /// 生成 Controller 代码。
        /// </summary>
        private static string GenerateCtrlCode(string panelName, string author, string description, string timestamp)
        {
            string fileName = panelName + "Ctrl.cs";
            string desc = GetDescription(panelName, "Ctrl", description);

            StringBuilder sb = new StringBuilder();
            sb.Append(GenerateFileHeader(fileName, author, desc, timestamp));
            sb.Append("using UnityEngine;\n");
            sb.Append("using DualEnigma.Framework.UI;\n\n");
            sb.Append("namespace DualEnigma.UI\n");
            sb.Append("{\n");
            sb.Append("    public class " + panelName + "Ctrl : UICtrlBase\n");
            sb.Append("    {\n");
            sb.Append("        private " + panelName + "Model _model;\n");
            sb.Append("        private " + panelName + "View _view;\n\n");
            sb.Append("        protected override void OnCreate()\n");
            sb.Append("        {\n");
            sb.Append("            _model = new " + panelName + "Model();\n");
            sb.Append("            _view = GetComponent<" + panelName + "View>();\n");
            sb.Append("        }\n\n");
            sb.Append("        protected override void OnShow()\n");
            sb.Append("        {\n");
            sb.Append("        }\n\n");
            sb.Append("        protected override void OnHide()\n");
            sb.Append("        {\n");
            sb.Append("        }\n");
            sb.Append("    }\n");
            sb.Append("}\n");

            return sb.ToString();
        }

        /// <summary>
        /// 生成 Model 代码。
        /// </summary>
        private static string GenerateModelCode(string panelName, string author, string description, string timestamp)
        {
            string fileName = panelName + "Model.cs";
            string desc = GetDescription(panelName, "Model", description);

            StringBuilder sb = new StringBuilder();
            sb.Append(GenerateFileHeader(fileName, author, desc, timestamp));
            sb.Append("using DualEnigma.Framework.UI;\n\n");
            sb.Append("namespace DualEnigma.UI\n");
            sb.Append("{\n");
            sb.Append("    public class " + panelName + "Model : UIModelBase\n");
            sb.Append("    {\n");
            sb.Append("    }\n");
            sb.Append("}\n");

            return sb.ToString();
        }

        /// <summary>
        /// 生成 View 代码。
        /// </summary>
        private static string GenerateViewCode(string panelName, string author, string description, string timestamp)
        {
            string fileName = panelName + "View.cs";
            string desc = GetDescription(panelName, "View", description);

            StringBuilder sb = new StringBuilder();
            sb.Append(GenerateFileHeader(fileName, author, desc, timestamp));
            sb.Append("using UnityEngine;\n");
            sb.Append("using DualEnigma.Framework.UI;\n\n");
            sb.Append("namespace DualEnigma.UI\n");
            sb.Append("{\n");
            sb.Append("    public class " + panelName + "View : UIViewBase\n");
            sb.Append("    {\n");
            sb.Append("        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====\n\n");
            sb.Append("        // ===== Auto Bind End =====\n");
            sb.Append("    }\n");
            sb.Append("}\n");

            return sb.ToString();
        }
    }
}

#endif
