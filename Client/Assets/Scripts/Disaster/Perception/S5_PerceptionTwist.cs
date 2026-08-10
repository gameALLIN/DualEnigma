using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Character;

namespace DualEnigma.Disaster.Perception
{
    public class S5_PerceptionTwist : PerceptionDisaster
    {
        private GameObject _effectObject;
        private bool _controlsReversed;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("S5_PerceptionTwist_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _controlsReversed = true;
            Debug.Log($"[S5_PerceptionTwist] {Params.Name} 开始 (操作反转)");
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
            _controlsReversed = false;
            Debug.Log($"[S5_PerceptionTwist] {Params.Name} 结束");
        }

        public bool IsControlsReversed => _controlsReversed;
    }
}
