/// ============================================================
/// 文件名: PerceptionDisaster.cs
/// 创建时间: 2026-07-17
/// 作者: DualEnigma
/// 描述: 感知灾害（S系列 3xx），含幻觉、迷雾、感官扭曲等感知类灾害。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Disaster
{
    public class PerceptionDisaster : DisasterBase
    {
        public override void OnStart()
        {
            IsRunning = true;
            Debug.Log($"[PerceptionDisaster] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            Debug.Log($"[PerceptionDisaster] {Params.Name} 结束");
        }
    }
}
