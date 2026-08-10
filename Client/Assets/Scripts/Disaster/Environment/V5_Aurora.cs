using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster.Environment
{
    public class V5_Aurora : EnvironmentDisaster
    {
        private GameObject _effectObject;
        private float _damageAccumulator;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("V5_Aurora_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            Debug.Log($"[V5_Aurora] {Params.Name} 开始 (DPS={Params.BaseDPS})");
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
            Debug.Log($"[V5_Aurora] {Params.Name} 结束");
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
            shelterSys.DealDamage(CharacterType.Ignis, dmg);
        }
    }
}
