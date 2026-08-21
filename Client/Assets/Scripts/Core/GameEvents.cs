/// ============================================================
/// 文件名: GameEvents.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 全局事件结构体定义。所有事件为 struct 并实现 IEventData 接口，
///       避免发布时的 GC 开销。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.Core;
using DualEnigma.Building;

namespace DualEnigma.Core
{
    /// <summary>阶段切换事件（由 GameStateMachine 发布）</summary>
    public struct PhaseChangedEvent : IEventData
    {
        public GamePhase phase;
    }

    /// <summary>碎片被收集事件（由 FragmentSystem 发布）</summary>
    public struct FragmentCollectedEvent : IEventData
    {
        public int fragmentId;
        public byte playerId;
        public bool isJumping;
        public int multiplier;
        public float posX;   // 接住瞬间碎片世界坐标（服务器同接几何判定用）
        public float posY;
    }

    /// <summary>碎片自然消失事件（由 FragmentController 发布，FragmentSystem 订阅）</summary>
    public struct FragmentDespawnedEvent : IEventData
    {
        public int fragmentId;
    }

    /// <summary>碎片转化为温砖事件（由 FragmentSystem 发布）</summary>
    public struct FragmentWarmBrickConvertedEvent : IEventData
    {
        public int fragmentId;
        public Vector2 position;
    }

    /// <summary>建筑放置完成事件（由 BuildingSystem 发布）</summary>
    public struct BuildingPlacedEvent : IEventData
    {
        public int buildingId;
        public BuildingType type;
        public Vector2Int gridPos;
    }

    /// <summary>建筑被摧毁事件（由 DisasterSystem 发布）</summary>
    public struct BuildingDestroyedEvent : IEventData
    {
        public int buildingId;
    }

    /// <summary>灾害冲击阶段开始事件（由 DisasterSystem 发布）</summary>
    public struct DisasterStartedEvent : IEventData
    {
        public int disasterId;
    }

    /// <summary>灾害冲击阶段结束事件（由 DisasterSystem 发布）</summary>
    public struct DisasterEndedEvent : IEventData
    {
    }

    /// <summary>技能释放事件（由 SkillSystem 发布）</summary>
    public struct SkillActivatedEvent : IEventData
    {
        public int skillId;
        public byte playerId;
        public Vector2 targetPos;
    }

    /// <summary>天赋选择完成事件（由 TalentSystem 发布）</summary>
    public struct TalentSelectedEvent : IEventData
    {
        public int talentId;
        public byte playerId;
    }

    /// <summary>角色受伤事件（由 ShelterSystem 发布）</summary>
    public struct PlayerDamagedEvent : IEventData
    {
        public byte playerId;
        public int damage;
    }

    /// <summary>角色死亡事件（由 ShelterSystem 发布，触发游戏结束）</summary>
    public struct PlayerDiedEvent : IEventData
    {
        public byte playerId;
    }

    /// <summary>角色治疗事件（由 ShelterSystem 发布）</summary>
    public struct PlayerHealedEvent : IEventData
    {
        public byte playerId;
        public int amount;
    }

    /// <summary>单局开始事件（由 GameManager 发布）</summary>
    public struct GameStartEvent : IEventData
    {
    }

    /// <summary>材料产出完成事件（由 SynthesisSystem 发布）</summary>
    public struct MaterialProducedEvent : IEventData
    {
        public byte playerId;
        public int materialType;
        public int count;
    }

    /// <summary>单局结束事件（由 GameManager 发布）。isManualExit = 玩家主动退出（不弹结算面板）</summary>
    public struct GameEndEvent : IEventData
    {
        public bool isVictory;
        public bool isManualExit;
    }

    /// <summary>游戏暂停事件（由 GameManager 发布）</summary>
    public struct GamePauseEvent : IEventData
    {
    }

    /// <summary>游戏恢复事件（由 GameManager 发布）</summary>
    public struct GameResumeEvent : IEventData
    {
    }
}
