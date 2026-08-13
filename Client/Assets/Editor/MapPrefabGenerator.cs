/// ============================================================
/// 文件名: MapPrefabGenerator.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 地图预制体生成器Editor工具。将程序化生成的地图Sprite保存为
///       Texture2D .asset、Sprite .asset 和 GameObject .prefab 资源文件。
///       菜单：DualEnigma/生成第一关地图预制体。
/// ============================================================

using UnityEngine;
using UnityEditor;
using DualEnigma.Art;

namespace DualEnigma.Editor
{
    /// <summary>
    /// 第一关地图预制体生成器。
    /// 基于 GDD §9 地图设计规范，生成包含背景、天空、地面、墙壁、
    /// 出生点、安全区、建筑网格的完整地图预制体。
    /// 引用：MapSpriteGenerator.cs, CharacterPrefabGenerator.cs (代码模式)
    /// </summary>
    public static class MapPrefabGenerator
    {
        private const string TEXTURE_DIR = "Assets/ArtResources/Textures/Map";
        private const string PREFAB_DIR = "Assets/ArtResources/Prefabs/Maps";

        /// <summary>Ground 层索引 (TagManager Layer 7 = Ground)</summary>
        private const int _groundLayer = 7;

        /// <summary>
        /// 菜单入口：生成第一关地图预制体。
        /// </summary>
        [MenuItem("DualEnigma/生成第一关地图预制体")]
        public static void Generate()
        {
            EnsureDirectory(TEXTURE_DIR);
            EnsureDirectory(PREFAB_DIR);

            // 生成并保存所有地图Sprite
            Sprite bgSprite = SaveSpriteAsset(MapSpriteGenerator.GenerateBackgroundSprite(), "Map_Background");
            Sprite skySprite = SaveSpriteAsset(MapSpriteGenerator.GenerateSkySprite(), "Map_Sky");
            Sprite groundSprite = SaveSpriteAsset(MapSpriteGenerator.GenerateGroundSprite(), "Map_Ground");
            Sprite wallSprite = SaveSpriteAsset(MapSpriteGenerator.GenerateWallSprite(), "Map_Wall");
            Sprite safeZoneSprite = SaveSpriteAsset(MapSpriteGenerator.GenerateSafeZoneSprite(), "Map_SafeZone");
            Sprite gridSprite = SaveSpriteAsset(MapSpriteGenerator.GenerateGridSprite(), "Map_BuildingGrid");

            // 构建地图 GameObject 层级
            GameObject root = BuildMapHierarchy(
                bgSprite, skySprite, groundSprite, wallSprite, safeZoneSprite, gridSprite);

            // 保存为预制体
            string prefabPath = $"{PREFAB_DIR}/Map_Level1.prefab";
            DeleteExistingAsset(prefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MapPrefabGenerator] 第一关地图预制体生成完毕！\n" +
                      $"  Texture/Sprite 路径: {TEXTURE_DIR}/\n" +
                      $"  Prefab 路径: {prefabPath}");
        }

