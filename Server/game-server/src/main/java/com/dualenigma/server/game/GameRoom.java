package com.dualenigma.server.game;

import com.dualenigma.network.ClientSession;
import com.dualenigma.server.logic.ConflictResolver;
import com.dualenigma.server.logic.FragmentPlanner;
import com.dualenigma.v1.C2S_HighFreqState;
import com.dualenigma.v1.Envelope;
import com.dualenigma.v1.S2C_HighFreqState;
import com.dualenigma.v1.S2C_OpponentDisconnect;
import com.dualenigma.v1.Vec2;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * 房间管理（2 人匹配 + 会话持有）.
 * 每个房间持有一局游戏的完整状态；广播走 proto Envelope 二进制帧.
 */
public class GameRoom {

    private static final Logger log = LoggerFactory.getLogger(GameRoom.class);

    private final String roomCode;
    private final ClientSession[] players = new ClientSession[2];
    private final GameManager gameManager;
    private final long createdAt = System.currentTimeMillis();
    private int playerCount = 0;
    private boolean gameStarted = false;

    /** 未开局房间的最大存活时间（毫秒），超时由 RoomManager 回收 */
    private static final long LOBBY_TIMEOUT_MS = 10 * 60 * 1000L;

    public GameRoom(String roomCode, FragmentPlanner fragmentPlanner, ConflictResolver conflictResolver) {
        this.roomCode = roomCode;
        this.gameManager = new GameManager(this, fragmentPlanner, conflictResolver);
    }

    /**
     * 向房间内全部在线玩家广播 proto Envelope.
     */
    public void broadcastToAll(Envelope env) {
        byte[] payload = env.toByteArray();
        for (ClientSession player : players) {
            if (player != null) {
                player.send(payload);
            }
        }
    }

    /**
     * 玩家加入房间.
     * @return true 如果加入成功
     */
    public boolean addPlayer(ClientSession session) {
        if (playerCount >= 2) return false;

        int playerId = playerCount;
        session.setPlayerId(playerId);
        session.setRoomCode(roomCode);
        players[playerId] = session;
        playerCount++;

        // 开局由房主显式发起（C2S_StartGame），不再满员自动开始
        return true;
    }

    /**
     * 玩家断线（WebSocket 关闭/心跳超时，由 RoomManager 回调）.
     * 大厅阶段：释放席位并通知对方（对方客户端将开始按钮置灰，房主可重新邀请）.
     * 对局阶段：保留席位（重连窗口/AI 接管归断线重连里程碑），仅通知对方.
     */
    public void onPlayerDisconnect(int playerId) {
        if (playerId < 0 || playerId > 1) return;
        if (players[playerId] == null) return; // 未入房或已处理

        players[playerId] = null;
        log.info("Player {} disconnected from room {} (started={})", playerId, roomCode, gameStarted);

        if (!gameStarted) {
            // 大厅阶段释放席位，房主可再邀请新好友补位
            playerCount--;
        }

        // lobby=大厅离开（重置开始按钮）；waiting=对局中断线（等待重连/AI 接管）
        Envelope env = Envelope.newBuilder()
                .setPlayerId(playerId)          // 离开者走信封 player_id
                .setTimestamp(System.currentTimeMillis())
                .setOpponentDisconnect(S2C_OpponentDisconnect.newBuilder()
                        .setState(gameStarted ? "waiting" : "lobby"))
                .build();
        broadcastToAll(env);
    }

    /** 房间内两名玩家的会话（元素可能为 null，供满员广播使用） */
    public ClientSession[] getPlayers() {
        return players;
    }

    /**
     * 玩家重连.
     */
    public boolean onPlayerReconnect(ClientSession session, int playerId) {
        if (playerId < 0 || playerId > 1) return false;
        players[playerId] = session;
        session.setPlayerId(playerId);
        session.setRoomCode(roomCode);
        log.info("Player {} reconnected to room {}", playerId, roomCode);

        // 发送全量快照
        gameManager.createSnapshot();
        // TODO: 发送 S2C_ReconnectSnapshot
        return true;
    }

    /**
     * 启动游戏（由 RoomManager 在房主请求开局并校验通过后调用）.
     */
    public void startGame() {
        gameStarted = true;
        gameManager.start();
        log.info("Game started in room {}", roomCode);
    }

    // --- 事件代理方法 ---

    public void forwardHighFreqState(int playerId, C2S_HighFreqState state) {
        if (playerId < 0 || playerId > 1) return;
        ClientSession target = players[1 - playerId];
        if (target == null) return;

        // 入库权威快照（供 10Hz 中频广播与重连快照使用）
        gameManager.updatePlayerHighFreq(playerId, state);

        // C2S 与 S2C 高频是两个 proto 消息类型，逐字段拷贝转发
        S2C_HighFreqState fwd = S2C_HighFreqState.newBuilder()
                .setPlayerId(playerId)
                .setPosition(Vec2.newBuilder()
                        .setX(state.getPosition().getX())
                        .setY(state.getPosition().getY()))
                .setVelocity(Vec2.newBuilder()
                        .setX(state.getVelocity().getX())
                        .setY(state.getVelocity().getY()))
                .setAnimState(state.getAnimState())
                .setFacing(state.getFacing())
                .setHp(state.getHp())
                .setShelterEnergy(state.getShelterEnergy())
                .build();

        Envelope env = Envelope.newBuilder()
                .setPlayerId(playerId)
                .setTimestamp(System.currentTimeMillis())
                .setHighFreqStateS2C(fwd)
                .build();
        target.send(env.toByteArray());
    }

    /**
     * 碎片接住上报 → 几何仲裁 + 记账（GameManager），返回 NetErrorCode 码供 Handler 层回执.
     */
    public int onFragmentCaught(int playerId, int fragmentId, float fragX, float fragY) {
        return gameManager.onFragmentCaught(playerId, fragmentId, fragX, fragY);
    }

    public void onBuildingPlace(int playerId, int buildingType, int material, int gridX, int gridY) {
        // TODO: BuildingManager.place()
    }

    public void onBuildingRemove(int playerId, int buildingId) {
        // TODO: BuildingManager.remove()
    }

    public void onSynthesize(int playerId, int[] fragmentIds) {
        // TODO: SynthesisValidator.validate()
    }

    public void onSkillActivate(int playerId, int skillId, float targetX, float targetY) {
        // TODO: SkillExecutor.execute()
    }

    public void onTalentSelect(int playerId, int talentId) {
        // TODO: TalentPool.select()
    }

    /**
     * 向指定玩家发送消息.
     */
    public void sendToPlayer(int playerId, Envelope env) {
        if (playerId < 0 || playerId > 1) return;
        ClientSession target = players[playerId];
        if (target != null) {
            target.send(env.toByteArray());
        }
    }

    /**
     * 向双方广播.
     */
    public void broadcast(Envelope env) {
        sendToPlayer(0, env);
        sendToPlayer(1, env);
    }

    /**
     * 房间是否可销毁：
     * 开局后状态机停止（对局结束），或未开局超过 10 分钟（等人超时/房主离开）.
     */
    public boolean isExpired() {
        if (gameStarted) {
            return !gameManager.getStateMachine().isRunning();
        }
        return System.currentTimeMillis() - createdAt > LOBBY_TIMEOUT_MS;
    }

    // --- Getters ---

    public String getRoomCode() { return roomCode; }
    public GameManager getGameManager() { return gameManager; }
    public int getPlayerCount() { return playerCount; }
    public boolean isGameStarted() { return gameStarted; }
}
