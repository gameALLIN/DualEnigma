using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;
using CharacterController = DualEnigma.Character.CharacterController;

namespace DualEnigma.Disaster.Physics
{
    public class P4_Tornado : PhysicsDisaster
    {
        private GameObject _effectObject;
        private float _damageAccumulator;
        private Vector3 _tornadoPosition;
        private float _moveTimer;
        private const float MoveSpeed = 2f;
        private const float LiftForce = 5f;
        private System.Random _random;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("P4_Tornado_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _tornadoPosition = new Vector3(Params.Position.x, Params.Position.y, 0f);
            _random = new System.Random((int)Params.RandomSeed);
            _moveTimer = 0f;
            Debug.Log($"[P4_Tornado] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _moveTimer += deltaTime;
            _tornadoPosition.x += (float)(_random.NextDouble() - 0.5) * MoveSpeed * deltaTime * 4f;
            _tornadoPosition.y += (float)(_random.NextDouble() - 0.5) * MoveSpeed * deltaTime * 2f;
            if (_effectObject != null)
                _effectObject.transform.position = _tornadoPosition;

            LiftCharacters(deltaTime);
            DealCharacterDamage(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[P4_Tornado] {Params.Name} 结束");
        }

        private void LiftCharacters(float deltaTime)
        {
            var charSys = _cachedCharacterSystem;
            if (charSys == null) return;

            TryLiftCharacter(charSys.GetCharacter(CharacterType.Aqua));
            TryLiftCharacter(charSys.GetCharacter(CharacterType.Ignis));
        }

        private void TryLiftCharacter(CharacterController character)
        {
            if (character == null) return;
            float dist = Vector2.Distance(character.transform.position, _tornadoPosition);
            if (dist > Params.Range) return;

            var rb = character.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.AddForce(Vector2.up * LiftForce, ForceMode2D.Impulse);
        }

        private void DealCharacterDamage(float deltaTime)
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            var charSys = _cachedCharacterSystem;
            if (charSys == null) return;

            float dps = Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier;
            _damageAccumulator += dps * deltaTime;
            if (_damageAccumulator < 1f) return;

            int dmg = Mathf.FloorToInt(_damageAccumulator);
            _damageAccumulator -= dmg;

            var aqua = charSys.GetCharacter(CharacterType.Aqua);
            if (aqua != null)
            {
                float dist = Vector2.Distance(aqua.transform.position, _tornadoPosition);
                if (dist <= Params.Range)
                    shelterSys.DealDamage(CharacterType.Aqua, dmg);
            }

            var ignis = charSys.GetCharacter(CharacterType.Ignis);
            if (ignis != null)
            {
                float dist = Vector2.Distance(ignis.transform.position, _tornadoPosition);
                if (dist <= Params.Range)
                    shelterSys.DealDamage(CharacterType.Ignis, dmg);
            }
        }
    }
}
