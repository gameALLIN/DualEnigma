/// ============================================================
/// 文件名: NetworkMessages.cs
/// 创建时间: 2026-07-14
/// 作者: DualEnigma
/// 描述: 网络消息数据结构定义。使用 C# 类模拟 Protobuf 消息，
///       暂不引入 protobuf 库。
/// 引用：网络通信.md §3 同步内容分类、§6.3 重连同步消息结构
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Building;
using DualEnigma.Synthesis;
using DualEnigma.Fragment;
using DualEnigma.Disaster;

namespace DualEnigma.Network
{
    /// <summary>
    /// 高频状态消息（20Hz，不可靠UDP）。
    /// 引用：网络通信.md §3.2 高频状态同步详情
    /// </summary>
    public class HighFrequencyState
    {
        /// <summary>玩家ID（0=Aqua, 1=Ignis）</summary>
        public byte playerId;
        /// <summary>角色世界坐标</summary>
        public Vector2 position;
        /// <summary>角色速度向量</summary>
        public Vector2 velocity;
        /// <summary>动画状态枚举</summary>
        public AnimState animState;
        /// <summary>朝向（true=右, false=左）</summary>
        public bool facing;
        /// <summary>发送时间戳（毫秒）</summary>
        public uint timestamp;
    }

    /// <summary>
    /// 中频状态消息（10Hz，可靠UDP）。
    /// 引用：网络通信.md §3.3 中频状态同步详情
    /// </summary>
    public class MidFrequencyState
    {
        /// <summary>玩家ID</summary>
        public byte playerId;
        /// <summary>当前生命值</summary>
        public short hp;
        /// <summary>庇护能量值（0-100）</summary>
        public byte shelterEnergy;
        /// <summary>携带的碎片类型列表</summary>
        public byte[] carriedFragments;
    }

    /// <summary>
    /// 关键事件消息（可靠有序TCP）。
    /// 引用：网络通信.md §3.4 关键事件同步详情
    /// </summary>
    public class KeyEvent
    {
        /// <summary>事件唯一ID（用于去重和顺序处理）</summary>
        public uint eventId;
        /// <summary>事件类型枚举</summary>
        public ushort eventType;
        /// <summary>发起玩家ID</summary>
        public byte playerId;
        /// <summary>事件发生时间戳（毫秒）</summary>
        public uint timestamp;
        /// <summary>事件特定数据</summary>
        public byte[] payload;
    }

    /// <summary>
    /// 阶段切换事件消息（可靠有序TCP）。
    /// 引用：网络通信.md §3.1 同步类型总览
    /// </summary>
    public class PhaseChangeEvent
    {
        /// <summary>目标游戏阶段</summary>
        public GamePhase phase;
        /// <summary>切换时间戳（毫秒）</summary>
        public uint timestamp;
    }

    /// <summary>
    /// 天赋选择事件消息（可靠TCP）。
    /// 引用：网络通信.md §3.1 同步类型总览
    /// </summary>
    public class TalentSelectEvent
    {
        /// <summary>天赋ID</summary>
        public int talentId;
        /// <summary>玩家ID</summary>
        public byte playerId;
    }

    /// <summary>
    /// 心跳包。
    /// 引用：网络通信.md §7.2 超时与心跳机制
    /// </summary>
    public class HeartbeatPacket
    {
        /// <summary>发送时间戳（毫秒）</summary>
        public uint timestamp;
    }

    // ================================================================
    // 重连快照子结构
    // 引用：网络通信.md §6.2 重连全量状态同步、§6.3 重连同步消息结构
    // ================================================================

    /// <summary>
    /// 玩家状态快照（重连同步用）。
    /// </summary>
    public class PlayerStateSnapshot
    {
        /// <summary>玩家ID</summary>
        public byte playerId;
        /// <summary>当前生命值</summary>
        public short hp;
        /// <summary>庇护能量值</summary>
        public byte shelterEnergy;
        /// <summary>角色位置</summary>
        public Vector2 position;
        /// <summary>角色速度</summary>
        public Vector2 velocity;
        /// <summary>动画状态</summary>
        public AnimState animState;
        /// <summary>朝向</summary>
        public bool facing;
        /// <summary>携带的碎片类型列表</summary>
        public byte[] carriedFragments;
    }

