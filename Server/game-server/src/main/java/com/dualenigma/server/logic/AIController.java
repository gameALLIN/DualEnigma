package com.dualenigma.server.logic;

import com.dualenigma.server.game.GameRoom;
import com.dualenigma.network.model.PlayerState;
import com.dualenigma.v1.Envelope;
import com.dualenigma.v1.S2C_HighFreqState;
import org.springframework.stereotype.Component;

/**
 * 断线 AI 接管逻辑.
 * 保守策略：跟随存活玩家、保持庇护距离、不主动行动.
 */
@Component
public class AIController {

    private static final float TICK_INTERVAL_SEC = 0.05f;  // 20Hz
    private static final float FOLLOW_DISTANCE = 2.5f;
    private static final float MOVE_SPEED = 4.0f;          // 4 格/s

    /**
     * AI 控制掉线玩家.
     */
    public void onTick(PlayerState aiPlayer, PlayerState partner, GameRoom room) {
        // 1. 移动：朝向伙伴，保持 2 格距离
        float dx = partner.getPosX() - aiPlayer.getPosX();
        float dy = partner.getPosY() - aiPlayer.getPosY();
        float distance = (float) Math.sqrt(dx * dx + dy * dy);

        if (distance > FOLLOW_DISTANCE) {
            float nx = dx / distance;
            float ny = dy / distance;
            aiPlayer.setPosX(aiPlayer.getPosX() + nx * MOVE_SPEED * TICK_INTERVAL_SEC);
            aiPlayer.setPosY(aiPlayer.getPosY() + ny * MOVE_SPEED * TICK_INTERVAL_SEC);
        }

        // AI 不捡碎片、不建造、不放技能（保守行为）

        // 广播 AI 玩家状态（proto Envelope 二进制帧）
        S2C_HighFreqState state = S2C_HighFreqState.newBuilder()
                .setPlayerId(aiPlayer.getPlayerId())
                // TODO: 填充 position, velocity, animState, facing
                .build();
        Envelope env = Envelope.newBuilder()
                .setPlayerId(aiPlayer.getPlayerId())
                .setTimestamp(System.currentTimeMillis())
                .setHighFreqStateS2C(state)
                .build();
        room.broadcast(env);
    }
}
