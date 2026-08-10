/// ============================================================
/// 文件名: TimeSpaceDisaster.cs
/// 创建时间: 2026-07-17
/// 作者: DualEnigma
/// 描述: 时空灾害（T系列 2xx），含时间扭曲、空间裂隙等时空类灾害。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Disaster
{
    public class TimeSpaceDisaster : DisasterBase
    {
        public override void OnStart()
        {
            IsRunning = true;
            Debug.Log($"[TimeSpaceDisaster] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            Debug.Log($"[TimeSpaceDisaster] {Params.Name} 结束");
        }
    }
}
