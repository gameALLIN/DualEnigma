/// ============================================================
/// 文件名: UISpecViewBinder.cs
/// 创建时间: 2026-08-20
/// 作者: DualEnigma
/// 描述: View 字段自动绑定（《通用JSON预制体生成器》§5.6）。
///       遍历树中的视图脚本（根 View + 行视图/卡片视图，如 FriendRowView、
///       InviteCardView），把其 [SerializeField] 字段按名绑定到子树节点：
///       节点名匹配 m_Xxx / mi_Xxx / Xxx 三种形式（mi_ 统一映射 m_，
///       与 UIAutoBinder 命名规范一致）；按字段类型取节点上的对应组件
///       （GameObject/Transform/具体组件）。绑定发生在字段所在子树内
///       最近的视图脚本上，不跨越嵌套视图边界。
///       等价于手写生成器中每个面板 60~90 行的 BindViewFields/BindRowView 段。
/// 引用：UISpecPrefabBuilder.cs
/// ============================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using DualEnigma.Framework.UI;
using Object = UnityEngine.Object;

namespace DualEnigma.UI.Editor
{
    /// <summary>View 字段自动绑定器</summary>
    public static class UISpecViewBinder
    {
        // ================================================================
        //  绑定入口
        // ================================================================

        /// <summary>
        /// 遍历整棵构建产物树，对所有绑定目标组件（View/行视图等）执行字段绑定。
        /// 绑定失败不阻断，输出清单到控制台。
        /// </summary>
        /// <param name="root">构建完成的预制体根节点</param>
        public static void Bind(GameObject root)
        {
            int totalBound = 0;
            List<string> unbound = new List<string>();

            // 收集所有绑定目标（含未激活节点）
            foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!IsBindTarget(mb)) continue;
                totalBound += BindComponent(mb, unbound);
            }

            if (totalBound > 0)
                Debug.Log($"[UISpec] View 字段绑定完成: {totalBound} 个字段已绑定。");
            if (unbound.Count > 0)
                Debug.LogWarning("[UISpec] 以下字段未匹配到节点（不影响生成）:\n  " + string.Join("\n  ", unbound));
        }

        /// <summary>
        /// 判断组件是否为绑定目标：项目内（DualEnigma.*）的 MonoBehaviour，
        /// 排除 UIAutoBinder 与 Ctrl（Ctrl 无 [SerializeField] 对象引用字段）。
        /// </summary>
        public static bool IsBindTarget(Component c)
        {
            if (!(c is MonoBehaviour)) return false;
            Type t = c.GetType();
            if (typeof(UIAutoBinder).IsAssignableFrom(t)) return false;
            if (typeof(UICtrlBase).IsAssignableFrom(t)) return false;
            return t.Namespace != null && t.Namespace.StartsWith("DualEnigma", StringComparison.Ordinal);
        }

        /// <summary>类型是否为绑定目标类型（校验期使用）</summary>
        public static bool IsBindTargetType(Type t)
        {
            if (t == null || !typeof(MonoBehaviour).IsAssignableFrom(t)) return false;
            if (typeof(UIAutoBinder).IsAssignableFrom(t)) return false;
            if (typeof(UICtrlBase).IsAssignableFrom(t)) return false;
            return t.Namespace != null && t.Namespace.StartsWith("DualEnigma", StringComparison.Ordinal);
        }

        // ================================================================
        //  单组件绑定
        // ================================================================

        private static int BindComponent(MonoBehaviour target, List<string> unbound)
        {
            int bound = 0;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (!prop.name.StartsWith("m_", StringComparison.Ordinal)) continue;

                Type fieldType = FindFieldType(target.GetType(), prop.name);
                if (fieldType == null || !typeof(Object).IsAssignableFrom(fieldType)) continue;

                Transform node = FindNodeForField(target.transform, prop.name, target);
                if (node == null)
                {
                    unbound.Add($"{target.GetType().Name}.{prop.name}");
                    continue;
                }

                Object value = ResolveReference(node.gameObject, fieldType);
                if (value == null)
                {
                    unbound.Add($"{target.GetType().Name}.{prop.name}（节点 {node.name} 上无 {fieldType.Name} 组件）");
                    continue;
                }

                prop.objectReferenceValue = value;
                bound++;
            }

            if (bound > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
            return bound;
        }

        /// <summary>按字段类型取节点上的对应引用（具体组件 > Transform > GameObject）</summary>
        private static Object ResolveReference(GameObject go, Type fieldType)
        {
            if (fieldType == typeof(GameObject)) return go;
            if (typeof(Component).IsAssignableFrom(fieldType))
                return go.GetComponent(fieldType); // Transform/RectTransform/UGUI 组件均走此分支
            return null;
        }

        // ================================================================
        //  节点查找（不跨越嵌套视图边界）
        // ================================================================

        /// <summary>
        /// 在 scope 子树内查找字段对应节点。
        /// 候选名依次：m_Xxx（原名）→ Xxx（去前缀）→ mi_Xxx（mi_ 形式）。
        /// 不下钻到挂有其他绑定目标组件的子节点（嵌套视图自行绑定）。
        /// </summary>
        private static Transform FindNodeForField(Transform scope, string fieldName, Component self)
        {
            string stripped = fieldName.Substring(2); // m_Xxx → Xxx
            string miForm = "mi_" + stripped;

            return FindByName(scope, fieldName, self)
                ?? FindByName(scope, stripped, self)
                ?? FindByName(scope, miForm, self);
        }

        private static Transform FindByName(Transform scope, string nodeName, Component self)
        {
            return FindRecursive(scope, nodeName, self, isScopeRoot: true);
        }

        private static Transform FindRecursive(Transform node, string nodeName, Component self, bool isScopeRoot)
        {
            if (node.name == nodeName) return node;

            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                // 嵌套视图边界：子节点挂有其他绑定目标组件时不下钻
                if (!HostsOtherBindTarget(child, self))
                {
                    Transform hit = FindRecursive(child, nodeName, self, false);
                    if (hit != null) return hit;
                }
            }
            return null;
        }

        /// <summary>节点是否挂有 self 以外的绑定目标组件</summary>
        private static bool HostsOtherBindTarget(Transform node, Component self)
        {
            foreach (MonoBehaviour mb in node.GetComponents<MonoBehaviour>())
                if (mb != self && IsBindTarget(mb))
                    return true;
            return false;
        }

        // ================================================================
        //  反射辅助
        // ================================================================

        /// <summary>沿类型层级查找实例字段（含私有继承字段）</summary>
        private static Type FindFieldType(Type type, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                FieldInfo fi = t.GetField(fieldName, flags);
                if (fi != null) return fi.FieldType;
            }
            return null;
        }

        /// <summary>收集类型全部可序列化字段名（校验期字段可匹配性检查用）</summary>
        public static HashSet<string> GetSerializableFieldNames(Type type)
        {
            HashSet<string> names = new HashSet<string>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (FieldInfo fi in t.GetFields(flags))
                {
                    if (fi.IsPublic || fi.GetCustomAttribute(typeof(SerializeField)) != null)
                        names.Add(fi.Name);
                }
            }
            return names;
        }
    }
}
