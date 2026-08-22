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
 * 技能释放处理器（预留：SkillExecutor 未实现，schema 占位接线）.
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
        messageRouter.register(Envelope.BodyCase.SKILL_ACTIVATE, this);
    }

    @Override
    public void handle(ClientSession session, Envelope env) {
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        if (room != null) {
            room.onSkillActivate(session.getPlayerId(),
                    env.getSkillActivate().getSkillId(),
                    env.getSkillActivate().getTargetX(),
                    env.getSkillActivate().getTargetY());
        }
    }
}
