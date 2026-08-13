package com.dualenigma.server.entity;

import jakarta.persistence.*;

/**
 * 建筑状态表实体.
 */
@Entity
@Table(name = "building_state", indexes = {
    @Index(name = "idx_room", columnList = "room_id"),
    @Index(name = "uk_room_building", columnList = "room_id,building_id", unique = true)
})
public class BuildingStateEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "room_id", nullable = false, length = 16)
    private String roomId;

    @Column(name = "building_id", nullable = false)
    private int buildingId;

    @Column(name = "building_type", nullable = false)
    private int buildingType;

    @Column(nullable = false)
    private int material;

    @Column(name = "grid_x", nullable = false)
    private int gridX;

    @Column(name = "grid_y", nullable = false)
    private int gridY;

    @Column(name = "current_hp", nullable = false)
    private float currentHp;

    @Column(name = "max_hp", nullable = false)
    private float maxHp;

    @Column(name = "placed_by", nullable = false)
    private byte placedBy;

    // --- Getters & Setters ---

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    public String getRoomId() { return roomId; }
    public void setRoomId(String roomId) { this.roomId = roomId; }
    public int getBuildingId() { return buildingId; }
    public void setBuildingId(int buildingId) { this.buildingId = buildingId; }
    public int getBuildingType() { return buildingType; }
    public void setBuildingType(int buildingType) { this.buildingType = buildingType; }
    public int getMaterial() { return material; }
    public void setMaterial(int material) { this.material = material; }
    public int getGridX() { return gridX; }
    public void setGridX(int gridX) { this.gridX = gridX; }
    public int getGridY() { return gridY; }
    public void setGridY(int gridY) { this.gridY = gridY; }
    public float getCurrentHp() { return currentHp; }
    public void setCurrentHp(float currentHp) { this.currentHp = currentHp; }
    public float getMaxHp() { return maxHp; }
    public void setMaxHp(float maxHp) { this.maxHp = maxHp; }
    public byte getPlacedBy() { return placedBy; }
    public void setPlacedBy(byte placedBy) { this.placedBy = placedBy; }
}
