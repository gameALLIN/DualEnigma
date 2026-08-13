package com.dualenigma.server.data;

import org.springframework.stereotype.Component;

import java.util.HashMap;
import java.util.Map;

/**
 * 合成配方配置.
 * 根据灾难环境决定合成表，5 种材料（水砖/冰砖/火砖/岩浆砖/石砖）.
 * TODO: 从配置文件加载完整合成配方.
 */
@Component
public class SynthesisConfig {

    // 灾难类别 → (碎片组合 → 材料类型)
    private final Map<Integer, Map<String, Integer>> recipesByCategory = new HashMap<>();

    public SynthesisConfig() {
        // TODO: 初始化合成配方
    }

    /**
     * 查询碎片组合对应的合成结果.
     *
     * @param disasterCategory 灾难类别
     * @param fragmentKey      碎片组合 key（排序后的碎片类型列表）
     * @return 材料类型 (0-4)，-1 表示无匹配配方
     */
    public int getMaterial(int disasterCategory, String fragmentKey) {
        Map<String, Integer> recipes = recipesByCategory.get(disasterCategory);
        if (recipes == null) return -1;
        return recipes.getOrDefault(fragmentKey, -1);
    }
}
