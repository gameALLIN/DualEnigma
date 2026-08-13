package com.dualenigma.network.model;

/**
 * 技能运行时状态.
 */
public class SkillState {

    private int skillId;
    private int playerId;
    private float cooldownRemaining;
    private int useCount;

    public int getSkillId() { return skillId; }
    public void setSkillId(int skillId) { this.skillId = skillId; }
    public int getPlayerId() { return playerId; }
    public void setPlayerId(int playerId) { this.playerId = playerId; }
    public float getCooldownRemaining() { return cooldownRemaining; }
    public void setCooldownRemaining(float cooldownRemaining) { this.cooldownRemaining = cooldownRemaining; }
    public int getUseCount() { return useCount; }
    public void setUseCount(int useCount) { this.useCount = useCount; }
}
