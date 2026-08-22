/// ============================================================
/// 文件名: ClickSfxGenerator.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 点击音效程序化生成器。按 ClickEffectType 参数化合成短促 PCM 音
/// （正弦扫频/双音/噪声 × 指数衰减包络），零外部资源依赖。
///       与点击特效同一入口播放（ClickEffectSystem.Play）。
/// 引用：ClickEffectEnums.cs, ClickEffectSystem.cs
/// ============================================================

using UnityEngine;

namespace DualEnigma.Art
{
    /// <summary>点击音效合成器：ClickEffectType → AudioClip（按类型缓存由调用方管理）</summary>
    public static class ClickSfxGenerator
    {
        private const int SAMPLE_RATE = 22050;

        /// <summary>合成参数：主频起止/时长/泛音/噪声占比/衰减速率</summary>
        private readonly struct Spec
        {
            public readonly float F0, F1;      // 频率扫频起止（Hz）
            public readonly float Duration;    // 秒
            public readonly float Overtone;    // 二次泛音幅度比（0=纯音）
            public readonly float Noise;       // 噪声混合比（0=纯音，1=全噪声）
            public readonly float Decay;       // 指数衰减速率（越大越短促）
            public readonly float SecondTone;  // 叠加第二音（0=无，Hz；与主音同包络）
            public readonly float Volume;

            public Spec(float f0, float f1, float duration, float decay,
                float overtone = 0f, float noise = 0f, float secondTone = 0f, float volume = 0.5f)
            {
                F0 = f0; F1 = f1; Duration = duration; Decay = decay;
                Overtone = overtone; Noise = noise; SecondTone = secondTone; Volume = volume;
            }
        }

        /// <summary>按特效类型合成对应主题音效（水=水滴/火=噼啪/通用=blip…）</summary>
        public static AudioClip GenerateClip(ClickEffectType type)
        {
            Spec spec = GetSpec(type);
            int sampleCount = Mathf.CeilToInt(spec.Duration * SAMPLE_RATE);
            float[] data = new float[sampleCount];

            // 噪声源（固定种子保证音色稳定）
            var rng = new System.Random(20260822);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float k = sampleCount > 1 ? (float)i / (sampleCount - 1) : 0f;

                // 扫频相位：f(t) 线性插值 f0→f1，相位取梯形近似 ∫f dt
                float freq = Mathf.Lerp(spec.F0, spec.F1, k);
                float phase = 2f * Mathf.PI * (spec.F0 + freq) * 0.5f * t;

                float tone = Mathf.Sin(phase);
                if (spec.Overtone > 0f)
                    tone += spec.Overtone * Mathf.Sin(phase * 2f);          // 八度泛音
                if (spec.SecondTone > 0f)
                    tone += 0.6f * Mathf.Sin(2f * Mathf.PI * spec.SecondTone * t); // 叠加音
                tone /= 1f + spec.Overtone + (spec.SecondTone > 0f ? 0.6f : 0f);

                float noise = spec.Noise > 0f
                    ? ((float)rng.NextDouble() * 2f - 1f) * spec.Noise
                    : 0f;

                // 包络：指数衰减 + 末尾 3ms 线性淡出防爆音
                float envelope = Mathf.Exp(-t * spec.Decay);
                float fade = Mathf.Clamp01((spec.Duration - t) / 0.003f);

                data[i] = (tone * (1f - spec.Noise) + noise) * envelope * fade * spec.Volume;
            }

            AudioClip clip = AudioClip.Create($"ClickSfx_{type}", sampleCount, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>主题参数表：与特效语义一一对应</summary>
        private static Spec GetSpec(ClickEffectType type)
        {
            switch (type)
            {
                case ClickEffectType.WaterRipple:  // 水滴：高频快滑落
                    return new Spec(1250f, 320f, 0.10f, 18f, overtone: 0.3f, volume: 0.42f);

                case ClickEffectType.FireSpark:    // 噼啪：噪声突发
                    return new Spec(400f, 150f, 0.06f, 40f, noise: 0.85f, volume: 0.4f);

                case ClickEffectType.IceShatter:   // 冰裂：高频短脆
                    return new Spec(1800f, 900f, 0.06f, 35f, overtone: 0.2f, volume: 0.38f);

                case ClickEffectType.RockDust:     // 岩尘：低闷噪声
                    return new Spec(220f, 90f, 0.08f, 22f, noise: 0.7f, volume: 0.4f);

                case ClickEffectType.RingPulse:    // 通用 blip：中频扫落
                    return new Spec(700f, 350f, 0.07f, 30f, volume: 0.35f);

                case ClickEffectType.StarTwinkle:  // 星光：双音上行（近似琶音）
                    return new Spec(950f, 1420f, 0.08f, 25f, secondTone: 1900f, volume: 0.35f);

                case ClickEffectType.ElementMix:   // 交融：双音和弦（双生主题）
                    return new Spec(660f, 660f, 0.12f, 12f, secondTone: 990f, volume: 0.4f);

                case ClickEffectType.Poof:         // 烟雾：软噪声
                    return new Spec(300f, 120f, 0.09f, 15f, noise: 0.6f, volume: 0.3f);

                case ClickEffectType.Shockwave:    // 冲击：低频重击 + 噪声瞬态
                    return new Spec(180f, 45f, 0.13f, 8f, noise: 0.25f, volume: 0.55f);

                case ClickEffectType.WarmGlow:     // 暖光：柔和双音
                    return new Spec(520f, 520f, 0.16f, 10f, secondTone: 780f, volume: 0.32f);

                default:
                    return new Spec(700f, 350f, 0.07f, 30f, volume: 0.35f);
            }
        }
    }
}
