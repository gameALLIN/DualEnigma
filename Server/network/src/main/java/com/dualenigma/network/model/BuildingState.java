package com.dualenigma.network.model;

/**
 * 建筑运行时状态（内存模型）.
 */
public class BuildingState {

    private int buildingId;
    private int buildingType;
    private int material;
    private int gridX;
    private int gridY;
    private float hp;
    private float maxHp;
    private int placedBy;       // 放置者 playerId

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
    public float getHp() { return hp; }
    public void setHp(float hp) { this.hp = hp; }
    public float getMaxHp() { return maxHp; }
    public void setMaxHp(float maxHp) { this.maxHp = maxHp; }
    public int getPlacedBy() { return placedBy; }
    public void setPlacedBy(int placedBy) { this.placedBy = placedBy; }
}
