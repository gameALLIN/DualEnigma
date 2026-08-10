/// ============================================================
/// 文件名: DisasterSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 灾难系统管理器，管理灾难生成、渐进强度和伤害。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Data;
using DualEnigma.Building;
using DualEnigma.Synthesis;
using DualEnigma.Shelter;
using DualEnigma.Disaster.Element;
using DualEnigma.Disaster.Environment;
using DualEnigma.Disaster.TimeSpace;
using DualEnigma.Disaster.Perception;
using DualEnigma.Disaster.Physics;
using DualEnigma.Disaster.Mechanism;

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
            _config = DataManager.Instance.LoadConfig<DisasterConfig>("DisasterConfig");
            Debug.Log("[DisasterSystem] 灾难系统初始化完成");
        }

        /// <summary>
        /// 启动灾难。
        /// </summary>
        public void StartDisaster(DisasterId disasterId, float difficultyMultiplier, uint seed)
        {
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

            if (parameters == null)
            {
                Debug.LogError($"[DisasterSystem] 灾难配置未找到: {disasterId}");
                return;
            }

            DisasterParams paramsClone = new DisasterParams
            {
                Id = parameters.Id,
                Name = parameters.Name,
                Category = parameters.Category,
                Environment = parameters.Environment,
                BaseDPS = parameters.BaseDPS,
                Range = parameters.Range,
                Duration = parameters.Duration,
                RandomSeed = seed,
                DifficultyMultiplier = difficultyMultiplier,
                Position = parameters.Position
            };

            CurrentDisaster = CreateDisaster(disasterId, paramsClone);
            if (CurrentDisaster != null)
            {
                CurrentDisaster.OnStart();
                _elapsedTime = 0f;

                EventBus.Instance.Publish(new DisasterStartedEvent
                {
                    disasterId = (int)disasterId
                });

                Debug.Log($"[DisasterSystem] 灾难启动: {disasterId}, DPS={paramsClone.BaseDPS}, 难度×{difficultyMultiplier}");
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

            CurrentDisaster.Tick(deltaTime, _elapsedTime);
        }

        /// <summary>
        /// 获取当前灾难的实际位置（世界坐标）。
        /// 若无运行中的灾难，返回 Vector2.zero。
        /// </summary>
        public Vector2 GetDisasterPosition()
        {
            if (CurrentDisaster != null && CurrentDisaster.Params != null)
                return CurrentDisaster.Params.Position;
            return Vector2.zero;
        }

        private DisasterBase CreateDisaster(DisasterId id, DisasterParams parameters)
        {
            if (CurrentDisaster != null && CurrentDisaster.IsRunning)
            {
                CurrentDisaster.OnEnd();
            }

            DisasterBase disaster;
            int category = (int)id / 100;

            switch (category)
            {
                case 0:
                    disaster = id switch
                    {
                        DisasterId.E1 => new E1_FireSpray(),
                        DisasterId.E2 => new E2_FrostRay(),
                        DisasterId.E3 => new E3_ThunderStrike(),
                        DisasterId.E3Enhanced => new E3Enhanced_ThunderStrike(),
                        DisasterId.E4 => new E4_PoisonFog(),
                        DisasterId.E5 => new E5_WindBlade(),
                        DisasterId.E6 => new E6_LightBeam(),
                        DisasterId.E7 => new E7_Shadow(),
                        DisasterId.E8 => new E8_ElementStorm(),
                        _ => new ElementDisaster(),
                    };
                    break;
                case 1:
                    disaster = id switch
                    {
                        DisasterId.V1 => new V1_VolcanoEruption(),
                        DisasterId.V2 => new V2_Flood(),
                        DisasterId.V3 => new V3_Blizzard(),
                        DisasterId.V4 => new V4_Sandstorm(),
                        DisasterId.V5 => new V5_Aurora(),
                        DisasterId.V6 => new V6_AcidRain(),
                        _ => new EnvironmentDisaster(),
                    };
                    break;
                case 2:
                    disaster = id switch
                    {
                        DisasterId.T1 => new T1_TimeSlow(),
                        DisasterId.T2 => new T2_SpaceWarp(),
                        DisasterId.T3 => new T3_GravityAnomaly(),
                        DisasterId.T4 => new T4_TimeRift(),
                        DisasterId.T5 => new T5_TimeAccelerate(),
                        _ => new TimeSpaceDisaster(),
                    };
                    break;
                case 3:
                    disaster = id switch
                    {
                        DisasterId.S1 => new S1_Fog(),
                        DisasterId.S2 => new S2_Illusion(),
                        DisasterId.S3 => new S3_Deafness(),
                        DisasterId.S4 => new S4_Delusion(),
                        DisasterId.S5 => new S5_PerceptionTwist(),
                        _ => new PerceptionDisaster(),
                    };
                    break;
                case 4:
                    disaster = id switch
                    {
                        DisasterId.P1 => new P1_Meteor(),
                        DisasterId.P2 => new P2_Earthquake(),
                        DisasterId.P3 => new P3_FallingRocks(),
                        DisasterId.P4 => new P4_Tornado(),
                        DisasterId.P5 => new P5_Tsunami(),
                        _ => new PhysicsDisaster(),
                    };
                    break;
                case 5:
                    disaster = id switch
                    {
                        DisasterId.M1 => new M1_BuildingCorrosion(),
                        DisasterId.M2 => new M2_MaterialMutation(),
                        DisasterId.M3 => new M3_SynthesisInterference(),
                        DisasterId.M4 => new M4_SkillSeal(),
                        DisasterId.M5 => new M5_ShelterWeaken(),
                        DisasterId.M6 => new M6_Apocalypse(),
                        _ => new MechanismDisaster(),
                    };
                    break;
                default:
                    disaster = new GenericDisaster();
                    break;
            }

            DisasterParams clonedParams = CloneParams(parameters);
            disaster.Initialize(clonedParams);
            return disaster;
        }

        private DisasterParams CloneParams(DisasterParams original)
        {
            return new DisasterParams
            {
                Id = original.Id,
                Name = original.Name,
                Category = original.Category,
                Environment = original.Environment,
                BaseDPS = original.BaseDPS,
                Range = original.Range,
                Duration = original.Duration,
                RandomSeed = original.RandomSeed,
                DifficultyMultiplier = original.DifficultyMultiplier,
                Position = original.Position
            };
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
            ApplyDamageToBuildings(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            Debug.Log($"[GenericDisaster] {Params.Name} 结束");
        }
    }
}