    /// <summary>
    /// 建筑状态快照（重连同步用）。
    /// </summary>
    public class BuildingStateSnapshot
    {
        /// <summary>建筑唯一ID</summary>
        public int buildingId;
        /// <summary>建筑类型</summary>
        public BuildingType type;
        /// <summary>材料类型</summary>
        public MaterialType material;
        /// <summary>网格坐标X</summary>
        public int gridX;
        /// <summary>网格坐标Y</summary>
        public int gridY;
        /// <summary>当前HP</summary>
        public float currentHP;
    }

    /// <summary>
    /// 碎片状态快照（重连同步用）。
    /// </summary>
    public class FragmentStateSnapshot
    {
        /// <summary>碎片ID</summary>
        public int fragmentId;
        /// <summary>碎片类型</summary>
        public FragmentType type;
        /// <summary>碎片位置</summary>
        public Vector2 position;
    }

    /// <summary>
    /// 天赋数据快照（重连同步用）。
    /// </summary>
    public class TalentDataSnapshot
    {
        /// <summary>天赋ID</summary>
        public int talentId;
        /// <summary>玩家ID</summary>
        public byte playerId;
    }

    /// <summary>
    /// 技能状态快照（重连同步用）。
    /// </summary>
    public class SkillStateSnapshot
    {
        /// <summary>技能ID</summary>
        public int skillId;
        /// <summary>玩家ID</summary>
        public byte playerId;
        /// <summary>冷却剩余时间（秒）</summary>
        public float cooldownRemaining;
        /// <summary>可用次数</summary>
        public int useCount;
    }

    /// <summary>
    /// 灾难状态快照（重连同步用，仅灾害冲击阶段）。
    /// </summary>
    public class DisasterStateSnapshot
    {
        /// <summary>灾难ID</summary>
        public int disasterId;
        /// <summary>当前灾难强度（0-1）</summary>
        public float intensity;
        /// <summary>随机种子</summary>
        public uint randomSeed;
    }

    /// <summary>
    /// 重连全量状态快照。
    /// Host 在 Client 重连成功后发送，恢复 Client 到与 Host 完全一致的状态。
    /// 引用：网络通信.md §6.2 重连全量状态同步、§6.3 重连同步消息结构
    /// </summary>
    public class ReconnectSnapshot
    {
        // 游戏进度
        /// <summary>当前章节 (1-3)</summary>
        public int chapter;
        /// <summary>当前节 (1-4)</summary>
        public int section;
        /// <summary>当前轮 (1-3)</summary>
        public int round;
        /// <summary>当前游戏阶段</summary>
        public GamePhase currentPhase;
        /// <summary>阶段剩余毫秒</summary>
        public int phaseRemainingMs;
        /// <summary>累计得分</summary>
        public int score;

        // 角色状态
        /// <summary>两个玩家的完整状态</summary>
        public List<PlayerStateSnapshot> players;

        // 建筑状态
        /// <summary>所有建筑</summary>
        public List<BuildingStateSnapshot> buildings;

        // 碎片状态
        /// <summary>场上存活碎片</summary>
        public List<FragmentStateSnapshot> fragments;

        // 天赋/技能
        /// <summary>已选天赋列表</summary>
        public List<TalentDataSnapshot> talents;
        /// <summary>技能状态列表</summary>
        public List<SkillStateSnapshot> skills;

        // 灾难状态（仅灾害冲击阶段）
        /// <summary>当前灾难状态（可能为 null）</summary>
        public DisasterStateSnapshot disaster;

        // 时间戳
        /// <summary>快照时间戳（毫秒）</summary>
        public uint snapshotTimestamp;
    }
}
