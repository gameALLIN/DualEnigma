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
