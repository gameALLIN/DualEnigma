/// ============================================================
/// 文件名: ClickEffectPrefabGenerator.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 点击特效预制体生成器Editor工具。用 Unity 原生 ParticleSystem
///       构建 10 种点击反馈特效预制体，全部参数可在 Inspector 中手调。
///       材质：软圆粒子直接用 Unity 自带 Default-Particle；
///       涟漪环/四芒星/方形碎片 3 种形状用程序化贴图材质（零外部资源）。
///       菜单：DualEnigma/生成点击特效预制体。
/// ============================================================

using System;
using UnityEngine;
using UnityEditor;
using DualEnigma.Art;

namespace DualEnigma.Editor
{
    /// <summary>
    /// 点击特效预制体生成器Editor工具。
    /// 生成内容：
    /// 1. 程序化形状贴图 .asset（ArtResources/Textures/Particles/，Ring/Spark/Chip 3张）
    /// 2. 粒子材质 .mat（ArtResources/Materials/Particles/，Sprites/Default + 形状贴图）
    /// 3. 10 个特效预制体 .prefab（ArtResources/Prefabs/Effects/Click/）
    /// 预制体均为 Unity 原生 ParticleSystem 层级（主系统 stopAction=Destroy，
    /// 播完自动销毁），生成后可在 Inspector 自由调整任何参数。
    /// 引用：ParticleTextureGenerator.cs, ClickEffectEnums.cs, ClickEffectInput.cs (运行时播放)
    /// </summary>
    public static class ClickEffectPrefabGenerator
    {
        /// <summary>形状贴图输出目录</summary>
        private const string TEXTURE_DIR = "Assets/ArtResources/Textures/Particles";

        /// <summary>粒子材质输出目录</summary>
        private const string MATERIAL_DIR = "Assets/ArtResources/Materials/Particles";

        /// <summary>特效预制体输出目录</summary>
        private const string PREFAB_DIR = "Assets/ArtResources/Prefabs/Effects/Click";

        /// <summary>粒子渲染排序（高于角色/砖块的默认层0）</summary>
        private const int SORTING_ORDER = 100;

        /// <summary>Unity 自带软圆粒子材质（Default-Particle）</summary>
        private static Material _defaultParticleMat;

        /// <summary>涟漪环材质（程序化贴图）</summary>
        private static Material _ringMat;

        /// <summary>四芒星材质（程序化贴图）</summary>
        private static Material _sparkMat;

        /// <summary>方形碎片材质（程序化贴图）</summary>
        private static Material _chipMat;

