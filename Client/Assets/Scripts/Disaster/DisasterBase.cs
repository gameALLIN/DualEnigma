/// ============================================================
/// 文件名: DisasterBase.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 灾难基类，所有35种灾难继承此类。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Data;
using DualEnigma.Building;
using DualEnigma.Synthesis;
using DualEnigma.Shelter;
using DualEnigma.Character;
using DualEnigma.Fragment;

namespace DualEnigma.Disaster
{
    /// <summary>
    /// 灾难基类。所有35种灾难继承此类。
    /// 生命周期：OnStart → OnUpdate(渐进强度) → OnEnd
    /// 引用：灾难系统.md §3.1
    /// </summary>
    public abstract class DisasterBase
    {
        /// <summary>灾难参数</summary>
        public DisasterParams Params { get; protected set; }

        /// <summary>当前强度（0~1）</summary>
        public float CurrentIntensity { get; protected set; }

        /// <summary>是否正在运行</summary>
        public bool IsRunning { get; protected set; }

        /// <summary>已运行时间</summary>
        protected float ElapsedTime;

        /// <summary>灾难配置（用于读取 IntensityCurve 等）</summary>
        private DisasterConfig _disasterConfig;

        /// <summary>建筑快照缓存（复用避免每帧分配）</summary>
        private readonly List<BuildingData> _buildingSnapshotCache = new List<BuildingData>();

        protected IBuildSystem _cachedBuildSystem;
        protected IShelterSystem _cachedShelterSystem;
        protected ICharacterSystem _cachedCharacterSystem;
        protected IFragmentSystem _cachedFragmentSystem;
        protected ISynthesisSystem _cachedSynthesisSystem;

        protected void CacheServiceReferences()
        {
            _cachedBuildSystem = ServiceLocator.Get<IBuildSystem>();
            _cachedShelterSystem = ServiceLocator.Get<IShelterSystem>();
            _cachedCharacterSystem = ServiceLocator.Get<ICharacterSystem>();
            _cachedFragmentSystem = ServiceLocator.Get<IFragmentSystem>();
            _cachedSynthesisSystem = ServiceLocator.Get<ISynthesisSystem>();
        }

        /// <summary>灾难开始</summary>
        public abstract void OnStart();

        /// <summary>每帧更新</summary>
        public abstract void OnUpdate(float deltaTime, float elapsedTime);

        /// <summary>
        /// 外部调用入口，更新 ElapsedTime 后委托给 OnUpdate。
        /// </summary>
        public void Tick(float deltaTime, float elapsedTime)
        {
            ElapsedTime = elapsedTime;
            OnUpdate(deltaTime, elapsedTime);
        }

        /// <summary>灾难结束</summary>
        public abstract void OnEnd();

        /// <summary>
        /// 计算当前渐进强度。
        /// 优先从 DisasterConfig.IntensityCurve 读取，无配置时回退到硬编码时间轴。
        /// 引用：灾难系统设计.md §6.3 渐进入侵节律
        /// </summary>
        protected virtual float CalculateIntensity(float elapsedTime)
        {
            float[] curve = _disasterConfig != null ? _disasterConfig.IntensityCurve : null;

            if (curve == null || curve.Length == 0)
            {
                if (elapsedTime < 5f)
                    return Mathf.Lerp(0f, 0.3f, elapsedTime / 5f);
                if (elapsedTime < 10f)
                    return Mathf.Lerp(0.3f, 0.6f, (elapsedTime - 5f) / 5f);
                if (elapsedTime < 15f)
                    return Mathf.Lerp(0.6f, 1.0f, (elapsedTime - 10f) / 5f);
                return Mathf.Lerp(1.0f, 0.8f, (elapsedTime - 15f) / 5f);
            }

            const float segmentDuration = 5f;
            int segmentCount = curve.Length;

            if (elapsedTime >= segmentCount * segmentDuration)
                return curve[segmentCount - 1];

            int segmentIndex = Mathf.Clamp(Mathf.FloorToInt(elapsedTime / segmentDuration), 0, segmentCount - 1);
            float t = (elapsedTime - segmentIndex * segmentDuration) / segmentDuration;
            float startValue = segmentIndex == 0 ? 0f : curve[segmentIndex - 1];
            float endValue = curve[segmentIndex];

            return Mathf.Lerp(startValue, endValue, t);
        }

        /// <summary>初始化灾难参数</summary>
        public virtual void Initialize(DisasterParams parameters)
        {
            Params = parameters;
            CurrentIntensity = 0f;
            IsRunning = false;
            ElapsedTime = 0f;

            if (_disasterConfig == null)
                _disasterConfig = DataManager.Instance.LoadConfig<DisasterConfig>();

            CacheServiceReferences();
        }

        /// <summary>
        /// 对所有建筑施加灾害伤害。
        /// 子类在 OnUpdate 中调用此方法完成通用伤害逻辑。
        /// </summary>
        protected virtual void ApplyDamageToBuildings(float deltaTime)
        {
            var buildSystem = _cachedBuildSystem;
            if (buildSystem == null || buildSystem.Buildings.Count == 0)
                return;

            List<BuildingData> snapshot = _buildingSnapshotCache;
            snapshot.Clear();
            snapshot.AddRange(buildSystem.Buildings);

            foreach (var building in snapshot)
            {
                float resistanceCoeff = GetResistanceCoefficient(
                    building.Type, building.Material, Params.Environment);

                float zoneMultiplier = building.IsInSafeZone ? 0.5f : 1f;

                float damage = Params.BaseDPS * CurrentIntensity
                    * Params.DifficultyMultiplier * resistanceCoeff
                    * zoneMultiplier * deltaTime;

                if (damage > 0f)
                    buildSystem.DamageBuilding(building.BuildingId, damage);
            }
        }

        /// <summary>
        /// 获取建筑抗性系数。
        /// 引用：灾难系统设计.md §4.3 建筑×材料×环境 抗性矩阵
        /// </summary>
        protected virtual float GetResistanceCoefficient(
            BuildingType buildingType, MaterialType material, ShelterEnvironment env)
        {
            switch (env)
            {
                case ShelterEnvironment.Volcano:
                    if (material == MaterialType.IceBrick) return 0f;
                    if (material == MaterialType.WaterBrick) return 0.3f;
                    if (material == MaterialType.FireBrick || material == MaterialType.LavaBrick) return 1.5f;
                    return 0.6f;

                case ShelterEnvironment.Flood:
                    if (material == MaterialType.LavaBrick) return 0f;
                    if (material == MaterialType.FireBrick) return 0.3f;
                    if (material == MaterialType.WaterBrick || material == MaterialType.IceBrick) return 1.5f;
                    return 0.6f;

                case ShelterEnvironment.Blizzard:
                    if (material == MaterialType.IceBrick) return 0f;
                    if (material == MaterialType.FireBrick || material == MaterialType.LavaBrick) return 0.3f;
                    return 0.6f;

                case ShelterEnvironment.Earthquake:
                    if (material == MaterialType.StoneBrick)
                        return buildingType == BuildingType.ReinforcedTower ? 0f : 0.3f;
                    return 1.5f;

                case ShelterEnvironment.Meteorite:
                    if (material == MaterialType.StoneBrick)
                    {
                        if (buildingType == BuildingType.Shelter) return 0f;
                        return 0.3f;
                    }
                    return 1.0f;

                default:
                    return 1.0f;
            }
        }
    }
}
