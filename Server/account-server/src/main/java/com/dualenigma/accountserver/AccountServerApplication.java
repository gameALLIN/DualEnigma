package com.dualenigma.accountserver;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

/**
 * DualEnigma Account Server — 账号服务器启动入口.
 *
 * REST API 端口: 8081
 * 负责：注册、登录、Token 签发与验证
 */
@SpringBootApplication
public class AccountServerApplication {

    public static void main(String[] args) {
        SpringApplication.run(AccountServerApplication.class, args);
    }
}
