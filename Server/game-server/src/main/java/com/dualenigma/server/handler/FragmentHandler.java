package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.RespSender;
import com.dualenigma.network.protocol.NetErrorCode;
import com.dualenigma.server.game.GameRoom;
import com.dualenigma.server.game.RoomManager;
import com.dualenigma.v1.Envelope;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 碎片接住处理器.
 * C2S_FragmentCaught → 几何仲裁（同接翻倍/防重）→ 统一回执
 * 0 成功（含过期/重复幂等）/ 4002 判定被拒 / 2001 不在房间中（防呆）.
 */
@Component
public class FragmentHandler implements MessageHandler {

    private final MessageRouter messageRouter;
    private final RoomManager roomManager;
    private final RespSender respSender;

    public FragmentHandler(MessageRouter messageRouter, RoomManager roomManager, RespSender respSender) {
        this.messageRouter = messageRouter;
        this.roomManager = roomManager;
        this.respSender = respSender;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(Envelope.BodyCase.FRAGMENT_CAUGHT, this);
    }

    @Override
    public void handle(ClientSession session, Envelope env) {
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        int code;
        if (room == null) {
            code = NetErrorCode.ROOM_NOT_FOUND;                      // 不在房间中的上报（防呆）
        } else {
            code = room.onFragmentCaught(session.getPlayerId(),
                    env.getFragmentCaught().getFragmentId(),
                    env.getFragmentCaught().getPosX(),
                    env.getFragmentCaught().getPosY());
        }
        respSender.reply(session, env.getReqId(), code);
    }
}
