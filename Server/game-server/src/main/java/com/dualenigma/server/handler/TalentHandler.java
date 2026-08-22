package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.server.game.GameRoom;
import com.dualenigma.server.game.RoomManager;
import com.dualenigma.v1.Envelope;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 天赋选择处理器（预留：TalentPool 未实现，schema 占位接线）.
 * C2S_TalentSelect → room.onTalentSelect（TODO）
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
        messageRouter.register(Envelope.BodyCase.TALENT_SELECT, this);
    }

    @Override
    public void handle(ClientSession session, Envelope env) {
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        if (room != null) {
            room.onTalentSelect(session.getPlayerId(), env.getTalentSelect().getTalentId());
        }
    }
}
