/// ============================================================
/// 文件名: UISpecExtractor.cs
/// 创建时间: 2026-08-20
/// 作者: DualEnigma
/// 描述: ui-spec 提取器。从 <页面>.html 中定位
///       <script type="application/json" id="ui-spec">...</script> 标签，
///       取出 JSON 文本并反序列化为 UISpecNode 树。
///       设计稿与数据源同体，无需独立 .json 文件。
/// 引用：UISpecNode.cs
/// ============================================================

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DualEnigma.UI.Editor
{
    /// <summary>
    /// UISpecExtractor：读 HTML 文件 → 正则提取 ui-spec JSON → JsonUtility 反序列化。
    /// </summary>
    public static class UISpecExtractor
    {
        /// <summary>ui-spec script 标签匹配（id 与 type 属性顺序不敏感）</summary>
        private static readonly Regex SpecTag = new Regex(
            "<script[^>]*id=\"ui-spec\"[^>]*>(?<json>.*?)</script>",
            RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// 从 HTML 文件提取 ui-spec 根节点。
        /// </summary>
        /// <param name="htmlPath">HTML 文件绝对路径</param>
        /// <returns>反序列化后的根节点</returns>
        /// <exception cref="UISpecException">标签缺失或 JSON 语法错误时抛出</exception>
        public static UISpecNode ExtractFromFile(string htmlPath)
        {
            if (!File.Exists(htmlPath))
                throw new UISpecException("文件不存在: " + htmlPath);
            return Extract(File.ReadAllText(htmlPath), Path.GetFileName(htmlPath));
        }

        /// <summary>
        /// 从 HTML 文本提取 ui-spec 根节点。
        /// </summary>
        /// <param name="html">HTML 全文</param>
        /// <param name="sourceName">来源名（用于错误信息）</param>
        /// <exception cref="UISpecException">标签缺失或 JSON 语法错误时抛出</exception>
        public static UISpecNode Extract(string html, string sourceName)
        {
            Match m = SpecTag.Match(html);
            if (!m.Success)
                throw new UISpecException($"{sourceName}: 未找到 <script id=\"ui-spec\"> 标签");

            string json = m.Groups["json"].Value.Trim();
            // 防御：gen_ui_html.py 导出时会把 "</" 转义为 "<\/"，还原为标准 JSON 文本
            json = json.Replace("<\\/", "</");

            UISpecNode root;
            try
            {
                root = JsonUtility.FromJson<UISpecNode>(json);
            }
            catch (Exception e)
            {
                throw new UISpecException($"{sourceName}: ui-spec JSON 解析失败 — {e.Message}");
            }
            if (root == null || string.IsNullOrEmpty(root.name))
                throw new UISpecException($"{sourceName}: ui-spec JSON 解析结果为空或缺少根节点名");
            return root;
        }

        /// <summary>
        /// 提取原始 JSON 文本（干跑校验与编辑器显示用）。
        /// </summary>
        public static bool TryExtractJson(string html, out string json)
        {
            Match m = SpecTag.Match(html);
            if (m.Success)
            {
                json = m.Groups["json"].Value.Trim().Replace("<\\/", "</");
                return true;
            }
            json = null;
            return false;
        }
    }

    /// <summary>ui-spec 提取/解析失败的阻断性异常</summary>
    public sealed class UISpecException : Exception
    {
        public UISpecException(string message) : base(message) { }
    }
}
