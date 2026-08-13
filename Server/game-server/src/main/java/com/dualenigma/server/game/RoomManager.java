package com.dualenigma.server.game;

import com.dualenigma.network.ClientSession;
import com.dualenigma.server.util.IdGenerator;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.util.Queue;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.concurrent.ConcurrentMap;

/**
 * 房间池管理（创建/销毁/查询/匹配）.
 */
@Component
public class RoomManager {

    private static final Logger log = LoggerFactory.getLogger(RoomManager.class);

    private final ConcurrentMap<String, GameRoom> rooms = new ConcurrentHashMap<>();
    private final Queue<GameRoom> waitingQueue = new ConcurrentLinkedQueue<>();

    /**
     * 玩家连接 → 匹配房间.
     */
    public void onPlayerConnect(ClientSession session, String roomCode) {
        if (roomCode != null && !roomCode.isEmpty()) {
            // 指定 roomCode — 好友联机
            GameRoom room = rooms.get(roomCode);
            if (room != null && room.getPlayerCount() < 2) {
                addPlayerToRoom(room, session);
                return;
            }
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
        room.addPlayer(session);
        log.info("Player joined room {} (count={})", room.getRoomCode(), room.getPlayerCount());
    }

    /**
     * 获取房间.
     */
    public GameRoom getRoom(String roomCode) {
        return rooms.get(roomCode);
    }

    /**
     * 清理过期房间（每 60 秒执行一次）.
     */
    @Scheduled(fixedRate = 60000)
    public void cleanupExpiredRooms() {
        rooms.entrySet().removeIf(entry -> {
            if (entry.getValue().isExpired()) {
                log.info("Cleaning up expired room: {}", entry.getKey());
                return true;
            }
            return false;
        });
    }
}
