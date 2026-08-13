package com.dualenigma.server.logic;

import com.dualenigma.network.model.DisasterParams;
import com.dualenigma.network.model.DisasterState;
import com.dualenigma.server.util.RandomFactory;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.Random;

/**
 * 灾难选型与种子生成.
 * 在 Preview 阶段选灾难，生成随机种子同步给双方.
 */
@Component
public class DisasterSelector {

    private static final Logger log = LoggerFactory.getLogger(DisasterSelector.class);

    /**
     * 选择本轮灾难并生成随机种子.
     *
     * @param chapter 当前章节 (1-3)
     * @param section 当前节 (1-4)
     * @param round   当前轮 (1-3)
     * @return 灾难状态（含类型、参数、种子）
     */
    public DisasterState select(int chapter, int section, int round) {
        Random rng = RandomFactory.create(System.nanoTime());

        int disasterId = pickDisasterId(rng, chapter);

        float chapterMult = chapter == 1 ? 0.8f : chapter == 2 ? 1.0f : 1.2f;
        float roundMult = round == 1 ? 1.0f : round == 2 ? 1.3f : 1.6f;
        float difficultyMult = chapterMult * roundMult;

        long seed = rng.nextLong();

        DisasterParams params = loadDisasterParams(disasterId);

        DisasterState state = new DisasterState();
        state.setDisasterId(disasterId);
        state.setDifficultyMult(difficultyMult);
        state.setRandomSeed(seed);
        state.setParams(params);
        state.setActive(false);
        state.setElapsedTime(0f);

        log.info("Disaster selected: id={}, diffMult={}, chapter={}, round={}",
                disasterId, difficultyMult, chapter, round);
        return state;
    }

    /**
     * 根据章节限制灾难池范围选择灾难 ID.
     */
    private int pickDisasterId(Random rng, int chapter) {
        // TODO: 根据章节限制灾难池范围
        // Chapter 1: disasters 0-11 (12 kinds)
        // Chapter 2: disasters 12-23 (12 kinds)
        // Chapter 3: disasters 24-34 (11 kinds)
        int poolStart = (chapter - 1) * 12;
        int poolEnd = chapter == 3 ? 34 : poolStart + 11;
        return poolStart + rng.nextInt(poolEnd - poolStart + 1);
    }

    /**
     * 加载灾难参数.
     */
    private DisasterParams loadDisasterParams(int disasterId) {
        // TODO: 从 DisasterConfig 加载参数
        DisasterParams params = new DisasterParams();
        params.setName("Disaster_" + disasterId);
        params.setBaseDPS(3.0f);
        params.setRange(10.0f);
        params.setDuration(20.0f);
        return params;
    }
}
