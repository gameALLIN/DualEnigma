package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 技能释放请求.
 *
 * data: { "skillId": 3, "targetPos": {"x": 5.0, "y": 2.0} }
 */
public class C2S_SkillActivate extends Message {

    private SkillActivateData data = new SkillActivateData();

    public static class Vec2 {
        private float x;
        private float y;

        public float getX() { return x; }
        public void setX(float x) { this.x = x; }
        public float getY() { return y; }
        public void setY(float y) { this.y = y; }
    }

    public static class SkillActivateData {
        private int skillId;
        private Vec2 targetPos;

        public int getSkillId() { return skillId; }
        public void setSkillId(int skillId) { this.skillId = skillId; }
        public Vec2 getTargetPos() { return targetPos; }
        public void setTargetPos(Vec2 targetPos) { this.targetPos = targetPos; }
    }

    public SkillActivateData getData() { return data; }
    public void setData(SkillActivateData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_SKILL_ACTIVATE; }
}
