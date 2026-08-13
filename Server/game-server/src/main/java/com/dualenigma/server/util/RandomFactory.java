package com.dualenigma.server.util;

import java.util.Random;

/**
 * 种子化随机数工厂.
 * 确保相同种子产生相同序列（用于碎片掉落、灾难效果同步）.
 */
public final class RandomFactory {

    private RandomFactory() {}

    /**
     * 创建种子化 Random.
     */
    public static Random create(long seed) {
        return new Random(seed);
    }

    /**
     * 创建基于当前时间的 Random.
     */
    public static Random create() {
        return new Random();
    }
}
