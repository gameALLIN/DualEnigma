package com.dualenigma.network;

import com.dualenigma.network.protocol.NetErrorCode;
import com.dualenigma.v1.Envelope;
import com.dualenigma.v1.S2C_Resp;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

/**
 * 统一回执发送器。所有 S2C_Resp 一律经此发出（禁止散落手工构造，保证信封格式一致）.
 * proto 信封：playerId=-1 + timestamp + resp{reqId, code, message}，二进制帧发送.
 */
@Component
public class RespSender {

    private static final Logger log = LoggerFactory.getLogger(RespSender.class);

    public RespSender() {}

    /**
     * 向指定会话回执。reqId=0（旧客户端未携带）同样回执，客户端旧版会安全忽略.
     */
    public void reply(ClientSession session, int reqId, int code, String message) {
        if (session == null || !session.isOpen()) return;
        try {
            Envelope env = Envelope.newBuilder()
                    .setPlayerId(-1)
                    .setTimestamp(System.currentTimeMillis())
                    .setResp(S2C_Resp.newBuilder()
                            .setReqId(reqId)
                            .setCode(code)
                            .setMessage(message != null ? message : ""))
                    .build();
            session.send(env.toByteArray());
        } catch (Exception e) {
            log.warn("Failed to send resp(reqId={}, code={}): {}", reqId, code, e.getMessage());
        }
    }

    /**
     * 便捷重载：NetErrorCode 常量版，文案取默认映射.
     */
    public void reply(ClientSession session, int reqId, int code) {
        reply(session, reqId, code, NetErrorMsg.of(code));
    }

    /**
     * 码值 → 默认中文文案（客户端有本地兜底文案，展示以服务器为准）.
     */
    static final class NetErrorMsg {
        private NetErrorMsg() {}

        static String of(int code) {
            return switch (code) {
                case NetErrorCode.OK -> "ok";
                case NetErrorCode.TOKEN_INVALID -> "Token 无效或已过期";
                case NetErrorCode.UNKNOWN_TYPE -> "不支持的消息类型";
                case NetErrorCode.ROOM_NOT_FOUND -> "房间不存在或已失效";
                case NetErrorCode.ROOM_FULL -> "房间已满";
                case NetErrorCode.GAME_STARTED -> "对局已开始，无法加入";
                case NetErrorCode.NOT_HOST -> "只有房主可以开始对局";
                case NetErrorCode.NOT_FULL -> "人数未满，无法开始";
                case NetErrorCode.ALREADY_STARTED -> "对局已在进行";
                case NetErrorCode.FRAGMENT_REJECTED -> "碎片判定未通过";
                default -> "未知错误";
            };
        }
    }
}
