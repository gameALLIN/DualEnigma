package com.dualenigma.server.game;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;

/**
 * 逻辑帧调度器 (20Hz / 50ms).
 *
 * 每 Tick 执行：
 * 1. GameStateMachine.onTick()        — 阶段计时器推进
 * 2. ShelterCalculator.update()      — 庇护能量计算
 * 3. DamageCalculator.update()       — 伤害计算（仅 DisasterImpact 阶段）
 * 4. ConflictResolver.checkTimeouts() — 碎片接住超时判定
 * 5. 每 5 个 Tick (10Hz)              — 发送 S2C_MidFreqState
 * 6. AIController.onTick() (若需AI接管) — AI 控制
 */
public class GameTickScheduler {

    private static final Logger log = LoggerFactory.getLogger(GameTickScheduler.class);

    private static final long TICK_INTERVAL_MS = 50;  // 20Hz

    private final ScheduledExecutorService scheduler = Executors.newSingleThreadScheduledExecutor();
    private ScheduledFuture<?> tickTask;

    /**
     * 启动逻辑帧循环.
     */
    public void start(GameManager gameManager) {
        tickTask = scheduler.scheduleAtFixedRate(() -> {
            try {
                gameManager.onTick();
            } catch (Exception e) {
                log.error("Error during game tick: {}", e.getMessage(), e);
            }
        }, 0, TICK_INTERVAL_MS, TimeUnit.MILLISECONDS);
        log.info("Game tick scheduler started (20Hz)");
    }

    /**
     * 停止逻辑帧循环.
     */
    public void stop() {
        if (tickTask != null) {
            tickTask.cancel(false);
            tickTask = null;
        }
        log.info("Game tick scheduler stopped");
    }
}
