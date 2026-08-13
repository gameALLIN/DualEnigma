package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;
import com.dualenigma.network.protocol.c2s.C2S_TalentSelect;
import com.dualenigma.server.game.GameRoom;
import com.dualenigma.server.game.RoomManager;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 天赋选择处理器.
 * C2S_TalentSelect → TalentPool.select()
 */
@Component
public class TalentHandler implements MessageHandler {

    private final MessageRouter messageRouter;
    private final RoomManager roomManager;

    public TalentHandler(MessageRouter messageRouter, RoomManager roomManager) {
        this.messageRouter = messageRouter;
        this.roomManager = roomManager;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(MessageType.C2S_TALENT_SELECT, this);
    }

    @Override
    public void handle(ClientSession session, Message msg) {
        C2S_TalentSelect select = (C2S_TalentSelect) msg;
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        if (room != null) {
            room.onTalentSelect(session.getPlayerId(), select.getData().getTalentId());
        }
    }
}
