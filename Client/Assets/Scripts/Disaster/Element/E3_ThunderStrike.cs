using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster.Element
{
    public class E3_ThunderStrike : ElementDisaster
    {
        private GameObject _effectObject;
        private float _strikeTimer;
        private const float StrikeInterval = 3f;
        private System.Random _random;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("E3_ThunderStrike_Effect");
            _effectObject.transform.position = Params.Position;
            _effectObject.AddComponent<ParticleSystem>();
            _random = new System.Random((int)Params.RandomSeed);
            _strikeTimer = 0f;
            Debug.Log($"[E3_ThunderStrike] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _strikeTimer += deltaTime;
            if (_strikeTimer >= StrikeInterval)
            {
                _strikeTimer = 0f;
                StrikeCharacter();
            }
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[E3_ThunderStrike] {Params.Name} 结束");
        }

        private void StrikeCharacter()
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            int damage = Mathf.RoundToInt(Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier * 2f);
            if (damage <= 0) return;

            bool hitAqua = _random.NextDouble() < 0.5;
            if (hitAqua)
                shelterSys.DealDamage(CharacterType.Aqua, damage);
            else
                shelterSys.DealDamage(CharacterType.Ignis, damage);

            Debug.Log($"[E3_ThunderStrike] 雷击命中 {(hitAqua ? "Aqua" : "Ignis")} -{damage}HP");
        }
    }
}
