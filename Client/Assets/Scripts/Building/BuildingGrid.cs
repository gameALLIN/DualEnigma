/// ============================================================
/// 文件名: BuildingGrid.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 15×8格建筑区域网格管理。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Building
{
    /// <summary>
    /// 网格坐标 ↔ 世界坐标统一换算。
    /// 建筑网格 15×8 铺设在地图安全区（世界中心 (0,-4)，与 MapLoader 一致）：
    /// 网格左下角 (0,0) 对应世界 (-7,-7.5)，每格 1 世界单位。
    /// 所有"网格坐标 vs 角色世界坐标"的比较必须经此换算，不得混用两套坐标系。
    /// </summary>
    public static class GridCoord
    {
        /// <summary>网格宽（与 BuildingGrid.Width 一致）</summary>
        public const int Width = BuildingGrid.Width;

        /// <summary>网格高（与 BuildingGrid.Height 一致）</summary>
        public const int Height = BuildingGrid.Height;

        /// <summary>网格中心的世界坐标（安全区中心）</summary>
        public static readonly Vector2 WorldCenter = new Vector2(0f, -4f);

        /// <summary>网格坐标 → 世界坐标（格子中心）</summary>
        public static Vector2 WorldFromGrid(Vector2Int gridPos)
        {
            return new Vector2(
                gridPos.x - (Width - 1) * 0.5f + WorldCenter.x,
                gridPos.y - (Height - 1) * 0.5f + WorldCenter.y);
        }

        /// <summary>世界坐标 → 最近网格坐标（越界返回 false）</summary>
        public static bool TryGridFromWorld(Vector2 worldPos, out Vector2Int gridPos)
        {
            float x = worldPos.x - WorldCenter.x + (Width - 1) * 0.5f;
            float y = worldPos.y - WorldCenter.y + (Height - 1) * 0.5f;
            int gx = Mathf.RoundToInt(x);
            int gy = Mathf.RoundToInt(y);
            gridPos = new Vector2Int(gx, gy);
            return gx >= 0 && gx < Width && gy >= 0 && gy < Height;
        }
    }

    /// <summary>
    /// 15×8格建筑区域网格管理。
    /// 引用：建造系统.md §3.2
    /// </summary>
    public class BuildingGrid
    {
        public const int Width = 15;
        public const int Height = 8;

        private readonly int[,] _grid = new int[Width, Height];

        /// <summary>网格坐标是否被占用</summary>
        public bool IsOccupied(Vector2Int pos)
        {
            if (!IsValid(pos)) return false;
            return _grid[pos.x, pos.y] >= 0;
        }

        /// <summary>设置网格占用</summary>
        public void SetOccupied(Vector2Int pos, int buildingId)
        {
            if (IsValid(pos))
                _grid[pos.x, pos.y] = buildingId;
        }

        /// <summary>清除网格占用</summary>
        public void ClearOccupied(Vector2Int pos)
        {
            if (IsValid(pos))
                _grid[pos.x, pos.y] = -1;
        }

        /// <summary>获取指定网格位置的建筑ID</summary>
        public int GetBuildingIdAt(Vector2Int pos)
        {
            if (!IsValid(pos)) return -1;
            return _grid[pos.x, pos.y];
        }

        /// <summary>清空整个网格</summary>
        public void Clear()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    _grid[x, y] = -1;
        }

        private bool IsValid(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }
    }
}
