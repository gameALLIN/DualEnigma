# DualEnigma Server

多模块 Maven 项目 · Java 21 · Spring Boot 4.1.0 · MariaDB 11.4

## 模块划分

| 模块 | 类型 | 端口 | 职责 |
|------|------|------|------|
| **network** | 库 (library) | — | WebSocket 传输层 + 消息协议 (9 C2S + 16 S2C) + 内存模型 + GamePhase 枚举 |
| **game-server** | 可执行 (boot) | 8080 (WebSocket) | 游戏逻辑服务器：房间管理、权威状态机、碎片/灾难/建筑/庇护、20Hz 逻辑帧 |
| **account-server** | 可执行 (boot) | 8081 (REST API) | 账号服务器：注册、登录、JWT Token 签发与验证 |

### 模块依赖关系

```
network (纯基础设施, 零业务依赖)
   ↑
   ├── game-server (依赖 network)
   └── account-server (独立, 不依赖 network)
```

## 快速启动

### Win 本地开发

```bash
# 1. 确保 MariaDB 11.4 运行在 localhost:3306
# 2. 导入数据库
mysql -u root -p < account-server/src/main/resources/db/schema.sql
mysql -u root -p < game-server/src/main/resources/db/schema.sql

# 3. 分别启动（两个终端）
cd Server
mvn -pl game-server -am spring-boot:run      # 终端 1: 游戏服 :8080
mvn -pl account-server -am spring-boot:run   # 终端 2: 账号服 :8081
```

### Mac Linux Docker 部署

```bash
cd Server
cp .env.example .env
# 编辑 .env 设置密码
docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env up -d --build
```

## 工程结构

```
Server/
├── pom.xml                              # 主 POM (聚合模块, packaging=pom)
├── docker-compose.yml                   # 容器编排 (MariaDB + game-server + account-server)
├── docker-compose.prod.yml              # 生产覆盖
├── .env.example
│
├── network/                             # 网络基础设施模块（库, 非可执行）
│   ├── pom.xml                          # 依赖: spring-boot-starter-websocket + jackson
│   └── src/main/java/com/dualenigma/network/
│       ├── GameWebSocketHandler.java   # WebSocket 连接管理
│       ├── MessageRouter.java          # 消息路由分发
│       ├── MessageCodec.java           # JSON 编解码
│       ├── ClientSession.java          # 客户端会话封装
│       ├── HeartbeatManager.java       # 心跳 + 断线检测
│       ├── MessageHandler.java         # 处理器接口
│       ├── config/
│       │   └── WebSocketConfig.java    # WebSocket 端点配置
│       ├── protocol/
│       │   ├── Message.java            # 消息基类 (@JsonSubTypes)
│       │   ├── MessageType.java        # 消息类型枚举
│       │   ├── GamePhase.java          # 7 阶段枚举
│       │   ├── c2s/                    # 9 个客户端→服务器消息
│       │   └── s2c/                    # 16 个服务器→客户端消息
│       └── model/                      # 9 个内存游戏状态模型
│
├── game-server/                         # 游戏逻辑服务器（依赖 network）
│   ├── pom.xml                          # 依赖: network + JPA + MariaDB
│   ├── Dockerfile
│   └── src/main/java/com/dualenigma/server/
│       ├── GameServerApplication.java  # @SpringBootApplication + @EnableScheduling
│       ├── config/ServerConfig.java    # 服务器运行参数
│       ├── game/                       # 房间管理 + 权威状态机 + 逻辑帧调度
│       ├── handler/                    # 8 个消息处理器 (桥接 network ↔ game)
│       ├── logic/                      # 权威逻辑 (碎片/灾难/伤害/建筑/庇护/天赋/AI)
│       ├── entity/                     # JPA 数据库实体 (8 张游戏表)
│       ├── repository/                 # Spring Data JPA 仓库
│       ├── data/                       # 静态游戏数据配置
│       └── util/                       # 工具类 (ID生成/随机工厂/常量)
│
└── account-server/                      # 账号服务器（独立, 不依赖 network）
    ├── pom.xml                          # 依赖: spring-boot-starter-web + JPA + JWT
    ├── Dockerfile
    └── src/main/java/com/dualenigma/accountserver/
        ├── AccountServerApplication.java
        ├── config/                      # CORS 配置
        ├── controller/                  # REST API (注册/登录/账号管理)
        ├── service/                     # AuthService + AccountService
        ├── entity/                      # PlayerAccount (player_account 表)
        ├── repository/                  # PlayerAccountRepository
        ├── dto/                         # 请求/响应 DTO
        └── security/                    # JWT 工具类
```

## 数据库表划分

| 表 | 所属模块 | 说明 |
|----|---------|------|
| player_account | account-server | 账号注册/登录 |
| game_room | game-server | 房间管理 |
| game_progress | game-server | 游戏进度 (36 轮) |
| player_state | game-server | 玩家运行时状态 |
| building_state | game-server | 建筑状态 |
| fragment_state | game-server | 碎片状态 |
| talent_record | game-server | 天赋选择记录 |
| skill_state | game-server | 技能状态 |
| disaster_state | game-server | 灾难状态 |
| game_result | game-server | 对局结算 |

## REST API (account-server :8081)

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/auth/register | 注册 |
| POST | /api/auth/login | 登录 |
| GET | /api/account/info | 查询账号信息 (需 Token) |
| PUT | /api/account/name | 更新昵称 (需 Token) |

## 技术文档

- [服务端技术架构](../TechnicalDocs/Server/服务端技术架构.md)
- [环境搭建](../TechnicalDocs/Server/环境搭建.md)
- [通信协议](../TechnicalDocs/Server/通信协议.md)
- [核心模块](../TechnicalDocs/Server/核心模块.md)
- [权威逻辑](../TechnicalDocs/Server/权威逻辑.md)
- [同步策略](../TechnicalDocs/Server/同步策略.md)
- [断线重连](../TechnicalDocs/Server/断线重连.md)
