package com.dualenigma.server.data;

import org.springframework.stereotype.Component;

import java.util.HashMap;
import java.util.Map;

/**
 * 技能卡牌配置.
 * 28 张技能卡（水人 E×8+Q×6，火人 E×8+Q×6）.
 * 稀有度权重: 普通 50%, 稀有 35%, 史诗 15%.
 * TODO: 从配置文件加载完整技能数据.
 */
@Component
public class SkillConfig {

    private final Map<Integer, SkillData> skills = new HashMap<>();

    public static class SkillData {
        public final int skillId;
        public final int playerId;      // 0=Aqua, 1=Ignis
        public final String slot;       // E or Q
        public final int rarity;        // 0=普通, 1=稀有, 2=史诗
        public final float cooldown;
        public final String name;
        public final String description;

        public SkillData(int skillId, int playerId, String slot, int rarity,
                         float cooldown, String name, String description) {
            this.skillId = skillId;
            this.playerId = playerId;
            this.slot = slot;
            this.rarity = rarity;
            this.cooldown = cooldown;
            this.name = name;
            this.description = description;
        }
    }

    public SkillConfig() {
        // TODO: 初始化 28 张技能卡
    }

    /**
     * 获取技能数据.
     */
    public SkillData getSkill(int skillId) {
        return skills.get(skillId);
    }
}
