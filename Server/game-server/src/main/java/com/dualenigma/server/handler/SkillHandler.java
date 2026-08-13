package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;
import com.dualenigma.network.protocol.c2s.C2S_SkillActivate;
import com.dualenigma.server.game.GameRoom;
import com.dualenigma.server.game.RoomManager;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 技能释放处理器.
 * C2S_SkillActivate → 服务器判定 → 广播 S2C_SkillResult
 */
@Component
public class SkillHandler implements MessageHandler {

    private final MessageRouter messageRouter;
    private final RoomManager roomManager;

    public SkillHandler(MessageRouter messageRouter, RoomManager roomManager) {
        this.messageRouter = messageRouter;
        this.roomManager = roomManager;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(MessageType.C2S_SKILL_ACTIVATE, this);
    }

    @Override
    public void handle(ClientSession session, Message msg) {
        C2S_SkillActivate skill = (C2S_SkillActivate) msg;
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        if (room != null) {
            room.onSkillActivate(session.getPlayerId(), skill.getData().getSkillId(),
                    skill.getData().getTargetPos().getX(), skill.getData().getTargetPos().getY());
        }
    }
}
