/// ============================================================
/// 文件名: GameStateMachine.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 游戏状态机，管理单轮 7 阶段流转。
///       继承 Singleton&lt;T&gt;，Host 驱动阶段切换并同步给 Client。
///       阶段切换时通过 EventBus 发布 PhaseChangedEvent。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.Core;

namespace DualEnigma.Core
{
    /// <summary>
    /// 游戏阶段枚举，对应单轮 90 秒的 7 个阶段。
    /// 引用：核心系统.md §2.1
    /// </summary>
    public enum GamePhase
    {
        /// <summary>① 预告（5秒）：显示灾难类型、庇护环境、建筑蓝图</summary>
        Preview,
        /// <summary>② 碎片收集（15秒）：碎片在建筑区域外掉落</summary>
        FragmentCollect,
        /// <summary>③ 灾害预告（5秒）：灾害方向指示，玩家应返回建筑区域</summary>
        DisasterPreview,
        /// <summary>④ 建造（20秒）：合成材料并按蓝图建造</summary>
        Build,
        /// <summary>⑤ 灾害冲击（20秒）：灾害渐进入侵</summary>
        DisasterImpact,
        /// <summary>⑥ 修整（10秒）：评估损伤、快速修补</summary>
        Rest,
        /// <summary>⑦ 升级（15秒）：天赋选择（3选1）</summary>
        Upgrade,
    }

    /// <summary>
    /// 游戏状态机，管理单轮 7 阶段流转。
    /// 继承 Singleton&lt;T&gt;，Host 驱动阶段切换并同步给 Client。
    /// 阶段切换时通过 EventBus 发布 PhaseChangedEvent。
    /// </summary>
    public class GameStateMachine : Singleton<GameStateMachine>
    {
        /// <summary>各阶段时长（秒），按 GamePhase 枚举顺序排列</summary>
        private static readonly float[] _phaseDurations =
        {
            5f,   // Preview
            15f,  // FragmentCollect
            5f,   // DisasterPreview
            20f,  // Build
            20f,  // DisasterImpact
            10f,  // Rest
            15f,  // Upgrade
        };

        /// <summary>当前阶段</summary>
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Preview;

        /// <summary>阶段剩余时间（秒）</summary>
        public float PhaseRemainingTime { get; private set; }

        /// <summary>是否暂停</summary>
        public bool IsPaused { get; private set; }

        /// <summary>状态机是否正在运行</summary>
        public bool IsRunning { get; private set; }

        /// <summary>是否由服务器驱动阶段（联机模式为 true，本地计时停用）</summary>
        public bool IsNetworkDriven { get; private set; }

        protected override void OnSingletonInitialized()
        {
            Debug.Log("[GameStateMachine] 游戏状态机初始化完成");
        }

        private void Update()
        {
            if (!IsRunning || IsPaused)
                return;

            if (IsNetworkDriven) return; // 联机模式阶段只由 S2C_PhaseChange 驱动

            PhaseRemainingTime -= Time.deltaTime;

            if (PhaseRemainingTime <= 0f)
            {
                NextPhase();
            }
        }

        /// <summary>
        /// 开始新一轮，重置到 Preview 阶段并启动计时器。
        /// </summary>
        public void StartNewRound()
        {
            IsRunning = true;
            IsPaused = false;
            SetPhase(GamePhase.Preview);
        }

        /// <summary>
        /// 切换到下一阶段。Upgrade 后回到 Preview。
        /// </summary>
        public void NextPhase()
        {
            int nextIndex = ((int)CurrentPhase + 1) % _phaseDurations.Length;
            SetPhase((GamePhase)nextIndex);
        }

        /// <summary>
        /// 暂停或恢复状态机。
        /// </summary>
        /// <param name="paused">true 暂停，false 恢复</param>
        public void SetPaused(bool paused)
        {
            IsPaused = paused;
        }

        /// <summary>
        /// 停止状态机运行。
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>
        /// 切换网络驱动模式（联机开局 true / 结束 false）。
        /// </summary>
        public void SetNetworkDriven(bool enabled)
        {
            IsNetworkDriven = enabled;
            IsRunning = enabled;
            if (!enabled) IsPaused = false;
        }

        /// <summary>
        /// 应用服务器下发的阶段（剩余时长已按服务器时钟差值折算）。
        /// </summary>
        public void ApplyServerPhase(GamePhase phase, float remainingSeconds)
        {
            CurrentPhase = phase;
            PhaseRemainingTime = Mathf.Max(0.5f, remainingSeconds);

            EventBus.Instance.Publish(new PhaseChangedEvent { phase = phase });

            Debug.Log($"[GameStateMachine] 服务器阶段 → {phase} (剩余 {PhaseRemainingTime:F1}s)");
        }

        /// <summary>
        /// 设置当前阶段，重置计时器并发布事件。
        /// </summary>
        private void SetPhase(GamePhase phase)
        {
            CurrentPhase = phase;
            PhaseRemainingTime = _phaseDurations[(int)phase];

            EventBus.Instance.Publish(new PhaseChangedEvent { phase = phase });

            Debug.Log($"[GameStateMachine] 阶段切换 → {phase} (时长 {_phaseDurations[(int)phase]}s)");
        }
    }
}
