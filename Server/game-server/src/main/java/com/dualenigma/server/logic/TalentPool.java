package com.dualenigma.server.logic;

import com.dualenigma.network.model.TalentData;
import com.dualenigma.server.util.RandomFactory;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Random;

/**
 * 天赋池管理 + 3 选 1 抽取.
 *
 * 天赋总数: 48 个
 * 选择次数: 36 次（每轮升级阶段 1 次）
 * 保底机制: 连续 3 次未选稀有天赋 → 第 4 次必出
 */
@Component
public class TalentPool {

    private static final int TALENT_PICK_COUNT = 3;

    /**
     * 为玩家生成天赋 3 选 1 选项.
     */
    public List<TalentData> rollOptions(int playerId, int globalRound) {
        Random rng = RandomFactory.create(System.nanoTime() + playerId * 1000L + globalRound);

        List<TalentData> pool = loadTalentPool();
        Collections.shuffle(pool, rng);

        List<TalentData> options = new ArrayList<>();
        for (int i = 0; i < TALENT_PICK_COUNT && i < pool.size(); i++) {
            options.add(pool.get(i));
        }
        return options;
    }

    /**
     * 加载天赋池.
     */
    private List<TalentData> loadTalentPool() {
        // TODO: 从 TalentConfig 加载完整天赋列表
        return new ArrayList<>();
    }
}
