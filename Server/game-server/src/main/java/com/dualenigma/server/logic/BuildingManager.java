package com.dualenigma.server.logic;

import com.dualenigma.network.model.BuildingState;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 建筑状态管理（放置/受损/摧毁）.
 */
@Component
public class BuildingManager {

    private final Map<Integer, BuildingState> buildings = new ConcurrentHashMap<>();
    private int nextBuildingId = 1;

    // 建筑区域: 15 × 8 格
    private static final int GRID_X_MIN = -7;
    private static final int GRID_X_MAX = 7;
    private static final int GRID_Y_MIN = -3;
    private static final int GRID_Y_MAX = 4;

    /**
     * 放置建筑.
     *
     * @return 建筑状态（含分配的 buildingId），null 表示放置失败
     */
    public BuildingState place(int playerId, int buildingType, int material, int gridX, int gridY) {
        if (isGridOccupied(gridX, gridY)) return null;
        if (!isValidGrid(gridX, gridY)) return null;

        BuildingState building = new BuildingState();
        building.setBuildingId(nextBuildingId++);
        building.setBuildingType(buildingType);
        building.setMaterial(material);
        building.setGridX(gridX);
        building.setGridY(gridY);
        building.setMaxHp(getMaxHP(buildingType, material));
        building.setHp(building.getMaxHp());
        building.setPlacedBy(playerId);

        buildings.put(building.getBuildingId(), building);
        return building;
    }

    /**
     * 建筑受伤.
     */
    public void applyDamage(int buildingId, float damage) {
        BuildingState building = buildings.get(buildingId);
        if (building == null) return;

        building.setHp(building.getHp() - damage);
        if (building.getHp() <= 0f) {
            buildings.remove(buildingId);
        }
    }

    /**
     * 获取所有建筑（用于伤害计算和快照）.
     */
    public List<BuildingState> getAllBuildings() {
        return new ArrayList<>(buildings.values());
    }

    /**
     * 同步建筑 HP（修整阶段校正）.
     */
    public List<BuildingState> syncBuildingHP() {
        return getAllBuildings();
    }

    /**
     * 清空所有建筑（新轮次重置）.
     */
    public void clear() {
        buildings.clear();
        nextBuildingId = 1;
    }

    private boolean isGridOccupied(int gridX, int gridY) {
        return buildings.values().stream()
                .anyMatch(b -> b.getGridX() == gridX && b.getGridY() == gridY);
    }

    private boolean isValidGrid(int gridX, int gridY) {
        return gridX >= GRID_X_MIN && gridX <= GRID_X_MAX
                && gridY >= GRID_Y_MIN && gridY <= GRID_Y_MAX;
    }

    private float getMaxHP(int buildingType, int material) {
        // TODO: 从 BuildingConfig 查询
        return 100.0f;
    }
}
