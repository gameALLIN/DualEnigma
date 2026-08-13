using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;
using DualEnigma.Building;
using DualEnigma.Synthesis;

namespace DualEnigma.Disaster.Element
{
    public class E4_PoisonFog : ElementDisaster
    {
        private GameObject _effectObject;
        private float _damageAccumulator;
        private const float CharacterDpsMultiplier = 0.5f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("E4_PoisonFog_Effect");
            _effectObject.transform.position = Params.Position;
            _effectObject.AddComponent<ParticleSystem>();
            Debug.Log($"[E4_PoisonFog] {Params.Name} 开始 (DPS={Params.BaseDPS})");
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
            Debug.Log($"[E4_PoisonFog] {Params.Name} 结束");
        }

        protected override float GetResistanceCoefficient(
            BuildingType buildingType, MaterialType material, ShelterEnvironment env)
        {
            float baseCoeff = base.GetResistanceCoefficient(buildingType, material, env);
            return baseCoeff * CharacterDpsMultiplier;
        }

        private void DealCharacterDamage(float deltaTime)
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            float dps = Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier * CharacterDpsMultiplier;
            _damageAccumulator += dps * deltaTime;
            if (_damageAccumulator < 1f) return;

            int dmg = Mathf.FloorToInt(_damageAccumulator);
            _damageAccumulator -= dmg;

            shelterSys.DealDamage(CharacterType.Aqua, dmg);
            shelterSys.DealDamage(CharacterType.Ignis, dmg);
        }
    }
}
