using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster.Physics
{
    public class P3_FallingRocks : PhysicsDisaster
    {
        private GameObject _effectObject;
        private float _dropTimer;
        private const float DropInterval = 1.5f;
        private System.Random _random;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("P3_FallingRocks_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _random = new System.Random((int)Params.RandomSeed);
            _dropTimer = 0f;
            Debug.Log($"[P3_FallingRocks] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _dropTimer += deltaTime;
            if (_dropTimer >= DropInterval)
            {
                _dropTimer = 0f;
                DropRock();
            }
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[P3_FallingRocks] {Params.Name} 结束");
        }

        private void DropRock()
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            int damage = Mathf.RoundToInt(Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier * 1.5f);
            if (damage <= 0) return;

            bool hitAqua = _random.NextDouble() < 0.5;
            if (hitAqua)
                shelterSys.DealDamage(CharacterType.Aqua, damage);
            else
                shelterSys.DealDamage(CharacterType.Ignis, damage);
        }
    }
}
