package com.dualenigma.server.game;

import com.dualenigma.network.model.GameSnapshot;
import com.dualenigma.network.model.PlayerState;
import com.dualenigma.network.protocol.GamePhase;
import com.dualenigma.network.protocol.NetErrorCode;
import com.dualenigma.server.logic.ConflictResolver;
import com.dualenigma.server.logic.FragmentPlanner;
import com.dualenigma.server.util.Constants;
import com.dualenigma.v1.C2S_HighFreqState;
import com.dualenigma.v1.Envelope;
import com.dualenigma.v1.S2C_FragmentDropPlan;
import com.dualenigma.v1.S2C_FragmentResult;
import com.dualenigma.v1.S2C_MidFreqState;
import com.dualenigma.v1.Vec2;
import com.dualenigma.network.model.FragmentDropPlan;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * 单局游戏管理（权威状态机驱动）.
 * 每个房间持有一个实例，管理一局完整游戏的生命周期；广播走 proto Envelope 二进制帧.
 */
public class GameManager {

    private static final Logger log = LoggerFactory.getLogger(GameManager.class);

    private final GameRoom room;
    private final GameStateMachine stateMachine;
    private final GameTickScheduler tickScheduler;
    private final FragmentPlanner fragmentPlanner;
    private final ConflictResolver conflictResolver;

    // 游戏进度
    private int chapter = 1;
    private int section = 1;
    private int round = 1;
    private int score = 0;

    // 本轮掉落计划：fragmentId → 碎片类型（接住记账与合法性校验依据）
    private final Map<Integer, Integer> roundFragmentTypes = new HashMap<>();
    // 已完成判定的碎片（防重复上报/迟到包重复广播）
    private final Set<Integer> resolvedFragments = new HashSet<>();

    // 服务端权威记账：各玩家携带的碎片类型列表（索引 = playerId）
    private final List<List<Integer>> carriedFragments = List.of(new ArrayList<>(), new ArrayList<>());

    // 中频广播计数（每 2 个 Tick = 10Hz）
    private int tickCounter = 0;

    // 玩家状态
    private final PlayerState[] players = new PlayerState[2];

