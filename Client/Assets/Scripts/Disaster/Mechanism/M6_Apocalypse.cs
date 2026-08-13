using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster.Mechanism
{
    public class M6_Apocalypse : MechanismDisaster
    {
        private GameObject _effectObject;
        private float _damageAccumulator;
        private float _shockwaveTimer;
        private const float ShockwaveInterval = 3f;
        private const float SlowMultiplier = 0.7f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("M6_Apocalypse_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _shockwaveTimer = 0f;

            var charSys = _cachedCharacterSystem;
            if (charSys != null)
            {
                var aqua = charSys.GetCharacter(CharacterType.Aqua);
                if (aqua != null) aqua.SetMoveSpeedMultiplier(SlowMultiplier);
                var ignis = charSys.GetCharacter(CharacterType.Ignis);
                if (ignis != null) ignis.SetMoveSpeedMultiplier(SlowMultiplier);
            }

            var shelterSys = _cachedShelterSystem;
            if (shelterSys != null)
            {
                shelterSys.ModifyParams(new ShelterParams
                {
                    RecoveryRate = 10f,
                    ConsumptionRate = 50f,
                });
            }

            Debug.Log($"[M6_Apocalypse] {Params.Name} 开始 (全属性灾难)");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);
            DealCharacterDamage(deltaTime);

            _shockwaveTimer += deltaTime;
            if (_shockwaveTimer >= ShockwaveInterval)
            {
                _shockwaveTimer = 0f;
                Shockwave();
            }
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

            var shelterSys = _cachedShelterSystem;
            if (shelterSys != null)
            {
                shelterSys.ModifyParams(new ShelterParams
                {
                    RecoveryRate = 20f,
                    ConsumptionRate = 33f,
                });
            }

            Debug.Log($"[M6_Apocalypse] {Params.Name} 结束");
        }

        private void DealCharacterDamage(float deltaTime)
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            float dps = Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier * 1.5f;
            _damageAccumulator += dps * deltaTime;
            if (_damageAccumulator < 1f) return;

            int dmg = Mathf.FloorToInt(_damageAccumulator);
            _damageAccumulator -= dmg;

            shelterSys.DealDamage(CharacterType.Aqua, dmg);
            shelterSys.DealDamage(CharacterType.Ignis, dmg);
        }

        private void Shockwave()
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            int damage = Mathf.RoundToInt(Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier);
            if (damage <= 0) return;

            shelterSys.DealDamage(CharacterType.Aqua, damage);
            shelterSys.DealDamage(CharacterType.Ignis, damage);
            Debug.Log($"[M6_Apocalypse] 终焉冲击波 双方-{damage}HP");
        }
    }
}
