using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using DualEnigma.Character;
using DualEnigma.Fragment;
using DualEnigma.Disaster;
using DualEnigma.Skill;
using DualEnigma.Talent;
using DualEnigma.Synthesis;
using DualEnigma.Building;
using DualEnigma.Shelter;

namespace DualEnigma.Editor
{
    public static class ConfigAssetGenerator
    {
        private const string OUTPUT_DIR = "Assets/AssetPackage/Data";

        [MenuItem("DualEnigma/生成配置资产")]
        public static void Generate()
        {
            EnsureDirectory(OUTPUT_DIR);

            GenerateCharacterConfig();
            GenerateFragmentConfig();
            GenerateDisasterConfig();
            GenerateBuildingConfig();
            GenerateShelterConfig();
            GenerateSynthesisConfig();
            GenerateSkillConfig();
            GenerateTalentConfig();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ConfigAssetGenerator] 全部配置资产生成完毕");
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = "Assets";
                string folder = "AssetPackage";
                if (!AssetDatabase.IsValidFolder("Assets/AssetPackage"))
                    AssetDatabase.CreateFolder(parent, folder);
                if (!AssetDatabase.IsValidFolder(path))
                    AssetDatabase.CreateFolder("Assets/AssetPackage", "Data");
            }
        }

        private static T CreateOrLoad<T>(string fullPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, fullPath);
            }
            return asset;
        }

        private static void GenerateCharacterConfig()
        {
            string path = $"{OUTPUT_DIR}/CharacterConfig.asset";
            var config = CreateOrLoad<CharacterConfig>(path);

            config.AquaStats = new CharacterStats
            {
                Type = CharacterType.Aqua,
                MaxHP = 100,
                CurrentHP = 100,
                MoveSpeed = 4f,
                JumpHeight = 2f,
                CanDoubleJump = false,
                CarryLimit = 3
            };
            config.IgnisStats = new CharacterStats
            {
                Type = CharacterType.Ignis,
                MaxHP = 100,
                CurrentHP = 100,
                MoveSpeed = 4f,
                JumpHeight = 2f,
                CanDoubleJump = true,
                CarryLimit = 3
            };

            EditorUtility.SetDirty(config);
            Debug.Log("[ConfigAssetGenerator] CharacterConfig 生成完毕");
        }

        private static void GenerateFragmentConfig()
        {
            string path = $"{OUTPUT_DIR}/FragmentConfig.asset";
            var config = CreateOrLoad<FragmentConfig>(path);
            var so = new SerializedObject(config);

            so.FindProperty("_previewCount").intValue = 5;
            so.FindProperty("_collectPhaseCount").intValue = 25;
            so.FindProperty("_dropRangeMin").floatValue = -10f;
            so.FindProperty("_dropRangeMax").floatValue = 10f;

            var densityProp = so.FindProperty("_densityFactors");
            densityProp.arraySize = 3;
            densityProp.GetArrayElementAtIndex(0).floatValue = 1.0f;
            densityProp.GetArrayElementAtIndex(1).floatValue = 0.85f;
            densityProp.GetArrayElementAtIndex(2).floatValue = 0.7f;

            var lifetimeProp = so.FindProperty("_lifetimes");
            lifetimeProp.arraySize = 3;
            lifetimeProp.GetArrayElementAtIndex(0).floatValue = 3.5f;
            lifetimeProp.GetArrayElementAtIndex(1).floatValue = 3.0f;
            lifetimeProp.GetArrayElementAtIndex(2).floatValue = 2.5f;

            var typeProp = so.FindProperty("_typeProbabilities");
            typeProp.arraySize = 3;
            typeProp.GetArrayElementAtIndex(0).FindPropertyRelative("type").enumValueIndex = (int)FragmentType.IceCrystal;
            typeProp.GetArrayElementAtIndex(0).FindPropertyRelative("probability").floatValue = 0.55f;
            typeProp.GetArrayElementAtIndex(1).FindPropertyRelative("type").enumValueIndex = (int)FragmentType.Lava;
            typeProp.GetArrayElementAtIndex(1).FindPropertyRelative("probability").floatValue = 0.30f;
            typeProp.GetArrayElementAtIndex(2).FindPropertyRelative("type").enumValueIndex = (int)FragmentType.Rock;
            typeProp.GetArrayElementAtIndex(2).FindPropertyRelative("probability").floatValue = 0.15f;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ConfigAssetGenerator] FragmentConfig 生成完毕");
        }

        private static void GenerateDisasterConfig()
        {
            string path = $"{OUTPUT_DIR}/DisasterConfig.asset";
            var config = CreateOrLoad<DisasterConfig>(path);
            var so = new SerializedObject(config);

            var curveProp = so.FindProperty("_intensityCurve");
            curveProp.arraySize = 4;
            curveProp.GetArrayElementAtIndex(0).floatValue = 0.3f;
            curveProp.GetArrayElementAtIndex(1).floatValue = 0.6f;
            curveProp.GetArrayElementAtIndex(2).floatValue = 1.0f;
            curveProp.GetArrayElementAtIndex(3).floatValue = 0.8f;

            var disastersProp = so.FindProperty("_disasters");
            var ids = System.Enum.GetValues(typeof(DisasterId));
            int count = 0;
            foreach (DisasterId id in ids)
            {
                if (id == DisasterId.E3Enhanced) continue;
                count++;
            }
            disastersProp.arraySize = count;

            int idx = 0;
            foreach (DisasterId id in ids)
            {
                if (id == DisasterId.E3Enhanced) continue;
                var elem = disastersProp.GetArrayElementAtIndex(idx);
                elem.FindPropertyRelative("Id").enumValueIndex = (int)id;
                elem.FindPropertyRelative("Name").stringValue = id.ToString();
                int cat = (int)id / 100;
                elem.FindPropertyRelative("Category").enumValueIndex = cat;
                elem.FindPropertyRelative("Environment").enumValueIndex = GetEnvironmentForCategory(cat);
                elem.FindPropertyRelative("BaseDPS").floatValue = GetBaseDPSForCategory(cat);
                elem.FindPropertyRelative("Range").floatValue = 5f;
                elem.FindPropertyRelative("Duration").floatValue = 20f;
                elem.FindPropertyRelative("RandomSeed").uintValue = (uint)idx + 1;
                elem.FindPropertyRelative("DifficultyMultiplier").floatValue = 1f;
                idx++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ConfigAssetGenerator] DisasterConfig 生成完毕");
        }

        private static int GetEnvironmentForCategory(int category)
        {
            switch (category)
            {
                case 0: return (int)ShelterEnvironment.Volcano;
                case 1: return (int)ShelterEnvironment.Flood;
                case 2: return (int)ShelterEnvironment.Blizzard;
                case 3: return (int)ShelterEnvironment.Earthquake;
                case 4: return (int)ShelterEnvironment.Meteorite;
                case 5: return (int)ShelterEnvironment.Volcano;
                default: return 0;
            }
        }

        private static float GetBaseDPSForCategory(int category)
        {
            switch (category)
            {
                case 0: return 5f;
                case 1: return 5f;
                case 2: return 3f;
                case 3: return 3f;
                case 4: return 8f;
                case 5: return 4f;
                default: return 5f;
            }
        }

        private static void GenerateBuildingConfig()
        {
            string path = $"{OUTPUT_DIR}/BuildingConfig.asset";
            var config = CreateOrLoad<BuildingConfig>(path);
            var so = new SerializedObject(config);

            so.FindProperty("_gridWidth").intValue = 15;
            so.FindProperty("_gridHeight").intValue = 8;
            so.FindProperty("_placeTime").floatValue = 0.5f;
            so.FindProperty("_repairTime").floatValue = 1f;
            so.FindProperty("_demolishTime").floatValue = 1f;
            so.FindProperty("_defaultSafeZoneRadius").floatValue = 5f;

            var hpProp = so.FindProperty("_buildingHP");
            hpProp.arraySize = 5;
            hpProp.GetArrayElementAtIndex(0).floatValue = 50f;
            hpProp.GetArrayElementAtIndex(1).floatValue = 50f;
            hpProp.GetArrayElementAtIndex(2).floatValue = 60f;
            hpProp.GetArrayElementAtIndex(3).floatValue = 40f;
            hpProp.GetArrayElementAtIndex(4).floatValue = 35f;

            var facingProp = so.FindProperty("_hasFacing");
            facingProp.arraySize = 5;
            facingProp.GetArrayElementAtIndex(0).boolValue = true;
            facingProp.GetArrayElementAtIndex(1).boolValue = true;
            facingProp.GetArrayElementAtIndex(2).boolValue = false;
            facingProp.GetArrayElementAtIndex(3).boolValue = false;
            facingProp.GetArrayElementAtIndex(4).boolValue = true;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ConfigAssetGenerator] BuildingConfig 生成完毕");
        }

        private static void GenerateShelterConfig()
        {
            string path = $"{OUTPUT_DIR}/ShelterConfig.asset";
            var config = CreateOrLoad<ShelterConfig>(path);
            var so = new SerializedObject(config);

            so.FindProperty("_params.MaxEnergy").floatValue = 100f;
            so.FindProperty("_params.RecoveryRate").floatValue = 20f;
            so.FindProperty("_params.ConsumptionRate").floatValue = 33f;
            so.FindProperty("_params.ShelterDistance").floatValue = 3f;
            so.FindProperty("_params.FragmentCollectDistance").floatValue = 5f;
            so.FindProperty("_params.FragmentCollectConsumptionRate").floatValue = 25f;
            so.FindProperty("_params.DamageMultiplier").floatValue = 1f;
            so.FindProperty("_params.BufferTime").floatValue = 3f;

            var envRates = so.FindProperty("_environmentDamageRates");
            envRates.arraySize = 5;
            envRates.GetArrayElementAtIndex(0).floatValue = 3f;
            envRates.GetArrayElementAtIndex(1).floatValue = 3f;
            envRates.GetArrayElementAtIndex(2).floatValue = 2f;
            envRates.GetArrayElementAtIndex(3).floatValue = 3f;
            envRates.GetArrayElementAtIndex(4).floatValue = 0f;

            so.FindProperty("_dyingProtectThreshold").floatValue = 30f;
            so.FindProperty("_dyingProtectReduction").floatValue = 0.3f;
            so.FindProperty("_chapterRestoreHP").intValue = 15;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ConfigAssetGenerator] ShelterConfig 生成完毕");
        }

        private static void GenerateSynthesisConfig()
        {
            string path = $"{OUTPUT_DIR}/SynthesisConfig.asset";
            var config = CreateOrLoad<SynthesisConfig>(path);
            var so = new SerializedObject(config);

            PopulateRecipes(so.FindProperty("_volcanoRecipes"), FragmentType.Lava, MaterialType.FireBrick);
            PopulateRecipes(so.FindProperty("_floodRecipes"), FragmentType.IceCrystal, MaterialType.WaterBrick);
            PopulateRecipes(so.FindProperty("_blizzardRecipes"), FragmentType.IceCrystal, MaterialType.IceBrick);
            PopulateRecipes(so.FindProperty("_earthquakeRecipes"), FragmentType.Rock, MaterialType.StoneBrick);
            PopulateRecipes(so.FindProperty("_meteoriteRecipes"), FragmentType.Rock, MaterialType.LavaBrick);

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ConfigAssetGenerator] SynthesisConfig 生成完毕");
        }

        private static void PopulateRecipes(SerializedProperty listProp, FragmentType inputType, MaterialType outputType)
        {
            listProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var elem = listProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("InputType").enumValueIndex = (int)inputType;
                elem.FindPropertyRelative("OutputType").enumValueIndex = (int)outputType;
                elem.FindPropertyRelative("RequiredCount").intValue = i + 1;
                elem.FindPropertyRelative("SynthesisTime").floatValue = (i + 1) * 2f;
            }
        }

        private static void GenerateSkillConfig()
        {
            string path = $"{OUTPUT_DIR}/SkillConfig.asset";
            var config = CreateOrLoad<SkillConfig>(path);
            var so = new SerializedObject(config);

            var weights = so.FindProperty("_drawWeights");
            weights.arraySize = 3;
            weights.GetArrayElementAtIndex(0).floatValue = 0.5f;
            weights.GetArrayElementAtIndex(1).floatValue = 0.35f;
            weights.GetArrayElementAtIndex(2).floatValue = 0.15f;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ConfigAssetGenerator] SkillConfig 生成完毕");
        }

        private static void GenerateTalentConfig()
        {
            string path = $"{OUTPUT_DIR}/TalentConfig.asset";
            var config = CreateOrLoad<TalentConfig>(path);
            var so = new SerializedObject(config);

            var ch1 = so.FindProperty("_chapter1Rates");
            ch1.arraySize = 3;
            ch1.GetArrayElementAtIndex(0).floatValue = 0.75f;
            ch1.GetArrayElementAtIndex(1).floatValue = 0.20f;
            ch1.GetArrayElementAtIndex(2).floatValue = 0.05f;

            var ch2 = so.FindProperty("_chapter2Rates");
            ch2.arraySize = 3;
            ch2.GetArrayElementAtIndex(0).floatValue = 0.55f;
            ch2.GetArrayElementAtIndex(1).floatValue = 0.35f;
            ch2.GetArrayElementAtIndex(2).floatValue = 0.10f;

            var ch3 = so.FindProperty("_chapter3Rates");
            ch3.arraySize = 3;
            ch3.GetArrayElementAtIndex(0).floatValue = 0.40f;
            ch3.GetArrayElementAtIndex(1).floatValue = 0.40f;
            ch3.GetArrayElementAtIndex(2).floatValue = 0.20f;

            so.FindProperty("_rarityBoostThreshold").intValue = 3;
            so.FindProperty("_minFirstAidAppearances").intValue = 2;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ConfigAssetGenerator] TalentConfig 生成完毕");
        }
    }
}
