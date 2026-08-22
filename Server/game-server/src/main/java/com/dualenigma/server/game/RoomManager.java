package com.dualenigma.server.game;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.HeartbeatManager;
import com.dualenigma.network.RespSender;
import com.dualenigma.network.protocol.NetErrorCode;
import com.dualenigma.server.logic.ConflictResolver;
import com.dualenigma.server.logic.FragmentPlanner;
import com.dualenigma.server.util.IdGenerator;
import com.dualenigma.v1.Envelope;
import com.dualenigma.v1.S2C_ConnectAck;
import com.dualenigma.v1.S2C_GameStart;
import com.dualenigma.v1.S2C_PlayerJoined;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

import java.util.Queue;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.concurrent.ConcurrentMap;

/**
 * 房间池管理（创建/销毁/查询/匹配）.
 * 加入成功后向玩家发送 S2C_ConnectAck（含 playerId + roomCode），
 * 房主把 roomCode 通过 account-server 邀请好友，实现好友开房.
 */
@Component
public class RoomManager implements HeartbeatManager.DisconnectListener {

    private static final Logger log = LoggerFactory.getLogger(RoomManager.class);

    private final ConcurrentMap<String, GameRoom> rooms = new ConcurrentHashMap<>();
    private final Queue<GameRoom> waitingQueue = new ConcurrentLinkedQueue<>();
    private final FragmentPlanner fragmentPlanner;
    private final ConflictResolver conflictResolver;
    private final OnlineRegistry onlineRegistry;
    private final HeartbeatManager heartbeatManager;
    private final RespSender respSender;

    public RoomManager(FragmentPlanner fragmentPlanner,
                       ConflictResolver conflictResolver, OnlineRegistry onlineRegistry,
                       HeartbeatManager heartbeatManager, RespSender respSender) {
        this.fragmentPlanner = fragmentPlanner;
        this.conflictResolver = conflictResolver;
        this.onlineRegistry = onlineRegistry;
        this.heartbeatManager = heartbeatManager;
        this.respSender = respSender;
    }

    @PostConstruct
    public void init() {
        // 会话断开 → 房间断线处理（释放席位/通知对方/空房回收）
        heartbeatManager.addDisconnectListener(this);
    }

    /**
     * 会话断开回调（HeartbeatManager 触发）.
     * 找到会话所在房间 → 释放席位并通知对方；大厅房清空则立即回收.
     */
    @Override
    public void onDisconnect(ClientSession session) {
        String roomCode = session.getRoomCode();
        if (roomCode == null || roomCode.isEmpty()) return;

        GameRoom room = rooms.get(roomCode);
        if (room == null) return;

        room.onPlayerDisconnect(session.getPlayerId());

        // 大厅房间人走空 → 立即回收（防止空房残留在匹配队列）
        if (!room.isGameStarted() && room.getPlayerCount() <= 0) {
            rooms.remove(roomCode);
            waitingQueue.remove(room);
            log.info("Empty lobby room {} removed", roomCode);
        }
    }

    /**
     * 玩家连接 → 匹配房间.
     * 指定 roomCode（好友联机）加入失败时不回退自动匹配，避免被邀请人掉进陌生人房间.
     *
     * @return NetErrorCode 码：0 成功（成功回执已随 ConnectAck 前发出）；
     *         2001 房间不存在 / 2002 已满 / 2003 已开局
     */
    public int onPlayerConnect(ClientSession session, String roomCode, int reqId) {
        if (roomCode != null && !roomCode.isEmpty()) {
            // 指定 roomCode — 好友联机
            GameRoom room = rooms.get(roomCode);
            if (room == null) {
                log.warn("Join room by code failed, roomCode={} not found", roomCode);
                return NetErrorCode.ROOM_NOT_FOUND;
            }
            if (room.getPlayerCount() >= 2) {
                log.warn("Join room by code failed, roomCode={} is full", roomCode);
                return NetErrorCode.ROOM_FULL;
            }
            if (room.isGameStarted()) {
                log.warn("Join room by code failed, roomCode={} already started", roomCode);
                return NetErrorCode.GAME_STARTED;
            }

            // 好友房从自动匹配队列移除，防止陌生人在好友加入前插队
            waitingQueue.remove(room);
            return addPlayerToRoom(room, session, reqId);
        }

        // 自动匹配 — 从等待队列取
        GameRoom waitingRoom = waitingQueue.poll();
        if (waitingRoom != null) {
            return addPlayerToRoom(waitingRoom, session, reqId);
        }

        // 创建新房间（随机码，撞码重试）
        String newCode = IdGenerator.nextRoomCode();
        while (rooms.containsKey(newCode)) {
            newCode = IdGenerator.nextRoomCode();
        }
        GameRoom newRoom = new GameRoom(newCode, fragmentPlanner, conflictResolver);
        rooms.put(newCode, newRoom);
        int code = addPlayerToRoom(newRoom, session, reqId);
        waitingQueue.add(newRoom);
        return code;
    }

