using UnityEngine;
using DualEnigma.Disaster;

namespace DualEnigma.Disaster.Perception
{
    public class S4_Delusion : PerceptionDisaster
    {
        private GameObject _effectObject;
        private float _delusionTimer;
        private const float DelusionInterval = 5f;
        private static readonly string[] FakeMessages =
        {
            "警告: 火砖建筑即将崩塌!",
            "提示: 防洪堤已被摧毁!",
            "警报: 庇护能量即将耗尽!",
            "注意: 碎片即将消失!",
        };
        private System.Random _random;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("S4_Delusion_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _random = new System.Random((int)Params.RandomSeed);
            _delusionTimer = 0f;
            Debug.Log($"[S4_Delusion] {Params.Name} 开始");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _delusionTimer += deltaTime;
            if (_delusionTimer >= DelusionInterval)
            {
                _delusionTimer = 0f;
                ShowFakeMessage();
            }
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[S4_Delusion] {Params.Name} 结束");
        }

        private void ShowFakeMessage()
        {
            int idx = _random.Next(FakeMessages.Length);
            Debug.Log($"[S4_Delusion] 错误UI: {FakeMessages[idx]}");
        }
    }
}
