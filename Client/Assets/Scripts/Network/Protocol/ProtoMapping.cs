/// ============================================================
/// 文件名: ProtoMapping.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: proto 生成类型 ↔ 本地枚举/类型映射收口（PC-1 Task C1.4）。
///       GamePhasePb → GamePhase（UNSPECIFIED 兜底 Preview+警告）；
///       AnimState 字符串解析收口（原 Enum.TryParse 逻辑迁此）。
/// 引用：Generated/Game.cs（Dualenigma.V1）, Core/GamePhase
/// ============================================================

using UnityEngine;
using DualEnigma.V1;
using DualEnigma.Core;

namespace DualEnigma.Network
{
    /// <summary>proto 类型 ↔ 本地类型映射</summary>
    public static class ProtoMapping
    {
        /// <summary>GamePhasePb → 本地 GamePhase（UNSPECIFIED → Preview + LogWarning 兜底）</summary>
        public static GamePhase ToGamePhase(GamePhasePb pb)
        {
            switch (pb)
            {
                case GamePhasePb.Preview: return GamePhase.Preview;
                case GamePhasePb.FragmentCollect: return GamePhase.FragmentCollect;
                case GamePhasePb.DisasterPreview: return GamePhase.DisasterPreview;
                case GamePhasePb.Build: return GamePhase.Build;
                case GamePhasePb.DisasterImpact: return GamePhase.DisasterImpact;
                case GamePhasePb.Rest: return GamePhase.Rest;
                case GamePhasePb.Upgrade: return GamePhase.Upgrade;
                default:
                    Debug.LogWarning($"[ProtoMapping] 未知阶段 {pb}，兜底 Preview");
                    return GamePhase.Preview;
            }
        }

        /// <summary>AnimState 字符串收口（解析失败 → Idle 兜底）</summary>
        public static AnimState ToAnimState(string animState)
        {
            return System.Enum.TryParse(animState, out AnimState state) ? state : AnimState.Idle;
        }

        /// <summary>Vec2（proto）→ Vector2</summary>
        public static Vector2 ToVector2(Vec2 v)
        {
            return v != null ? new Vector2(v.X, v.Y) : Vector2.zero;
        }
    }
}