    public GameManager(GameRoom room, FragmentPlanner fragmentPlanner, ConflictResolver conflictResolver) {
        this.room = room;
        this.fragmentPlanner = fragmentPlanner;
        this.conflictResolver = conflictResolver;
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
     * Preview 阶段钩子（onPhaseEnter）负责生成并广播每轮的碎片掉落计划.
     */
    public void start() {
        stateMachine.start();
        tickScheduler.start(this);
        log.info("Game started in room {}", room.getRoomCode());
    }

    /**
     * 阶段进入钩子（由 GameStateMachine 在广播 PhaseChange 之后调用）.
     */
    public void onPhaseEnter(GamePhase phase) {
        switch (phase) {
            case Preview -> generateAndBroadcastPlan();
            // 其余阶段的玩法逻辑（灾难模拟/建筑同步/天赋推送）在对应里程碑接入
            default -> { }
        }
    }

    /**
     * 一轮 7 阶段结束时推进全局进度（3 章 × 4 节 × 3 轮）.
     *
     * @return false 表示 36 轮全部完成
     */
    public boolean advanceRound() {
        if (round < Constants.ROUNDS_PER_SECTION) {
            round++;
            return true;
        }
        round = 1;
        if (section < Constants.SECTIONS_PER_CHAPTER) {
            section++;
            return true;
        }
        section = 1;
        if (chapter < Constants.TOTAL_CHAPTERS) {
            chapter++;
            return true;
        }
        return false;
    }

    /**
     * 生成种子化碎片掉落计划并广播双方（双方各自模拟物理掉落）.
     * 每轮 Preview 进入时调用，seed 每轮变化.
     */
    private void generateAndBroadcastPlan() {
        roundFragmentTypes.clear();
        resolvedFragments.clear();

        // TODO(disaster): category/density 接入 DisasterSelector（灾难里程碑实现，当前固定 0/1.0）
        long seed = System.nanoTime();
        List<FragmentDropPlan> plan = fragmentPlanner.generatePlan(0, 1.0f, seed);
        for (FragmentDropPlan p : plan) {
            roundFragmentTypes.put(p.getFragmentId(), p.getType());
        }

        S2C_FragmentDropPlan.Builder body = S2C_FragmentDropPlan.newBuilder();
        for (FragmentDropPlan p : plan) {
            body.addPlan(S2C_FragmentDropPlan.PlanItem.newBuilder()
                    .setFragmentId(p.getFragmentId())
                    .setType(p.getType())
                    .setPosition(Vec2.newBuilder().setX(p.getPosX()).setY(p.getPosY()))
                    .setDropTime(p.getDropTime())
                    .setSeed(p.getSeed()));
        }

        Envelope env = Envelope.newBuilder()
                .setPlayerId(-1)
                .setTimestamp(System.currentTimeMillis())
                .setFragmentDropPlan(body)
                .build();
        room.broadcastToAll(env);
        log.info("Fragment plan broadcast: {} items (round {}-{}-{})", plan.size(), chapter, section, round);
    }

    /**
     * 逻辑帧回调 (20Hz).
     */
    public void onTick() {
        long currentTime = System.currentTimeMillis();
        stateMachine.onTick(currentTime);

        // TODO: ShelterCalculator.update(players[0], players[1], deltaTime)
        // TODO: DamageCalculator.update (DisasterImpact phase only)
        // TODO: AIController.onTick (if AI takeover active)

        // 10Hz 中频快照广播（20Hz 逻辑帧每 2 Tick 一次）
        tickCounter++;
        if (tickCounter % 2 == 0) {
            broadcastMidFreqState();
        }
    }

    /**
     * 玩家上报接住碎片 → 几何仲裁（即时判定，无等待窗口）.
     * 以碎片位置与双方玩家位置的权威快照判定单独/同时接住，
     * 与上报到达时序无关，免疫延迟与抖动.
     *
     * @return NetErrorCode 码：0 成功（含过期/重复上报幂等回 0）/
     *         4002 上报者不在判定半径或 playerId 越界（防呆）
     */
    public int onFragmentCaught(int playerId, int fragmentId, float fragX, float fragY) {
        if (playerId < 0 || playerId > 1) {
            return NetErrorCode.FRAGMENT_REJECTED;
        }
        if (!roundFragmentTypes.containsKey(fragmentId) || resolvedFragments.contains(fragmentId)) {
            // 过期/重复上报：预期竞态，回执层面幂等成功（不广播 FragmentResult 的静默语义保留）
            log.debug("Ignored stale/duplicate catch: player {} fragment {}", playerId, fragmentId);
            return NetErrorCode.OK;
        }

        PlayerState reporter = players[playerId];
        PlayerState other = players[1 - playerId];
        ConflictResolver.FragmentCatchResult result = conflictResolver.judge(
                fragmentId, playerId, 1 - playerId,
                fragX, fragY,
                reporter.getPosX(), reporter.getPosY(),
                other.getPosX(), other.getPosY());

        if (result == null) {
            log.warn("Rejected catch (reporter not in radius): player {} fragment {} at ({},{}), player at ({},{})",
                    playerId, fragmentId, fragX, fragY, reporter.getPosX(), reporter.getPosY());
            return NetErrorCode.FRAGMENT_REJECTED;
        }
        resolveCatch(result);
        return NetErrorCode.OK;
    }

    /**
     * 落定接住判定：记账 + 广播结果双方.
     * 单独接住 = 1 个；同时接住 = 双方各得 2 个（翻倍）.
     */
    private void resolveCatch(ConflictResolver.FragmentCatchResult result) {
        resolvedFragments.add(result.fragmentId());
        int type = roundFragmentTypes.getOrDefault(result.fragmentId(), 0);

        if (result.isSimultaneous()) {
            addCarried(result.winnerPlayerId(), type, 2);
            addCarried(result.secondPlayerId(), type, 2);
            log.info("Fragment {} simultaneous catch in room {}: both players gain x2 (type={})",
                    result.fragmentId(), room.getRoomCode(), type);
        } else {
            addCarried(result.winnerPlayerId(), type, 1);
            log.info("Fragment {} caught by player {} in room {} (type={})",
                    result.fragmentId(), result.winnerPlayerId(), room.getRoomCode(), type);
        }

        Envelope env = Envelope.newBuilder()
                .setPlayerId(-1)
                .setTimestamp(System.currentTimeMillis())
                .setFragmentResult(S2C_FragmentResult.newBuilder()
                        .setFragmentId(result.fragmentId())
                        .setPlayerId(result.winnerPlayerId())
                        .setMultiplier(result.multiplier())
                        .setIsSimultaneous(result.isSimultaneous()))
                .build();
        room.broadcastToAll(env);
    }

    /**
     * 服务端记账：向玩家背包加入 count 个指定类型碎片，并同步到权威快照.
     */
    private void addCarried(int playerId, int type, int count) {
        if (playerId < 0 || playerId > 1) return;
        List<Integer> list = carriedFragments.get(playerId);
        for (int i = 0; i < count; i++) {
            list.add(type);
        }
        players[playerId].setCarriedFragments(list.stream().mapToInt(Integer::intValue).toArray());
    }

    /**
     * 记录客户端上报的高频状态到权威快照（供中频广播与重连快照使用）.
     */
    public void updatePlayerHighFreq(int playerId, C2S_HighFreqState state) {
        if (playerId < 0 || playerId > 1) return;
        PlayerState p = players[playerId];
        if (state.hasPosition()) {
            p.setPosX(state.getPosition().getX());
            p.setPosY(state.getPosition().getY());
        }
        if (state.hasVelocity()) {
            p.setVelocityX(state.getVelocity().getX());
            p.setVelocityY(state.getVelocity().getY());
        }
        p.setAnimState(state.getAnimState());
        p.setFacing(state.getFacing());
        p.setHp(state.getHp());
        p.setShelterEnergy(state.getShelterEnergy());
    }

    /**
     * 10Hz 广播双方 HP/能量/携带碎片快照.
     */
    private void broadcastMidFreqState() {
        S2C_MidFreqState.Builder body = S2C_MidFreqState.newBuilder();
        for (PlayerState p : players) {
            S2C_MidFreqState.PlayerMidFreq.Builder m = S2C_MidFreqState.PlayerMidFreq.newBuilder()
                    .setPlayerId(p.getPlayerId())
                    .setHp(p.getHp())
                    .setShelterEnergy(p.getShelterEnergy());   // proto 直接 float（JSON 时代 Math.round 成 int）
            if (p.getCarriedFragments() != null) {
                for (int type : p.getCarriedFragments()) {
                    m.addCarriedFragments(type);
                }
            }
            body.addPlayers(m);
        }

        Envelope env = Envelope.newBuilder()
                .setPlayerId(-1)
                .setTimestamp(System.currentTimeMillis())
                .setMidFreqState(body)
                .build();
        room.broadcastToAll(env);
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
