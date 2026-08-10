using UnityEngine;
using DualEnigma.Disaster;

namespace DualEnigma.Disaster.Physics
{
    public class P2_Earthquake : PhysicsDisaster
    {
        private GameObject _effectObject;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("P2_Earthquake_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            Debug.Log($"[P2_Earthquake] {Params.Name} 开始");
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
            Debug.Log($"[P2_Earthquake] {Params.Name} 结束");
        }
    }
}
