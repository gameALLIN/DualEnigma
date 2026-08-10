using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Fragment;

namespace DualEnigma.Disaster.TimeSpace
{
    public class T5_TimeAccelerate : TimeSpaceDisaster
    {
        private GameObject _effectObject;
        private const float DespawnMultiplier = 2f;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("T5_TimeAccelerate_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            Debug.Log($"[T5_TimeAccelerate] {Params.Name} 开始 (碎片消失速度×{DespawnMultiplier})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);
            AccelerateFragmentDespawn(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[T5_TimeAccelerate] {Params.Name} 结束");
        }

        private void AccelerateFragmentDespawn(float deltaTime)
        {
            var fragSys = _cachedFragmentSystem;
            if (fragSys == null) return;

            var fragments = fragSys.GetActiveFragments();
            foreach (var frag in fragments)
            {
                if (frag == null) continue;
                var rb = frag.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.velocity *= 1f + (DespawnMultiplier - 1f) * deltaTime * 0.1f;
            }
        }
    }
}
