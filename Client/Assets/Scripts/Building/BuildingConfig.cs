/// ============================================================
/// 文件名: BuildingConfig.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 建造系统配置数据（ScriptableObject）。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Building
{
    /// <summary>
    /// 建造系统配置。
    /// 引用：建造系统.md §6.1
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingConfig", menuName = "DualEnigma/BuildingConfig")]
    public class BuildingConfig : ScriptableObject
    {
        [Header("建筑类型参数")]
        [SerializeField] private float[] _buildingHP = { 50f, 40f, 60f, 40f, 40f };
        [SerializeField] private bool[] _hasFacing = { true, true, false, false, true };

        [Header("建筑区域")]
        [SerializeField] private int _gridWidth = 15;
        [SerializeField] private int _gridHeight = 8;
        [SerializeField] private float _placeTime = 0.5f;
        [SerializeField] private float _repairTime = 1f;
        [SerializeField] private float _demolishTime = 1f;

        [Header("蓝图块数范围")]
        [SerializeField] private int[] _minBlocks = { 3, 5, 7 };
        [SerializeField] private int[] _maxBlocks = { 4, 6, 8 };

        [Header("安全区")]
        [SerializeField] private Vector2Int _defaultSafeZoneCenter = new Vector2Int(7, 4);
        [SerializeField] private float _defaultSafeZoneRadius = 5f;

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public float PlaceTime => _placeTime;
        public float RepairTime => _repairTime;
        public float DemolishTime => _demolishTime;

        public Vector2Int DefaultSafeZoneCenter => _defaultSafeZoneCenter;
        public float DefaultSafeZoneRadius => _defaultSafeZoneRadius;

        public float GetBuildingHP(BuildingType type) => _buildingHP[(int)type];
        public bool HasFacing(BuildingType type) => _hasFacing[(int)type];
        public int GetMinBlocks(int round) => _minBlocks[Mathf.Clamp(round - 1, 0, 2)];
        public int GetMaxBlocks(int round) => _maxBlocks[Mathf.Clamp(round - 1, 0, 2)];
    }
}
