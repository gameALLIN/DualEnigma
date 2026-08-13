package com.dualenigma.server.data;

import org.springframework.stereotype.Component;

import java.util.HashMap;
import java.util.Map;

/**
 * 建筑配置.
 * 5 种建筑类型 × 5 种材料 = 25 种组合.
 * 抗性矩阵: 免疫(0×), 强抗性(0.3×), 抗性(0.6×), 无加成(1.0×), 弱点(1.5×)
 * TODO: 从配置文件加载完整建筑参数.
 */
@Component
public class BuildingConfig {

    // 建筑类型: 0=墙壁, 1=平台, 2=斜坡, 3=加固塔, 4=特殊
    // 材料: 0=水砖, 1=冰砖, 2=火砖, 3=岩浆砖, 4=石砖

    private final Map<String, Float> maxHpMap = new HashMap<>();
    private final Map<String, Float> resistanceMatrix = new HashMap<>();

    public BuildingConfig() {
        // TODO: 初始化建筑 HP 和抗性矩阵
    }

    /**
     * 获取建筑最大 HP.
     */
    public float getMaxHp(int buildingType, int material) {
        return maxHpMap.getOrDefault(buildingType + "_" + material, 100.0f);
    }

    /**
     * 获取抗性系数.
     *
     * @param buildingType  建筑类型
     * @param material      材料类型
     * @param disasterType  灾难类型
     * @return 伤害系数 (0=免疫, 0.3=强抗, 0.6=抗, 1.0=正常, 1.5=弱点)
     */
    public float getResistanceMultiplier(int buildingType, int material, int disasterType) {
        return resistanceMatrix.getOrDefault(buildingType + "_" + material + "_" + disasterType, 1.0f);
    }
}
