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
    public class E6_LightBeam : ElementDisaster
    {
        private GameObject _effectObject;
        private float _damageAccumulator;
        private const float HighDpsMultiplier = 2.0f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("E6_LightBeam_Effect");
            _effectObject.transform.position = Params.Position;
            _effectObject.AddComponent<ParticleSystem>();
            Debug.Log($"[E6_LightBeam] {Params.Name} 开始 (DPS={Params.BaseDPS})");
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
            Debug.Log($"[E6_LightBeam] {Params.Name} 结束");
        }

        private void DealCharacterDamage(float deltaTime)
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            float dps = Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier * HighDpsMultiplier;
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
