/// ============================================================
/// 文件名: ParticleTextureGenerator.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 粒子贴图生成器。运行时程序化生成白色+Alpha粒子贴图并缓存，
///       供 ParticleSystem 材质使用，零外部资源依赖。
///       贴图为纯白 RGB + Alpha 渐变，可被 startColor 任意染色复用。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Art
{
    /// <summary>
    /// 粒子贴图生成器。
    /// 首次获取时在内存中绘制并缓存（Dictionary），全游戏共用 5 张贴图。
    /// 引用：ClickEffectEnums.cs (ParticleTextureType), ClickEffectFactory.cs
    /// </summary>
    public static class ParticleTextureGenerator
    {
        /// <summary>贴图缓存（域重载后自动重建）</summary>
        private static readonly Dictionary<ParticleTextureType, Texture2D> _textureCache =
            new Dictionary<ParticleTextureType, Texture2D>();

        /// <summary>
        /// 获取指定类型的粒子贴图（不存在则生成）。
        /// </summary>
        public static Texture2D GetTexture(ParticleTextureType type)
        {
            if (_textureCache.TryGetValue(type, out Texture2D cached) && cached != null)
                return cached;

            Texture2D tex = GenerateTexture(type);
            _textureCache[type] = tex;
            return tex;
        }

        /// <summary>
        /// 按类型分发绘制。
        /// </summary>
        private static Texture2D GenerateTexture(ParticleTextureType type)
        {
            switch (type)
            {
                case ParticleTextureType.Soft: return GenerateSoftCircle();
                case ParticleTextureType.Dot: return GenerateHardDot();
                case ParticleTextureType.Ring: return GenerateRing();
                case ParticleTextureType.Spark: return GenerateSparkStar();
                case ParticleTextureType.Chip: return GenerateChip();
                default: return GenerateSoftCircle();
            }
        }

        /// <summary>
        /// 柔光圆 32×32：径向渐变透明（边缘平方衰减），Bilinear 平滑采样。
        /// 用于光晕、闪光、烟尘。
        /// </summary>
        private static Texture2D GenerateSoftCircle()
        {
            const int size = 32;
            Texture2D tex = CreateParticleTexture(size, FilterMode.Bilinear);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 15.5f;
                    float dy = y - 15.5f;
                    float t = Mathf.Sqrt(dx * dx + dy * dy) / 15.5f;

                    // 平方衰减：中心实、边缘柔
                    float falloff = Mathf.Clamp01(1f - t);
                    float alpha = falloff * falloff;
                    tex.SetPixel(x, y, White(alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 实心圆点 24×24：硬边圆（1.5px 软边过渡），Point 像素风采样。
        /// 用于液滴、光点、聚拢粒子。
        /// </summary>
        private static Texture2D GenerateHardDot()
        {
            const int size = 24;
            Texture2D tex = CreateParticleTexture(size, FilterMode.Point);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 11.5f;
                    float dy = y - 11.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    const float radius = 11f;

                    float alpha;
                    if (dist <= radius - 1.5f)
                        alpha = 1f;
                    else if (dist <= radius)
                        alpha = (radius - dist) / 1.5f;
                    else
                        alpha = 0f;

                    tex.SetPixel(x, y, White(alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 圆环 32×32：环形带（中心半径12.5，半宽2.5，两侧软衰减），Bilinear 采样。
        /// 用于涟漪、脉冲、冲击波。
        /// </summary>
        private static Texture2D GenerateRing()
        {
            const int size = 32;
            Texture2D tex = CreateParticleTexture(size, FilterMode.Bilinear);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 15.5f;
                    float dy = y - 15.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // 环带：|dist - rMid| <= halfW，峰值在 rMid
                    const float rMid = 12.5f;
                    const float halfW = 2.5f;
                    float t = Mathf.Clamp01(1f - Mathf.Abs(dist - rMid) / halfW);
                    float alpha = t * t;
                    tex.SetPixel(x, y, White(alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 四芒星 32×32：十字星形（臂长14.5、半宽1.5 + 中心圆核），Point 采样。
        /// 用于星光、火花。
        /// </summary>
        private static Texture2D GenerateSparkStar()
        {
            const int size = 32;
            Texture2D tex = CreateParticleTexture(size, FilterMode.Point);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float adx = Mathf.Abs(x - 15.5f);
                    float ady = Mathf.Abs(y - 15.5f);
                    float dist = Mathf.Sqrt(adx * adx + ady * ady);

                    float alpha;
                    if (dist <= 2.5f)
                    {
                        // 中心圆核
                        alpha = 1f;
                    }
                    else if ((adx <= 1.5f || ady <= 1.5f) && Mathf.Max(adx, ady) <= 14.5f)
                    {
                        // 十字臂，沿臂长衰减
                        alpha = 1f - Mathf.Max(adx, ady) / 14.5f;
                    }
                    else
                    {
                        alpha = 0f;
                    }

                    tex.SetPixel(x, y, White(alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 方形碎片 12×12：实心方块，Point 采样。
        /// 用于冰屑、岩屑等碎粒。
        /// </summary>
        private static Texture2D GenerateChip()
        {
            const int size = 12;
            Texture2D tex = CreateParticleTexture(size, FilterMode.Point);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, White(1f));

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 创建白色底的粒子贴图（RGB 恒为白，仅 Alpha 变化）。
        /// </summary>
        private static Texture2D CreateParticleTexture(int size, FilterMode filterMode)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp,
                name = $"ParticleTex_{size}",
            };
            return tex;
        }

        /// <summary>
        /// 白色 + 指定透明度。
        /// </summary>
        private static Color White(float alpha)
        {
            return new Color(1f, 1f, 1f, alpha);
        }
    }
}
