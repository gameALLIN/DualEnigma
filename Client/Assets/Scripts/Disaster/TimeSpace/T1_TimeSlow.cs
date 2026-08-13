using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster.TimeSpace
{
    public class T1_TimeSlow : TimeSpaceDisaster
    {
        private GameObject _effectObject;
        private const float SlowMultiplier = 0.5f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("T1_TimeSlow_Effect");
            _effectObject.AddComponent<ParticleSystem>();

            var charSys = _cachedCharacterSystem;
            if (charSys != null)
            {
                var aqua = charSys.GetCharacter(CharacterType.Aqua);
                if (aqua != null) aqua.SetMoveSpeedMultiplier(SlowMultiplier);
                var ignis = charSys.GetCharacter(CharacterType.Ignis);
                if (ignis != null) ignis.SetMoveSpeedMultiplier(SlowMultiplier);
            }

            Debug.Log($"[T1_TimeSlow] {Params.Name} 开始 (移速/建造速度-50%)");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);
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

            Debug.Log($"[T1_TimeSlow] {Params.Name} 结束");
        }
    }
}