        /// <summary>
        /// 菜单入口：生成 10 种点击特效预制体。
        /// </summary>
        [MenuItem("DualEnigma/生成点击特效预制体")]
        public static void Generate()
        {
            EnsureDirectory(TEXTURE_DIR);
            EnsureDirectory(MATERIAL_DIR);
            EnsureDirectory(PREFAB_DIR);

            // Unity 自带软圆粒子材质（真·零贴图）
            _defaultParticleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            if (_defaultParticleMat == null)
            {
                Debug.LogError("[ClickEffectPrefabGenerator] 未找到 Unity 自带 Default-Particle 材质，中止生成");
                return;
            }

            // 3 种形状贴图材质（自带材质无法表现环形/星形/方形）
            _ringMat = SaveShapeMaterial(ParticleTextureType.Ring);
            _sparkMat = SaveShapeMaterial(ParticleTextureType.Spark);
            _chipMat = SaveShapeMaterial(ParticleTextureType.Chip);

            // 10 种特效预制体（名称顺序与 ClickEffectType 枚举一致）
            GeneratePrefab("ClickEffect_WaterRipple", BuildWaterRipple);
            GeneratePrefab("ClickEffect_FireSpark", BuildFireSpark);
            GeneratePrefab("ClickEffect_IceShatter", BuildIceShatter);
            GeneratePrefab("ClickEffect_RockDust", BuildRockDust);
            GeneratePrefab("ClickEffect_RingPulse", BuildRingPulse);
            GeneratePrefab("ClickEffect_StarTwinkle", BuildStarTwinkle);
            GeneratePrefab("ClickEffect_ElementMix", BuildElementMix);
            GeneratePrefab("ClickEffect_Poof", BuildPoof);
            GeneratePrefab("ClickEffect_Shockwave", BuildShockwave);
            GeneratePrefab("ClickEffect_WarmGlow", BuildWarmGlow);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ClickEffectPrefabGenerator] 点击特效预制体生成完毕！\n" +
                      $"  贴图路径: {TEXTURE_DIR}/\n" +
                      $"  材质路径: {MATERIAL_DIR}/\n" +
                      $"  预制体路径: {PREFAB_DIR}/ (共10个，可在 Inspector 中自由调整参数)");
        }

        // ──────────────────────────────────────────────
        //  资产生成
        // ──────────────────────────────────────────────

        /// <summary>
        /// 构建特效 GameObject 并保存为预制体，随后销毁临时对象。
        /// </summary>
        private static void GeneratePrefab(string prefabName, Action<GameObject> builder)
        {
            GameObject root = new GameObject(prefabName);
            builder(root);

            string prefabPath = $"{PREFAB_DIR}/{prefabName}.prefab";
            DeleteExistingAsset(prefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[ClickEffectPrefabGenerator] 预制体已保存: {prefabPath}");
        }

        /// <summary>
        /// 生成形状贴图并创建对应粒子材质，均保存为资产并返回持久化材质。
        /// </summary>
        private static Material SaveShapeMaterial(ParticleTextureType type)
        {
            // ---- 1. 生成贴图并保存为 .asset ----
            Texture2D tex = ParticleTextureGenerator.CreateTexture(type);
            tex.name = $"ParticleTex_{type}";
            string texPath = $"{TEXTURE_DIR}/ParticleTex_{type}.asset";
            DeleteExistingAsset(texPath);
            AssetDatabase.CreateAsset(tex, texPath);

            // ---- 2. 创建材质（Sprites/Default 支持粒子顶点色染色）----
            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Material mat = new Material(Shader.Find("Sprites/Default"))
            {
                name = $"ParticleMat_{type}",
                mainTexture = savedTex,
            };

            string matPath = $"{MATERIAL_DIR}/ParticleMat_{type}.mat";
            DeleteExistingAsset(matPath);
            AssetDatabase.CreateAsset(mat, matPath);
            Debug.Log($"[ClickEffectPrefabGenerator] 粒子材质已保存: {matPath}");

            return AssetDatabase.LoadAssetAtPath<Material>(matPath);
        }

        /// <summary>
        /// 贴图类型 → 材质映射。Soft/Dot 用 Unity 自带软圆材质。
        /// </summary>
        private static Material GetMaterial(ParticleTextureType type)
        {
            switch (type)
            {
                case ParticleTextureType.Ring: return _ringMat;
                case ParticleTextureType.Spark: return _sparkMat;
                case ParticleTextureType.Chip: return _chipMat;
                default: return _defaultParticleMat;
            }
        }

        // ──────────────────────────────────────────────
        //  10 种特效构建（Unity 原生 ParticleSystem 模块配置）
        // ──────────────────────────────────────────────

        /// <summary>
        /// 水波纹：三圈间隔扩散涟漪 + 水滴迸溅（水元素）。
        /// </summary>
        private static void BuildWaterRipple(GameObject root)
        {
            // 主系统：三圈扩散水波环（Ring 形状贴图）
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Ring);
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
            ParticleSystem droplets = CreateChildSystem(root.transform, "Droplets", ParticleTextureType.Dot);
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
        private static void BuildFireSpark(GameObject root)
        {
            // 主系统：中心炽白闪光
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft);
            var main = master.main;
            main.duration = 0.7f;
            main.startLifetime = 0.18f;
            main.startSize = 0.5f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetSizeCurve(master, 1f, 0.15f);
            SetColorFade(master, new Color32(0xFF, 0xF9, 0xC4, 0xE6), new Color32(0xFF, 0x6F, 0x00, 0x00));

            // 子系统：火星四射（Spark 四芒星贴图）
            ParticleSystem sparks = CreateChildSystem(root.transform, "Sparks", ParticleTextureType.Spark);
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
        private static void BuildIceShatter(GameObject root)
        {
            // 主系统：白色冰蓝脉冲环
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Ring);
            var main = master.main;
            main.duration = 0.7f;
            main.startLifetime = 0.4f;
            main.startSize = 0.7f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetSizeCurve(master, 0.2f, 1.5f);
            SetColorFade(master, new Color32(0xE1, 0xF5, 0xFE, 0xFF), new Color32(0x81, 0xD4, 0xFA, 0x00));

            // 子系统：冰屑飞散（Chip 方形碎片贴图）
            ParticleSystem chips = CreateChildSystem(root.transform, "Chips", ParticleTextureType.Chip);
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
        private static void BuildRockDust(GameObject root)
        {
            // 主系统：烟尘团升腾扩散
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft);
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
            vol.x = new ParticleSystem.MinMaxCurve(0f);
            vol.y = new ParticleSystem.MinMaxCurve(0.35f);
            vol.z = new ParticleSystem.MinMaxCurve(0f);

            SetSizeCurve(master, 0.6f, 1.6f);
            SetColorFade(master, new Color32(0xB0, 0xBE, 0xC5, 0xB3), new Color32(0x90, 0xA4, 0xAE, 0x00));

            // 子系统：岩石碎屑弹开
            ParticleSystem chips = CreateChildSystem(root.transform, "Chips", ParticleTextureType.Chip);
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
        private static void BuildRingPulse(GameObject root)
        {
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Ring);
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
        private static void BuildStarTwinkle(GameObject root)
        {
            // 主系统：中心微光（兼做生命周期载体）
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft);
            var main = master.main;
            main.duration = 0.9f;
            main.startLifetime = 0.5f;
            main.startSize = 0.3f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetSizeCurve(master, 1f, 0f);
            SetColorFade(master, new Color32(0xFF, 0xF9, 0xC4, 0xCC), new Color32(0xFF, 0xE0, 0x82, 0x00));

            // 子系统：四芒星错时弹出
            ParticleSystem stars = CreateChildSystem(root.transform, "Stars", ParticleTextureType.Spark);
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
        private static void BuildElementMix(GameObject root)
        {
            // 主系统：聚拢完成后的中心白闪（延迟爆发）
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft);
            var main = master.main;
            main.duration = 0.75f;
            main.startLifetime = 0.25f;
            main.startSize = 0.5f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0.3f, (short)1) });
            SetSizeCurve(master, 1f, 0.2f);
            SetColorFade(master, new Color32(0xFF, 0xFF, 0xFF, 0xF2), new Color32(0xFF, 0xD5, 0x4F, 0x00));

            // 子系统A：水蓝光点从环边向心聚拢
            ParticleSystem blueIn = CreateChildSystem(root.transform, "BlueIn", ParticleTextureType.Dot);
            ConfigureConverge(blueIn, new Color32(0x4F, 0xC3, 0xF7, 0xFF), 0f);

            // 子系统B：火橙光点从环边向心聚拢（稍迟启动）
            ParticleSystem orangeIn = CreateChildSystem(root.transform, "OrangeIn", ParticleTextureType.Dot);
            ConfigureConverge(orangeIn, new Color32(0xFF, 0x6F, 0x00, 0xFF), 0.05f);
        }

        /// <summary>
        /// 烟雾消散：烟团缓慢升腾淡出（通用轻反馈）。
        /// </summary>
        private static void BuildPoof(GameObject root)
        {
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft);
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
            vol.x = new ParticleSystem.MinMaxCurve(0f);
            vol.y = new ParticleSystem.MinMaxCurve(0.25f);
            vol.z = new ParticleSystem.MinMaxCurve(0f);

            SetSizeCurve(master, 0.6f, 1.5f);
            SetColorFade(master, new Color32(0xCF, 0xD8, 0xDC, 0x8C), new Color32(0xB0, 0xBE, 0xC5, 0x00));
        }

        /// <summary>
        /// 冲击波：快慢双层冲击环 + 高速光点（通用强反馈）。
        /// </summary>
        private static void BuildShockwave(GameObject root)
        {
            // 主系统：快速白环
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Ring);
            var main = master.main;
            main.duration = 0.7f;
            main.startLifetime = 0.22f;
            main.startSize = 0.8f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetSizeCurve(master, 0.15f, 1.5f);
            SetColorFade(master, new Color32(0xFF, 0xFF, 0xFF, 0xFF), new Color32(0x4F, 0xC3, 0xF7, 0x00));

            // 子系统：慢速厚环
            ParticleSystem slowRing = CreateChildSystem(root.transform, "SlowRing", ParticleTextureType.Ring);
            var rMain = slowRing.main;
            rMain.duration = 0.7f;
            rMain.startLifetime = 0.5f;
            rMain.startSize = 0.9f;
            slowRing.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetSizeCurve(slowRing, 0.25f, 1.8f);
            SetColorFade(slowRing, new Color32(0x81, 0xD4, 0xFA, 0xCC), new Color32(0x02, 0x77, 0xBD, 0x00));

            // 子系统：高速光点
            ParticleSystem dots = CreateChildSystem(root.transform, "Dots", ParticleTextureType.Dot);
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
        private static void BuildWarmGlow(GameObject root)
        {
            // 主系统：中心暖光呼吸
            ParticleSystem master = CreateMasterSystem(root, ParticleTextureType.Soft);
            var main = master.main;
            main.duration = 1.1f;
            main.startLifetime = 0.85f;
            main.startSize = 0.55f;
            master.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            SetBreatheSize(master);
            SetColorFade(master, new Color32(0xFF, 0xD1, 0x80, 0xE6), new Color32(0xFF, 0x8A, 0x65, 0x00));

            // 子系统：暖色光点环绕
            ParticleSystem orbitDots = CreateChildSystem(root.transform, "OrbitDots", ParticleTextureType.Dot);
            var oMain = orbitDots.main;
            oMain.duration = 1.1f;
            oMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            oMain.startSize = 0.09f;
            oMain.startSpeed = 0f;
            orbitDots.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)7) });
            SetRadialShape(orbitDots, 0.35f, thickness: 0f);

            // 光点缓慢上漂（本引擎无 orbitVelocity 模块，以纵向速度近似环绕动感）
            var drift = orbitDots.velocityOverLifetime;
            drift.enabled = true;
            drift.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            drift.y = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            drift.z = new ParticleSystem.MinMaxCurve(0f);

            SetColorFade(orbitDots, new Color32(0xFF, 0x8A, 0x65, 0xFF), new Color32(0xFF, 0xAB, 0x91, 0x00));
        }

        // ──────────────────────────────────────────────
        //  构建辅助
        // ──────────────────────────────────────────────

        /// <summary>
        /// 在特效根节点创建主 ParticleSystem（stopAction=Destroy，实例播完自动销毁）。
        /// </summary>
        private static ParticleSystem CreateMasterSystem(GameObject root, ParticleTextureType tex)
        {
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            ConfigureDefaults(ps, tex);

            // 粒子模块为结构体：取局部变量后赋值字段（setter 内部写回原生系统，不可整体回写）
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.Destroy;
            return ps;
        }

        /// <summary>
        /// 创建子 ParticleSystem（辅助层，随根节点一起销毁）。
        /// </summary>
        private static ParticleSystem CreateChildSystem(
            Transform parent, string name, ParticleTextureType tex)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ConfigureDefaults(ps, tex);
            return ps;
        }

        /// <summary>
        /// 通用默认配置：不循环、自动播放、世界空间模拟、零速率（纯 Burst 驱动）。
        /// </summary>
        private static void ConfigureDefaults(ParticleSystem ps, ParticleTextureType tex)
        {
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetMaterial(tex);
            renderer.sortingOrder = SORTING_ORDER;
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

        // ──────────────────────────────────────────────
        //  资产工具
        // ──────────────────────────────────────────────

        /// <summary>
        /// 确保目录存在，不存在则逐级创建。
        /// </summary>
        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// <summary>
        /// 如果指定路径已存在资产，则删除（覆盖更新）。
        /// </summary>
        private static void DeleteExistingAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }
    }
}
