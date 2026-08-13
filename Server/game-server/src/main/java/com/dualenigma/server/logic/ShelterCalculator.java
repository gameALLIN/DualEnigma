package com.dualenigma.server.logic;

import com.dualenigma.network.model.PlayerState;
import org.springframework.stereotype.Component;

/**
 * 庇护能量计算.
 *
 * 3 格内: +20/s 恢复
 * 超出 3 格: -33/s 消耗
 * 能量耗尽: 3 秒缓冲后扣血
 */
@Component
public class ShelterCalculator {

    private static final float MAX_ENERGY = 100f;
    private static final float RECOVERY_RATE = 20f;     // +20/s 在范围内
    private static final float CONSUMPTION_RATE = 33f;  // -33/s 超出范围
    private static final float SHELTER_DISTANCE = 3f;    // 3 格
    private static final float BUFFER_TIME = 3f;         // 3 秒缓冲

    /**
     * 更新双方庇护能量.
     */
    public void updateEnergy(PlayerState aqua, PlayerState ignis, float deltaTime) {
        float distance = calculateDistance(aqua, ignis);
        boolean inRange = distance <= SHELTER_DISTANCE;

        updateSingleEnergy(aqua, inRange, deltaTime);
        updateSingleEnergy(ignis, inRange, deltaTime);
    }

    private void updateSingleEnergy(PlayerState player, boolean inRange, float deltaTime) {
        float energy = player.getShelterEnergy();

        if (inRange) {
            energy = Math.min(MAX_ENERGY, energy + RECOVERY_RATE * deltaTime);
        } else {
            energy = Math.max(0f, energy - CONSUMPTION_RATE * deltaTime);
        }

        player.setShelterEnergy(energy);

        // 能量耗尽 → 缓冲期 → 扣血
        if (energy <= 0f) {
            if (!player.isBuffering()) {
                player.setBuffering(true);
                player.setBufferTimer(BUFFER_TIME);
            } else {
                player.setBufferTimer(player.getBufferTimer() - deltaTime);
            }
        } else {
            player.setBuffering(false);
            player.setBufferTimer(0f);
        }
    }

    private float calculateDistance(PlayerState a, PlayerState b) {
        float dx = a.getPosX() - b.getPosX();
        float dy = a.getPosY() - b.getPosY();
        return (float) Math.sqrt(dx * dx + dy * dy);
    }
}
