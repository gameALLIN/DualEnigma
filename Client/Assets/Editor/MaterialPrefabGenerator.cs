/// ============================================================
/// 文件名: MaterialPrefabGenerator.cs
/// 创建时间: 2026-08-21
/// 作者: DualEnigma
/// 描述: 建筑材料预制体生成器Editor工具。将程序化生成的6种材料砖块Sprite
///       保存为 Texture2D .asset、Sprite .asset 和 GameObject .prefab 资源文件。
///       菜单：DualEnigma/生成建筑材料预制体。
/// ============================================================

using UnityEngine;
using UnityEditor;
using DualEnigma.Art;
using DualEnigma.Synthesis;

namespace DualEnigma.Editor
{
    /// <summary>
    /// 建筑材料预制体生成器Editor工具。
    /// 生成6种材料砖块预制体（水砖/冰砖/火砖/岩浆砖/石砖/温砖）。
    /// 建筑按蓝图由砖块排列而成（防火墙=竖排、防洪堤=横排等），
    /// 砖块即建筑的最小视觉单元，1格1块（32×32px = 1×1单位）。
    /// 1. Texture2D 保存为 .asset（ArtResources/Textures/Materials/）
    /// 2. Sprite 保存为 .asset（ArtResources/Textures/Materials/）
    /// 3. GameObject 保存为 .prefab（ArtResources/Prefabs/Materials/）
    /// 引用：MaterialSpriteGenerator.cs, MaterialType.cs, FragmentPrefabGenerator.cs (代码模式)
    /// </summary>
    public static class MaterialPrefabGenerator
    {
        /// <summary>Texture2D 与 Sprite 资源输出目录</summary>
        private const string TEXTURE_DIR = "Assets/ArtResources/Textures/Materials";

        /// <summary>预制体输出目录</summary>
        private const string PREFAB_DIR = "Assets/ArtResources/Prefabs/Materials";

        /// <summary>
        /// 菜单入口：生成6种材料砖块预制体。
        /// </summary>
        [MenuItem("DualEnigma/生成建筑材料预制体")]
        public static void Generate()
        {
            EnsureDirectory(TEXTURE_DIR);
            EnsureDirectory(PREFAB_DIR);

            GenerateMaterialPrefab(MaterialType.WaterBrick);
            GenerateMaterialPrefab(MaterialType.IceBrick);
            GenerateMaterialPrefab(MaterialType.FireBrick);
            GenerateMaterialPrefab(MaterialType.LavaBrick);
            GenerateMaterialPrefab(MaterialType.StoneBrick);
            GenerateMaterialPrefab(MaterialType.WarmBrick);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MaterialPrefabGenerator] 建筑材料预制体生成完毕！\n" +
                      $"  Texture/Sprite 路径: {TEXTURE_DIR}/\n" +
                      $"  Prefab 路径: {PREFAB_DIR}/");
        }

        /// <summary>
        /// 为单个材料类型生成完整的资源：Texture2D、Sprite、Prefab。
        /// 砖块为静态视觉单元，仅含 SpriteRenderer；
        /// 物理碰撞/Ground 层等由程序侧按需添加。
        /// </summary>
        /// <param name="type">材料类型</param>
        private static void GenerateMaterialPrefab(MaterialType type)
        {
            string spriteName = MaterialSpriteGenerator.GetSpriteName(type);

            // ---- 1. 生成 Sprite（内含 Texture2D）----
            Sprite generatedSprite = MaterialSpriteGenerator.GenerateMaterialSprite(type);
            Texture2D tex = generatedSprite.texture;
            tex.name = spriteName;

            // ---- 2. 保存 Texture2D 为 .asset ----
            string texPath = $"{TEXTURE_DIR}/{spriteName}.asset";
            DeleteExistingAsset(texPath);
            AssetDatabase.CreateAsset(tex, texPath);
            Debug.Log($"[MaterialPrefabGenerator] Texture2D 已保存: {texPath}");

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
            Debug.Log($"[MaterialPrefabGenerator] Sprite 已保存: {spritePath}");

            // 加载已保存的 Sprite 资产，确保预制体引用的是持久化资产而非临时对象
            Sprite savedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            // ---- 4. 创建 GameObject 并添加组件 ----
            GameObject go = new GameObject(spriteName);

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = savedSprite;

            // ---- 5. 保存为 .prefab ----
            string prefabPath = $"{PREFAB_DIR}/{spriteName}.prefab";
            DeleteExistingAsset(prefabPath);
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Debug.Log($"[MaterialPrefabGenerator] 预制体已保存: {prefabPath}");

            // ---- 6. 清理临时 GameObject ----
            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// 确保目录存在，不存在则逐级创建。
        /// </summary>
        /// <param name="path">相对于项目根的完整路径，如 "Assets/ArtResources/Textures/Materials"</param>
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
