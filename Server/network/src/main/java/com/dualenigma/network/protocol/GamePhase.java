package com.dualenigma.network.protocol;

/**
 * 游戏阶段枚举 — 与客户端 DualEnigma.Core.GamePhase 完全对齐.
 *
 * 7 阶段 90 秒/轮：
 * ① Preview(5s) → ② FragmentCollect(15s) → ③ DisasterPreview(5s)
 * → ④ Build(20s) → ⑤ DisasterImpact(20s) → ⑥ Rest(10s) → ⑦ Upgrade(15s)
 */
public enum GamePhase {
    Preview,           // ① 预告（5秒）
    FragmentCollect,   // ② 碎片收集（15秒）
    DisasterPreview,   // ③ 灾害预告（5秒）
    Build,             // ④ 建造（20秒）
    DisasterImpact,    // ⑤ 灾害冲击（20秒）
    Rest,              // ⑥ 修整（10秒）
    Upgrade,           // ⑦ 升级（15秒）
}
