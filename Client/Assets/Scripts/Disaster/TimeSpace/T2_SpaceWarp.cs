using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Fragment;

namespace DualEnigma.Disaster.TimeSpace
{
    public class T2_SpaceWarp : TimeSpaceDisaster
    {
        private GameObject _effectObject;
        private float _warpTimer;
        private const float WarpInterval = 2f;
        private System.Random _random;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("T2_SpaceWarp_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _random = new System.Random((int)Params.RandomSeed);
            _warpTimer = 0f;
            Debug.Log($"[T2_SpaceWarp] {Params.Name} 开始");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _warpTimer += deltaTime;
            if (_warpTimer >= WarpInterval)
            {
                _warpTimer = 0f;
                WarpFragments();
            }
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[T2_SpaceWarp] {Params.Name} 结束");
        }

        private void WarpFragments()
        {
            var fragSys = _cachedFragmentSystem;
            if (fragSys == null) return;

            var fragments = fragSys.GetActiveFragments();
            foreach (var frag in fragments)
            {
                if (frag == null) continue;
                float offsetX = (float)(_random.NextDouble() - 0.5) * 4f;
                float offsetY = (float)(_random.NextDouble() - 0.5) * 2f;
                frag.transform.position += new Vector3(offsetX, offsetY, 0f);
            }
        }
    }
}
