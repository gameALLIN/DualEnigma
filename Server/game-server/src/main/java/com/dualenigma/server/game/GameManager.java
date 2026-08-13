package com.dualenigma.server.game;

import com.dualenigma.network.model.GameSnapshot;
import com.dualenigma.network.model.PlayerState;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.List;

/**
 * 单局游戏管理（权威状态机驱动）.
 * 每个房间持有一个实例，管理一局完整游戏的生命周期.
 */
public class GameManager {

    private static final Logger log = LoggerFactory.getLogger(GameManager.class);

    private final GameRoom room;
    private final GameStateMachine stateMachine;
    private final GameTickScheduler tickScheduler;

    // 游戏进度
    private int chapter = 1;
    private int section = 1;
    private int round = 1;
    private int score = 0;

    // 玩家状态
    private final PlayerState[] players = new PlayerState[2];

    public GameManager(GameRoom room) {
        this.room = room;
        this.stateMachine = new GameStateMachine();
        this.tickScheduler = new GameTickScheduler();
        players[0] = new PlayerState();
        players[0].setPlayerId(0);
        players[0].setHp(100);
        players[0].setShelterEnergy(100f);
        players[1] = new PlayerState();
        players[1].setPlayerId(1);
        players[1].setHp(100);
        players[1].setShelterEnergy(100f);
    }

    /**
     * 启动游戏.
     */
    public void start() {
        stateMachine.start();
        tickScheduler.start(this);
        log.info("Game started in room {}", room.getRoomCode());
    }

    /**
     * 逻辑帧回调 (20Hz).
     */
    public void onTick() {
        long currentTime = System.currentTimeMillis();
        stateMachine.onTick(currentTime);

        // TODO: ShelterCalculator.update(players[0], players[1], deltaTime)
        // TODO: DamageCalculator.update (DisasterImpact phase only)
        // TODO: ConflictResolver.checkTimeouts()
        // TODO: 每 5 个 Tick 发送 S2C_MidFreqState
        // TODO: AIController.onTick (if AI takeover active)
    }

    /**
     * 停止游戏.
     */
    public void stop() {
        tickScheduler.stop();
        log.info("Game stopped in room {}", room.getRoomCode());
    }

    /**
     * 生成全量快照（重连用）.
     */
    public GameSnapshot createSnapshot() {
        GameSnapshot snapshot = new GameSnapshot();
        snapshot.setChapter(chapter);
        snapshot.setSection(section);
        snapshot.setRound(round);
        snapshot.setCurrentPhase(stateMachine.getCurrentPhase());
        snapshot.setPhaseEndTime(stateMachine.getPhaseEndTime());
        snapshot.setScore(score);
        snapshot.setPlayers(List.of(players[0], players[1]));
        snapshot.setSnapshotTimestamp(System.currentTimeMillis());
        // TODO: 填充 buildings, fragments, talents, skills, disaster
        return snapshot;
    }

    public PlayerState getPlayer(int playerId) {
        return players[playerId];
    }

    public GameStateMachine getStateMachine() { return stateMachine; }
}
