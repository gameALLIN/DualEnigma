package com.dualenigma.server.entity;

import jakarta.persistence.*;

/**
 * 碎片状态表实体.
 */
@Entity
@Table(name = "fragment_state", indexes = {
    @Index(name = "idx_room", columnList = "room_id"),
    @Index(name = "idx_room_state", columnList = "room_id,state")
})
public class FragmentStateEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "room_id", nullable = false, length = 16)
    private String roomId;

    @Column(name = "fragment_id", nullable = false)
    private int fragmentId;

    @Column(name = "fragment_type", nullable = false)
    private byte fragmentType;  // 0=冰晶, 1=熔岩, 2=岩石

    @Column(name = "pos_x", nullable = false)
    private float posX;

    @Column(name = "pos_y", nullable = false)
    private float posY;

    @Column(nullable = false, length = 16)
    private String state = "Falling";

    @Column(name = "drop_time", nullable = false)
    private float dropTime;

    // --- Getters & Setters ---

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    public String getRoomId() { return roomId; }
    public void setRoomId(String roomId) { this.roomId = roomId; }
    public int getFragmentId() { return fragmentId; }
    public void setFragmentId(int fragmentId) { this.fragmentId = fragmentId; }
    public byte getFragmentType() { return fragmentType; }
    public void setFragmentType(byte fragmentType) { this.fragmentType = fragmentType; }
    public float getPosX() { return posX; }
    public void setPosX(float posX) { this.posX = posX; }
    public float getPosY() { return posY; }
    public void setPosY(float posY) { this.posY = posY; }
    public String getState() { return state; }
    public void setState(String state) { this.state = state; }
    public float getDropTime() { return dropTime; }
    public void setDropTime(float dropTime) { this.dropTime = dropTime; }
}
