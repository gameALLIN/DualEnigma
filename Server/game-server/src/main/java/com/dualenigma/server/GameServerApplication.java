package com.dualenigma.server;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableScheduling;

/**
 * DualEnigma Game Server — Spring Boot 启动入口.
 *
 * WebSocket 端点: ws://{host}:8080/game
 */
@SpringBootApplication(scanBasePackages = {"com.dualenigma.server", "com.dualenigma.network"})
@EnableScheduling
public class GameServerApplication {

    public static void main(String[] args) {
        SpringApplication.run(GameServerApplication.class, args);
    }
}
