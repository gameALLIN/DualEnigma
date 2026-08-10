/// ============================================================
/// 文件名: EnvironmentDisaster.cs
/// 创建时间: 2026-07-17
/// 作者: DualEnigma
/// 描述: 环境灾害（V系列 1xx），含暴风雪、洪水、火山喷发等环境类灾害。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Disaster
{
    public class EnvironmentDisaster : DisasterBase
    {
        public override void OnStart()
        {
            IsRunning = true;
            Debug.Log($"[EnvironmentDisaster] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            Debug.Log($"[EnvironmentDisaster] {Params.Name} 结束");
        }
    }
}
