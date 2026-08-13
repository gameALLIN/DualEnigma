package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 建筑放置请求.
 *
 * data: { "buildingType": 1, "material": 2, "gridX": 5, "gridY": 3 }
 */
public class C2S_BuildingPlace extends Message {

    private BuildingPlaceData data = new BuildingPlaceData();

    public static class BuildingPlaceData {
        private int buildingType;
        private int material;
        private int gridX;
        private int gridY;

        public int getBuildingType() { return buildingType; }
        public void setBuildingType(int buildingType) { this.buildingType = buildingType; }
        public int getMaterial() { return material; }
        public void setMaterial(int material) { this.material = material; }
        public int getGridX() { return gridX; }
        public void setGridX(int gridX) { this.gridX = gridX; }
        public int getGridY() { return gridY; }
        public void setGridY(int gridY) { this.gridY = gridY; }
    }

    public BuildingPlaceData getData() { return data; }
    public void setData(BuildingPlaceData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_BUILDING_PLACE; }
}
