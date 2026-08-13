using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster.Element
{
    public class E3Enhanced_ThunderStrike : ElementDisaster
    {
        private GameObject _effectObject;
        private float _strikeTimer;
        private const float StrikeInterval = 1.5f;
        private const float DamageMultiplier = 2.0f;
        private System.Random _random;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("E3Enhanced_ThunderStrike_Effect");
            _effectObject.transform.position = Params.Position;
            _effectObject.AddComponent<ParticleSystem>();
            _random = new System.Random((int)Params.RandomSeed);
            _strikeTimer = 0f;
            Debug.Log($"[E3Enhanced_ThunderStrike] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _strikeTimer += deltaTime;
            if (_strikeTimer >= StrikeInterval)
            {
                _strikeTimer = 0f;
                StrikeCharacters();
            }
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[E3Enhanced_ThunderStrike] {Params.Name} 结束");
        }

        private void StrikeCharacters()
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            int damage = Mathf.RoundToInt(Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier * DamageMultiplier);
            if (damage <= 0) return;

            shelterSys.DealDamage(CharacterType.Aqua, damage);
            shelterSys.DealDamage(CharacterType.Ignis, damage);

            Debug.Log($"[E3Enhanced_ThunderStrike] 强化雷击双方 -{damage}HP");
        }
    }
}
