package com.dualenigma.server.util;

import java.util.concurrent.ThreadLocalRandom;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;

/**
 * 全局 ID 生成器.
 * 线程安全，用于生成碎片 ID、建筑 ID、房间码等.
 */
public final class IdGenerator {

    /** 房间码字符集（去除易混淆的 0/O/1/I） */
    private static final char[] ROOM_CODE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".toCharArray();

    private static final AtomicInteger fragmentIdCounter = new AtomicInteger(0);
    private static final AtomicInteger buildingIdCounter = new AtomicInteger(0);
    private static final AtomicLong legacyCounter = new AtomicLong(0);

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
     * 生成房间码（6 位随机字母数字，去除易混淆字符）.
     * 递增十六进制（00000A）可读性差且无随机性，改为随机码；
     * 撞码由 RoomManager 建房时重试兜底.
     */
    public static String nextRoomCode() {
        ThreadLocalRandom random = ThreadLocalRandom.current();
        StringBuilder sb = new StringBuilder(6);
        for (int i = 0; i < 6; i++) {
            sb.append(ROOM_CODE_CHARS[random.nextInt(ROOM_CODE_CHARS.length)]);
        }
        return sb.toString();
    }

    /**
     * 重置所有计数器（测试用）.
     */
    public static void reset() {
        fragmentIdCounter.set(0);
        buildingIdCounter.set(0);
        legacyCounter.set(0);
    }
}
