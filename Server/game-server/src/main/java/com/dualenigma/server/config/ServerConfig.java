package com.dualenigma.server.config;

import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

/**
 * 游戏服务器运行参数（对应 application.yml 中的 dualenigma.game 配置）.
 */
@Configuration
@ConfigurationProperties(prefix = "dualenigma.game")
public class ServerConfig {

    private int tickRate = 20;
    private int tickIntervalMs = 50;
    private int highFreqRate = 20;
    private int midFreqRate = 10;
    private long heartbeatInterval = 1000;
    private long heartbeatTimeout = 5000;
    private long reconnectWindow = 30000;
    private long aiTakeoverTimeout = 30000;
    private long finalTimeout = 120000;
    private int maxRooms = 100;

    // --- Getters & Setters ---

    public int getTickRate() { return tickRate; }
    public void setTickRate(int tickRate) { this.tickRate = tickRate; }

    public int getTickIntervalMs() { return tickIntervalMs; }
    public void setTickIntervalMs(int tickIntervalMs) { this.tickIntervalMs = tickIntervalMs; }

    public int getHighFreqRate() { return highFreqRate; }
    public void setHighFreqRate(int highFreqRate) { this.highFreqRate = highFreqRate; }

    public int getMidFreqRate() { return midFreqRate; }
    public void setMidFreqRate(int midFreqRate) { this.midFreqRate = midFreqRate; }

    public long getHeartbeatInterval() { return heartbeatInterval; }
    public void setHeartbeatInterval(long heartbeatInterval) { this.heartbeatInterval = heartbeatInterval; }

    public long getHeartbeatTimeout() { return heartbeatTimeout; }
    public void setHeartbeatTimeout(long heartbeatTimeout) { this.heartbeatTimeout = heartbeatTimeout; }

    public long getReconnectWindow() { return reconnectWindow; }
    public void setReconnectWindow(long reconnectWindow) { this.reconnectWindow = reconnectWindow; }

    public long getAiTakeoverTimeout() { return aiTakeoverTimeout; }
    public void setAiTakeoverTimeout(long aiTakeoverTimeout) { this.aiTakeoverTimeout = aiTakeoverTimeout; }

    public long getFinalTimeout() { return finalTimeout; }
    public void setFinalTimeout(long finalTimeout) { this.finalTimeout = finalTimeout; }

    public int getMaxRooms() { return maxRooms; }
    public void setMaxRooms(int maxRooms) { this.maxRooms = maxRooms; }
}
