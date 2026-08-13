using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster.TimeSpace
{
    public class T4_TimeRift : TimeSpaceDisaster
    {
        private GameObject _effectObject;
        private float _teleportTimer;
        private const float TeleportInterval = 5f;
        private System.Random _random;
        private Vector2 _mapCenter = Vector2.zero;
        private const float TeleportRange = 10f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("T4_TimeRift_Effect");
            _effectObject.transform.position = Params.Position;
            _effectObject.AddComponent<ParticleSystem>();
            _random = new System.Random((int)Params.RandomSeed);
            _teleportTimer = 0f;
            Debug.Log($"[T4_TimeRift] {Params.Name} 开始");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _teleportTimer += deltaTime;
            if (_teleportTimer >= TeleportInterval)
            {
                _teleportTimer = 0f;
                TeleportCharacters();
            }
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[T4_TimeRift] {Params.Name} 结束");
        }

        private void TeleportCharacters()
        {
            var charSys = _cachedCharacterSystem;
            if (charSys == null) return;

            var aqua = charSys.GetCharacter(CharacterType.Aqua);
            if (aqua != null)
            {
                float x = _mapCenter.x + (float)(_random.NextDouble() - 0.5) * TeleportRange * 2f;
                float y = _mapCenter.y + (float)(_random.NextDouble() - 0.5) * TeleportRange;
                aqua.transform.position = new Vector3(x, y, 0f);
            }

            var ignis = charSys.GetCharacter(CharacterType.Ignis);
            if (ignis != null)
            {
                float x = _mapCenter.x + (float)(_random.NextDouble() - 0.5) * TeleportRange * 2f;
                float y = _mapCenter.y + (float)(_random.NextDouble() - 0.5) * TeleportRange;
                ignis.transform.position = new Vector3(x, y, 0f);
            }

            Debug.Log("[T4_TimeRift] 角色被随机传送");
        }
    }
}
