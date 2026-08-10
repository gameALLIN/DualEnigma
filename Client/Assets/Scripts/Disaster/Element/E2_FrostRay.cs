using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;
using DualEnigma.Building;
using DualEnigma.Synthesis;

namespace DualEnigma.Disaster.Element
{
    public class E2_FrostRay : ElementDisaster
    {
        private GameObject _effectObject;
        private float _damageAccumulator;
        private const float SlowMultiplier = 0.5f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("E2_FrostRay_Effect");
            _effectObject.transform.position = Params.Position;
            _effectObject.AddComponent<ParticleSystem>();

            var charSys = _cachedCharacterSystem;
            if (charSys != null)
            {
                var aqua = charSys.GetCharacter(CharacterType.Aqua);
                if (aqua != null) aqua.SetMoveSpeedMultiplier(SlowMultiplier);
                var ignis = charSys.GetCharacter(CharacterType.Ignis);
                if (ignis != null) ignis.SetMoveSpeedMultiplier(SlowMultiplier);
            }

            Debug.Log($"[E2_FrostRay] {Params.Name} 开始 (DPS={Params.BaseDPS})");
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
                var ignis = charSys.GetCharacter(CharacterType.Ignis);
                if (ignis != null) ignis.SetMoveSpeedMultiplier(1f);
            }

            Debug.Log($"[E2_FrostRay] {Params.Name} 结束");
        }

        protected override float GetResistanceCoefficient(
            BuildingType buildingType, MaterialType material, ShelterEnvironment env)
        {
            float baseCoeff = base.GetResistanceCoefficient(buildingType, material, env);
            if (material == MaterialType.FireBrick || material == MaterialType.LavaBrick)
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
