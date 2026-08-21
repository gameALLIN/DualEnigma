package com.dualenigma.server.game;

import com.dualenigma.network.protocol.GamePhase;
import com.dualenigma.network.protocol.s2c.S2C_PhaseChange;
import com.dualenigma.server.util.Constants;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * 权威阶段计时器（7 阶段流转）.
 * 服务器独占阶段推进，客户端被动接收 S2C_PhaseChange.
 */
public class GameStateMachine {

    private static final Logger log = LoggerFactory.getLogger(GameStateMachine.class);

    private GamePhase currentPhase = GamePhase.Preview;
    private long phaseEndTime;
    private boolean running = false;
    private final GameRoom room;

    public GameStateMachine(GameRoom room) {
        this.room = room;
    }

    /**
     * 启动状态机.
     */
    public void start() {
        running = true;
        setPhase(GamePhase.Preview);
    }

    /**
     * 每个逻辑 Tick 调用.
     */
    public void onTick(long currentTime) {
        if (!running) return;
        if (currentTime >= phaseEndTime) {
            nextPhase();
        }
    }

    /**
     * 切换到下一阶段.
     */
    private void nextPhase() {
        GamePhase[] phases = GamePhase.values();
        int nextIndex = (currentPhase.ordinal() + 1) % phases.length;

        // 7 阶段循环完成 = 一轮结束
        if (nextIndex == 0) {
            if (!room.getGameManager().advanceRound()) {
                log.info("All 36 rounds completed, game finished");
                stop();
                return;
            }
            log.info("Round completed, advancing to next round");
        }
        setPhase(phases[nextIndex]);
    }

    /**
     * 设置当前阶段并广播.
     */
    private void setPhase(GamePhase phase) {
        currentPhase = phase;
        phaseEndTime = System.currentTimeMillis() + Constants.getPhaseDurationMs(phase);

        // 构建广播消息
        S2C_PhaseChange change = new S2C_PhaseChange();
        change.setPlayerId(-1);
        change.setTimestamp(System.currentTimeMillis());
        change.getData().setPhase(phase);
        change.getData().setDurationMs((int) Constants.getPhaseDurationMs(phase));
        change.getData().setPhaseEndTime(phaseEndTime);
        room.broadcastToAll(change);

        // 阶段进入钩子（在 PhaseChange 广播之后，保证客户端先知道阶段再收阶段内容）
        room.getGameManager().onPhaseEnter(phase);

        log.info("Phase changed to: {} (duration={}ms)", phase, Constants.getPhaseDurationMs(phase));
    }

    public GamePhase getCurrentPhase() { return currentPhase; }
    public long getPhaseEndTime() { return phaseEndTime; }
    public boolean isRunning() { return running; }

    public void stop() { running = false; }
}
