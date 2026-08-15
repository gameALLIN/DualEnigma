package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 高频角色状态同步 (20Hz).
 *
 * data: { "position": {"x":0,"y":0}, "velocity": {"x":0,"y":0}, "animState": "Run", "facing": true }
 */
public class C2S_HighFreqState extends Message {

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
        private Vec2 position;
        private Vec2 velocity;
        private String animState;
        private boolean facing;
        private int hp;
        private float shelterEnergy;

        public Vec2 getPosition() { return position; }
        public void setPosition(Vec2 position) { this.position = position; }
        public Vec2 getVelocity() { return velocity; }
        public void setVelocity(Vec2 velocity) { this.velocity = velocity; }
        public String getAnimState() { return animState; }
        public void setAnimState(String animState) { this.animState = animState; }
        public boolean isFacing() { return facing; }
        public void setFacing(boolean facing) { this.facing = facing; }
        public int getHp() { return hp; }
        public void setHp(int hp) { this.hp = hp; }
        public float getShelterEnergy() { return shelterEnergy; }
        public void setShelterEnergy(float shelterEnergy) { this.shelterEnergy = shelterEnergy; }
    }

    public HighFreqData getData() { return data; }
    public void setData(HighFreqData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_HIGH_FREQ_STATE; }
}
