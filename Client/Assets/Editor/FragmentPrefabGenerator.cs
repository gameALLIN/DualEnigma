/// ============================================================
/// 文件名: FragmentPrefabGenerator.cs
/// 创建时间: 2026-08-21
/// 作者: DualEnigma
/// 描述: 碎片预制体生成器Editor工具。将程序化生成的3种碎片Sprite保存为
///       Texture2D .asset、Sprite .asset 和 GameObject .prefab 资源文件。
///       菜单：DualEnigma/生成掉落物预制体。
/// ============================================================

using UnityEngine;
using UnityEditor;
using DualEnigma.Art;
using DualEnigma.Fragment;

namespace DualEnigma.Editor
{
    /// <summary>
    /// 碎片预制体生成器Editor工具。
    /// 生成3种掉落碎片预制体（冰晶★/熔岩★★/岩石★★★，第一关碎片配比 70/20/10，
    /// 3种类型全关卡通用）：
    /// 1. Texture2D 保存为 .asset（ArtResources/Textures/Fragments/）
    /// 2. Sprite 保存为 .asset（ArtResources/Textures/Fragments/）
    /// 3. GameObject 保存为 .prefab（ArtResources/Prefabs/Fragments/）
    /// 组件结构与 FragmentSystem.SpawnFragment 动态创建路径一致：
    /// SpriteRenderer + Rigidbody2D + BoxCollider2D(isTrigger) + FragmentController。
    /// 引用：FragmentSpriteGenerator.cs, FragmentController.cs, CharacterPrefabGenerator.cs (代码模式)
    /// </summary>
    public static class FragmentPrefabGenerator
    {
        /// <summary>Texture2D 与 Sprite 资源输出目录</summary>
        private const string TEXTURE_DIR = "Assets/ArtResources/Textures/Fragments";

        /// <summary>预制体输出目录</summary>
        private const string PREFAB_DIR = "Assets/ArtResources/Prefabs/Fragments";

        /// <summary>
        /// 菜单入口：生成冰晶/熔岩/岩石3种碎片预制体。
        /// </summary>
        [MenuItem("DualEnigma/生成掉落物预制体")]
        public static void Generate()
        {
            EnsureDirectory(TEXTURE_DIR);
            EnsureDirectory(PREFAB_DIR);

            // 碰撞体尺寸按各碎片视觉体量配置（世界单位，1格=1单位）
            GenerateFragmentPrefab(FragmentType.IceCrystal, new Vector2(0.4f, 0.6f));
            GenerateFragmentPrefab(FragmentType.Lava, new Vector2(0.5f, 0.5f));
            GenerateFragmentPrefab(FragmentType.Rock, new Vector2(0.55f, 0.55f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FragmentPrefabGenerator] 掉落物预制体生成完毕！\n" +
                      $"  Texture/Sprite 路径: {TEXTURE_DIR}/\n" +
                      $"  Prefab 路径: {PREFAB_DIR}/");
        }

        /// <summary>
        /// 为单个碎片类型生成完整的资源：Texture2D、Sprite、Prefab。
        /// </summary>
        /// <param name="type">碎片类型</param>
        /// <param name="colliderSize">Trigger 碰撞体尺寸（世界单位）</param>
        private static void GenerateFragmentPrefab(FragmentType type, Vector2 colliderSize)
        {
            string spriteName = FragmentSpriteGenerator.GetSpriteName(type);

            // ---- 1. 生成 Sprite（内含 Texture2D）----
            Sprite generatedSprite = FragmentSpriteGenerator.GenerateFragmentSprite(type);
            Texture2D tex = generatedSprite.texture;
            tex.name = spriteName;

            // ---- 2. 保存 Texture2D 为 .asset ----
            string texPath = $"{TEXTURE_DIR}/{spriteName}.asset";
            DeleteExistingAsset(texPath);
            AssetDatabase.CreateAsset(tex, texPath);
            Debug.Log($"[FragmentPrefabGenerator] Texture2D 已保存: {texPath}");

            // ---- 3. 从已保存的 Texture2D 创建 Sprite 并保存为单独 .asset ----
            // 重新从资产加载 Texture2D，确保 Sprite 的纹理引用指向持久化资产
            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Sprite sprite = Sprite.Create(
                savedTex,
                new Rect(0, 0, savedTex.width, savedTex.height),
                new Vector2(0.5f, 0.5f), // 枢轴在中心
                ProceduralSpriteGenerator.PixelsPerUnit, // PPU=32
                0u, // extrude
                SpriteMeshType.FullRect
            );
            sprite.name = spriteName;

            string spritePath = $"{TEXTURE_DIR}/{spriteName}_Sprite.asset";
            DeleteExistingAsset(spritePath);
            AssetDatabase.CreateAsset(sprite, spritePath);
            Debug.Log($"[FragmentPrefabGenerator] Sprite 已保存: {spritePath}");

            // 加载已保存的 Sprite 资产，确保预制体引用的是持久化资产而非临时对象
            Sprite savedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            // ---- 4. 创建 GameObject 并添加组件（参考 FragmentSystem.SpawnFragment）----
            GameObject go = new GameObject(spriteName);

            // SpriteRenderer：赋值已保存的 Sprite 资产
            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = savedSprite;

            // Rigidbody2D：gravityScale=1, freezeRotation=true（FragmentController.Awake 亦会设置）
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.freezeRotation = true;

            // BoxCollider2D：isTrigger=true，供角色 Trigger 检测接住碎片
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = colliderSize;

            // FragmentController：管理存续倒计时和状态
            go.AddComponent<FragmentController>();

            // ---- 5. 保存为 .prefab ----
            string prefabPath = $"{PREFAB_DIR}/{spriteName}.prefab";
            DeleteExistingAsset(prefabPath);
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Debug.Log($"[FragmentPrefabGenerator] 预制体已保存: {prefabPath}");

            // ---- 6. 清理临时 GameObject ----
            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// 确保目录存在，不存在则逐级创建。
        /// </summary>
        /// <param name="path">相对于项目根的完整路径，如 "Assets/ArtResources/Textures/Fragments"</param>
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
        /// <param name="path">资产路径</param>
        private static void DeleteExistingAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }
    }
}
