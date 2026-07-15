/// ============================================================
/// 文件名: SynthesisSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 合成系统管理器，管理合成台交互和材料产出。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
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
            var config = Resources.Load<SynthesisConfig>("SynthesisConfig");
            if (config != null)
            {
                CurrentRecipes = config.GetRecipes(environment);
            }
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
        /// </summary>
        public SynthesisRecipe? TryStartSynthesis(byte playerId, FragmentType fragmentType, MaterialType desiredOutput)
        {
            foreach (var recipe in CurrentRecipes)
            {
                if (recipe.InputType != fragmentType || recipe.OutputType != desiredOutput)
                    continue;

                int required = _m1ElementDepletion ? recipe.RequiredCount * 2 : recipe.RequiredCount;

                _activeRecipes[playerId] = recipe;
                _synthesisTimers[playerId] = recipe.SynthesisTime;
                Debug.Log($"[SynthesisSystem] 玩家{playerId}开始合成: {recipe.OutputType}, 需{required}个{fragmentType}, {recipe.SynthesisTime}秒");
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
        /// 打断合成（移动或被击中时调用）。
        /// </summary>
        public void InterruptSynthesis(byte playerId)
        {
            _activeRecipes.Remove(playerId);
            _synthesisTimers.Remove(playerId);
            Debug.Log($"[SynthesisSystem] 玩家{playerId}合成被打断，碎片返还");
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
    }
}
