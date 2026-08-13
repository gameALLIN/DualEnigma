package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

import java.util.List;

/**
 * S2C: 技能判定结果.
 *
 * data: { "skillId": 3, "playerId": 1, "targetPos": {"x":5,"y":2}, "effects": [{ "type": "damage", "targetPlayerId": 0, "value": 10 }] }
 */
public class S2C_SkillResult extends Message {

    private SkillResultData data = new SkillResultData();

    public static class Vec2 {
        private float x;
        private float y;

        public float getX() { return x; }
        public void setX(float x) { this.x = x; }
        public float getY() { return y; }
        public void setY(float y) { this.y = y; }
    }

    public static class SkillEffect {
        private String type;          // damage / freeze / heal / etc.
        private int targetPlayerId;
        private float value;
        private float duration;

        public String getType() { return type; }
        public void setType(String type) { this.type = type; }
        public int getTargetPlayerId() { return targetPlayerId; }
        public void setTargetPlayerId(int targetPlayerId) { this.targetPlayerId = targetPlayerId; }
        public float getValue() { return value; }
        public void setValue(float value) { this.value = value; }
        public float getDuration() { return duration; }
        public void setDuration(float duration) { this.duration = duration; }
    }

    public static class SkillResultData {
        private int skillId;
        private int playerId;
        private Vec2 targetPos;
        private List<SkillEffect> effects;

        public int getSkillId() { return skillId; }
        public void setSkillId(int skillId) { this.skillId = skillId; }
        public int getPlayerId() { return playerId; }
        public void setPlayerId(int playerId) { this.playerId = playerId; }
        public Vec2 getTargetPos() { return targetPos; }
        public void setTargetPos(Vec2 targetPos) { this.targetPos = targetPos; }
        public List<SkillEffect> getEffects() { return effects; }
        public void setEffects(List<SkillEffect> effects) { this.effects = effects; }
    }

    public SkillResultData getData() { return data; }
    public void setData(SkillResultData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_SKILL_RESULT; }
}
