using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster.Physics
{
    public class P1_Meteor : PhysicsDisaster
    {
        private GameObject _effectObject;
        private float _strikeTimer;
        private const float StrikeInterval = 2.5f;
        private System.Random _random;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("P1_Meteor_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _random = new System.Random((int)Params.RandomSeed);
            _strikeTimer = 0f;
            Debug.Log($"[P1_Meteor] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _strikeTimer += deltaTime;
            if (_strikeTimer >= StrikeInterval)
            {
                _strikeTimer = 0f;
                MeteorStrike();
            }
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[P1_Meteor] {Params.Name} 结束");
        }

        private void MeteorStrike()
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            int damage = Mathf.RoundToInt(Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier * 3f);
            if (damage <= 0) return;

            bool hitAqua = _random.NextDouble() < 0.5;
            if (hitAqua)
                shelterSys.DealDamage(CharacterType.Aqua, damage);
            else
                shelterSys.DealDamage(CharacterType.Ignis, damage);

            Debug.Log($"[P1_Meteor] 陨石命中 {(hitAqua ? "Aqua" : "Ignis")} -{damage}HP");
        }
    }
}
