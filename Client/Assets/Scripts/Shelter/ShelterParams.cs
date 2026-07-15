/// ============================================================
/// 文件名: ShelterParams.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 庇护能量参数，可通过天赋修改。
/// ============================================================

using System;

namespace DualEnigma.Shelter
{
    /// <summary>
    /// 庇护能量参数。可通过天赋修改。
    /// 引用：双生庇护系统设计.md §二 能量系统 / §5.2 天赋修改
    /// </summary>
    [Serializable]
    public class ShelterParams
    {
        /// <summary>能量最大值（默认100，天赋"能量扩容"+30/次）</summary>
        public float MaxEnergy = 100f;

        /// <summary>恢复速率（默认+20/s，天赋"能量恢复"+30%/次）</summary>
        public float RecoveryRate = 20f;

        /// <summary>消耗速率（默认-33/s）</summary>
        public float ConsumptionRate = 33f;

        /// <summary>庇护距离（默认3格，碎片收集阶段5格，天赋"庇护扩展"+1格/次）</summary>
        public float ShelterDistance = 3f;

        /// <summary>碎片收集阶段庇护距离</summary>
        public float FragmentCollectDistance = 5f;

        /// <summary>碎片收集阶段消耗速率</summary>
        public float FragmentCollectConsumptionRate = 25f;

        /// <summary>耗尽后扣血速率乘数（天赋"护体"×0.5/次，封底×0.1）</summary>
        public float DamageMultiplier = 1f;

        /// <summary>3秒缓冲（能量耗尽后3秒才开始扣血）</summary>
        public float BufferTime = 3f;
    }
}
