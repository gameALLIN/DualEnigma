using UnityEngine;
using DualEnigma.Disaster;

namespace DualEnigma.Disaster.Perception
{
    public class S1_Fog : PerceptionDisaster
    {
        private GameObject _effectObject;
        private float _visibilityFactor = 1f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("S1_Fog_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _visibilityFactor = 1f;
            Debug.Log($"[S1_Fog] {Params.Name} 开始");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _visibilityFactor = Mathf.Lerp(1f, 0.3f, CurrentIntensity);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            _visibilityFactor = 1f;
            Debug.Log($"[S1_Fog] {Params.Name} 结束");
        }
    }
}
