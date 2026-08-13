/// ============================================================
/// 文件名: MapLoader.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 地图加载器，程序化生成地图场景对象（背景、天空、地面、墙壁、安全区、网格）。
///       地图尺寸 40×20格，原点在地图中心。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.Core;
using DualEnigma.Art;

namespace DualEnigma.Core
{
    /// <summary>
    /// 地图加载器。在 GameLaunch 中调用，程序化生成全部地图 GameObject。
    /// 布局（原点=地图中心）：
    ///   背景 40×20  (0, 0)    z=10
    ///   天空 40×10  (0, 5)    z=9
    ///   地面 40×2   (0,-9)    z=5  Ground层 + BoxCollider2D
    ///   左壁 2×4   (-19,-6)   z=5  Ground层 + BoxCollider2D
    ///   右壁 2×4   ( 19,-6)   z=5  Ground层 + BoxCollider2D
    ///   安全区 15×8 (0,-4)    z=1
    ///   网格   15×8 (0,-4)    z=0
    /// </summary>
    public static class MapLoader
    {
        private const int GROUND_LAYER = 7;

        /// <summary>地图根节点名称</summary>
        private const string MAP_ROOT_NAME = "MapRoot";

        /// <summary>
        /// 加载完整地图，返回地图根节点。
        /// </summary>
        public static GameObject Load()
        {
            GameObject root = new GameObject(MAP_ROOT_NAME);
            Transform rootT = root.transform;

            CreateSpriteObject(rootT, "Background", MapSpriteGenerator.GenerateBackgroundSprite(),
                new Vector3(0, 0, 10), 40, 20);

            CreateSpriteObject(rootT, "Sky", MapSpriteGenerator.GenerateSkySprite(),
                new Vector3(0, 5, 9), 40, 10);

            // 地面：Ground 层 + 碰撞体
            GameObject ground = CreateSpriteObject(rootT, "Ground", MapSpriteGenerator.GenerateGroundSprite(),
                new Vector3(0, -9, 5), 40, 2);
            SetupGroundCollider(ground);

            // 左壁
            GameObject leftWall = CreateSpriteObject(rootT, "Wall_Left", MapSpriteGenerator.GenerateWallSprite(),
                new Vector3(-19, -6, 5), 2, 4);
            SetupGroundCollider(leftWall);

            // 右壁
            GameObject rightWall = CreateSpriteObject(rootT, "Wall_Right", MapSpriteGenerator.GenerateWallSprite(),
                new Vector3(19, -6, 5), 2, 4);
            SetupGroundCollider(rightWall);

            // 安全区标记
            CreateSpriteObject(rootT, "SafeZone", MapSpriteGenerator.GenerateSafeZoneSprite(),
                new Vector3(0, -4, 1), 15, 8);

            // 建筑网格
            CreateSpriteObject(rootT, "BuildingGrid", MapSpriteGenerator.GenerateGridSprite(),
                new Vector3(0, -4, 0), 15, 8);

            Debug.Log("[MapLoader] 地图加载完成");
            return root;
        }

        /// <summary>
        /// 创建带 SpriteRenderer 的地图对象。
        /// </summary>
        private static GameObject CreateSpriteObject(
            Transform parent, string name, Sprite sprite, Vector3 position, float width, float height)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Mathf.RoundToInt(-position.z * 10);

            return go;
        }

        /// <summary>
        /// 为地面/墙壁设置 Ground 层和 BoxCollider2D。
        /// </summary>
        private static void SetupGroundCollider(GameObject go)
        {
            go.layer = GROUND_LAYER;
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.autoTiling = false;
        }
    }
}
