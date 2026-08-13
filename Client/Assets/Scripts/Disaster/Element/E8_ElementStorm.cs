using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster.Element
{
    public class E8_ElementStorm : ElementDisaster
    {
        private GameObject _effectObject;
        private float _damageAccumulator;
        private float _elementTimer;
        private int _currentElementIndex;
        private static readonly string[] ElementNames = { "Fire", "Ice", "Lightning" };
        private const float ElementSwitchInterval = 5f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("E8_ElementStorm_Effect");
            _effectObject.transform.position = Params.Position;
            _effectObject.AddComponent<ParticleSystem>();
            _elementTimer = 0f;
            _currentElementIndex = 0;
            Debug.Log($"[E8_ElementStorm] {Params.Name} 开始 (DPS={Params.BaseDPS}, 初始元素={ElementNames[0]})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _elementTimer += deltaTime;
            if (_elementTimer >= ElementSwitchInterval)
            {
                _elementTimer = 0f;
                _currentElementIndex = (_currentElementIndex + 1) % ElementNames.Length;
                Debug.Log($"[E8_ElementStorm] 元素切换至 {ElementNames[_currentElementIndex]}");
            }

            DealCharacterDamage(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[E8_ElementStorm] {Params.Name} 结束");
        }

        private void DealCharacterDamage(float deltaTime)
        {
            var shelterSys = _cachedShelterSystem;
            if (shelterSys == null) return;

            float elementMultiplier = 1f + _currentElementIndex * 0.2f;
            float dps = Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier * elementMultiplier;
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
