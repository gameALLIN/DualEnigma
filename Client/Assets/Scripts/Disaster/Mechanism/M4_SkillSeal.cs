using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Skill;
using DualEnigma.Character;

namespace DualEnigma.Disaster.Mechanism
{
    public class M4_SkillSeal : MechanismDisaster
    {
        private GameObject _effectObject;
        private int _sealedSkillId = -1;
        private System.Random _random;

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("M4_SkillSeal_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _random = new System.Random((int)Params.RandomSeed);
            _sealedSkillId = _random.Next(1, 5);
            Debug.Log($"[M4_SkillSeal] {Params.Name} 开始 (封印技能#{_sealedSkillId})");
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
            _sealedSkillId = -1;
            Debug.Log($"[M4_SkillSeal] {Params.Name} 结束");
        }

        public bool IsSkillSealed(int skillId)
        {
            return IsRunning && skillId == _sealedSkillId;
        }
    }
}
