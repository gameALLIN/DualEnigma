package com.dualenigma.server.logic;

import com.dualenigma.network.model.BuildingState;
import com.dualenigma.network.model.DisasterState;
import com.dualenigma.network.model.PlayerState;
import org.springframework.stereotype.Component;

import java.util.List;

/**
 * HP/伤害计算（角色 + 建筑）.
 * 服务器权威计算所有伤害，客户端不自行扣血.
 */
@Component
public class DamageCalculator {

    // 濒死保护
    private static final float CRITICAL_HP_THRESHOLD = 30f;
    private static final float CRITICAL_DAMAGE_MULTIPLIER = 0.7f;

    /**
     * 计算灾难期间角色受到的环境伤害.
     *
     * @param player    玩家状态
     * @param disaster  灾难状态
     * @param buildings 建筑列表
     * @param deltaTime 时间增量
     * @return 实际伤害值（0 表示无伤害）
     */
    public int calculatePlayerDamage(PlayerState player, DisasterState disaster,
                                       List<BuildingState> buildings, float deltaTime) {
        // 1. 判定角色是否在建筑安全区域内
        if (isInBuildingZone(player.getPosX(), player.getPosY(), buildings)) {
            return 0;
        }

        // 2. 计算灾难渐进强度
        float intensity = calculateDisasterIntensity(disaster);

        // 3. 基础 DPS
        float baseDPS = disaster.getParams().getBaseDPS();

        // 4. 濒死保护
        float damageMultiplier = 1.0f;
        if (player.getHp() <= CRITICAL_HP_THRESHOLD) {
            damageMultiplier *= CRITICAL_DAMAGE_MULTIPLIER;
        }

        // 5. 计算最终伤害
        float damage = baseDPS * intensity * damageMultiplier * deltaTime;
        return (int) Math.ceil(damage);
    }

    /**
     * 计算灾难对建筑的伤害.
     */
    public float calculateBuildingDamage(BuildingState building, DisasterState disaster, float deltaTime) {
        float intensity = calculateDisasterIntensity(disaster);
        float baseDPS = disaster.getParams().getBaseDPS();
        float resistanceMultiplier = getResistanceMultiplier(building, disaster);
        return baseDPS * intensity * resistanceMultiplier * deltaTime;
    }

    /**
     * 灾难渐进强度时间轴.
     * 0-5s: 30%, 5-10s: 60%, 10-15s: 100%, 15-20s: 80%
     */
    private float calculateDisasterIntensity(DisasterState disaster) {
        float elapsed = disaster.getElapsedTime();
        if (elapsed < 5f) return 0.3f;
        if (elapsed < 10f) return 0.6f;
        if (elapsed < 15f) return 1.0f;
        return 0.8f;
    }

    /**
     * 判定角色是否在建筑安全区域内.
     */
    private boolean isInBuildingZone(float posX, float posY, List<BuildingState> buildings) {
        // TODO: 实现建筑区域安全判定
        return false;
    }

    /**
     * 获取建筑抗性系数.
     * 免疫(0×), 强抗性(0.3×), 抗性(0.6×), 无加成(1.0×), 弱点(1.5×)
     */
    private float getResistanceMultiplier(BuildingState building, DisasterState disaster) {
        // TODO: 根据建筑类型 × 材料 × 灾难环境查抗性矩阵
        return 1.0f;
    }
}
