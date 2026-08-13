package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 建筑拆除请求.
 *
 * data: { "buildingId": 7 }
 */
public class C2S_BuildingRemove extends Message {

    private BuildingRemoveData data = new BuildingRemoveData();

    public static class BuildingRemoveData {
        private int buildingId;

        public int getBuildingId() { return buildingId; }
        public void setBuildingId(int buildingId) { this.buildingId = buildingId; }
    }

    public BuildingRemoveData getData() { return data; }
    public void setData(BuildingRemoveData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_BUILDING_REMOVE; }
}
