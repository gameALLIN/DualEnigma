using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Synthesis;

namespace DualEnigma.Disaster.Mechanism
{
    public class M3_SynthesisInterference : MechanismDisaster
    {
        private GameObject _effectObject;
        private const float TimeMultiplier = 2f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("M3_SynthesisInterference_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            Debug.Log($"[M3_SynthesisInterference] {Params.Name} 开始 (合成时间×{TimeMultiplier})");
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
            Debug.Log($"[M3_SynthesisInterference] {Params.Name} 结束");
        }

        public float GetSynthesisTimeMultiplier()
        {
            return IsRunning ? TimeMultiplier : 1f;
        }
    }
}