    /**
     * 加入房间（成功时先 resp(0) 再 ConnectAck，保证回执先于领域消息到达）.
     *
     * @return NetErrorCode 码：0 成功 / 2002 并发满员
     */
    private int addPlayerToRoom(GameRoom room, ClientSession session, int reqId) {
        if (!room.addPlayer(session)) {
            log.warn("Room {} is full, reject session {}", room.getRoomCode(), session.getSessionId());
            return NetErrorCode.ROOM_FULL;
        }

        // 已识别身份的玩家注册在线状态（组队中；匿名会话内部忽略）
        onlineRegistry.register(session);

        log.info("Player joined room {} (count={})", room.getRoomCode(), room.getPlayerCount());

        // 成功回执紧邻 ConnectAck 之前（Handler 层不再对成功路径回执）
        respSender.reply(session, reqId, NetErrorCode.OK);
        sendConnectAck(session, session.getPlayerId(), room.getRoomCode());

        // 通知房间内全体玩家：有人进房（房主据此点亮"开始对局"按钮）
        broadcastPlayerJoined(room, session.getPlayerId());
        return NetErrorCode.OK;
    }

    /**
     * 广播玩家进房通知到房间内全部玩家.
     */
    private void broadcastPlayerJoined(GameRoom room, int joinedPlayerId) {
        Envelope env = Envelope.newBuilder()
                .setPlayerId(-1)
                .setTimestamp(System.currentTimeMillis())
                .setPlayerJoined(S2C_PlayerJoined.newBuilder()
                        .setPlayerId(joinedPlayerId)
                        .setPlayerCount(room.getPlayerCount()))
                .build();
        room.broadcastToAll(env);
    }

    /**
     * 房主请求开始对局：校验房主身份 + 满员，通过后启动并广播 GameStart.
     *
     * @return NetErrorCode 码：0 成功 / 2001 房间不存在 / 3001 非房主 / 3002 未满员 / 3003 已开局
     */
    public int requestStart(ClientSession session) {
        GameRoom room = rooms.get(session.getRoomCode());
        if (room == null) {
            log.warn("Start request rejected: session {} room not found", session.getSessionId());
            return NetErrorCode.ROOM_NOT_FOUND;
        }
        if (session.getPlayerId() != 0) {
            log.warn("Start request in room {} rejected: player {} is not host",
                    room.getRoomCode(), session.getPlayerId());
            return NetErrorCode.NOT_HOST;
        }
        if (room.getPlayerCount() < 2) {
            log.warn("Start request in room {} rejected: room not full", room.getRoomCode());
            return NetErrorCode.NOT_FULL;
        }
        if (room.isGameStarted()) {
            return NetErrorCode.ALREADY_STARTED;
        }

        room.startGame();
        onlineRegistry.markInGame(OnlineRegistry.collectAccountIds(room.getPlayers()));
        broadcastGameStart(room);
        return NetErrorCode.OK;
    }

    /**
     * 广播开局消息到房间内全部玩家（第 1-1-1 轮起）.
     */
    private void broadcastGameStart(GameRoom room) {
        Envelope env = Envelope.newBuilder()
                .setPlayerId(-1)
                .setTimestamp(System.currentTimeMillis())
                .setGameStart(S2C_GameStart.newBuilder()
                        .setChapter(1)
                        .setSection(1)
                        .setRound(1))
                .build();
        room.broadcastToAll(env);
        log.info("Room {} full → GameStart broadcast to both players", room.getRoomCode());
    }

    /**
     * 发送连接确认（playerId + roomCode），客户端凭 roomCode 邀请好友或展示房间码.
     */
    private void sendConnectAck(ClientSession session, int playerId, String roomCode) {
        Envelope env = Envelope.newBuilder()
                .setPlayerId(-1)
                .setTimestamp(System.currentTimeMillis())
                .setConnectAck(S2C_ConnectAck.newBuilder()
                        .setPlayerId(playerId)
                        .setRoomCode(roomCode))
                .build();
        session.send(env.toByteArray());
    }

    /**
     * 获取房间.
     */
    public GameRoom getRoom(String roomCode) {
        return rooms.get(roomCode);
    }

    /**
     * 清理过期房间（每 60 秒执行一次）：
     * 开局后结束的房间 + 未开局超过 10 分钟的大厅房间（含等待队列中的残留）.
     */
    @Scheduled(fixedRate = 60000)
    public void cleanupExpiredRooms() {
        rooms.entrySet().removeIf(entry -> {
            if (entry.getValue().isExpired()) {
                waitingQueue.remove(entry.getValue());
                log.info("Cleaning up expired room: {}", entry.getKey());
                return true;
            }
            return false;
        });
    }
}
