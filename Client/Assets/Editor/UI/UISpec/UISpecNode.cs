/// ============================================================
/// 文件名: UISpecNode.cs
/// 创建时间: 2026-08-20
/// 作者: DualEnigma
/// 描述: ui-spec 节点数据契约（POCO）。对应《通用JSON预制体生成器》§四
///       v1.2 规范：name/active/components/anchors/pivot/position/size/
///       text/fontSize/align/color/background/layout/children 及扩展字段
///       ref/note/sprite/fontStyle/scale/rotation。
///       使用 Unity 内置 JsonUtility 反序列化（Schema 固定，零第三方依赖）。
/// 引用：UISpecExtractor.cs, UISpecPrefabBuilder.cs
/// ============================================================

using System;
using UnityEngine;

namespace DualEnigma.UI.Editor
{
    /// <summary>锚点定义（0~1 区间，对应 anchorMin/anchorMax）</summary>
    [Serializable]
    public class UISpecAnchors
    {
        /// <summary>左下锚点 [x, y]</summary>
        public float[] min = { 0.5f, 0.5f };

        /// <summary>右上锚点 [x, y]</summary>
        public float[] max = { 0.5f, 0.5f };

        /// <summary>anchorMin 向量</summary>
        public Vector2 Min => Vec2(min, new Vector2(0.5f, 0.5f));

        /// <summary>anchorMax 向量</summary>
        public Vector2 Max => Vec2(max, new Vector2(0.5f, 0.5f));

        /// <summary>是否为点锚（min == max）</summary>
        public bool IsPoint =>
            min != null && max != null && min.Length >= 2 && max.Length >= 2 &&
            Mathf.Approximately(min[0], max[0]) && Mathf.Approximately(min[1], max[1]);

        internal static Vector2 Vec2(float[] arr, Vector2 fallback)
        {
            if (arr == null || arr.Length < 2) return fallback;
            return new Vector2(arr[0], arr[1]);
        }
    }

    /// <summary>LayoutGroup 布局参数（padding 顺序：[左, 上, 右, 下]）</summary>
    [Serializable]
    public class UISpecLayout
    {
        /// <summary>布局类型："horizontal" / "vertical"</summary>
        public string type = "vertical";

        /// <summary>子节点间距</summary>
        public float spacing;

        /// <summary>内边距 [左, 上, 右, 下]</summary>
        public float[] padding = { 0f, 0f, 0f, 0f };

        /// <summary>子节点对齐（TextAnchor 名，如 "UpperLeft"）</summary>
        public string align = "UpperLeft";

        /// <summary>是否水平布局</summary>
        public bool IsHorizontal => type == "horizontal";

        /// <summary>转为 RectOffset（注意构造函数参数序为 left/right/top/bottom）</summary>
        public RectOffset ToRectOffset()
        {
            float[] p = padding ?? new float[4];
            float V(int i) => p.Length > i ? p[i] : 0f;
            return new RectOffset((int)V(0), (int)V(2), (int)V(1), (int)V(3));
        }
    }

    /// <summary>
    /// ui-spec 节点。字段与 JSON 键一一对应；扩展字段全部可选，
    /// 旧规格零改动即可被解释（JsonUtility 对缺失键保留默认值）。
    /// </summary>
    [Serializable]
    public class UISpecNode
    {
        // ===== 公共字段（§4.1） =====

        /// <summary>节点名。m_Xxx / mi_Xxx 前缀（后首字母大写）参与 View 字段自动绑定</summary>
        public string name = "Node";

        /// <summary>false = 隐藏节点，构建时 SetActive(false)</summary>
        public bool active = true;

        /// <summary>组件类型列表；.cs 后缀 = 脚本组件；RectTransform 隐含于所有节点</summary>
        public string[] components = new string[0];

        /// <summary>锚点（缺失时按居中点锚处理）</summary>
        public UISpecAnchors anchors = new UISpecAnchors();

        /// <summary>轴心 [x, y]，缺省 [0.5, 0.5]</summary>
        public float[] pivot = { 0.5f, 0.5f };

        /// <summary>→ anchoredPosition（Unity 坐标系，y 向上）</summary>
        public float[] position = { 0f, 0f };

        /// <summary>→ sizeDelta（锚点拉伸时可负数收缩）</summary>
        public float[] size = { 0f, 0f };

        /// <summary>子节点</summary>
        public UISpecNode[] children = new UISpecNode[0];

        // ===== 组件字段（§4.2） =====

        /// <summary>文本内容（仅 Text 节点；键缺失 = 非文本节点）</summary>
        public string text;

        /// <summary>字号</summary>
        public int fontSize;

        /// <summary>文本对齐（TextAnchor 名）</summary>
        public string align;

        /// <summary>文本颜色（#RRGGBB / rgba(r,g,b,a)）</summary>
        public string color;

        /// <summary>背景色或渐变（#RRGGBB / rgba(...) / linear-gradient(...)）</summary>
        public string background;

        /// <summary>LayoutGroup 参数（仅 Horizontal/VerticalLayoutGroup 节点）</summary>
        public UISpecLayout layout;

        // ===== v1.2 扩展字段（§4.4，全部可选） =====

        /// <summary>嵌套预制体引用（相对 AssetPackage/Prefabs/UI/ 的路径，P3 启用）</summary>
        public string @ref;

        /// <summary>节点备注（仅文档用途，解释器忽略）</summary>
        public string note;

        /// <summary>程序化 Sprite 资产路径（相对 Assets/，不含扩展名），替代纯色背景</summary>
        public string sprite;

        /// <summary>Text 字形："italic" / "bold" / "boldItalic"</summary>
        public string fontStyle;

        /// <summary>节点缩放 → RectTransform.localScale，缺省 [1, 1]</summary>
        public float[] scale = { 1f, 1f };

        /// <summary>旋转角度（度）→ localRotation = Euler(0, 0, deg)</summary>
        public float rotation;

        /// <summary>弹性宽度（仅 LayoutElement 节点；&gt;0 时该列在布局中弹性占满剩余空间）</summary>
        public float flexibleWidth;

        // ===== 便捷访问 =====

        /// <summary>pivot 向量</summary>
        public Vector2 Pivot => UISpecAnchors.Vec2(pivot, new Vector2(0.5f, 0.5f));

        /// <summary>anchoredPosition 向量</summary>
        public Vector2 Position => UISpecAnchors.Vec2(position, Vector2.zero);

        /// <summary>sizeDelta 向量</summary>
        public Vector2 Size => UISpecAnchors.Vec2(size, Vector2.zero);

        /// <summary>localScale 向量</summary>
        public Vector2 Scale => UISpecAnchors.Vec2(scale, Vector2.one);

        /// <summary>是否为文本节点（JSON 含 text 键，空串也算）</summary>
        public bool HasText => text != null;

        /// <summary>组件列表是否含指定组件</summary>
        public bool HasComponent(string comp)
        {
            if (components == null) return false;
            for (int i = 0; i < components.Length; i++)
                if (components[i] == comp) return true;
            return false;
        }

        /// <summary>按名查找直接子节点</summary>
        public UISpecNode FindChild(string childName)
        {
            if (children == null) return null;
            for (int i = 0; i < children.Length; i++)
                if (children[i] != null && children[i].name == childName) return children[i];
            return null;
        }
    }
}
