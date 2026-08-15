package com.dualenigma.server.game;

import com.dualenigma.network.model.GameSnapshot;
import com.dualenigma.network.model.PlayerState;
import com.dualenigma.network.protocol.c2s.C2S_HighFreqState;
import com.dualenigma.network.protocol.s2c.S2C_FragmentDropPlan;
import com.dualenigma.network.protocol.s2c.S2C_MidFreqState;
import com.dualenigma.server.logic.FragmentPlanner;
import com.dualenigma.network.model.FragmentDropPlan;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
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
    private final FragmentPlanner fragmentPlanner;

    // 游戏进度
    private int chapter = 1;
    private int section = 1;
    private int round = 1;
    private int score = 0;

    // 中频广播计数（每 2 个 Tick = 10Hz）
    private int tickCounter = 0;

    // 玩家状态
    private final PlayerState[] players = new PlayerState[2];

    public GameManager(GameRoom room, FragmentPlanner fragmentPlanner) {
        this.room = room;
        this.fragmentPlanner = fragmentPlanner;
        this.stateMachine = new GameStateMachine(this.room);
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
        broadcastFragmentPlan();
        log.info("Game started in room {}", room.getRoomCode());
    }

    /**
     * 开局生成种子化碎片掉落计划并广播双方（双方各自模拟物理掉落）.
     */
    private void broadcastFragmentPlan() {
        List<FragmentDropPlan> plan = fragmentPlanner.generatePlan(0, 1.0f, System.currentTimeMillis());
        S2C_FragmentDropPlan msg = new S2C_FragmentDropPlan();
        msg.setTimestamp(System.currentTimeMillis());
        List<S2C_FragmentDropPlan.FragmentDropItem> items = new ArrayList<>();
        for (FragmentDropPlan p : plan) {
            S2C_FragmentDropPlan.FragmentDropItem item = new S2C_FragmentDropPlan.FragmentDropItem();
            item.setFragmentId(p.getFragmentId());
            item.setType(p.getType());
            S2C_FragmentDropPlan.Vec2 pos = new S2C_FragmentDropPlan.Vec2();
            pos.setX(p.getPosX());
            pos.setY(p.getPosY());
            item.setPosition(pos);
            item.setDropTime(p.getDropTime());
            item.setSeed(p.getSeed());
            items.add(item);
        }
        msg.getData().setPlan(items);
        room.broadcastToAll(msg);
        log.info("Fragment plan broadcast: {} items", items.size());
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
        // TODO: AIController.onTick (if AI takeover active)

        // 10Hz 中频快照广播（20Hz 逻辑帧每 2 Tick 一次）
        tickCounter++;
        if (tickCounter % 2 == 0) {
            broadcastMidFreqState();
        }
    }

    /**
     * 记录客户端上报的高频状态到权威快照（供中频广播与重连快照使用）.
     */
    public void updatePlayerHighFreq(int playerId, C2S_HighFreqState state) {
        if (playerId < 0 || playerId > 1) return;
        PlayerState p = players[playerId];
        if (state.getData().getPosition() != null) {
            p.setPosX(state.getData().getPosition().getX());
            p.setPosY(state.getData().getPosition().getY());
        }
        if (state.getData().getVelocity() != null) {
            p.setVelocityX(state.getData().getVelocity().getX());
            p.setVelocityY(state.getData().getVelocity().getY());
        }
        p.setAnimState(state.getData().getAnimState());
        p.setFacing(state.getData().isFacing());
        p.setHp(state.getData().getHp());
        p.setShelterEnergy(state.getData().getShelterEnergy());
    }

    /**
     * 10Hz 广播双方 HP/能量/携带碎片快照.
     */
    private void broadcastMidFreqState() {
        S2C_MidFreqState msg = new S2C_MidFreqState();
        msg.setTimestamp(System.currentTimeMillis());
        List<S2C_MidFreqState.PlayerMidFreq> list = new ArrayList<>();
        for (PlayerState p : players) {
            S2C_MidFreqState.PlayerMidFreq m = new S2C_MidFreqState.PlayerMidFreq();
            m.setPlayerId(p.getPlayerId());
            m.setHp(p.getHp());
            m.setShelterEnergy(Math.round(p.getShelterEnergy()));
            m.setCarriedFragments(p.getCarriedFragments() != null ? p.getCarriedFragments() : new int[0]);
            list.add(m);
        }
        msg.getData().setPlayers(list);
        room.broadcastToAll(msg);
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
