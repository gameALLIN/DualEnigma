using UnityEngine;
using DualEnigma.Disaster;
using DualEnigma.Core;
using DualEnigma.Character;
using DualEnigma.Synthesis;
using System.Collections.Generic;
using CharacterController = DualEnigma.Character.CharacterController;

namespace DualEnigma.Disaster.Mechanism
{
    public class M2_MaterialMutation : MechanismDisaster
    {
        private GameObject _effectObject;
        private float _mutationTimer;
        private const float MutationInterval = 3f;
        private System.Random _random;
        private static readonly MaterialType[] MaterialPool =
        {
            MaterialType.WaterBrick, MaterialType.IceBrick,
            MaterialType.FireBrick, MaterialType.LavaBrick, MaterialType.StoneBrick,
        };

        public override void OnStart()
        {
            IsRunning = true;
            _effectObject = new GameObject("M2_MaterialMutation_Effect");
            _effectObject.AddComponent<ParticleSystem>();
            _random = new System.Random((int)Params.RandomSeed);
            _mutationTimer = 0f;
            Debug.Log($"[M2_MaterialMutation] {Params.Name} 开始");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);

            _mutationTimer += deltaTime;
            if (_mutationTimer >= MutationInterval)
            {
                _mutationTimer = 0f;
                MutateCarriedMaterials();
            }
        }

        public override void OnEnd()
        {
            IsRunning = false;
            if (_effectObject != null)
                Object.Destroy(_effectObject);
            Debug.Log($"[M2_MaterialMutation] {Params.Name} 结束");
        }

        private void MutateCarriedMaterials()
        {
            var charSys = _cachedCharacterSystem;
            if (charSys == null) return;

            MutateCharacter(charSys.GetCharacter(CharacterType.Aqua));
            MutateCharacter(charSys.GetCharacter(CharacterType.Ignis));
        }

        private void MutateCharacter(CharacterController character)
        {
            if (character == null || character.Stats == null) return;

            var materials = new List<MaterialType>(character.Stats.CarriedMaterials.Keys);
            if (materials.Count == 0) return;

            int idx = _random.Next(materials.Count);
            MaterialType oldType = materials[idx];
            int count = character.Stats.CarriedMaterials[oldType];
            MaterialType newType = MaterialPool[_random.Next(MaterialPool.Length)];

            if (newType == oldType) return;

            character.TryConsumeMaterial(oldType, count);
            character.AddMaterial(newType, count);
            Debug.Log($"[M2_MaterialMutation] {character.PlayerId} 材料 {oldType}→{newType} ×{count}");
        }
    }
}
