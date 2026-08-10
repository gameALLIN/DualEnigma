using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;
using DualEnigma.Building;
using DualEnigma.Synthesis;

namespace DualEnigma.Disaster.Environment
{
    public class V3_Blizzard : EnvironmentDisaster
    {
        private GameObject _effectObject;
        private float _damageAccumulator;
        private const float SlowMultiplier = 0.5f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("V3_Blizzard_Effect");
            _effectObject.AddComponent<ParticleSystem>();

            var charSys = _cachedCharacterSystem;
            if (charSys != null)
            {
                var aqua = charSys.GetCharacter(CharacterType.Aqua);
                if (aqua != null) aqua.SetMoveSpeedMultiplier(SlowMultiplier);
            }

            Debug.Log($"[V3_Blizzard] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);
            DealCharacterDamage(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);

            var charSys = _cachedCharacterSystem;
            if (charSys != null)
            {
                var aqua = charSys.GetCharacter(CharacterType.Aqua);
                if (aqua != null) aqua.SetMoveSpeedMultiplier(1f);
            }

            Debug.Log($"[V3_Blizzard] {Params.Name} 结束");
        }

        protected override float GetResistanceCoefficient(
            BuildingType buildingType, MaterialType material, ShelterEnvironment env)
        {
            if (material == MaterialType.IceBrick)
                return 0f;
            return base.GetResistanceCoefficient(buildingType, material, env);
        }

        private void DealCharacterDamage(float deltaTime)
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            float dps = Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier;
            _damageAccumulator += dps * deltaTime;
            if (_damageAccumulator < 1f) return;

            int dmg = Mathf.FloorToInt(_damageAccumulator);
            _damageAccumulator -= dmg;
            shelterSys.DealDamage(CharacterType.Aqua, dmg);
        }
    }
}
