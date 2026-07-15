/// ============================================================
/// 文件名: DisasterSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 灾难系统管理器，管理灾难生成、渐进强度和伤害。
/// ============================================================

using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Building;
using DualEnigma.Synthesis;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster
{
    /// <summary>
    /// 灾难系统管理器。继承 Singleton<T>，注册 IDisasterSystem 到 ServiceLocator。
    /// 引用：灾难系统.md §3.2
    /// </summary>
    public class DisasterSystem : Singleton<DisasterSystem>, IDisasterSystem
    {
        /// <summary>当前运行的灾难</summary>
        public DisasterBase CurrentDisaster { get; private set; }

        /// <summary>灾难配置</summary>
        private DisasterConfig _config;

        /// <summary>已运行时间</summary>
        private float _elapsedTime;

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<IDisasterSystem>(this);
            Debug.Log("[DisasterSystem] 灾难系统初始化完成");
        }

        /// <summary>
        /// 启动灾难。
        /// </summary>
        public void StartDisaster(DisasterId disasterId, float difficultyMultiplier, uint seed)
        {
            if (_config == null)
                _config = Resources.Load<DisasterConfig>("DisasterConfig");

            DisasterParams parameters;
            if (disasterId == DisasterId.E3Enhanced && _config != null && _config.E3Enhanced != null)
            {
                parameters = _config.E3Enhanced;
            }
            else if (_config != null)
            {
                parameters = _config.GetDisaster(disasterId);
            }
            else
            {
                parameters = new DisasterParams
                {
                    Id = disasterId,
                    Name = disasterId.ToString(),
                    BaseDPS = 3f,
                    Range = 10f,
                    Duration = 20f,
                };
            }

            parameters.RandomSeed = seed;
            parameters.DifficultyMultiplier = difficultyMultiplier;

            CurrentDisaster = CreateDisaster(disasterId);
            if (CurrentDisaster != null)
            {
                CurrentDisaster.Initialize(parameters);
                CurrentDisaster.OnStart();
                _elapsedTime = 0f;

                EventBus.Instance.Publish(new DisasterStartedEvent
                {
                    disasterId = (int)disasterId
                });

                Debug.Log($"[DisasterSystem] 灾难启动: {disasterId}, DPS={parameters.BaseDPS}, 难度×{difficultyMultiplier}");
            }
        }

        /// <summary>
        /// 停止灾难。
        /// </summary>
        public void StopDisaster()
        {
            if (CurrentDisaster == null) return;

            CurrentDisaster.OnEnd();
            CurrentDisaster = null;

            EventBus.Instance.Publish(new DisasterEndedEvent());

            Debug.Log("[DisasterSystem] 灾难结束");
        }

        /// <summary>
        /// 每帧更新。
        /// </summary>
        public void OnUpdate(float deltaTime)
        {
            if (CurrentDisaster == null || !CurrentDisaster.IsRunning)
                return;

            _elapsedTime += deltaTime;

            if (_elapsedTime >= CurrentDisaster.Params.Duration)
            {
                StopDisaster();
                return;
            }

            CurrentDisaster.OnUpdate(deltaTime, _elapsedTime);
        }

        private DisasterBase CreateDisaster(DisasterId id)
        {
            return new GenericDisaster();
        }
    }

    /// <summary>
    /// 通用灾难实现，用于暂未编写专属逻辑的灾难。
    /// </summary>
    public class GenericDisaster : DisasterBase
    {
        public override void OnStart()
        {
            IsRunning = true;
            Debug.Log($"[GenericDisaster] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);

            var buildSystem = ServiceLocator.Get<IBuildSystem>();
            if (buildSystem == null || buildSystem.Buildings.Count == 0)
                return;

            // 复制列表避免迭代时 DamageBuilding 移除元素导致异常
            List<BuildingData> snapshot = new List<BuildingData>(buildSystem.Buildings);

            foreach (var building in snapshot)
            {
                float resistanceCoeff = GetResistanceCoefficient(
                    building.Type, building.Material, Params.Environment);

                // 建筑区域内 50% 减免
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
        private float GetResistanceCoefficient(
            BuildingType buildingType, MaterialType material, ShelterEnvironment env)
        {
            // 简化抗性矩阵：根据材料类型和环境判断抗性等级
            // ★★★ 免疫(0×) / ★★ 强抗性(0.3×) / ★ 抗性(0.6×) / — 无加成(1.0×) / ✗ 弱点(1.5×)

            switch (env)
            {
                case ShelterEnvironment.Volcano:
                    // 火山环境：冰砖免疫，水砖抗性，火砖/岩浆砖弱点
                    if (material == MaterialType.IceBrick) return 0f;
                    if (material == MaterialType.WaterBrick) return 0.3f;
                    if (material == MaterialType.FireBrick || material == MaterialType.LavaBrick) return 1.5f;
                    return 0.6f; // 石砖

                case ShelterEnvironment.Flood:
                    // 洪水环境：岩浆砖免疫，火砖抗性，水砖/冰砖弱点
                    if (material == MaterialType.LavaBrick) return 0f;
                    if (material == MaterialType.FireBrick) return 0.3f;
                    if (material == MaterialType.WaterBrick || material == MaterialType.IceBrick) return 1.5f;
                    return 0.6f;

                case ShelterEnvironment.Blizzard:
                    // 暴风雪环境：冰砖强化(免疫+HP50%)，火砖/岩浆砖抗性
                    if (material == MaterialType.IceBrick) return 0f;
                    if (material == MaterialType.FireBrick || material == MaterialType.LavaBrick) return 0.3f;
                    return 0.6f;

                case ShelterEnvironment.Earthquake:
                    // 地震环境：石砖抗震，加固塔+石砖免疫
                    if (material == MaterialType.StoneBrick)
                        return buildingType == BuildingType.ReinforcedTower ? 0f : 0.3f;
                    return 1.5f; // 非石砖脆裂

                case ShelterEnvironment.Meteorite:
                    // 陨石环境：石砖抗冲击，避难所+石砖坚固
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

        public override void OnEnd()
        {
            IsRunning = false;
            Debug.Log($"[GenericDisaster] {Params.Name} 结束");
        }
    }
}
