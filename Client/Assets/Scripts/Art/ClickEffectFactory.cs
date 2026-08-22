/// ============================================================
/// 文件名: ClickEffectFactory.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 点击特效工厂。运行时纯代码构建 10 种点击反馈 ParticleSystem 特效，
///       零预制体、零外部资源依赖，Play 后自动销毁。
///       贴图由 ParticleTextureGenerator 内存生成，材质用 Sprites/Default
///       （工程内已有大量 Sprite 使用，必然打入包体，且支持粒子顶点色）。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Art
{
    /// <summary>
    /// 点击特效工厂。
    /// 用法：ClickEffectFactory.Play(ClickEffectType.WaterRipple, worldPos);
    /// 或：  ClickEffectFactory.PlayAtScreenPoint(type, Input.mousePosition);
    /// 主 ParticleSystem 挂在根节点并设置 stopAction=Destroy，
    /// 特效播完整个 GameObject 自动销毁，无需手动清理。
    /// 引用：ClickEffectEnums.cs, ParticleTextureGenerator.cs
    /// </summary>
    public static class ClickEffectFactory
    {
        /// <summary>粒子材质缓存（按贴图类型，全游戏共用 5 个）</summary>
        private static readonly Dictionary<ParticleTextureType, Material> _materialCache =
            new Dictionary<ParticleTextureType, Material>();

        /// <summary>
        /// 在世界坐标播放点击特效。
        /// </summary>
        /// <param name="type">特效类型</param>
        /// <param name="worldPosition">世界坐标（建议 z=0 平面）</param>
        /// <param name="sortingOrder">渲染排序（默认100，高于角色/砖块）</param>
        /// <returns>特效根 GameObject（播完自动销毁，可无视返回值）</returns>
        public static GameObject Play(ClickEffectType type, Vector3 worldPosition, int sortingOrder = 100)
        {
            GameObject root = new GameObject($"ClickEffect_{type}");
            root.transform.position = worldPosition;

            // 先失活 → 完整配置 → 激活触发 playOnAwake，确保以最终配置开始播放
            root.SetActive(false);

            switch (type)
            {
                case ClickEffectType.WaterRipple:
                    BuildWaterRipple(root, sortingOrder);
                    break;
                case ClickEffectType.FireSpark:
                    BuildFireSpark(root, sortingOrder);
                    break;
                case ClickEffectType.IceShatter:
                    BuildIceShatter(root, sortingOrder);
                    break;
                case ClickEffectType.RockDust:
                    BuildRockDust(root, sortingOrder);
                    break;
                case ClickEffectType.RingPulse:
                    BuildRingPulse(root, sortingOrder);
                    break;
                case ClickEffectType.StarTwinkle:
                    BuildStarTwinkle(root, sortingOrder);
                    break;
                case ClickEffectType.ElementMix:
                    BuildElementMix(root, sortingOrder);
                    break;
                case ClickEffectType.Poof:
                    BuildPoof(root, sortingOrder);
                    break;
                case ClickEffectType.Shockwave:
                    BuildShockwave(root, sortingOrder);
                    break;
                case ClickEffectType.WarmGlow:
                    BuildWarmGlow(root, sortingOrder);
                    break;
            }

            root.SetActive(true);
            return root;
        }

        /// <summary>
        /// 在屏幕坐标播放点击特效（自动投影到 z=0 平面）。
        /// 适用于鼠标点击屏幕任意位置的反馈。
        /// </summary>
        /// <param name="type">特效类型</param>
        /// <param name="screenPoint">屏幕坐标（如 Input.mousePosition）</param>
        /// <param name="camera">目标相机（空则用 Camera.main）</param>
        /// <param name="sortingOrder">渲染排序</param>
        /// <returns>特效根 GameObject</returns>
        public static GameObject PlayAtScreenPoint(
            ClickEffectType type, Vector3 screenPoint, Camera camera = null, int sortingOrder = 100)
        {
            Camera cam = camera != null ? camera : Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[ClickEffectFactory] 未找到相机，无法转换屏幕坐标");
                return null;
            }

            // 2D 正交相机：屏幕点投影到 z=0 平面
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(
                screenPoint.x, screenPoint.y, -cam.transform.position.z));
            return Play(type, world, sortingOrder);
        }

        // ──────────────────────────────────────────────
        //  10 种特效构建
        // ──────────────────────────────────────────────

        /// <summary>
        /// 水波纹：三圈间隔扩散涟漪 + 水滴迸溅（水元素）。
        /// </summary>
        private static void BuildWaterRipple(GameObject root, int sortingOrder)
        {
            // 主系统：三圈扩散水波环
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Ring, sortingOrder);
            var main = master.main;
            main.duration = 1.0f;
            main.startLifetime = 0.55f;
            main.startSize = 0.8f;

            master.emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)1),
                new ParticleSystem.Burst(0.18f, (short)1),
                new ParticleSystem.Burst(0.36f, (short)1),
            });

            SetSizeCurve(master, 0.15f, 1.3f);
            SetColorFade(master, new Color32(0x4F, 0xC3, 0xF7, 0xFF), new Color32(0x02, 0x77, 0xBD, 0x00));

            // 子系统：水滴向外迸溅后落地
            ParticleSystem droplets = CreateChildSystem(root.transform, "Droplets", ParticleTextureType.Dot, sortingOrder);
            var dMain = droplets.main;
            dMain.duration = 1.0f;
            dMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.5f);
            dMain.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 2.6f);
            dMain.startSize = 0.09f;
            dMain.gravityModifier = 1.5f;
            dMain.startRotation = RandomRotation();
            droplets.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)7) });
            SetRadialShape(droplets, 0.05f);
            SetSizeCurve(droplets, 1f, 0.3f);
            SetColorFade(droplets, new Color32(0xB3, 0xE5, 0xFC, 0xFF), new Color32(0x4F, 0xC3, 0xF7, 0x00));
        }

        /// <summary>
        /// 火花迸溅：中心闪光 + 火星四射坠落（火元素）。
        /// </summary>
        private static void BuildFireSpark(GameObject root, int sortingOrder)
        {
            // 主系统：中心炽白闪光
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft, sortingOrder);
            var main = master.main;
            main.duration = 0.7f;
            main.startLifetime = 0.18f;
            main.startSize = 0.5f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetSizeCurve(master, 1f, 0.15f);
            SetColorFade(master, new Color32(0xFF, 0xF9, 0xC4, 0xE6), new Color32(0xFF, 0x6F, 0x00, 0x00));

            // 子系统：火星四射
            ParticleSystem sparks = CreateChildSystem(root.transform, "Sparks", ParticleTextureType.Spark, sortingOrder);
            var sMain = sparks.main;
            sMain.duration = 0.7f;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            sMain.startSize = 0.15f;
            sMain.gravityModifier = 3f;
            sMain.startRotation = RandomRotation();
            sparks.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)12) });
            SetRadialShape(sparks, 0.05f);
            SetSizeCurve(sparks, 1f, 0f);
            SetColorFade(sparks, new Color32(0xFF, 0xE0, 0x82, 0xFF), new Color32(0xBF, 0x36, 0x0C, 0x00));
        }

        /// <summary>
        /// 冰晶碎裂：白色脉冲环 + 冰屑飞散（冰元素）。
        /// </summary>
        private static void BuildIceShatter(GameObject root, int sortingOrder)
        {
            // 主系统：白色冰蓝脉冲环
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Ring, sortingOrder);
            var main = master.main;
            main.duration = 0.7f;
            main.startLifetime = 0.4f;
            main.startSize = 0.7f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetSizeCurve(master, 0.2f, 1.5f);
            SetColorFade(master, new Color32(0xE1, 0xF5, 0xFE, 0xFF), new Color32(0x81, 0xD4, 0xFA, 0x00));

            // 子系统：冰屑飞散
            ParticleSystem chips = CreateChildSystem(root.transform, "Chips", ParticleTextureType.Chip, sortingOrder);
            var cMain = chips.main;
            cMain.duration = 0.7f;
            cMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.5f);
            cMain.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
            cMain.startSize = 0.12f;
            cMain.gravityModifier = 2.5f;
            cMain.startRotation = RandomRotation();
            chips.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)8) });
            SetRadialShape(chips, 0.05f);
            SetSizeCurve(chips, 1f, 0.4f);
            SetColorFade(chips, new Color32(0xB3, 0xE5, 0xFC, 0xFF), new Color32(0x81, 0xD4, 0xFA, 0x00));
        }

        /// <summary>
        /// 岩石碎尘：灰尘升腾 + 碎屑弹开（岩元素）。
        /// </summary>
        private static void BuildRockDust(GameObject root, int sortingOrder)
        {
            // 主系统：烟尘团升腾扩散
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft, sortingOrder);
            var main = master.main;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)5) });
            SetRadialShape(master, 0.12f);

            // 缓慢上升
            var vol = master.velocityOverLifetime;
            vol.enabled = true;
            vol.linear = new Vector3(0f, 0.35f, 0f);

            SetSizeCurve(master, 0.6f, 1.6f);
            SetColorFade(master, new Color32(0xB0, 0xBE, 0xC5, 0xB3), new Color32(0x90, 0xA4, 0xAE, 0x00));

            // 子系统：岩石碎屑弹开
            ParticleSystem chips = CreateChildSystem(root.transform, "Chips", ParticleTextureType.Chip, sortingOrder);
            var cMain = chips.main;
            cMain.duration = 1.0f;
            cMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.5f);
            cMain.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 2.6f);
            cMain.startSize = 0.1f;
            cMain.gravityModifier = 2.8f;
            cMain.startRotation = RandomRotation();
            chips.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)6) });
            SetRadialShape(chips, 0.05f);
            SetSizeCurve(chips, 1f, 0.3f);
            SetColorFade(chips, new Color32(0x9E, 0x9E, 0x9E, 0xFF), new Color32(0x5A, 0x5A, 0x5A, 0x00));
        }

        /// <summary>
        /// 圆环脉冲：单圈快速扩散淡出（通用默认）。
        /// </summary>
        private static void BuildRingPulse(GameObject root, int sortingOrder)
        {
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Ring, sortingOrder);
            var main = master.main;
            main.duration = 0.6f;
            main.startLifetime = 0.42f;
            main.startSize = 0.85f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetSizeCurve(master, 0.2f, 1.5f);
            SetColorFade(master, new Color32(0xFF, 0xFF, 0xFF, 0xFF), new Color32(0x4F, 0xC3, 0xF7, 0x00));
        }

        /// <summary>
        /// 星光闪烁：四颗四芒星错位弹出（通用强调）。
        /// </summary>
        private static void BuildStarTwinkle(GameObject root, int sortingOrder)
        {
            // 主系统：中心微光（兼做生命周期载体）
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft, sortingOrder);
            var main = master.main;
            main.duration = 0.9f;
            main.startLifetime = 0.5f;
            main.startSize = 0.3f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetSizeCurve(master, 1f, 0f);
            SetColorFade(master, new Color32(0xFF, 0xF9, 0xC4, 0xCC), new Color32(0xFF, 0xE0, 0x82, 0x00));

            // 子系统：四芒星错时弹出
            ParticleSystem stars = CreateChildSystem(root.transform, "Stars", ParticleTextureType.Spark, sortingOrder);
            var sMain = stars.main;
            sMain.duration = 0.9f;
            sMain.startLifetime = 0.28f;
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.26f);
            sMain.startRotation = RandomRotation();
            stars.emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0.05f, (short)1),
                new ParticleSystem.Burst(0.2f, (short)1),
                new ParticleSystem.Burst(0.35f, (short)1),
                new ParticleSystem.Burst(0.5f, (short)1),
            });
            SetRadialShape(stars, 0.28f, thickness: 1f);
            SetPopSize(stars);
            SetColorFade(stars, new Color32(0xFF, 0xFF, 0xFF, 0xFF), new Color32(0xFF, 0xF9, 0xC4, 0x00));
        }

        /// <summary>
        /// 元素交融：蓝橙光点向心聚拢 + 中心白闪（水火双生主题）。
        /// </summary>
        private static void BuildElementMix(GameObject root, int sortingOrder)
        {
            // 主系统：聚拢完成后的中心白闪（延迟爆发）
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft, sortingOrder);
            var main = master.main;
            main.duration = 0.75f;
            main.startLifetime = 0.25f;
            main.startSize = 0.5f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0.3f, (short)1) });
            SetSizeCurve(master, 1f, 0.2f);
            SetColorFade(master, new Color32(0xFF, 0xFF, 0xFF, 0xF2), new Color32(0xFF, 0xD5, 0x4F, 0x00));

            // 子系统A：水蓝光点从环边向心聚拢
            ParticleSystem blueIn = CreateChildSystem(root.transform, "BlueIn", ParticleTextureType.Dot, sortingOrder);
            ConfigureConverge(blueIn, new Color32(0x4F, 0xC3, 0xF7, 0xFF), 0f);

            // 子系统B：火橙光点从环边向心聚拢（稍迟启动）
            ParticleSystem orangeIn = CreateChildSystem(root.transform, "OrangeIn", ParticleTextureType.Dot, sortingOrder);
            ConfigureConverge(orangeIn, new Color32(0xFF, 0x6F, 0x00, 0xFF), 0.05f);
        }

        /// <summary>
        /// 烟雾消散：烟团缓慢升腾淡出（通用轻反馈）。
        /// </summary>
        private static void BuildPoof(GameObject root, int sortingOrder)
        {
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft, sortingOrder);
            var main = master.main;
            main.duration = 0.9f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.36f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
            main.startRotation = RandomRotation();
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)6) });
            SetRadialShape(master, 0.12f);

            var vol = master.velocityOverLifetime;
            vol.enabled = true;
            vol.linear = new Vector3(0f, 0.25f, 0f);

            SetSizeCurve(master, 0.6f, 1.5f);
            SetColorFade(master, new Color32(0xCF, 0xD8, 0xDC, 0x8C), new Color32(0xB0, 0xBE, 0xC5, 0x00));
        }

        /// <summary>
        /// 冲击波：快慢双层冲击环 + 高速光点（通用强反馈）。
        /// </summary>
        private static void BuildShockwave(GameObject root, int sortingOrder)
        {
            // 主系统：快速白环
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Ring, sortingOrder);
            var main = master.main;
            main.duration = 0.7f;
            main.startLifetime = 0.22f;
            main.startSize = 0.8f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetSizeCurve(master, 0.15f, 1.5f);
            SetColorFade(master, new Color32(0xFF, 0xFF, 0xFF, 0xFF), new Color32(0x4F, 0xC3, 0xF7, 0x00));

            // 子系统：慢速厚环
            ParticleSystem slowRing = CreateChildSystem(root.transform, "SlowRing", ParticleTextureType.Ring, sortingOrder);
            var rMain = slowRing.main;
            rMain.duration = 0.7f;
            rMain.startLifetime = 0.5f;
            rMain.startSize = 0.9f;
            slowRing.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetSizeCurve(slowRing, 0.25f, 1.8f);
            SetColorFade(slowRing, new Color32(0x81, 0xD4, 0xFA, 0xCC), new Color32(0x02, 0x77, 0xBD, 0x00));

            // 子系统：高速光点
            ParticleSystem dots = CreateChildSystem(root.transform, "Dots", ParticleTextureType.Dot, sortingOrder);
            var dMain = dots.main;
            dMain.duration = 0.7f;
            dMain.startLifetime = 0.2f;
            dMain.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 4.2f);
            dMain.startSize = 0.08f;
            dots.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)8) });
            SetRadialShape(dots, 0.05f);
            SetSizeCurve(dots, 1f, 0f);
            SetColorFade(dots, new Color32(0xB3, 0xE5, 0xFC, 0xFF), new Color32(0x4F, 0xC3, 0xF7, 0x00));
        }

        /// <summary>
        /// 温暖光晕：暖光呼吸脉动 + 环绕光点（温砖/庇护主题）。
        /// </summary>
        private static void BuildWarmGlow(GameObject root, int sortingOrder)
        {
            // 主系统：中心暖光呼吸
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft, sortingOrder);
            var main = master.main;
            main.duration = 1.1f;
            main.startLifetime = 0.85f;
            main.startSize = 0.55f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetBreatheSize(master);
            SetColorFade(master, new Color32(0xFF, 0xD1, 0x80, 0xE6), new Color32(0xFF, 0x8A, 0x65, 0x00));

            // 子系统：暖色光点环绕
            ParticleSystem orbitDots = CreateChildSystem(root.transform, "OrbitDots", ParticleTextureType.Dot, sortingOrder);
            var oMain = orbitDots.main;
            oMain.duration = 1.1f;
            oMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            oMain.startSize = 0.09f;
            oMain.startSpeed = 0f;
            orbitDots.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)7) });
            SetRadialShape(orbitDots, 0.35f, thickness: 0f);

            // 环绕速度（绕特效中心公转）
            var orbit = orbitDots.orbitVelocity;
            orbit.enabled = true;
            orbit.y = new ParticleSystem.MinMaxCurve(2.2f, 3.0f);

            SetColorFade(orbitDots, new Color32(0xFF, 0x8A, 0x65, 0xFF), new Color32(0xFF, 0xAB, 0x91, 0x00));
        }

        // ──────────────────────────────────────────────
        //  构建辅助
        // ──────────────────────────────────────────────

        /// <summary>
        /// 在特效根节点创建主 ParticleSystem（stopAction=Destroy，播完销毁整个特效）。
        /// </summary>
        private static ParticleSystem CreateMasterSystem(GameObject root, ParticleTextureType tex, int sortingOrder)
        {
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            ConfigureDefaults(ps, tex, sortingOrder);
            ps.main.stopAction = ParticleSystemStopAction.Destroy;
            return ps;
        }

        /// <summary>
        /// 创建子 ParticleSystem（辅助层，随根节点一起销毁）。
        /// </summary>
        private static ParticleSystem CreateChildSystem(
            Transform parent, string name, ParticleTextureType tex, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ConfigureDefaults(ps, tex, sortingOrder);
            return ps;
        }

        /// <summary>
        /// 通用默认配置：不循环、自动播放、世界空间模拟、零速率（纯 Burst 驱动）。
        /// </summary>
        private static void ConfigureDefaults(ParticleSystem ps, ParticleTextureType tex, int sortingOrder)
        {
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetMaterial(tex);
            renderer.sortingOrder = sortingOrder;
        }

        /// <summary>
        /// 获取粒子材质（Sprites/Default + 程序化贴图，按类型缓存复用）。
        /// </summary>
        private static Material GetMaterial(ParticleTextureType type)
        {
            if (_materialCache.TryGetValue(type, out Material cached) && cached != null)
                return cached;

            Shader shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader)
            {
                name = $"ParticleMat_{type}",
                mainTexture = ParticleTextureGenerator.GetTexture(type),
            };
            _materialCache[type] = mat;
            return mat;
        }

        /// <summary>
        /// 配置向心聚拢子系统（ElementMix 专用）。
        /// </summary>
        private static void ConfigureConverge(ParticleSystem ps, Color32 color, float delay)
        {
            var main = ps.main;
            main.duration = 0.75f;
            main.startLifetime = 0.32f;
            main.startSize = 0.1f;
            main.startSpeed = -1.6f; // 负速度 = 从生成环边向圆心聚拢

            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(delay, (short)8) });
            SetRadialShape(ps, 0.55f, thickness: 0f); // 仅在环边生成
            SetSizeCurve(ps, 1f, 0.4f);
            SetColorFade(ps, color, new Color32(color.r, color.g, color.b, 0));
        }

        /// <summary>
        /// 设置径向圆形发射形状。
        /// </summary>
        /// <param name="ps">目标系统</param>
        /// <param name="radius">发射半径</param>
        /// <param name="thickness">1=圆盘内随机，0=仅圆周边</param>
        private static void SetRadialShape(ParticleSystem ps, float radius, float thickness = 0.5f)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = thickness;
            shape.arc = 360f;
        }

        /// <summary>
        /// 尺寸随生命周期线性变化（乘在 startSize 上）。
        /// </summary>
        private static void SetSizeCurve(ParticleSystem ps, float from, float to)
        {
            var sizeOl = ps.sizeOverLifetime;
            sizeOl.enabled = true;
            sizeOl.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, from),
                new Keyframe(1f, to)));
        }

        /// <summary>
        /// 尺寸先弹出后收缩（星光闪爆）。
        /// </summary>
        private static void SetPopSize(ParticleSystem ps)
        {
            var sizeOl = ps.sizeOverLifetime;
            sizeOl.enabled = true;
            sizeOl.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.35f, 1f),
                new Keyframe(1f, 0f)));
        }

        /// <summary>
        /// 尺寸呼吸脉动（暖光晕）。
        /// </summary>
        private static void SetBreatheSize(ParticleSystem ps)
        {
            var sizeOl = ps.sizeOverLifetime;
            sizeOl.enabled = true;
            sizeOl.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.15f),
                new Keyframe(0.3f, 1f),
                new Keyframe(1f, 0.1f)));
        }

        /// <summary>
        /// 颜色随生命周期渐隐（起始色→结束色，Alpha 衰减到结束值）。
        /// </summary>
        private static void SetColorFade(ParticleSystem ps, Color32 from, Color32 to)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(from, 0f),
                    new GradientColorKey(to, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(from.a / 255f, 0f),
                    new GradientAlphaKey(to.a / 255f, 1f),
                });

            var colorOl = ps.colorOverLifetime;
            colorOl.enabled = true;
            colorOl.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        /// <summary>
        /// 随机初始旋转（0~2π）。
        /// </summary>
        private static ParticleSystem.MinMaxCurve RandomRotation()
        {
            return new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        }
    }
}
