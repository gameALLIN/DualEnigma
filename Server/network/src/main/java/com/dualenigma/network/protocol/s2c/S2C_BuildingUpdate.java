package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

import java.util.List;

/**
 * S2C: 建筑变更通知.
 *
 * action: place/destroy/sync
 * data: { "action": "place", "building": { "buildingId": 7, "buildingType": 1, "material": 2, "gridX": 5, "gridY": 3, "currentHP": 100.0 } }
 */
public class S2C_BuildingUpdate extends Message {

    private BuildingUpdateData data = new BuildingUpdateData();

    public static class BuildingInfo {
        private int buildingId;
        private int buildingType;
        private int material;
        private int gridX;
        private int gridY;
        private float currentHP;

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
        public float getCurrentHP() { return currentHP; }
        public void setCurrentHP(float currentHP) { this.currentHP = currentHP; }
    }

    public static class BuildingUpdateData {
        private String action;        // place / destroy / sync
        private BuildingInfo building;
        private List<BuildingInfo> buildings;  // sync 模式下为批量列表

        public String getAction() { return action; }
        public void setAction(String action) { this.action = action; }
        public BuildingInfo getBuilding() { return building; }
        public void setBuilding(BuildingInfo building) { this.building = building; }
        public List<BuildingInfo> getBuildings() { return buildings; }
        public void setBuildings(List<BuildingInfo> buildings) { this.buildings = buildings; }
    }

    public BuildingUpdateData getData() { return data; }
    public void setData(BuildingUpdateData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_BUILDING_UPDATE; }
}
