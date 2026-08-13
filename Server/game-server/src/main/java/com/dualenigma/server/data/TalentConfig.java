package com.dualenigma.server.data;

import com.dualenigma.network.model.TalentData;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.List;

/**
 * 天赋数据配置.
 * 48 个天赋定义，可叠加.
 * TODO: 从配置文件加载完整天赋列表.
 */
@Component
public class TalentConfig {

    private final List<TalentData> talentPool = new ArrayList<>();

    public TalentConfig() {
        // TODO: 初始化 48 个天赋
    }

    /**
     * 获取天赋池.
     */
    public List<TalentData> getTalentPool() {
        return new ArrayList<>(talentPool);
    }

    /**
     * 根据 ID 获取天赋.
     */
    public TalentData getTalent(int talentId) {
        return talentPool.stream()
                .filter(t -> t.getTalentId() == talentId)
                .findFirst()
                .orElse(null);
    }
}
