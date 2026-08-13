package com.dualenigma.server.entity;

import jakarta.persistence.*;

/**
 * 技能状态表实体.
 */
@Entity
@Table(name = "skill_state", indexes = {
    @Index(name = "idx_room_player", columnList = "room_id,player_id")
})
public class SkillStateEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "room_id", nullable = false, length = 16)
    private String roomId;

    @Column(name = "player_id", nullable = false)
    private byte playerId;

    @Column(name = "skill_id", nullable = false)
    private int skillId;

    @Column(name = "cooldown_remaining", nullable = false)
    private float cooldownRemaining = 0.0f;

    @Column(name = "use_count", nullable = false)
    private int useCount = 0;

    // --- Getters & Setters ---

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    public String getRoomId() { return roomId; }
    public void setRoomId(String roomId) { this.roomId = roomId; }
    public byte getPlayerId() { return playerId; }
    public void setPlayerId(byte playerId) { this.playerId = playerId; }
    public int getSkillId() { return skillId; }
    public void setSkillId(int skillId) { this.skillId = skillId; }
    public float getCooldownRemaining() { return cooldownRemaining; }
    public void setCooldownRemaining(float cooldownRemaining) { this.cooldownRemaining = cooldownRemaining; }
    public int getUseCount() { return useCount; }
    public void setUseCount(int useCount) { this.useCount = useCount; }
}