        /// <summary>
        /// 构建地图GameObject层级结构。
        /// 坐标系：地面顶部 Y=0，地图宽40格(X: -20~20)，高20格(Y: -2~18)。
        /// </summary>
        private static GameObject BuildMapHierarchy(
            Sprite bgSprite, Sprite skySprite, Sprite groundSprite,
            Sprite wallSprite, Sprite safeZoneSprite, Sprite gridSprite)
        {
            // ---- 根节点 ----
            GameObject root = new GameObject("Map_Level1");
            root.transform.position = Vector3.zero;

            // ---- 背景层 (Y=8, Z=-10) — 20格高居中 ----
            GameObject background = CreateSpriteChild("Background", root.transform,
                bgSprite, new Vector3(0f, 8f, -10f));
            background.GetComponent<SpriteRenderer>().sortingOrder = -10;

            // ---- 天空层 (Y=13, Z=-8) — 上方10格居中 ----
            GameObject sky = CreateSpriteChild("Sky", root.transform,
                skySprite, new Vector3(0f, 13f, -8f));
            sky.GetComponent<SpriteRenderer>().sortingOrder = -8;

            // ---- 地面层 (Y=-1, Z=0) — 2格高，顶部对齐Y=0 ----
            GameObject ground = CreateSpriteChild("Ground", root.transform,
                groundSprite, new Vector3(0f, -1f, 0f));
            ground.layer = _groundLayer;
            // BoxCollider2D: 40格宽 × 2格高
            BoxCollider2D groundCol = ground.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(40f, 2f);
            groundCol.offset = Vector2.zero;

            // ---- 墙壁层 ----
            GameObject walls = new GameObject("Walls");
            walls.transform.SetParent(root.transform, false);

            // 左墙 (X=-19, Y=1) — 2格宽，4格高，中心偏移
            GameObject wallLeft = CreateSpriteChild("Wall_Left", walls.transform,
                wallSprite, new Vector3(-19f, 1f, 0f));
            wallLeft.layer = _groundLayer;
            BoxCollider2D leftCol = wallLeft.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(2f, 4f);

            // 右墙 (X=19, Y=1)
            GameObject wallRight = CreateSpriteChild("Wall_Right", walls.transform,
                wallSprite, new Vector3(19f, 1f, 0f));
            wallRight.layer = _groundLayer;
            BoxCollider2D rightCol = wallRight.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(2f, 4f);

            // ---- 出生点 ----
            GameObject spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(root.transform, false);

            GameObject spawnAqua = new GameObject("Spawn_Aqua");
            spawnAqua.transform.SetParent(spawnPoints.transform, false);
            spawnAqua.transform.position = new Vector3(-2f, 0f, 0f);

            GameObject spawnIgnis = new GameObject("Spawn_Ignis");
            spawnIgnis.transform.SetParent(spawnPoints.transform, false);
            spawnIgnis.transform.position = new Vector3(2f, 0f, 0f);

            // ---- 安全区 (15×8格居中, Y=4) ----
            GameObject safeZone = CreateSpriteChild("SafeZone", root.transform,
                safeZoneSprite, new Vector3(0f, 4f, -1f));
            safeZone.GetComponent<SpriteRenderer>().sortingOrder = -1;

            // ---- 建筑网格 (与安全区重叠) ----
            GameObject buildingGrid = CreateSpriteChild("BuildingGrid", root.transform,
                gridSprite, new Vector3(0f, 4f, -0.5f));
            buildingGrid.GetComponent<SpriteRenderer>().sortingOrder = 0;

            // ---- 碎片掉落区域标记 ----
            GameObject fragmentZones = new GameObject("FragmentDropZones");
            fragmentZones.transform.SetParent(root.transform, false);

            GameObject dropZoneLeft = new GameObject("DropZone_Left");
            dropZoneLeft.transform.SetParent(fragmentZones.transform, false);
            dropZoneLeft.transform.position = new Vector3(-5f, 16f, 0f);

            GameObject dropZoneRight = new GameObject("DropZone_Right");
            dropZoneRight.transform.SetParent(fragmentZones.transform, false);
            dropZoneRight.transform.position = new Vector3(5f, 16f, 0f);

            GameObject dropZoneCenter = new GameObject("DropZone_Center");
            dropZoneCenter.transform.SetParent(fragmentZones.transform, false);
            dropZoneCenter.transform.position = new Vector3(0f, 16f, 0f);

            return root;
        }

        /// <summary>
        /// 创建带 SpriteRenderer 的子 GameObject。
        /// </summary>
        private static GameObject CreateSpriteChild(
            string name, Transform parent, Sprite sprite, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            return go;
        }

        /// <summary>
        /// 生成Sprite，保存Texture2D和Sprite为.asset，返回持久化的Sprite引用。
        /// </summary>
        private static Sprite SaveSpriteAsset(Sprite generatedSprite, string assetName)
        {
            Texture2D tex = generatedSprite.texture;
            tex.name = assetName;

            // 保存 Texture2D
            string texPath = $"{TEXTURE_DIR}/{assetName}.asset";
            DeleteExistingAsset(texPath);
            AssetDatabase.CreateAsset(tex, texPath);

            // 从持久化 Texture 重新创建 Sprite 并保存
            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Sprite sprite = Sprite.Create(
                savedTex,
                new Rect(0, 0, savedTex.width, savedTex.height),
                new Vector2(0.5f, 0.5f),
                ProceduralSpriteGenerator.PixelsPerUnit,
                0u,
                SpriteMeshType.FullRect
            );
            sprite.name = assetName;

            string spritePath = $"{TEXTURE_DIR}/{assetName}_Sprite.asset";
            DeleteExistingAsset(spritePath);
            AssetDatabase.CreateAsset(sprite, spritePath);

            return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        /// <summary>
        /// 确保目录存在，不存在则逐级创建。
        /// </summary>
        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
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
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }
    }
}
