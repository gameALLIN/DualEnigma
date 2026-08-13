package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;
import com.dualenigma.network.protocol.c2s.C2S_Connect;
import com.dualenigma.server.game.RoomManager;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 连接请求处理器.
 * C2S_Connect → RoomManager.onPlayerConnect()
 */
@Component
public class ConnectHandler implements MessageHandler {

    private static final Logger log = LoggerFactory.getLogger(ConnectHandler.class);

    private final MessageRouter messageRouter;
    private final RoomManager roomManager;

    public ConnectHandler(MessageRouter messageRouter, RoomManager roomManager) {
        this.messageRouter = messageRouter;
        this.roomManager = roomManager;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(MessageType.C2S_CONNECT, this);
    }

    @Override
    public void handle(ClientSession session, Message msg) {
        C2S_Connect connectMsg = (C2S_Connect) msg;
        String roomCode = connectMsg.getData().getRoomCode();
        log.info("Player connecting, roomCode={}", roomCode);
        roomManager.onPlayerConnect(session, roomCode);
    }
}
