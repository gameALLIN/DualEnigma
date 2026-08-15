package com.dualenigma.server.game;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageCodec;
import com.dualenigma.server.util.IdGenerator;
import com.dualenigma.network.protocol.s2c.S2C_ConnectAck;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.TextMessage;

import java.io.IOException;
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
public class RoomManager {

    private static final Logger log = LoggerFactory.getLogger(RoomManager.class);

    private final ConcurrentMap<String, GameRoom> rooms = new ConcurrentHashMap<>();
    private final Queue<GameRoom> waitingQueue = new ConcurrentLinkedQueue<>();
    private final MessageCodec messageCodec;

    public RoomManager(MessageCodec messageCodec) {
        this.messageCodec = messageCodec;
    }

    /**
     * 玩家连接 → 匹配房间.
     * 指定 roomCode（好友联机）加入失败时不回退自动匹配，避免被邀请人掉进陌生人房间.
     */
    public void onPlayerConnect(ClientSession session, String roomCode) {
        if (roomCode != null && !roomCode.isEmpty()) {
            // 指定 roomCode — 好友联机
            GameRoom room = rooms.get(roomCode);
            if (room != null && room.getPlayerCount() < 2 && !room.isGameStarted()) {
                // 好友房从自动匹配队列移除，防止陌生人在好友加入前插队
                waitingQueue.remove(room);
                addPlayerToRoom(room, session);
            } else {
                // 房间不存在/已满/已开局：不回退匹配，客户端等待超时后可重新邀请
                log.warn("Join room by code failed, roomCode={}, exists={}, count={}, started={}",
                        roomCode, room != null,
                        room != null ? room.getPlayerCount() : -1,
                        room != null && room.isGameStarted());
            }
            return;
        }

        // 自动匹配 — 从等待队列取
        GameRoom waitingRoom = waitingQueue.poll();
        if (waitingRoom != null) {
            addPlayerToRoom(waitingRoom, session);
        } else {
            // 创建新房间
            String newCode = IdGenerator.nextRoomCode();
            GameRoom newRoom = new GameRoom(newCode);
            rooms.put(newCode, newRoom);
            addPlayerToRoom(newRoom, session);
            waitingQueue.add(newRoom);
        }
    }

    private void addPlayerToRoom(GameRoom room, ClientSession session) {
        if (!room.addPlayer(session)) {
            log.warn("Room {} is full, reject session {}", room.getRoomCode(), session.getSessionId());
            return;
        }

        log.info("Player joined room {} (count={})", room.getRoomCode(), room.getPlayerCount());
        sendConnectAck(session, session.getPlayerId(), room.getRoomCode());
    }

    /**
     * 发送连接确认（playerId + roomCode），客户端凭 roomCode 邀请好友或展示房间码.
     */
    private void sendConnectAck(ClientSession session, int playerId, String roomCode) {
        try {
            S2C_ConnectAck ack = new S2C_ConnectAck();
            ack.getData().setPlayerId(playerId);
            ack.getData().setRoomCode(roomCode);
            String json = messageCodec.encode(ack);
            session.getWebSocketSession().sendMessage(new TextMessage(json));
        } catch (IOException e) {
            log.error("Failed to send ConnectAck to session {}: {}",
                    session.getSessionId(), e.getMessage());
        }
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
