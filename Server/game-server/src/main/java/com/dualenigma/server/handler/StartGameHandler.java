package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;
import com.dualenigma.server.game.RoomManager;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 开局请求处理器.
 * C2S_StartGame → RoomManager.requestStart()（校验房主 + 满员后广播 GameStart）
 */
@Component
public class StartGameHandler implements MessageHandler {

    private static final Logger log = LoggerFactory.getLogger(StartGameHandler.class);

    private final MessageRouter messageRouter;
    private final RoomManager roomManager;

    public StartGameHandler(MessageRouter messageRouter, RoomManager roomManager) {
        this.messageRouter = messageRouter;
        this.roomManager = roomManager;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(MessageType.C2S_START_GAME, this);
    }

    @Override
    public void handle(ClientSession session, Message msg) {
        log.info("Start game request from player {} in room {}",
                session.getPlayerId(), session.getRoomCode());
        roomManager.requestStart(session);
    }
}
