using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster.Mechanism
{
    public class M5_ShelterWeaken : MechanismDisaster
    {
        private GameObject _effectObject;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("M5_ShelterWeaken_Effect");
            _effectObject.AddComponent<ParticleSystem>();

            var shelterSys = _cachedShelterSystem;
            if (shelterSys != null)
                shelterSys.SetM5Weakening(true);

            Debug.Log($"[M5_ShelterWeaken] {Params.Name} 开始 (能量恢复减半，消耗翻倍)");
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

            var shelterSys = _cachedShelterSystem;
            if (shelterSys != null)
                shelterSys.SetM5Weakening(false);

            Debug.Log($"[M5_ShelterWeaken] {Params.Name} 结束");
        }
    }
}
