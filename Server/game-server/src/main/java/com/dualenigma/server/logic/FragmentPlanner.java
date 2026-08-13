package com.dualenigma.server.logic;

import com.dualenigma.network.model.FragmentDropPlan;
import com.dualenigma.server.util.IdGenerator;
import com.dualenigma.server.util.RandomFactory;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.List;
import java.util.Random;

/**
 * 碎片掉落计划生成（种子化）.
 * 在 Preview 阶段生成完整计划，同步给双方.
 */
@Component
public class FragmentPlanner {

    private static final Logger log = LoggerFactory.getLogger(FragmentPlanner.class);

    // 碎片类型概率
    private static final double ICE_CRYSTAL_PROB = 0.55;   // 冰晶 ★
    private static final double LAVA_PROB = 0.30;            // 熔岩 ★★
    // 岩石 ★★★ = 1 - 0.55 - 0.30 = 0.15

    private static final float DROP_X_MIN = -10f;
    private static final float DROP_X_MAX = 10f;
    private static final float DROP_Y_MIN = 8f;
    private static final float DROP_Y_MAX = 13f;

    private static final int PREVIEW_COUNT = 5;
    private static final float COLLECT_BASE_COUNT = 25f;

    /**
     * 生成碎片掉落计划.
     *
     * @param disasterCategory 灾难类别（0=元素, 1=环境, 2=时空, 3=感知, 4=物理, 5=机制）
     * @param densityFactor    密度系数
     * @param seed             随机种子
     * @return 掉落计划列表
     */
    public List<FragmentDropPlan> generatePlan(int disasterCategory, float densityFactor, long seed) {
        Random rng = RandomFactory.create(seed);
        List<FragmentDropPlan> plan = new ArrayList<>();

        int collectCount = Math.max(Math.round(COLLECT_BASE_COUNT * densityFactor), 10);

        // 预告阶段碎片（1s 间隔）
        for (int i = 0; i < PREVIEW_COUNT; i++) {
            plan.add(new FragmentDropPlan(
                IdGenerator.nextFragmentId(),
                generateType(rng, disasterCategory),
                generateX(rng),
                generateY(rng),
                i * 1.0f,
                rng.nextLong()
            ));
        }

        // 收集阶段碎片（0.5s 间隔）
        for (int i = 0; i < collectCount; i++) {
            plan.add(new FragmentDropPlan(
                IdGenerator.nextFragmentId(),
                generateType(rng, disasterCategory),
                generateX(rng),
                generateY(rng),
                PREVIEW_COUNT + i * 0.5f,
                rng.nextLong()
            ));
        }

        log.debug("Generated fragment plan: {} items (category={}, density={})",
                plan.size(), disasterCategory, densityFactor);
        return plan;
    }

    private int generateType(Random rng, int disasterCategory) {
        double roll = rng.nextDouble();
        double iceProb = ICE_CRYSTAL_PROB;
        double lavaProb = LAVA_PROB;

        // 灾难类别影响概率微调
        if (disasterCategory == 0) {  // 元素类
            iceProb += 0.025;
            lavaProb += 0.025;
        } else if (disasterCategory == 3) {  // 感知类
            iceProb -= 0.025;
            lavaProb -= 0.025;
        }

        if (roll < iceProb) return 0;        // 冰晶
        if (roll < iceProb + lavaProb) return 1; // 熔岩
        return 2;                            // 岩石
    }

    private float generateX(Random rng) {
        return DROP_X_MIN + rng.nextFloat() * (DROP_X_MAX - DROP_X_MIN);
    }

    private float generateY(Random rng) {
        return DROP_Y_MIN + rng.nextFloat() * (DROP_Y_MAX - DROP_Y_MIN);
    }
}
