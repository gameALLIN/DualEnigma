package com.dualenigma.server.util;

import com.dualenigma.network.protocol.GamePhase;

import java.util.Map;

/**
 * 服务器常量定义（阶段时长/频率/阈值/网格参数）.
 */
public final class Constants {

    private Constants() {}

    // ─── 阶段时长 (ms) ───
    private static final Map<GamePhase, Long> PHASE_DURATIONS = Map.of(
        GamePhase.Preview,          5000L,
        GamePhase.FragmentCollect, 15000L,
        GamePhase.DisasterPreview,  5000L,
        GamePhase.Build,            20000L,
        GamePhase.DisasterImpact,   20000L,
        GamePhase.Rest,             10000L,
        GamePhase.Upgrade,           15000L
    );

    /**
     * 获取阶段时长.
     */
    public static long getPhaseDurationMs(GamePhase phase) {
        return PHASE_DURATIONS.getOrDefault(phase, 5000L);
    }

    // ─── 逻辑帧 ───
    public static final int TICK_RATE_HZ = 20;         // 逻辑帧率
    public static final long TICK_INTERVAL_MS = 50;     // 逻辑帧间隔

    // ─── 同步频率 ───
    public static final int HIGH_FREQ_RATE_HZ = 20;     // 高频状态同步
    public static final int MID_FREQ_RATE_HZ = 10;       // 中频状态同步

    // ─── 心跳 ───
    public static final long HEARTBEAT_INTERVAL_MS = 1000;
    public static final long HEARTBEAT_TIMEOUT_MS = 5000;

    // ─── 断线重连 ───
    public static final long RECONNECT_WINDOW_MS = 30000;
    public static final long AI_TAKEOVER_TIMEOUT_MS = 30000;
    public static final long FINAL_TIMEOUT_MS = 120000;

    // ─── 碎片 ───
    public static final long SIMULTANEOUS_WINDOW_MS = 100;
    public static final float FRAGMENT_DESPAWN_SEC = 3.0f;
    /** 同接几何判定半径（格）：双方玩家距碎片均在此半径内判为同时接住 */
    public static final float FRAGMENT_CATCH_RADIUS = 1.2f;

    // ─── 庇护 ───
    public static final float MAX_SHELTER_ENERGY = 100f;
    public static final float SHELTER_RECOVERY_RATE = 20f;
    public static final float SHELTER_CONSUMPTION_RATE = 33f;
    public static final float SHELTER_DISTANCE = 3f;
    public static final float SHELTER_BUFFER_TIME = 3f;

    // ─── 建筑区域 ───
    public static final int GRID_X_MIN = -7;
    public static final int GRID_X_MAX = 7;
    public static final int GRID_Y_MIN = -3;
    public static final int GRID_Y_MAX = 4;

    // ─── 玩家 ───
    public static final int MAX_HP = 100;

    // ─── 游戏结构 ───
    public static final int TOTAL_CHAPTERS = 3;
    public static final int SECTIONS_PER_CHAPTER = 4;
    public static final int ROUNDS_PER_SECTION = 3;
    public static final int TOTAL_ROUNDS = 36;  // 3 × 4 × 3
}
