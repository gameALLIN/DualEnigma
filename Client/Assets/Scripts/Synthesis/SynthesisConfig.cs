/// ============================================================
/// 文件名: SynthesisConfig.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 合成系统配置数据（ScriptableObject）。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Shelter;

namespace DualEnigma.Synthesis
{
    /// <summary>
    /// 合成系统配置。
    /// 引用：合成系统.md §6.1
    /// </summary>
    [CreateAssetMenu(fileName = "SynthesisConfig", menuName = "DualEnigma/SynthesisConfig")]
    public class SynthesisConfig : ScriptableObject
    {
        [Header("火山环境配方")]
        [SerializeField] private List<SynthesisRecipe> _volcanoRecipes;
        [Header("洪水环境配方")]
        [SerializeField] private List<SynthesisRecipe> _floodRecipes;
        [Header("暴风雪环境配方")]
        [SerializeField] private List<SynthesisRecipe> _blizzardRecipes;
        [Header("地震环境配方")]
        [SerializeField] private List<SynthesisRecipe> _earthquakeRecipes;
        [Header("陨石环境配方")]
        [SerializeField] private List<SynthesisRecipe> _meteoriteRecipes;

        /// <summary>获取指定环境的合成配方列表</summary>
        public List<SynthesisRecipe> GetRecipes(ShelterEnvironment env)
        {
            switch (env)
            {
                case ShelterEnvironment.Volcano: return _volcanoRecipes;
                case ShelterEnvironment.Flood: return _floodRecipes;
                case ShelterEnvironment.Blizzard: return _blizzardRecipes;
                case ShelterEnvironment.Earthquake: return _earthquakeRecipes;
                case ShelterEnvironment.Meteorite: return _meteoriteRecipes;
                default: return _volcanoRecipes;
            }
        }

        private void Reset()
        {
            _volcanoRecipes = new List<SynthesisRecipe>
            {
                new SynthesisRecipe { InputType = FragmentType.IceCrystal, OutputType = MaterialType.WaterBrick, RequiredCount = 2, SynthesisTime = 1f },
                new SynthesisRecipe { InputType = FragmentType.IceCrystal, OutputType = MaterialType.IceBrick, RequiredCount = 3, SynthesisTime = 1.5f },
                new SynthesisRecipe { InputType = FragmentType.Rock, OutputType = MaterialType.StoneBrick, RequiredCount = 2, SynthesisTime = 2f },
                new SynthesisRecipe { InputType = FragmentType.Lava, OutputType = MaterialType.IceBrick, RequiredCount = 3, SynthesisTime = 1.5f },
            };
            _floodRecipes = new List<SynthesisRecipe>
            {
                new SynthesisRecipe { InputType = FragmentType.IceCrystal, OutputType = MaterialType.StoneBrick, RequiredCount = 2, SynthesisTime = 2f },
                new SynthesisRecipe { InputType = FragmentType.Lava, OutputType = MaterialType.FireBrick, RequiredCount = 2, SynthesisTime = 1f },
                new SynthesisRecipe { InputType = FragmentType.Lava, OutputType = MaterialType.LavaBrick, RequiredCount = 3, SynthesisTime = 1.5f },
                new SynthesisRecipe { InputType = FragmentType.Rock, OutputType = MaterialType.LavaBrick, RequiredCount = 3, SynthesisTime = 1.5f },
            };
            _blizzardRecipes = new List<SynthesisRecipe>
            {
                new SynthesisRecipe { InputType = FragmentType.IceCrystal, OutputType = MaterialType.IceBrick, RequiredCount = 3, SynthesisTime = 1.5f },
                new SynthesisRecipe { InputType = FragmentType.Lava, OutputType = MaterialType.FireBrick, RequiredCount = 2, SynthesisTime = 1f },
                new SynthesisRecipe { InputType = FragmentType.Rock, OutputType = MaterialType.LavaBrick, RequiredCount = 3, SynthesisTime = 1.5f },
            };
            _earthquakeRecipes = new List<SynthesisRecipe>
            {
                new SynthesisRecipe { InputType = FragmentType.IceCrystal, OutputType = MaterialType.StoneBrick, RequiredCount = 2, SynthesisTime = 2f },
                new SynthesisRecipe { InputType = FragmentType.Lava, OutputType = MaterialType.StoneBrick, RequiredCount = 2, SynthesisTime = 2f },
                new SynthesisRecipe { InputType = FragmentType.Rock, OutputType = MaterialType.StoneBrick, RequiredCount = 2, SynthesisTime = 2f },
            };
            _meteoriteRecipes = new List<SynthesisRecipe>
            {
                new SynthesisRecipe { InputType = FragmentType.IceCrystal, OutputType = MaterialType.StoneBrick, RequiredCount = 2, SynthesisTime = 2f },
                new SynthesisRecipe { InputType = FragmentType.Lava, OutputType = MaterialType.StoneBrick, RequiredCount = 2, SynthesisTime = 2f },
                new SynthesisRecipe { InputType = FragmentType.Rock, OutputType = MaterialType.StoneBrick, RequiredCount = 2, SynthesisTime = 2f },
            };
        }
    }
}
