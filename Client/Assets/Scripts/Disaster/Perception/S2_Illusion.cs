using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Fragment;

namespace DualEnigma.Disaster.Perception
{
    public class S2_Illusion : PerceptionDisaster
    {
        private GameObject _effectObject;
        private float _illusionTimer;
        private const float SpawnInterval = 4f;
        private System.Random _random;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("S2_Illusion_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _random = new System.Random((int)Params.RandomSeed);
            _illusionTimer = 0f;
            Debug.Log($"[S2_Illusion] {Params.Name} 开始");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _illusionTimer += deltaTime;
            if (_illusionTimer >= SpawnInterval)
            {
                _illusionTimer = 0f;
                SpawnFakeFragment();
            }
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[S2_Illusion] {Params.Name} 结束");
        }

        private void SpawnFakeFragment()
        {
            float x = (float)(_random.NextDouble() - 0.5) * 30f;
            float y = (float)(_random.NextDouble() - 0.5) * 10f;
            var fakeObj = new GameObject("FakeFragment");
            fakeObj.transform.position = new Vector3(x, y, 0f);
            fakeObj.AddComponent<SpriteRenderer>();
            Object.Destroy(fakeObj, 3f);
            Debug.Log($"[S2_Illusion] 产生假碎片 @({x:F1}, {y:F1})");
        }
    }
}
