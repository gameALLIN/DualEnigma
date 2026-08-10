using UnityEngine;
using System.Collections.Generic;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Building;
using DualEnigma.Synthesis;

namespace DualEnigma.Disaster.Mechanism
{
    public class M1_BuildingCorrosion : MechanismDisaster
    {
        private GameObject _effectObject;
        private float _corrosionTimer;
        private const float CorrosionInterval = 1f;
        private System.Random _random;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("M1_BuildingCorrosion_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _random = new System.Random((int)Params.RandomSeed);
            _corrosionTimer = 0f;

            var synthesisSys = _cachedSynthesisSystem;
            if (synthesisSys != null)
                synthesisSys.SetM1ElementDepletion(true);

            Debug.Log($"[M1_BuildingCorrosion] {Params.Name} 开始");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _corrosionTimer += deltaTime;
            if (_corrosionTimer >= CorrosionInterval)
            {
                _corrosionTimer = 0f;
                CorrodeRandomBuilding(deltaTime);
            }
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);

            var synthesisSys = _cachedSynthesisSystem;
            if (synthesisSys != null)
                synthesisSys.SetM1ElementDepletion(false);

            Debug.Log($"[M1_BuildingCorrosion] {Params.Name} 结束");
        }

        private void CorrodeRandomBuilding(float deltaTime)
        {
            var buildSystem = _cachedBuildSystem;
            if (buildSystem == null || buildSystem.Buildings.Count == 0) return;

            List<BuildingData> snapshot = new List<BuildingData>(buildSystem.Buildings);
            int idx = _random.Next(snapshot.Count);
            var target = snapshot[idx];

            float corrosionDamage = Params.BaseDPS * CurrentIntensity * Params.DifficultyMultiplier * CorrosionInterval;
            if (corrosionDamage > 0f)
                buildSystem.DamageBuilding(target.BuildingId, corrosionDamage);
        }
    }
}
