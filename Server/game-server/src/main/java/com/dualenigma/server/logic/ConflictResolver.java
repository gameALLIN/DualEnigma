package com.dualenigma.server.logic;

import org.springframework.stereotype.Component;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 碎片接住冲突仲裁.
 * 服务器为唯一仲裁方，判定谁接住或同时接住.
 *
 * 同时接住判定窗口: 100ms
 */
@Component
public class ConflictResolver {

    private static final long SIMULTANEOUS_WINDOW_MS = 100;

    private final Map<Integer, CatchRecord> pendingCatches = new ConcurrentHashMap<>();

    private record CatchRecord(int playerId, long catchTime) {}

    /**
     * 处理碎片接住请求.
     *
     * @return 判定结果（含 playerId 和 multiplier），null 表示等待第二人
     */
    public FragmentCatchResult onCatch(int fragmentId, int playerId, long catchTime) {
        if (pendingCatches.containsKey(fragmentId)) {
            CatchRecord first = pendingCatches.get(fragmentId);

            if (first.playerId == playerId) return null;

            pendingCatches.remove(fragmentId);

            if (Math.abs(catchTime - first.catchTime) < SIMULTANEOUS_WINDOW_MS) {
                return new FragmentCatchResult(fragmentId, first.playerId, playerId, 3, true);
            } else {
                int winner = first.catchTime <= catchTime ? first.playerId : playerId;
                return new FragmentCatchResult(fragmentId, winner, -1, 1, false);
            }
        }

        pendingCatches.put(fragmentId, new CatchRecord(playerId, catchTime));
        return null;
    }

    /**
     * 超时清理：超过 100ms 窗口的暂存记录按单人接住处理.
     */
    public FragmentCatchResult checkTimeout(int fragmentId, long currentTime) {
        CatchRecord record = pendingCatches.get(fragmentId);
        if (record == null) return null;

        if (currentTime - record.catchTime >= SIMULTANEOUS_WINDOW_MS) {
            pendingCatches.remove(fragmentId);
            return new FragmentCatchResult(fragmentId, record.playerId, -1, 1, false);
        }
        return null;
    }

    /**
     * 碎片接住判定结果.
     */
    public record FragmentCatchResult(
            int fragmentId,
            int winnerPlayerId,       // 获得碎片的玩家 ID
            int secondPlayerId,       // 同时接住时的第二玩家 ID，-1 表示无
            int multiplier,           // 倍率 (1/2/3)
            boolean isSimultaneous    // 是否同时接住
    ) {}
}
