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
    public class E1_FireSpray : ElementDisaster
    {
        private GameObject _effectObject;
        private float _damageAccumulator;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("E1_FireSpray_Effect");
            _effectObject.transform.position = Params.Position;
            _effectObject.AddComponent<ParticleSystem>();
            Debug.Log($"[E1_FireSpray] {Params.Name} 开始 (DPS={Params.BaseDPS})");
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
            Debug.Log($"[E1_FireSpray] {Params.Name} 结束");
        }

        protected override float GetResistanceCoefficient(
            BuildingType buildingType, MaterialType material, ShelterEnvironment env)
        {
            float baseCoeff = base.GetResistanceCoefficient(buildingType, material, env);
            if (material == MaterialType.WaterBrick)
                return baseCoeff * 1.5f;
            return baseCoeff;
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

            switch (Params.Environment)
            {
                case ShelterEnvironment.Volcano:
                case ShelterEnvironment.Blizzard:
                    shelterSys.DealDamage(CharacterType.Aqua, dmg);
                    break;
                case ShelterEnvironment.Flood:
                    shelterSys.DealDamage(CharacterType.Ignis, dmg);
                    break;
                default:
                    shelterSys.DealDamage(CharacterType.Aqua, dmg);
                    shelterSys.DealDamage(CharacterType.Ignis, dmg);
                    break;
            }
        }
    }
}
