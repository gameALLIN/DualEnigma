/// ============================================================
/// 文件名: MechanismDisaster.cs
/// 创建时间: 2026-07-17
/// 作者: DualEnigma
/// 描述: 机制灾害（M系列 5xx），含合成干扰、建筑腐蚀等机制类灾害。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Disaster
{
    public class MechanismDisaster : DisasterBase
    {
        public override void OnStart()
        {
            IsRunning = true;
            Debug.Log($"[MechanismDisaster] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            Debug.Log($"[MechanismDisaster] {Params.Name} 结束");
        }
    }
}
