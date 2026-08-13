/// ============================================================
/// 文件名: CharacterPrefabGenerator.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 角色预制体生成器Editor工具。将程序化生成的角色Sprite保存为
///       Texture2D .asset、Sprite .asset 和 GameObject .prefab 资源文件，
///       而非仅运行时生成。菜单：DualEnigma/生成角色预制体。
/// ============================================================

using UnityEngine;
using UnityEditor;
using DualEnigma.Art;
using DualEnigma.Character;

namespace DualEnigma.Editor
{
    /// <summary>
    /// 角色预制体生成器Editor工具。
    /// 将程序化生成的角色Sprite持久化为项目资源：
    /// 1. Texture2D 保存为 .asset（ArtResources/Textures/）
    /// 2. Sprite 保存为 .asset（ArtResources/Textures/）
    /// 3. GameObject 保存为 .prefab（ArtResources/Prefabs/Characters/）
    /// 引用：CharacterSpriteGenerator.cs, CharacterSystem.CreateCharacter
    /// </summary>
    public static class CharacterPrefabGenerator
    {
        /// <summary>Texture2D 与 Sprite 资源输出目录</summary>
        private const string TEXTURE_DIR = "Assets/ArtResources/Textures";

        /// <summary>预制体输出目录</summary>
        private const string PREFAB_DIR = "Assets/ArtResources/Prefabs/Characters";

        /// <summary>
        /// 菜单入口：生成 Aqua 与 Ignis 两个角色的预制体。
        /// </summary>
        [MenuItem("DualEnigma/生成角色预制体")]
        public static void Generate()
        {
            EnsureDirectory(TEXTURE_DIR);
            EnsureDirectory(PREFAB_DIR);

            GenerateCharacterPrefab(CharacterType.Aqua, "Character_Aqua", 0, new Vector2(-2f, 0f));
            GenerateCharacterPrefab(CharacterType.Ignis, "Character_Ignis", 1, new Vector2(2f, 0f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CharacterPrefabGenerator] 角色预制体生成完毕！\n" +
                      $"  Texture/Sprite 路径: {TEXTURE_DIR}/\n" +
                      $"  Prefab 路径: {PREFAB_DIR}/");
        }

        /// <summary>
        /// 为单个角色类型生成完整的资源：Texture2D、Sprite、Prefab。
        /// </summary>
        /// <param name="type">角色类型</param>
        /// <param name="prefabName">预制体名称（不含扩展名）</param>
        /// <param name="playerId">玩家ID（0=Aqua, 1=Ignis）</param>
        /// <param name="spawnPosition">默认生成位置</param>
        private static void GenerateCharacterPrefab(
            CharacterType type, string prefabName, byte playerId, Vector2 spawnPosition)
        {
            string spriteName = type == CharacterType.Aqua ? "Sprite_Aqua" : "Sprite_Ignis";

            // ---- 1. 生成 Sprite（内含 Texture2D）----
            // CharacterSpriteGenerator.GenerateCharacterSprite 内部调用
            // ProceduralSpriteGenerator.CreateTexture → 绘制像素 → TextureToSprite
            Sprite generatedSprite = CharacterSpriteGenerator.GenerateCharacterSprite(type);
            Texture2D tex = generatedSprite.texture;
            tex.name = spriteName;

            // ---- 2. 保存 Texture2D 为 .asset ----
            string texPath = $"{TEXTURE_DIR}/{spriteName}.asset";
            DeleteExistingAsset(texPath);
            AssetDatabase.CreateAsset(tex, texPath);
            Debug.Log($"[CharacterPrefabGenerator] Texture2D 已保存: {texPath}");

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
            Debug.Log($"[CharacterPrefabGenerator] Sprite 已保存: {spritePath}");

            // 加载已保存的 Sprite 资产，确保预制体引用的是持久化资产而非临时对象
            Sprite savedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            // ---- 4. 创建 GameObject 并添加组件（参考 CharacterSystem.CreateCharacter）----
            GameObject go = new GameObject(prefabName);
            go.transform.position = spawnPosition;

            // SpriteRenderer：赋值已保存的 Sprite 资产
            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = savedSprite;

            // Rigidbody2D：gravityScale=1, freezeRotation=true
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.freezeRotation = true;

            // BoxCollider2D：size=(0.8, 1.8)
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.8f);

            // CharacterController：使用完全限定名避免与 UnityEngine.CharacterController 冲突
            go.AddComponent<DualEnigma.Character.CharacterController>();

            // ---- 5. 保存为 .prefab ----
            string prefabPath = $"{PREFAB_DIR}/{prefabName}.prefab";
            DeleteExistingAsset(prefabPath);
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Debug.Log($"[CharacterPrefabGenerator] 预制体已保存: {prefabPath}");

            // ---- 6. 清理临时 GameObject ----
            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// 确保目录存在，不存在则逐级创建。
        /// </summary>
        /// <param name="path">相对于项目根的完整路径，如 "Assets/ArtResources/Textures"</param>
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
