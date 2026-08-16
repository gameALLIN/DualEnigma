/// ============================================================
/// 文件名: GameManager.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 游戏全局管理器，持有全局状态，管理单局生命周期。
///       包含 GameProgress（关卡进度）和 GameState（全局状态）数据类。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.Core;
using DualEnigma.Framework.UI;
using DualEnigma.Network;
using DualEnigma.Character;
using DualEnigma.Shelter;
using DualEnigma.Synthesis;
using DualEnigma.Skill;
using DualEnigma.Disaster;
using DualEnigma.Fragment;
using DualEnigma.UI;

namespace DualEnigma.Core
{
    /// <summary>
    /// 当前关卡进度。3章 × 4节 × 3轮 = 36关。
    /// 编号规则：章-节-轮（如 2-3-1 = 第二章第三节第一轮）
    /// </summary>
    [System.Serializable]
    public class GameProgress
    {
        /// <summary>当前章节 (1-3)</summary>
        public int Chapter = 1;
        /// <summary>当前节 (1-4)</summary>
        public int Section = 1;
        /// <summary>当前轮 (1-3)</summary>
        public int Round = 1;

        /// <summary>全局轮次编号 (1-36)</summary>
        public int GlobalRound => (Chapter - 1) * 12 + (Section - 1) * 3 + Round;

        /// <summary>章节难度倍率</summary>
        public float ChapterMultiplier => Chapter switch
        {
            1 => 0.8f,
            2 => 1.0f,
            3 => 1.2f,
            _ => 1.0f,
        };

        /// <summary>轮次难度倍率</summary>
        public float RoundMultiplier => Round switch
        {
            1 => 1.0f,
            2 => 1.3f,
            3 => 1.6f,
            _ => 1.0f,
        };

        /// <summary>综合难度倍率 = 章节倍率 × 轮次倍率</summary>
        public float DifficultyMultiplier => ChapterMultiplier * RoundMultiplier;
    }

    /// <summary>
    /// 全局游戏状态，由 GameManager 持有。
    /// 包含关卡进度、角色状态、游戏暂停等。
    /// </summary>
    [System.Serializable]
    public class GameState
    {
        /// <summary>当前关卡进度</summary>
        public GameProgress Progress = new GameProgress();
        /// <summary>当前游戏阶段</summary>
        public GamePhase CurrentPhase = GamePhase.Preview;
        /// <summary>阶段剩余时间（秒）</summary>
        public float PhaseRemainingTime;
        /// <summary>游戏是否暂停</summary>
        public bool IsPaused;
        /// <summary>游戏是否结束</summary>
        public bool IsGameOver;
        /// <summary>是否为 Host（房主）</summary>
        public bool IsHost;
    }

    /// <summary>
    /// 游戏全局管理器，持有全局状态，管理单局生命周期。
    /// 继承 Singleton&lt;T&gt;。
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        /// <summary>全局游戏状态</summary>
        public GameState State { get; } = new GameState();

        /// <summary>退出对局流程是否已执行（防止 GameEndEvent 延迟协程与手动退出重复触发）</summary>
        private bool _exitToHomeInvoked;

        /// <summary>水人 HP（从 ShelterSystem 读取，ShelterSystem 为HP唯一权威）</summary>
        public int AquaHP => ShelterSystem.HasInstance ? ShelterSystem.Instance.AquaHP : 0;

        /// <summary>火人 HP（从 ShelterSystem 读取，ShelterSystem 为HP唯一权威）</summary>
        public int IgnisHP => ShelterSystem.HasInstance ? ShelterSystem.Instance.IgnisHP : 0;

        protected override void OnSingletonInitialized()
        {
            Debug.Log("[GameManager] 游戏管理器初始化完成");
        }

        /// <summary>
        /// 每帧驱动各业务系统的 OnUpdate，传递 Time.deltaTime。
        /// 使用 HasInstance 保护，避免单例未初始化时报错。
        /// </summary>
        private void Update()
        {
            if (State.IsPaused || State.IsGameOver)
                return;

            float dt = Time.deltaTime;

            // 能量循环 / 扣血
            if (ShelterSystem.HasInstance)
                ShelterSystem.Instance.OnUpdate(dt);

            // 合成计时
            if (SynthesisSystem.HasInstance)
                SynthesisSystem.Instance.OnUpdate(dt);

            // 技能冷却
            if (SkillSystem.HasInstance)
                SkillSystem.Instance.OnUpdate(dt);

            // 灾难更新
            if (DisasterSystem.HasInstance)
                DisasterSystem.Instance.OnUpdate(dt);

            // TODO: FragmentSystem.OnUpdate 尚未实现，待后续补充
            // if (FragmentSystem.HasInstance)
            //     FragmentSystem.Instance.OnUpdate(dt);
        }

