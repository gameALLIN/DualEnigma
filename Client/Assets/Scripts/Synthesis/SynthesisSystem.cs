/// ============================================================
/// 文件名: SynthesisSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 合成系统管理器，管理合成台交互和材料产出。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Data;
using DualEnigma.Character;
using DualEnigma.Fragment;
using DualEnigma.Shelter;

namespace DualEnigma.Synthesis
{
    /// <summary>
    /// 合成系统管理器。继承 Singleton<T>，注册 ISynthesisSystem 到 ServiceLocator。
    /// 引用：合成系统.md §3.1
    /// </summary>
    public class SynthesisSystem : Singleton<SynthesisSystem>, ISynthesisSystem
    {
        /// <summary>当前庇护环境对应的合成表</summary>
        public List<SynthesisRecipe> CurrentRecipes { get; private set; } = new List<SynthesisRecipe>();

        /// <summary>合成中的玩家计时器</summary>
        private readonly Dictionary<byte, float> _synthesisTimers = new Dictionary<byte, float>();
        /// <summary>合成中的玩家配方</summary>
        private readonly Dictionary<byte, SynthesisRecipe> _activeRecipes = new Dictionary<byte, SynthesisRecipe>();
        /// <summary>合成开始时消耗的碎片记录（用于打断返还）</summary>
        private readonly Dictionary<byte, ConsumedFragmentRecord> _consumedRecords = new Dictionary<byte, ConsumedFragmentRecord>();

        /// <summary>消耗碎片记录：存储合成开始时消耗的碎片信息</summary>
        private struct ConsumedFragmentRecord
        {
            /// <summary>消耗的碎片类型</summary>
            public FragmentType FragmentType;
            /// <summary>消耗的碎片数量</summary>
            public int Count;
            /// <summary>消耗的碎片ID列表（用于精确返还）</summary>
            public List<int> FragmentIds;
        }

        /// <summary>M1元素枯竭：碎片需求翻倍</summary>
        private bool _m1ElementDepletion;

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<ISynthesisSystem>(this);
            Debug.Log("[SynthesisSystem] 合成系统初始化完成");
        }

        /// <summary>
        /// 设置当前庇护环境。
        /// </summary>
        public void SetEnvironment(ShelterEnvironment environment)
        {
            var config = DataManager.Instance.LoadConfig<SynthesisConfig>("SynthesisConfig");
            if (config == null)
            {
                Debug.LogError("[SynthesisSystem] SynthesisConfig 未找到");
                return;
            }
            CurrentRecipes = config.GetRecipes(environment);
            Debug.Log($"[SynthesisSystem] 合成表切换 → {environment}, {CurrentRecipes.Count}条配方");
        }

        /// <summary>
        /// 获取指定碎片类型可合成的所有配方（供UI选择）。
        /// </summary>
        public List<SynthesisRecipe> GetAvailableRecipes(FragmentType fragmentType)
        {
            List<SynthesisRecipe> matches = new List<SynthesisRecipe>();
            foreach (var recipe in CurrentRecipes)
            {
                if (recipe.InputType == fragmentType)
                    matches.Add(recipe);
            }
            return matches;
        }

        /// <summary>
        /// 尝试开始合成（指定输出材料类型）。
        /// 引用：合成系统.md §4.2 合成流程
        /// </summary>
        public SynthesisRecipe? TryStartSynthesis(byte playerId, FragmentType fragmentType, MaterialType desiredOutput)
        {
            foreach (var recipe in CurrentRecipes)
            {
                if (recipe.InputType != fragmentType || recipe.OutputType != desiredOutput)
                    continue;

                int required = _m1ElementDepletion ? recipe.RequiredCount * 2 : recipe.RequiredCount;

                // 获取角色控制器
                var characterSystem = ServiceLocator.Get<ICharacterSystem>();
                if (characterSystem == null)
                {
                    Debug.LogWarning("[SynthesisSystem] CharacterSystem 未注册，无法验证碎片");
                    return null;
                }

                CharacterController character = characterSystem.GetCharacter((CharacterType)playerId);
                if (character == null || character.Stats == null)
                {
                    Debug.LogWarning($"[SynthesisSystem] 找不到玩家{playerId}的角色实例");
                    return null;
                }

                // 获取碎片系统以查询碎片类型
                var fragmentSystem = ServiceLocator.Get<IFragmentSystem>();
                if (fragmentSystem == null)
                {
                    Debug.LogWarning("[SynthesisSystem] FragmentSystem 未注册，无法验证碎片");
                    return null;
                }

                // 检查角色携带碎片中是否有足够数量的对应 FragmentType 碎片
                List<int> matchedIds = new List<int>();
                foreach (int fragmentId in character.Stats.CarriedFragmentIds)
                {
                    if (fragmentSystem.TryGetFragmentType(fragmentId, out FragmentType type) && type == fragmentType)
                    {
                        matchedIds.Add(fragmentId);
                        if (matchedIds.Count >= required)
                            break;
                    }
                }

                if (matchedIds.Count < required)
                {
                    Debug.Log($"[SynthesisSystem] 玩家{playerId}碎片不足: 需要{required}个{fragmentType}, 仅有{matchedIds.Count}个");
                    return null;
                }

                // 满足条件，消耗碎片
                foreach (int fragmentId in matchedIds)
                {
                    character.RemoveFragment(fragmentId);
                }

                // 记录消耗的碎片信息（用于打断返还）
                _consumedRecords[playerId] = new ConsumedFragmentRecord
                {
                    FragmentType = fragmentType,
                    Count = required,
                    FragmentIds = matchedIds
                };

                // 启动合成计时
                _activeRecipes[playerId] = recipe;
                _synthesisTimers[playerId] = recipe.SynthesisTime;
                Debug.Log($"[SynthesisSystem] 玩家{playerId}开始合成: {recipe.OutputType}, 消耗{required}个{fragmentType}, {recipe.SynthesisTime}秒");
                return recipe;
            }

            return null;
        }

