using UnityEngine;
using DualEnigma.Disaster;

namespace DualEnigma.Disaster.Perception
{
    public class S3_Deafness : PerceptionDisaster
    {
        private GameObject _effectObject;
        private float _originalVolume = 1f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("S3_Deafness_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _originalVolume = AudioListener.volume;
            AudioListener.volume = 0f;
            Debug.Log($"[S3_Deafness] {Params.Name} 开始 (音效已静音)");
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
            AudioListener.volume = _originalVolume;
            Debug.Log($"[S3_Deafness] {Params.Name} 结束");
        }
    }
}
