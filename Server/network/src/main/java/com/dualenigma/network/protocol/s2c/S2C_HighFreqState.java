package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * S2C: 高频状态转发 (20Hz).
 * 转发对方玩家的位置/速度/动画/朝向.
 *
 * data: { "playerId": 1, "position": {"x":7,"y":1}, "velocity": {"x":0,"y":0}, "animState": "Idle", "facing": false }
 */
public class S2C_HighFreqState extends Message {

    private HighFreqData data = new HighFreqData();

    public static class Vec2 {
        private float x;
        private float y;

        public float getX() { return x; }
        public void setX(float x) { this.x = x; }
        public float getY() { return y; }
        public void setY(float y) { this.y = y; }
    }

    public static class HighFreqData {
        private int playerId;
        private Vec2 position;
        private Vec2 velocity;
        private String animState;
        private boolean facing;

        public int getPlayerId() { return playerId; }
        public void setPlayerId(int playerId) { this.playerId = playerId; }
        public Vec2 getPosition() { return position; }
        public void setPosition(Vec2 position) { this.position = position; }
        public Vec2 getVelocity() { return velocity; }
        public void setVelocity(Vec2 velocity) { this.velocity = velocity; }
        public String getAnimState() { return animState; }
        public void setAnimState(String animState) { this.animState = animState; }
        public boolean isFacing() { return facing; }
        public void setFacing(boolean facing) { this.facing = facing; }
    }

    public HighFreqData getData() { return data; }
    public void setData(HighFreqData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_HIGH_FREQ_STATE; }
}
