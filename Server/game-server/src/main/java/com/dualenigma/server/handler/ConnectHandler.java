package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.RespSender;
import com.dualenigma.network.protocol.NetErrorCode;
import com.dualenigma.server.game.AccountValidator;
import com.dualenigma.server.game.RoomManager;
import com.dualenigma.v1.Envelope;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 连接请求处理器.
 * C2S_Connect → 校验账号身份（token 换 accountId）→ RoomManager.onPlayerConnect()
 * 失败回执拒绝码并关闭连接；成功回执(0)由 RoomManager 在 ConnectAck 前发出.
 */
@Component
public class ConnectHandler implements MessageHandler {

    private static final Logger log = LoggerFactory.getLogger(ConnectHandler.class);

    private final MessageRouter messageRouter;
    private final RoomManager roomManager;
    private final AccountValidator accountValidator;
    private final RespSender respSender;

    public ConnectHandler(MessageRouter messageRouter, RoomManager roomManager,
                          AccountValidator accountValidator, RespSender respSender) {
        this.messageRouter = messageRouter;
        this.roomManager = roomManager;
        this.accountValidator = accountValidator;
        this.respSender = respSender;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(Envelope.BodyCase.CONNECT, this);
    }

    @Override
    public void handle(ClientSession session, Envelope env) {
        String roomCode = env.getConnect().getRoomCode();

        // 账号身份：token 经 account-server 校验。失败 → 匿名（可进房，不进入在线列表）
        Long accountId = accountValidator.validate(env.getConnect().getToken());
        if (accountId != null) {
            session.setAccountId(accountId);
            log.info("Player connecting, accountId={}, roomCode={}", accountId, roomCode);
        } else {
            log.info("Anonymous player connecting, roomCode={}", roomCode);
        }

        int code = roomManager.onPlayerConnect(session, roomCode, env.getReqId());
        if (code == NetErrorCode.OK) {
            // 成功回执已由 RoomManager.addPlayerToRoom 在 ConnectAck 之前发出
            return;
        }
        respSender.reply(session, env.getReqId(), code);   // 2001/2002/2003
        session.close();                                    // 未入房空会话不保留
    }
}
