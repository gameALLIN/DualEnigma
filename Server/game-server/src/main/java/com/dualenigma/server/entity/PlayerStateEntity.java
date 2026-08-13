package com.dualenigma.server.entity;

import jakarta.persistence.*;

/**
 * 玩家运行时状态表实体（每局每玩家一条）.
 */
@Entity
@Table(name = "player_state", indexes = {
    @Index(name = "idx_room_player", columnList = "room_id,player_id")
})
public class PlayerStateEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "room_id", nullable = false, length = 16)
    private String roomId;

    @Column(name = "player_id", nullable = false)
    private byte playerId;  // 0=Aqua, 1=Ignis

    @Column(name = "account_id", nullable = false)
    private Long accountId;

    @Column(nullable = false)
    private int hp = 100;

    @Column(name = "shelter_energy", nullable = false)
    private float shelterEnergy = 100.0f;

    @Column(name = "pos_x", nullable = false)
    private float posX = 0.0f;

    @Column(name = "pos_y", nullable = false)
    private float posY = 0.0f;

    @Column(name = "velocity_x", nullable = false)
    private float velocityX = 0.0f;

    @Column(name = "velocity_y", nullable = false)
    private float velocityY = 0.0f;

    @Column(name = "anim_state", nullable = false, length = 16)
    private String animState = "Idle";

    @Column(nullable = false)
    private boolean facing = true;

    @Column(name = "carried_fragments", columnDefinition = "JSON")
    private String carriedFragments;

    // --- Getters & Setters ---

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    public String getRoomId() { return roomId; }
    public void setRoomId(String roomId) { this.roomId = roomId; }
    public byte getPlayerId() { return playerId; }
    public void setPlayerId(byte playerId) { this.playerId = playerId; }
    public Long getAccountId() { return accountId; }
    public void setAccountId(Long accountId) { this.accountId = accountId; }
    public int getHp() { return hp; }
    public void setHp(int hp) { this.hp = hp; }
    public float getShelterEnergy() { return shelterEnergy; }
    public void setShelterEnergy(float shelterEnergy) { this.shelterEnergy = shelterEnergy; }
    public float getPosX() { return posX; }
    public void setPosX(float posX) { this.posX = posX; }
    public float getPosY() { return posY; }
    public void setPosY(float posY) { this.posY = posY; }
    public float getVelocityX() { return velocityX; }
    public void setVelocityX(float velocityX) { this.velocityX = velocityX; }
    public float getVelocityY() { return velocityY; }
    public void setVelocityY(float velocityY) { this.velocityY = velocityY; }
    public String getAnimState() { return animState; }
    public void setAnimState(String animState) { this.animState = animState; }
    public boolean isFacing() { return facing; }
    public void setFacing(boolean facing) { this.facing = facing; }
    public String getCarriedFragments() { return carriedFragments; }
    public void setCarriedFragments(String carriedFragments) { this.carriedFragments = carriedFragments; }
}
