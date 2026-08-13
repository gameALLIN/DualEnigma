package com.dualenigma.server.util;

import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;

/**
 * 全局 ID 生成器.
 * 线程安全，用于生成碎片 ID、建筑 ID、房间码等.
 */
public final class IdGenerator {

    private static final AtomicInteger fragmentIdCounter = new AtomicInteger(0);
    private static final AtomicInteger buildingIdCounter = new AtomicInteger(0);
    private static final AtomicLong roomCodeCounter = new AtomicLong(0);

    private IdGenerator() {}

    /**
     * 生成碎片唯一 ID.
     */
    public static int nextFragmentId() {
        return fragmentIdCounter.incrementAndGet();
    }

    /**
     * 生成建筑唯一 ID.
     */
    public static int nextBuildingId() {
        return buildingIdCounter.incrementAndGet();
    }

    /**
     * 生成房间码（6 位字母数字）.
     */
    public static String nextRoomCode() {
        long n = roomCodeCounter.incrementAndGet();
        return String.format("%06X", n);
    }

    /**
     * 重置所有计数器（测试用）.
     */
    public static void reset() {
        fragmentIdCounter.set(0);
        buildingIdCounter.set(0);
        roomCodeCounter.set(0);
    }
}
