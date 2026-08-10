/// ============================================================
/// 文件名: ElementDisaster.cs
/// 创建时间: 2026-07-17
/// 作者: DualEnigma
/// 描述: 元素灾害（E系列 0xx），含火灾、冰冻、雷电等元素类灾害。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Disaster
{
    public class ElementDisaster : DisasterBase
    {
        public override void OnStart()
        {
            IsRunning = true;
            Debug.Log($"[ElementDisaster] {Params.Name} 开始 (DPS={Params.BaseDPS})");
        }

        public override void OnUpdate(float deltaTime, float elapsedTime)
        {
            CurrentIntensity = CalculateIntensity(elapsedTime);
            ApplyDamageToBuildings(deltaTime);
        }

        public override void OnEnd()
        {
            IsRunning = false;
            Debug.Log($"[ElementDisaster] {Params.Name} 结束");
        }
    }
}
