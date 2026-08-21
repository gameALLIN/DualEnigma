/// ============================================================
/// 文件名: UISpecBuildUtil.cs
/// 创建时间: 2026-08-20
/// 作者: DualEnigma
/// 描述: ui-spec 构建公用工具：颜色解析（#RRGGBB / rgba(r,g,b,a) /
///       linear-gradient(...)）、渐变 Sprite 资产生成、目录确保、
///       层级搜索。与手写生成器同一套视觉规范与资产约定。
/// 引用：UISpecNode.cs
/// ============================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DualEnigma.UI.Editor
{
    /// <summary>ui-spec 构建公用工具</summary>
    public static class UISpecBuildUtil
    {
        /// <summary>渐变纹理输出目录（与手写生成器一致）</summary>
        public const string TEXTURE_DIR = "Assets/ArtResources/Textures/UI";

        // ==================== 颜色解析 ====================

        private static readonly Regex RgbaRegex = new Regex(
            @"^rgba\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*([\d.]+)\s*\)$",
            RegexOptions.Compiled);

        /// <summary>
        /// 解析 spec 颜色字符串为 Unity Color。
        /// 支持 #RRGGBB（不透明）与 rgba(r,g,b,a)（半透明）。
        /// 无法识别时返回 null。
        /// </summary>
        public static Color? ParseColor(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            s = s.Trim();

            if (s.Length == 7 && s[0] == '#' && ColorUtility.TryParseHtmlString(s, out Color hex))
                return hex;

            Match m = RgbaRegex.Match(s);
            if (m.Success)
            {
                float r = int.Parse(m.Groups[1].Value) / 255f;
                float g = int.Parse(m.Groups[2].Value) / 255f;
                float b = int.Parse(m.Groups[3].Value) / 255f;
                float a = float.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
                return new Color(r, g, b, Mathf.Clamp01(a));
            }
            return null;
        }

        /// <summary>是否为渐变背景（linear-gradient(...)）</summary>
        public static bool IsGradient(string s) =>
            !string.IsNullOrEmpty(s) && s.TrimStart().StartsWith("linear-gradient", StringComparison.Ordinal);

        // ==================== 渐变 Sprite ====================

        private static readonly Regex GradientStopRegex = new Regex(
            @"(#[0-9a-fA-F]{6})\s+(\d+(?:\.\d+)?)%", RegexOptions.Compiled);

        /// <summary>
        /// 解析 linear-gradient(to top, #A 0%, #B 50%, #C 100%) 并生成/更新
        /// 垂直渐变 Texture2D + Sprite 资产（与手写生成器同一资产约定）。
        /// </summary>
        /// <param name="css">渐变 CSS 文本</param>
        /// <param name="assetName">资产名（不含扩展名）</param>
        /// <returns>持久化的 Sprite 引用；解析失败返回 null</returns>
        public static Sprite CreateGradientSprite(string css, string assetName)
        {
            var stops = new List<KeyValuePair<float, Color>>();
            foreach (Match m in GradientStopRegex.Matches(css))
            {
                if (!ColorUtility.TryParseHtmlString(m.Groups[1].Value, out Color c)) continue;
                float t = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) / 100f;
                stops.Add(new KeyValuePair<float, Color>(Mathf.Clamp01(t), c));
            }
            if (stops.Count < 2) return null;
            stops.Sort((a, b) => a.Key.CompareTo(b.Key));

            // CSS "to top"：0% 在底部，100% 在顶部；Unity 纹理原点在左下角，行序与之一致
            const int width = 4;
            const int height = 256;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                Color32 row = EvaluateGradient(stops, t);
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = row;
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.name = assetName;

            EnsureDirectory(TEXTURE_DIR);
            string texPath = TEXTURE_DIR + "/" + assetName + ".asset";
            DeleteExistingAsset(texPath);
            AssetDatabase.CreateAsset(tex, texPath);

            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Sprite sprite = Sprite.Create(
                savedTex,
                new Rect(0, 0, savedTex.width, savedTex.height),
                new Vector2(0.5f, 0.5f),
                100f, 0u, SpriteMeshType.FullRect);
            sprite.name = assetName;

            string spritePath = TEXTURE_DIR + "/" + assetName + "_Sprite.asset";
            DeleteExistingAsset(spritePath);
            AssetDatabase.CreateAsset(sprite, spritePath);

            return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        /// <summary>按停靠点分段线性插值求 t 处颜色</summary>
        private static Color EvaluateGradient(List<KeyValuePair<float, Color>> stops, float t)
        {
            if (t <= stops[0].Key) return stops[0].Value;
            if (t >= stops[stops.Count - 1].Key) return stops[stops.Count - 1].Value;
            for (int i = 0; i < stops.Count - 1; i++)
            {
                if (t > stops[i + 1].Key) continue;
                float span = stops[i + 1].Key - stops[i].Key;
                float k = span < 1e-6f ? 0f : (t - stops[i].Key) / span;
                return Color.Lerp(stops[i].Value, stops[i + 1].Value, k);
            }
            return stops[stops.Count - 1].Value;
        }

        // ==================== 资产管理 ====================

        /// <summary>确保目录存在，不存在则逐级创建（相对项目根，如 "Assets/ArtResources/Textures/UI"）</summary>
        public static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
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

        /// <summary>如果指定路径已存在资产，则删除（覆盖更新）</summary>
        public static void DeleteExistingAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        // ==================== 层级搜索 ====================

        /// <summary>递归查找指定名称的子节点 Transform（含自身以下的整棵子树）</summary>
        public static Transform FindDeepChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName) return child;
                Transform result = FindDeepChild(child, childName);
                if (result != null) return result;
            }
            return null;
        }
    }
}
