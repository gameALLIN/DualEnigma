package com.dualenigma.server.entity;

import jakarta.persistence.*;

/**
 * 灾难状态表实体.
 */
@Entity
@Table(name = "disaster_state", indexes = {
    @Index(name = "idx_room", columnList = "room_id")
})
public class DisasterStateEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "room_id", nullable = false, length = 16)
    private String roomId;

    @Column(name = "disaster_id", nullable = false)
    private int disasterId;

    @Column(name = "difficulty_mult", nullable = false)
    private float difficultyMult;

    @Column(name = "random_seed", nullable = false)
    private long randomSeed;

    @Column(name = "elapsed_time", nullable = false)
    private float elapsedTime = 0.0f;

    @Column(name = "is_active", nullable = false)
    private boolean isActive = false;

    // --- Getters & Setters ---

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    public String getRoomId() { return roomId; }
    public void setRoomId(String roomId) { this.roomId = roomId; }
    public int getDisasterId() { return disasterId; }
    public void setDisasterId(int disasterId) { this.disasterId = disasterId; }
    public float getDifficultyMult() { return difficultyMult; }
    public void setDifficultyMult(float difficultyMult) { this.difficultyMult = difficultyMult; }
    public long getRandomSeed() { return randomSeed; }
    public void setRandomSeed(long randomSeed) { this.randomSeed = randomSeed; }
    public float getElapsedTime() { return elapsedTime; }
    public void setElapsedTime(float elapsedTime) { this.elapsedTime = elapsedTime; }
    public boolean isActive() { return isActive; }
    public void setActive(boolean active) { isActive = active; }
}
