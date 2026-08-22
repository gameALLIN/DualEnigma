/// ============================================================
/// 文件名: BuildingVisualizer.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 建造视觉层。程序化渲染建筑蓝图（半透明）与已放置建筑（材料色），
///       坐标换算统一走 GridCoord（15×8 网格，安全区世界中心 (0,-4)）。
///       由 BuildingSystem 在蓝图生成/放置/摧毁时调用，纯表现无逻辑。
/// 引用：BuildingSystem.cs, GridCoord, MaterialSpriteGenerator
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Art;
using DualEnigma.Synthesis;
using DualEnigma.Core;

namespace DualEnigma.Building
{
    /// <summary>建造视觉层：蓝图块 + 建筑实例的 SpriteRenderer 管理</summary>
    public class BuildingVisualizer
    {
        /// <summary>视觉根节点（懒建）</summary>
        private Transform _root;

        /// <summary>蓝图块视觉：gridPos → 渲染节点</summary>
        private readonly Dictionary<Vector2Int, SpriteRenderer> _blueprintNodes = new Dictionary<Vector2Int, SpriteRenderer>();

        /// <summary>建筑视觉：buildingId → 渲染节点</summary>
        private readonly Dictionary<int, SpriteRenderer> _buildingNodes = new Dictionary<int, SpriteRenderer>();

        /// <summary>按材料类型缓存的 Sprite（MaterialSpriteGenerator 程序化生成）</summary>
        private readonly Dictionary<MaterialType, Sprite> _materialSprites = new Dictionary<MaterialType, Sprite>();

        /// <summary>蓝图占位 Sprite（半透明白色方块，懒建）</summary>
        private Sprite _blueprintSprite;

        private const int SORTING_ORDER = 2; // 地面网格(z=0)之上、角色之下

        private Transform Root
        {
            get
            {
                if (_root == null)
                {
                    var go = new GameObject("BuildingVisualRoot");
                    _root = go.transform;
                    Object.DontDestroyOnLoad(go);
                }
                return _root;
            }
        }

        /// <summary>渲染整张蓝图（Preview 阶段生成后调用；重复调用先清空旧视觉）</summary>
        public void RenderBlueprint(List<BlueprintBlock> blueprint)
        {
            ClearBlueprint();

            if (_blueprintSprite == null)
                _blueprintSprite = CreateSolidSprite(new Color32(0x4F, 0xC3, 0xF7, 0x55));

            foreach (var block in blueprint)
            {
                if (_blueprintNodes.ContainsKey(block.GridPosition)) continue;

                var node = new GameObject($"Blueprint_{block.GridPosition.x}_{block.GridPosition.y}");
                node.transform.SetParent(Root, false);
                node.transform.position = new Vector3(
                    GridCoord.WorldFromGrid(block.GridPosition).x,
                    GridCoord.WorldFromGrid(block.GridPosition).y,
                    0f);

                var sr = node.AddComponent<SpriteRenderer>();
                sr.sprite = _blueprintSprite;
                sr.sortingOrder = SORTING_ORDER;
                _blueprintNodes[block.GridPosition] = sr;
            }
        }

        /// <summary>蓝图块完成时高亮为材料色（半透明实色）</summary>
        public void MarkBlueprintCompleted(Vector2Int gridPos, MaterialType material)
        {
            if (!_blueprintNodes.TryGetValue(gridPos, out SpriteRenderer sr) || sr == null) return;
            sr.sprite = GetMaterialSprite(material);
            sr.color = new Color32(0xFF, 0xFF, 0xFF, 0x88); // 半透明
        }

        /// <summary>放置建筑：实心材料色块替换蓝图位置视觉</summary>
        public void ShowBuilding(int buildingId, Vector2Int gridPos, MaterialType material)
        {
            RemoveBlueprintNode(gridPos);

            var node = new GameObject($"Building_{buildingId}");
            node.transform.SetParent(Root, false);
            node.transform.position = new Vector3(
                GridCoord.WorldFromGrid(gridPos).x,
                GridCoord.WorldFromGrid(gridPos).y,
                0f);

            var sr = node.AddComponent<SpriteRenderer>();
            sr.sprite = GetMaterialSprite(material);
            sr.sortingOrder = SORTING_ORDER;
            _buildingNodes[buildingId] = sr;
        }

        /// <summary>建筑受击闪白反馈（按剩余 HP 比例调暗）</summary>
        public void UpdateBuildingVisual(int buildingId, float hpRatio)
        {
            if (!_buildingNodes.TryGetValue(buildingId, out SpriteRenderer sr) || sr == null) return;
            float gray = Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(hpRatio));
            sr.color = new Color(gray, gray, gray, 1f);
        }

        /// <summary>摧毁建筑：移除视觉</summary>
        public void RemoveBuilding(int buildingId)
        {
            if (_buildingNodes.TryGetValue(buildingId, out SpriteRenderer sr) && sr != null)
                Object.Destroy(sr.gameObject);
            _buildingNodes.Remove(buildingId);
        }

        /// <summary>清空全部视觉（新局/对局结束时调用）</summary>
        public void ClearAll()
        {
            ClearBlueprint();
            foreach (var kvp in _buildingNodes)
            {
                if (kvp.Value != null)
                    Object.Destroy(kvp.Value.gameObject);
            }
            _buildingNodes.Clear();
        }

        private void ClearBlueprint()
        {
            foreach (var kvp in _blueprintNodes)
            {
                if (kvp.Value != null)
                    Object.Destroy(kvp.Value.gameObject);
            }
            _blueprintNodes.Clear();
        }

        private void RemoveBlueprintNode(Vector2Int gridPos)
        {
            if (_blueprintNodes.TryGetValue(gridPos, out SpriteRenderer sr) && sr != null)
                Object.Destroy(sr.gameObject);
            _blueprintNodes.Remove(gridPos);
        }

        private Sprite GetMaterialSprite(MaterialType material)
        {
            if (!_materialSprites.TryGetValue(material, out Sprite sprite))
            {
                sprite = MaterialSpriteGenerator.GenerateMaterialSprite(material);
                _materialSprites[material] = sprite;
            }
            return sprite;
        }

        /// <summary>纯色方块 Sprite（蓝图占位用）</summary>
        private static Sprite CreateSolidSprite(Color32 color)
        {
            const int size = 24;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var fill = color;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, fill);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), ProceduralSpriteGenerator.PixelsPerUnit,
                0u, SpriteMeshType.FullRect);
        }
    }
}
