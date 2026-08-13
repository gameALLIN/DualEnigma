using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Fragment;

namespace DualEnigma.Disaster.TimeSpace
{
    public class T3_GravityAnomaly : TimeSpaceDisaster
    {
        private GameObject _effectObject;
        private bool _gravityReversed;
        private float _gravityTimer;
        private const float FlipInterval = 5f;
        private const float FlipDuration = 3f;
        private const float NormalGravity = -9.81f;
        private const float ReversedGravity = 9.81f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("T3_GravityAnomaly_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _gravityTimer = 0f;
            _gravityReversed = false;
            Debug.Log($"[T3_GravityAnomaly] {Params.Name} 开始");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _gravityTimer += deltaTime;
            if (!_gravityReversed && _gravityTimer >= FlipInterval)
            {
                _gravityTimer = 0f;
                _gravityReversed = true;
                Physics2D.gravity = new Vector2(0f, ReversedGravity);
                Debug.Log("[T3_GravityAnomaly] 重力反转");
            }
            else if (_gravityReversed && _gravityTimer >= FlipDuration)
            {
                _gravityTimer = 0f;
                _gravityReversed = false;
                Physics2D.gravity = new Vector2(0f, NormalGravity);
                Debug.Log("[T3_GravityAnomaly] 重力恢复");
            }
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Physics2D.gravity = new Vector2(0f, NormalGravity);
            Debug.Log($"[T3_GravityAnomaly] {Params.Name} 结束");
        }
    }
}
