using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster.Physics
{
    public class P5_Tsunami : PhysicsDisaster
    {
        private GameObject _effectObject;
        private float _damageAccumulator;
        private float _wavePositionX;
        private const float WaveSpeed = 3f;
        private const float WaveWidth = 5f;
        private bool _movingRight;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("P5_Tsunami_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _wavePositionX = -20f;
            _movingRight = true;
            Debug.Log($"[P5_Tsunami] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            if (_movingRight)
            {
                _wavePositionX += WaveSpeed * deltaTime;
                if (_wavePositionX > 20f)
                    _movingRight = false;
            }

            if (_effectObject != null)
                _effectObject.transform.position = new Vector3(_wavePositionX, 0f, 0f);

            DealCharacterDamage(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[P5_Tsunami] {Params.Name} 结束");
        }

        private void DealCharacterDamage(float deltaTime)
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            var charSys = _cachedCharacterSystem;
            if (charSys == null) return;

            float dps = Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier * 2f;
            _damageAccumulator += dps * deltaTime;
            if (_damageAccumulator < 1f) return;

            int dmg = Mathf.FloorToInt(_damageAccumulator);
            _damageAccumulator -= dmg;

            var aqua = charSys.GetCharacter(CharacterType.Aqua);
            if (aqua != null && Mathf.Abs(aqua.transform.position.x - _wavePositionX) < WaveWidth)
                shelterSys.DealDamage(CharacterType.Aqua, dmg);

            var ignis = charSys.GetCharacter(CharacterType.Ignis);
            if (ignis != null && Mathf.Abs(ignis.transform.position.x - _wavePositionX) < WaveWidth)
                shelterSys.DealDamage(CharacterType.Ignis, dmg);
        }
    }
}
