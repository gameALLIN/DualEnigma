/// ============================================================
/// 文件名: PhysicsDisaster.cs
/// 创建时间: 2026-07-17
/// 作者: DualEnigma
/// 描述: 物理灾害（P系列 4xx），含地震、陨石、冲击波等物理类灾害。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Disaster
{
    public class PhysicsDisaster : DisasterBase
    {
        public override void OnStart()
        {
            IsRunning = true;
            Debug.Log($"[PhysicsDisaster] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            Debug.Log($"[PhysicsDisaster] {Params.Name} 结束");
        }
    }
}