        /// <summary>
        /// 合成进度更新。
        /// </summary>
        public float GetSynthesisProgress(byte playerId)
        {
            if (!_activeRecipes.ContainsKey(playerId) || !_synthesisTimers.ContainsKey(playerId))
                return 0f;

            float totalTime = _activeRecipes[playerId].SynthesisTime;
            if (totalTime <= 0f) return 1f;

            float remaining = _synthesisTimers[playerId];
            return 1f - (remaining / totalTime);
        }

        /// <summary>
        /// 打断合成（移动或被击中时调用），返还已消耗的碎片。
        /// 引用：合成系统.md §4.2 打断规则
        /// </summary>
        public void InterruptSynthesis(byte playerId)
        {
            // 返还已消耗的碎片
            if (_consumedRecords.TryGetValue(playerId, out ConsumedFragmentRecord record))
            {
                var characterSystem = ServiceLocator.Get<ICharacterSystem>();
                if (characterSystem != null)
                {
                    CharacterController character = characterSystem.GetCharacter((CharacterType)playerId);
                    if (character != null)
                    {
                        int returned = 0;
                        foreach (int fragmentId in record.FragmentIds)
                        {
                            if (character.AddFragment(fragmentId))
                                returned++;
                            else
                                Debug.LogWarning($"[SynthesisSystem] 玩家{playerId}碎片背包已满，碎片{fragmentId}返还失败");
                        }
                        Debug.Log($"[SynthesisSystem] 玩家{playerId}合成被打断，返还{returned}/{record.Count}个{record.FragmentType}碎片");
                    }
                }
                _consumedRecords.Remove(playerId);
            }

            _activeRecipes.Remove(playerId);
            _synthesisTimers.Remove(playerId);
        }

        /// <summary>
        /// 每帧更新合成计时（由外部调用）。
        /// </summary>
        public void OnUpdate(float deltaTime)
        {
            if (_synthesisTimers.Count == 0) return;

            List<byte> completed = new List<byte>();

            foreach (var kvp in _synthesisTimers)
            {
                float remaining = kvp.Value - deltaTime;
                _synthesisTimers[kvp.Key] = remaining;

                if (remaining <= 0f)
                    completed.Add(kvp.Key);
            }

            foreach (byte playerId in completed)
            {
                SynthesisRecipe recipe = _activeRecipes[playerId];
                _synthesisTimers.Remove(playerId);
                _activeRecipes.Remove(playerId);
                _consumedRecords.Remove(playerId);

                EventBus.Instance.Publish(new MaterialProducedEvent
                {
                    playerId = playerId,
                    materialType = (int)recipe.OutputType,
                    count = 1
                });

                Debug.Log($"[SynthesisSystem] 玩家{playerId}合成完成: {recipe.OutputType}");
            }
        }

        /// <summary>设置 M1 元素枯竭状态</summary>
        public void SetM1ElementDepletion(bool enabled)
        {
            _m1ElementDepletion = enabled;
        }

        /// <summary>
        /// 合成台可用回调（由 SynthesisStation.Release 在队列中有等待者时调用）。
        /// 引用：合成系统.md §4.1 队列规则
        /// </summary>
        /// <param name="station">可用的合成台</param>
        /// <param name="playerId">被分配到的玩家ID</param>
        public void OnStationAvailable(SynthesisStation station, byte playerId)
        {
            Debug.Log($"[SynthesisSystem] 合成台{station.StationId}可用，通知玩家{playerId}");
            // 可扩展：发布事件通知 UI 或角色控制器
        }
    }
}