        /// <summary>
        /// 开始单局游戏。重置状态，启动状态机，发布 GameStartEvent。
        /// </summary>
        public void StartGame()
        {
            State.IsGameOver = false;
            State.IsPaused = false;
            _exitToHomeInvoked = false;
            ShelterSystem.Instance.ResetHP();

            if (NetworkSystem.HasInstance && NetworkSystem.Instance.IsConnected)
            {
                // 联机模式：角色按 本地/远程 重建，阶段由服务器权威驱动
                State.IsHost = NetworkSystem.Instance.LocalPlayerId == 0;
                CharacterSystem.Instance.RebuildForNetwork();
                _ = NetworkGameSync.Instance;
                GameStateMachine.Instance.SetNetworkDriven(true);
            }
            else
            {
                GameStateMachine.Instance.StartNewRound();
            }

            EventBus.Instance.Publish(new GameStartEvent());

            Debug.Log("[GameManager] 游戏开始");
        }

        /// <summary>
        /// 结束单局游戏。发布 GameEndEvent，停止状态机。
        /// </summary>
        /// <param name="isVictory">是否胜利</param>
        public void EndGame(bool isVictory)
        {
            if (State.IsGameOver)
                return;

            State.IsGameOver = true;
            GameStateMachine.Instance.SetNetworkDriven(false);
            GameStateMachine.Instance.Stop();
            EventBus.Instance.Publish(new GameEndEvent { isVictory = isVictory });

            Debug.Log($"[GameManager] 游戏结束 — {(isVictory ? "胜利" : "失败")}");
        }

        /// <summary>
        /// 退出对局并恢复主界面。对局内设置面板【退出对局】与对局自然结束（延迟）统一走此出口。
        /// 联机模式：断开服务器连接并弹出房间面板；单机：直接恢复栈内被隐藏的主界面面板。
        /// 幂等：重复调用（事件重入/协程重放）只执行一次，StartGame 时复位。
        /// </summary>
        public void ExitToHome()
        {
            if (_exitToHomeInvoked)
                return;
            _exitToHomeInvoked = true;

            State.IsPaused = false;
            State.IsGameOver = true;

            GameStateMachine.Instance.SetNetworkDriven(false);
            GameStateMachine.Instance.Stop();

            // 联机：断开连接，弹出房间面板恢复 UIHome 为栈顶
            if (NetworkSystem.HasInstance && NetworkSystem.Instance.IsConnected)
            {
                if (GameServerClient.HasInstance)
                    GameServerClient.Instance.Disconnect();
                UIManager.Instance.PopTo<UIHomeCtrl>();
            }

            // 恢复栈内被 SetPanelsVisible(false) 隐藏的主界面面板
            UIManager.Instance.SetPanelsVisible(true);

            EventBus.Instance.Publish(new GameEndEvent { isVictory = false });

            Debug.Log("[GameManager] 退出对局，返回主界面");
        }

        /// <summary>
        /// 暂停游戏。发布 GamePauseEvent，暂停状态机。
        /// </summary>
        public void PauseGame()
        {
            if (State.IsPaused || State.IsGameOver)
                return;

            State.IsPaused = true;
            GameStateMachine.Instance.SetPaused(true);
            EventBus.Instance.Publish(new GamePauseEvent());

            Debug.Log("[GameManager] 游戏暂停");
        }

        /// <summary>
        /// 恢复游戏。发布 GameResumeEvent，恢复状态机。
        /// </summary>
        public void ResumeGame()
        {
            if (!State.IsPaused || State.IsGameOver)
                return;

            State.IsPaused = false;
            GameStateMachine.Instance.SetPaused(false);
            EventBus.Instance.Publish(new GameResumeEvent());

            Debug.Log("[GameManager] 游戏恢复");
        }

        /// <summary>
        /// 推进到下一关。
        /// 轮次 1→2→3 循环，轮次 3 结束后进入下一节。
        /// 节 1→2→3→4 循环，节 4 结束后进入下一章。
        /// 章 3 轮次 3 结束 → EndGame(true)（胜利）。
        /// </summary>
        public void AdvanceToNextLevel()
        {
            GameProgress p = State.Progress;

            // 章 3 轮次 3 结束 → 胜利
            if (p.Chapter >= 3 && p.Section >= 4 && p.Round >= 3)
            {
                EndGame(true);
                return;
            }

            p.Round++;
            if (p.Round > 3)
            {
                p.Round = 1;
                p.Section++;
                if (p.Section > 4)
                {
                    p.Section = 1;
                    p.Chapter++;
                }
            }

            Debug.Log($"[GameManager] 关卡推进 → {p.Chapter}-{p.Section}-{p.Round} (全局: {p.GlobalRound})");
        }
    }
}
