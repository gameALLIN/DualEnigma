package com.dualenigma.server.logic;

import com.dualenigma.server.util.Constants;
import org.springframework.stereotype.Component;

/**
 * 碎片接住几何仲裁（无状态）.
 * 服务器为唯一仲裁方：以接住瞬间双方玩家与碎片的空间距离判定单独/同时接住，
 * 与上报到达时序无关，免疫网络延迟与抖动.
 *
 * 判定规则：
 * - 双方均在碎片判定半径内 → 同时接住（双方各得 2 个，翻倍）
 * - 仅上报者在半径内 → 单独接住（1 个）
 * - 上报者自己不在半径内 → 拒绝（位置失同步异常）
 */
@Component
public class ConflictResolver {

    /**
     * 几何判定接住结果.
     *
     * @param fragmentId 碎片 ID
     * @param reporterId 上报玩家
     * @param otherId    另一方玩家
     * @param fragX      碎片世界坐标 X（客户端碰撞瞬间上报）
     * @param fragY      碎片世界坐标 Y
     * @param reporterX  上报玩家位置（权威快照，20Hz 更新）
     * @param reporterY  上报玩家位置 Y
     * @param otherX     另一方玩家位置
     * @param otherY     另一方玩家位置 Y
     * @return 判定结果；null 表示上报者不在碎片半径内，拒绝
     */
    public FragmentCatchResult judge(int fragmentId, int reporterId, int otherId,
                                     float fragX, float fragY,
                                     float reporterX, float reporterY,
                                     float otherX, float otherY) {
        boolean reporterIn = inRadius(fragX, fragY, reporterX, reporterY);
        boolean otherIn = inRadius(fragX, fragY, otherX, otherY);

        if (!reporterIn) {
            return null;
        }
        if (otherIn) {
            return new FragmentCatchResult(fragmentId, reporterId, otherId, 2, true);
        }
        return new FragmentCatchResult(fragmentId, reporterId, -1, 1, false);
    }

    private boolean inRadius(float fragX, float fragY, float px, float py) {
        float dx = px - fragX;
        float dy = py - fragY;
        return dx * dx + dy * dy <= Constants.FRAGMENT_CATCH_RADIUS * Constants.FRAGMENT_CATCH_RADIUS;
    }

    /**
     * 碎片接住判定结果.
     */
    public record FragmentCatchResult(
            int fragmentId,
            int winnerPlayerId,       // 获得碎片的玩家 ID（同接时为上报者）
            int secondPlayerId,       // 同时接住时的第二玩家 ID，-1 表示无
            int multiplier,           // 倍率 (1=单独, 2=同接翻倍)
            boolean isSimultaneous    // 是否同时接住
    ) {}
}
