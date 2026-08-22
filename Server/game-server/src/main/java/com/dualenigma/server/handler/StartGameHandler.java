package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.RespSender;
import com.dualenigma.server.game.RoomManager;
import com.dualenigma.v1.Envelope;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 开局请求处理器.
 * C2S_StartGame → RoomManager.requestStart()（校验房主 + 满员后广播 GameStart）
 * 请求结果统一回执：0 成功（随后收到 GameStart 广播）/ 3001 非房主 / 3002 未满员 / 3003 已开局.
 * 失败不关连接（玩家仍在房间中可继续等待）.
 */
@Component
public class StartGameHandler implements MessageHandler {

    private static final Logger log = LoggerFactory.getLogger(StartGameHandler.class);

    private final MessageRouter messageRouter;
    private final RoomManager roomManager;
    private final RespSender respSender;

    public StartGameHandler(MessageRouter messageRouter, RoomManager roomManager, RespSender respSender) {
        this.messageRouter = messageRouter;
        this.roomManager = roomManager;
        this.respSender = respSender;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(Envelope.BodyCase.START_GAME, this);
    }

    @Override
    public void handle(ClientSession session, Envelope env) {
        log.info("Start game request from player {} in room {}",
                session.getPlayerId(), session.getRoomCode());
        int code = roomManager.requestStart(session);
        respSender.reply(session, env.getReqId(), code);
    }
}
