package com.dualenigma.server.game;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.c2s.C2S_HighFreqState;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * 房间管理（2 人匹配 + 会话持有）.
 * 每个房间持有一局游戏的完整状态.
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

    public GameRoom(String roomCode) {
        this.roomCode = roomCode;
        this.gameManager = new GameManager(this);
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

        if (playerCount == 2) {
            startGame();
        }
        return true;
    }

    /**
     * 玩家断线.
     */
    public void onPlayerDisconnect(int playerId) {
        log.info("Player {} disconnected from room {}", playerId, roomCode);
        // TODO: 通知对方 → 启动重连计时器 → 30s 后 AI 接管 → 120s 超时
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
     * 启动游戏.
     */
    private void startGame() {
        gameStarted = true;
        gameManager.start();
        log.info("Game started in room {}", roomCode);
    }

    // --- 事件代理方法 ---

    public void forwardHighFreqState(int playerId, C2S_HighFreqState state) {
        // TODO: 转发 S2C_HighFreqState 给对方 (opponentId = 1 - playerId)
    }

    public void onFragmentCaught(int playerId, int fragmentId) {
        // TODO: ConflictResolver.onCatch()
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
    public void sendToPlayer(int playerId, Message msg) {
        // TODO: 序列化并发送
    }

    /**
     * 向双方广播.
     */
    public void broadcast(Message msg) {
        sendToPlayer(0, msg);
        sendToPlayer(1, msg);
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
