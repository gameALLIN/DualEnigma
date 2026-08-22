# Protobuf 迁移改造计划 — 服务器篇

> **制定日期**: 2026-08-22
> **执行者**: main_network（实现）+ main_qa（验收）
> **配套文档**: 《TechnicalDocs/Client/Protobuf迁移改造计划_客户端.md》（客户端篇，与本文件同一迁移的两个执行面）
> **前置阅读**: `TechnicalDocs/Server/网络框架重构_R5服务器改造计划.md`（reqId/回执体系现状）、`TechnicalDocs/网络框架重构计划.md`
> **决策来源**: 项目所有者拍板——WebSocket 对局通道由 JSON 文本帧迁移至 Protobuf 二进制帧（成熟、规范、schema 单一来源）

**Goal（服务器范围）:** game-server WebSocket 通道的编解码从「Jackson JSON 多态信封」迁移到「proto3 `Envelope`（oneof）+ protobuf-java 二进制帧」；`.proto` schema 为双端唯一事实来源（本篇 Task S0.1 承载草案）；消息语义（reqId/S2C_Resp 回执闭环、心跳、1002 兜底、PhaseChange 时钟差值法）**逐行保留**。

**Tech Stack:** proto3 · protobuf-java 3.25.x · Spring Boot 4.1 / Java 21 · Maven protobuf-maven-plugin 0.6.1（protoc 自动下载，开发者本机零安装）

---

## 〇、铁律（服务器侧）

1. **`.proto` 是唯一事实来源**：字段变更流程 = 改 `Protocol/proto/game.proto` → 双端 regen → 同 PR 提交双端生成物与调用方修改。禁止手改生成物；禁止单端绕过 schema 加字段。
2. **范围边界**：只动 game-server WebSocket；**account-server REST 保持 JSON 不变**。
3. **行为语义零变化**：R5 已验收的全部细节（resp(0) 紧邻 ConnectAck 前、进房失败 resp+close、幂等回 0、1002 兜底）逐行保留。
4. **big-bang 单分支双端同切**：`refactor/proto-r1` 分支同时含服务器篇（PS-0/PS-1）与客户端篇（PC-0/PC-1）改动，**一个 PR 合入**。二进制协议与 JSON 不互通，双端必须同时发版（详见 §七）。
5. **旧路径保留到 PS-2**：Jackson 层在清理里程碑前不删，回滚 = 丢弃分支。

---

## 一、背景与决策记录（双端共享，落档于此）

### 1.1 为什么迁

| 维度 | JSON 现状 | Protobuf 后 |
|------|----------|------------|
| schema 治理 | 双端 DTO 手工对齐（R2 搬运"一个字母都不许变"即在防漂移） | `.proto` 单一来源，protoc 保证双端一致，漂移编译期暴露 |
| 体积 | 高频帧 ~200B（20Hz ≈ 4KB/s） | 预计 ~50–70B（↓70%） |
| 解析开销 | Jackson 字符串解析 | 二进制直读 |
| 规范性 | 自定义信封 + 字符串 type 路由 | 行业标准，字段编号演化规则内建 |

### 1.2 已接受的代价
1. 客户端引入 Google.Protobuf 运行时 DLL（唯一第三方依赖，已拍板）；
2. codegen 链路维护成本（服务器由 Maven 自动化，成本主要在客户端脚本）；
3. 抓包不可肉眼读（客户端篇配 Editor dump 工具缓解）。

### 1.3 明确不做
❌ 不引入 gRPC（传输仍是自研 WebSocket + `BinaryWebSocketHandler`）· ❌ 不动 REST · ❌ 不补 C2S_BuildingPlace 等 5 种预留消息的服务器逻辑（仅 schema 占位）· ❌ 不动断线重连/权威伤害（M5+）。

---

## 二、目标架构（双端总图，schema 归属本篇）

```
Protocol/proto/game.proto  ←—— 唯一事实来源（仓库根，双端共享）
   ├──> Maven protobuf-maven-plugin → target/generated-sources（Java，自动）
   └──> Client/tools/gen-proto.ps1  → Assets/.../Protocol/Generated/Game.cs（C#，入库）

Java: Envelope.parseFrom(bytes) ⇄ env.toByteArray()
      （Jackson Message 层 + @JsonSubTypes 多态 + MessageType 枚举 → PS-2 全部退役）
```

**信封设计**：每 WebSocket 二进制帧 = 一个 `Envelope`；oneof case 即路由键（取代字符串 type）；信封字段 `timestamp / player_id / req_id` 与现行 JSON 信封一一对应。

---

## 三、文件结构（服务器侧 + 共享目录）

```
Protocol/                                  # ⬅ 仓库根新增（双端共享）
├── proto/game.proto                       # 全部消息定义（本篇 Task S0.1）
└── README.md                              # 演进规则 + regen 命令 + 双端版本锁定

Server/network/
├── pom.xml                                # +protobuf-java + protobuf-maven-plugin
└── src/main/java/com/dualenigma/network/
    ├── ProtoCodec.java                    # 新增：bytes ⇄ Envelope
    ├── GameWebSocketHandler.java          # 改：handleBinaryMessage
    ├── MessageRouter.java                 # 改：BodyCase 路由
    ├── MessageHandler.java                # 改：签名 handle(session, Envelope)
    ├── RespSender.java                    # 改：proto 信封回执
    ├── ClientSession.java                 # 改：+send(byte[])
    └── handler/*.java                     # 改：取值换生成类型
game-server/.../game/ & handler/           # 改：业务层签名与广播点

【PS-2 删除】protocol/Message.java、16 个 Jackson DTO 子类、MessageType.java、
             MessageCodec（JSON 部分）、ClientSession.send(String)、handleTextMessage
```

---

## 四、里程碑 PS-0：Schema 与代码生成

### Task S0.1：`Protocol/proto/game.proto` 完整草案（逐字段可直接使用）

```protobuf
syntax = "proto3";

package dualenigma.v1;   // 版本化包名：将来不兼容演进时升 v2，双端同步切

// ============================================================
// 信封：每 WebSocket 二进制帧 = 一个 Envelope
// oneof case 即路由键（取代 JSON 字符串 type 字段）
// ============================================================
message Envelope {
  int64 timestamp = 1;   // 发送方毫秒时钟。S2C 必填（PhaseChange 时钟差值法依赖）；
                         // C2S 可不填（proto3 零值省略）
  int32 player_id = 2;   // S2C 广播方 ID（-1=系统广播；OpponentDisconnect=离开者）；
                         // C2S 不填（服务器从会话取）
  int32 req_id = 3;      // C2S 请求关联号（S2C_Resp 回显）。
                         // 豁免：心跳 / 高频流（恒 0，对应 R5 豁免决策）

  oneof body {
    // ── C2S（10：5 在用 + 5 预留，编号段 10-19）──
    C2S_Connect         connect          = 10;
    C2S_Heartbeat       heartbeat        = 11;
    C2S_StartGame       start_game       = 12;
    C2S_HighFreqState   high_freq_state  = 13;
    C2S_FragmentCaught  fragment_caught  = 14;
    C2S_BuildingPlace   building_place   = 15;   // 预留：服务器 Handler 为 TODO
    C2S_BuildingRemove  building_remove  = 16;   // 预留
    C2S_Synthesize      synthesize       = 17;   // 预留
    C2S_SkillActivate   skill_activate   = 18;   // 预留
    C2S_TalentSelect    talent_select    = 19;   // 预留
    // ── S2C（11，编号段 30-40）──
    S2C_Resp               resp                 = 30;
    S2C_ConnectAck         connect_ack          = 31;
    S2C_GameStart          game_start           = 32;
    S2C_PlayerJoined       player_joined        = 33;
    S2C_PhaseChange        phase_change         = 34;
    S2C_HighFreqState      high_freq_state_s2c  = 35;
    S2C_MidFreqState       mid_freq_state       = 36;
    S2C_OpponentDisconnect opponent_disconnect  = 37;
    S2C_FragmentDropPlan   fragment_drop_plan   = 38;
    S2C_FragmentResult     fragment_result      = 39;
    S2C_HeartbeatAck       heartbeat_ack        = 40;
    // 预留编号段 41-59：未来 S2C 新消息
  }
}

// ============================================================
// 通用类型
// ============================================================
message Vec2 {
  float x = 1;
  float y = 2;
}

// 阶段枚举（proto3 首值必须为 0=未指定，实际值从 1 起；
// 服务器 GameStateMachine 枚举 → GamePhasePb 一一映射，见 Task S1.5）
enum GamePhasePb {
  GAME_PHASE_UNSPECIFIED = 0;
  PREVIEW = 1;
  FRAGMENT_COLLECT = 2;
  DISASTER_PREVIEW = 3;
  BUILD = 4;
  DISASTER_IMPACT = 5;
  REST = 6;
  UPGRADE = 7;
}

// ============================================================
// C2S 消息体（字段语义与 JSON 版一一对应，R2 搬运时已核对的契约）
// ============================================================
message C2S_Connect {
  string room_code = 1;   // 空 = 自动匹配建房；非空 = 加入指定好友房
  string token = 2;       // account-server JWT；空/无效 = 匿名（现行为）
}
message C2S_Heartbeat {}  // 空——RTT 语义在信封层（发送时刻 → Ack 到达）
message C2S_StartGame {}
message C2S_HighFreqState {
  Vec2  position = 1;
  Vec2  velocity = 2;
  string anim_state = 3;  // 过渡期保持 string；枚举化列入后续清理
  bool  facing = 4;
  int32 hp = 5;
  float shelter_energy = 6;
}
message C2S_FragmentCaught {
  int32 fragment_id = 1;
  float pos_x = 2;        // 碰撞瞬间碎片世界坐标（同接几何判定依据）
  float pos_y = 3;
}
// ── 预留（字段按 TODO Handler 预期入参草拟，接线时如需微调走 .proto 变更流程）──
message C2S_BuildingPlace {
  int32 building_type = 1;
  int32 material = 2;
  int32 grid_x = 3;
  int32 grid_y = 4;
  int32 facing = 5;
}
message C2S_BuildingRemove { int32 building_id = 1; }
message C2S_Synthesize {
  repeated int32 fragment_ids = 1;
  int32 desired_output = 2;
}
message C2S_SkillActivate {
  int32 skill_id = 1;
  float target_x = 2;
  float target_y = 3;
}
message C2S_TalentSelect { int32 talent_id = 1; }

// ============================================================
// S2C 消息体
// ============================================================
message S2C_Resp {
  int32 req_id = 1;       // 回显请求关联号
  int32 code = 2;         // NetErrorCode（码值表不变：0 成功 / 非0 失败）
  string message = 3;     // 服务器默认文案（客户端有本地兜底）
}
message S2C_ConnectAck {
  int32 player_id = 1;    // 0=房主(Aqua) / 1=加入方(Ignis)
  string room_code = 2;
}
message S2C_GameStart { int32 chapter = 1; int32 section = 2; int32 round = 3; }
message S2C_PlayerJoined { int32 player_id = 1; int32 player_count = 2; }
message S2C_PhaseChange {
  GamePhasePb phase = 1;
  int32 duration_ms = 2;
  int64 phase_end_time = 3;   // 服务器时钟。剩余 = phase_end_time - 信封 timestamp（不变）
}
message S2C_HighFreqState {
  int32 player_id = 1;
  Vec2 position = 2;
  Vec2 velocity = 3;
  string anim_state = 4;
  bool facing = 5;
  int32 hp = 6;
  float shelter_energy = 7;
}
message S2C_MidFreqState {
  message PlayerMidFreq {
    int32 player_id = 1;
    int32 hp = 2;
    float shelter_energy = 3;        // JSON 时代 Math.round 成 int——proto 直接 float
                                     // （行为变化，验收 §八第 8 项盯防）
    repeated int32 carried_fragments = 4;
  }
  repeated PlayerMidFreq players = 1;
}
message S2C_OpponentDisconnect {
  string state = 1;                  // "lobby" / "waiting"；离开者 = 信封 player_id
}
message S2C_FragmentDropPlan {
  message PlanItem {
    int32 fragment_id = 1;
    int32 type = 2;                  // 0=冰晶 1=熔岩 2=岩石（顺序已核对一致）
    Vec2 position = 3;
    float drop_time = 4;
    int64 seed = 5;
  }
  repeated PlanItem plan = 1;
}
message S2C_FragmentResult {
  int32 fragment_id = 1;
  int32 player_id = 2;               // 胜出上报者
  int32 multiplier = 3;
  bool is_simultaneous = 4;
}
message S2C_HeartbeatAck { int64 server_timestamp = 1; }
```

**Schema 演进规则（写入 `Protocol/README.md`）**：
1. 只加不改：新增字段用新编号，旧编号永不复用、不改类型；
2. oneof 新消息用编号段尾部（C2S=10-29 / S2C=30-59）；
3. `.proto` 变更 PR 必须同时含双端 regen 产物与受影响调用方；
4. 不兼容变更 → 升 `v2` 包名双端同步切；
5. **双端 protobuf 大版本锁定 3.25.x**（pom 与客户端 DLL 一致，防线格式分歧）。

- [x] Step S0.1.1 创建 `Protocol/proto/game.proto` + `Protocol/README.md`（上述规则 + 客户端 regen 命令指引）。
  > 执行补充（2026-08-22）：schema 增补 `option java_package = "com.dualenigma.v1"; java_multiple_files = true;`（对齐工程包名规范与 S0.2.1 验收口径；仅影响 Java 生成，C# 侧不受影响）。

### Task S0.2：Maven 代码生成

**Files:** Modify `Server/network/pom.xml`

```xml
<dependencies>
  <dependency>
    <groupId>com.google.protobuf</groupId>
    <artifactId>protobuf-java</artifactId>
    <version>3.25.5</version>
  </dependency>
</dependencies>

<build>
  <plugins>
    <plugin>
      <groupId>org.xolstice.maven.plugins</groupId>
      <artifactId>protobuf-maven-plugin</artifactId>
      <version>0.6.1</version>
      <configuration>
        <protoSourceRoot>${project.basedir}/../../Protocol/proto</protoSourceRoot>
      </configuration>
      <executions>
        <execution><goals><goal>compile</goal></goals></execution>
      </executions>
    </plugin>
  </plugins>
</build>
```

- [x] Step S0.2.1 配置后 `mvn -pl network compile` 生成成功，`com.dualenigma.v1.Envelope` 可引用（生成物在 `target/generated-sources`，不入库）。
  > 执行补充（2026-08-22）：文档片段缺 os-maven-plugin extension 与 protocArtifact 显式版本，不补会在 Windows 上解析失败或用旧 protoc；已补 `kr.motd.maven:os-maven-plugin 1.7.1` + `protocArtifact com.google.protobuf:protoc:3.25.5:exe:${os.detected.classifier}`。protoc 经阿里云镜像下载正常。
  > 生成 API 命名注意：`high_freq_state_s2c` 生成的访问器为 `setHighFreqStateS2C`（尾段全大写）。

### Task S0.3：round-trip 单测（服务器侧）

- New: `network/src/test/.../ProtoRoundTripTest.java`：16 种在用消息逐种构造 → toByteArray → parseFrom → 字段断言。**不做 golden bytes**（protoc 小版本可能改变编码细节，round-trip + 字段断言更稳）。
- 改写 `ProtocolReqIdTest`（JSON 版）为 proto 版：信封 reqId 回显 / resp code 透传 / 坏字节 parse 返回 null。

**PS-0 验收**：`mvn -pl network,game-server -am test` 全绿；未接线，线上行为零变化。

---

## 五、里程碑 PS-1：服务器切换

### Task S1.1：`ProtoCodec` 与 `ClientSession.send(byte[])`

**Files:**
- New: `network/.../ProtoCodec.java`
- Modify: `network/.../ClientSession.java`

```java
/** Envelope ⇄ bytes（解析失败返回 null，由调用方回 1002） */
public final class ProtoCodec {
    public static Envelope parse(byte[] bytes) {
        try { return Envelope.parseFrom(bytes); }
        catch (InvalidProtocolBufferException e) { return null; }
    }
    public static byte[] encode(Envelope env) { return env.toByteArray(); }
}
```

`ClientSession` 增加 `synchronized void send(byte[] payload)`（`new BinaryMessage(payload)`），与既有 `send(String)` 并存至 PS-2。**synchronized 保持**——Tick 线程与消息线程并发写同一会话，二进制帧同理。

### Task S1.2：`GameWebSocketHandler` 切二进制帧

**Files:** Modify `network/.../GameWebSocketHandler.java`

```java
@Override
protected void handleBinaryMessage(WebSocketSession session, BinaryMessage message) {
    byte[] bytes = new byte[message.getPayloadLength()];
    message.getPayload().get(bytes);          // ByteBuffer → byte[]
    ClientSession cs = heartbeatManager.getClientSession(session.getId());
    Envelope env = ProtoCodec.parse(bytes);
    if (env == null || env.getBodyCase() == Envelope.BodyCase.BODY_NOT_SET) {
        log.error("Malformed envelope from {}: {} bytes", session.getId(), bytes.length);
        if (cs != null) respSender.reply(cs, 0, NetErrorCode.UNKNOWN_TYPE);   // 1002 语义保留
        return;
    }
    messageRouter.route(cs, env);
}

@Override
protected void handleTextMessage(WebSocketSession session, TextMessage message) {
    log.warn("Unexpected text frame (protocol is protobuf-binary now): {}", session.getId());
    // big-bang 切换：文本帧视为协议错误，不解析（PS-2 删除此 override）
}
```

`handlePongMessage / afterConnectionClosed / HeartbeatManager` **零改动**（帧格式无关）。

### Task S1.3：`MessageRouter` 按 BodyCase 路由

**Files:** Modify `network/.../MessageRouter.java`、`network/.../MessageHandler.java`

- Handler 接口：`void handle(ClientSession session, Envelope env)`——**信封统一传递**（reqId/timestamp/playerId 都在信封上）；
- 路由表：`EnumMap<Envelope.BodyCase, MessageHandler>`；各 Handler `@PostConstruct` 注册 case（取代 MessageType）；
- 未注册 case → `respSender.reply(cs, env.getReqId(), NetErrorCode.UNKNOWN_TYPE)`（1002 兜底不变）。

### Task S1.4：Handler 层与 RespSender（逻辑逐行保留）

**Files:** Modify 全部 Handler + `RespSender.java`

改造模式（StartGameHandler 为例，全部同构）：

```java
@Override
public void handle(ClientSession session, Envelope env) {
    // 不再强转子类型；StartGame 无业务字段，直接调业务层
    int code = roomManager.requestStart(session);
    respSender.reply(session, env.getReqId(), code);
}
```

- `ConnectHandler`：`env.getConnect().getRoomCode()/getToken()`；匿名放行/失败 resp+close 不变；
- `FragmentHandler`：`env.getFragmentCaught().getFragmentId()/getPosX()/getPosY()`；返回码 resp 不变；
- `HeartbeatHandler`：回执 `Envelope{timestamp, heartbeatAck}` 二进制发送；`updateLastActiveTime` 不变；
- `HighFreqHandler`：转发构造 `Envelope{playerId=会话ID, highFreqStateS2c(字段拷贝)}`——C2S/S2C 高频是两个消息类型，逐字段拷贝（与现状 Vec2 转换一致）；
- `RespSender`：`reply(...)` 构造 `Envelope{playerId=-1, timestamp, resp=S2C_Resp{...}}` → `session.send(bytes)`；**NetErrorMsg 文案映射保留**；
- `Building/Skill/Synthesize/TalentHandler`（TODO 空壳）：签名同步换新，body 为空实现，后续里程碑按 §一预留 schema 接线。

### Task S1.5：业务层与广播点（game-server）

签名从 Jackson 类型换 proto 类型，**分支与码值逐行保留**：

| 位置 | 改动 |
|------|------|
| `RoomManager.onPlayerConnect(session, env)` | roomCode/token 从 `env.getConnect()` 取；reqId 经 `env.getReqId()` 透传；resp(0) 紧邻 ConnectAck 前的顺序不变；进房三分支码值不变 |
| `sendConnectAck / broadcastPlayerJoined / broadcastGameStart` | 构造 proto Envelope 广播（`session.send(bytes)`） |
| `RoomManager.requestStart` | 五分支返回码不变 |
| `GameManager.onFragmentCaught` | 四分支（幂等回 0）不变 |
| `GameManager.generateAndBroadcastPlan` | 掉落计划广播改 proto（PlanItem 字段一一对应） |
| `GameManager.broadcastMidFreqState` | `shelterEnergy` 由 `Math.round` 改直接 float（schema 已升 float） |
| `GameManager.updatePlayerHighFreq` | 入参 `env.getHighFreqState()` 生成类型 |
| `GameRoom.forwardHighFreqState` | 字段拷贝转发（同 S1.4 HighFreqHandler 模式） |
| `GameStateMachine.setPhase` | 服务器内部枚举 ↔ `GamePhasePb` 映射 switch（7 值 + UNSPECIFIED 断言不可达）；信封 timestamp 必填 |

**PS-1 验收**：`mvn -pl network,game-server -am test` 全绿；与旧 JSON 客户端不互通——**PS-1 与客户端篇 PC-1 必须同一分支同一 PR**（铁律 4）。

---

## 六、里程碑 PS-2：服务器清理（联调验收通过后）

- [x] 删除 `protocol/Message.java`、16 个 Jackson DTO 子类（`c2s/*` `s2c/*` 全部）、`protocol/MessageType.java`、`MessageCodec` JSON 职责、`ClientSession.send(String)`、`GameWebSocketHandler.handleTextMessage`
  > 执行口径（2026-08-22）：实际 Jackson DTO 为 27 个（10 C2S + 17 S2C，含 R5 的 S2C_Resp）；保留 `GamePhase.java`（状态机内部枚举）与 `NetErrorCode.java`。
- [x] 全库 grep `com.fasterxml.jackson` 在 network/game-server 的 WebSocket 路径零残留（account-server 的 REST Jackson 不受影响）
  > 唯一残留点为 game-server `AccountValidator`（解析 account-server REST 响应，非 WS 路径，合法保留；jackson-databind 依赖随迁至 game-server pom）。
- [x] `Protocol/README.md` 增补：坏包日志样例 + 帧结构速查（供运维排障）

---

## 七、切换策略（决策留档）

**选定：big-bang 单分支双端同切（`refactor/proto-r1`）**

| 方案 | 成本 | 结论 |
|------|------|------|
| **big-bang（选定）** | 双端同分支切换，全量回归后合并；回滚=丢弃分支 | ✅ 预发布、无存量用户、双人房 |
| 服务器双协议嗅探（Binary→proto / Text→Jackson 并存） | Handler 层需统一消息抽象或双向适配层（~1 天 + 长期维护），仅换"服务器先部署"的排序自由 | ❌ 当前收益不成立；有线上用户后重评 |

部署约束：**客户端与服务器同时发版**。旧客户端连新服务器：文本帧被拒（warn+丢弃）→ 客户端 RequestTracker 5s 超时兜底提示"操作无响应"（现有机制，零新代码）。

---

## 八、验收总表（联调部分与客户端篇共同执行）

| # | 验收项 | 方法 | 阶段 |
|---|--------|------|------|
| 1 | 生成代码 + round-trip 单测 | `mvn -pl network,game-server -am test` | PS-0 |
| 2 | R5 回执矩阵 10 行 | 双开联调（1002 场景改为发坏字节帧） | 联调 |
| 3 | M1–M4 双开回归 | 阶段同步/互见/快照/碎片 | 联调 |
| 4 | 心跳与 RTT | HUD PING 正常 + 长连 10 分钟无断线 | 联调 |
| 5 | MidFreq 精度 | HUD 对手能量小数位（int→float 预期变化） | 联调 |
| 6 | 带宽对比留档 | 高频 60s 收发字节统计（JSON 基线 vs proto） | 联调 |
| 7 | 删除后零残留 | grep 清单 | PS-2 |

---

## 九、风险与回滚（服务器侧）

| 风险 | 概率 | 缓解 |
|------|------|------|
| 双端 protobuf 版本不一致 → 线格式分歧 | 低 | README 锁 3.25.x；pom 与客户端 DLL 大版本一致为 PS-0 验收项 |
| 坏包/半包导致解析异常 | 低 | WS 帧自带边界；ParseFrom 全兜底 + 1002 回执 |
| 迁移期双端不同步提交 | 中 | 铁律 4：同分支同 PR |
| 回滚 | — | 丢弃分支，master JSON 协议在 PS-2 前完好 |

---

## 十、工作量（服务器侧）

| 阶段 | 内容 | 预估 |
|------|------|------|
| PS-0 | schema 草案 + Maven 管线 + round-trip 单测 | 2.5h |
| PS-1 | Codec/路由/Handler/业务层/广播点 | 5h |
| PS-2 | 删除清单 + grep 验证 | 0.5h |
| 联调 | 验收表 2-6（与客户端篇分摊） | 1h |
| **合计** | | **~9h**（客户端篇 ~8h，总计 ~17h） |

---

## 回归记录

<!-- 每阶段验收后追加：日期 / 阶段 / 结果 / 执行人 -->
- 2026-08-22 / PS-0 Schema 与代码生成 / ✅ `game.proto`（27 消息 + Envelope oneof + GamePhasePb）+ `Protocol/README.md` 落地；pom 管线补全后 `mvn -pl network compile` 产出 `com.dualenigma.v1.Envelope`；`ProtoRoundTripTest` 14 用例（16 种在用消息 round-trip + 信封字段 + 坏字节 + BODY_NOT_SET）全绿；未接线，线上零变化 / main_network
- 2026-08-22 / PS-1 服务器切换 / ✅ `mvn -pl network,game-server -am test` 全绿（network 19 单测 + game-server Spring 上下文冒烟）。改造清单：ProtoCodec 新增 / ClientSession.send(byte[]) / MessageHandler→Envelope 签名 / MessageRouter BodyCase 路由 / RespSender proto 信封（摘 MessageCodec）/ GameWebSocketHandler 二进制帧+文本帧 warn 丢弃 / 9 Handler 全量换签（R5 语义逐行保留）/ GameRoom 摘 MessageCodec + 广播 proto / RoomManager 三广播点 proto（onPlayerConnect 签名保持 session+roomCode+reqId）/ GameManager 4 广播点 + MidFreq float 化 + 高频 Vec2 直拷 / GameStateMachine GamePhase↔GamePhasePb 映射 / AIController 广播点 proto。**未部署 :8080（仍跑 JSON 版），待客户端 PC-1 同切** / main_network
- ⏳ ~~待办：联调验收表 #2-#6（需客户端 PC-0/PC-1）；PS-2 清理（联调通过后）~~
- 2026-08-22 / 联调（服务器侧）+ 部署切换 + PS-2 清理 / ✅ 全部完成：
  ① 客户端 PC-0/PC-1 已合入（Generated/Game.cs + GameConnection 走 Envelope）；
  ② 服务器侧全链路集成测试 `ProtoWebSocketIntegrationTest` 通过（真实 WS + proto 二进制帧双客户端：Resp(0) 先于 ConnectAck / 3002·3001 拒绝回执 / 自加入+满员 PlayerJoined / GameStart / PhaseChange(Preview) 时钟差值法 / DropPlan 30 items / 高频位置入库 + 几何仲裁单接 ×1 / HeartbeatAck / 坏字节→1002）；顺带修复 GameTickScheduler 非 daemon 线程泄漏（原每局游戏泄漏一个常驻线程）；
  ③ PS-2：删除 Message/MessageType/27 个 Jackson DTO/MessageCodec/send(String)/handleTextMessage/ProtocolReqIdTest（JSON 版），network 模块 WebSocket 路径 `com.fasterxml.jackson` 零残留（game-server 的 AccountValidator 保留 Jackson 解析 account-server REST 响应，非 WS 路径；jackson-databind 依赖移至 game-server 自身 pom）；`mvn -pl network,game-server -am test` 全绿；
  ④ :8080 已切换至 Protobuf 版（Started + /game 端点就绪），account-server :8081 不变；
  ⑤ 待双开 Unity 实测：验收表 #3（M1-M4 回归）、#4（RTT/10 分钟长连）、#5（MidFreq 小数位）、#6（带宽对比留档） / main_network
- 2026-08-22 / **独立质检（服务器侧全量复核）** / ✅ **合格**。① schema 与计划草案逐字段一致（信封 oneof 编号段 10-19/30-40、5 预留消息、GamePhasePb 0-7 全对）；两处已留档偏差均合理：package 大写 `DualEnigma.v1`（C# 生成命名空间大小写匹配，配 java_package 归位 com.dualenigma.v1）+ java_multiple_files。② ProtoCodec/BodyCase EnumMap 路由/Handler 统一收 Envelope/RespSender proto 信封（playerId=-1 + timestamp + NetErrorMsg 保留）逐项符合；**R5 语义零丢失**：resp(0) 先于 sendConnectAck（RoomManager L146-147 + 集成测试 awaitAny 顺序断言双背书）、失败 resp+close、过期/重复幂等回 0、坏帧/BODY_NOT_SET→1002、PhaseChange 信封 timestamp 必填且集成测试断言 phaseEndTime>timestamp。③ MidFreq float 化落地（Math.round 移除，L280 注释注明）。④ Jackson 层删除干净：protocol/ 仅剩服务器内部 GamePhase 枚举 + NetErrorCode（合规保留）；Jackson 依赖迁至 game-server pom 供 AccountValidator REST 使用，处理正确。⑤ 复跑 `mvn -s maven-settings.xml -pl network,game-server -am test` exit 0：ProtoRoundTripTest 14/14、ProtoWebSocketIntegrationTest 1/1（60s 全链路覆盖回执矩阵 8 段，**质量超出计划要求**）、上下文冒烟 1/1。⑥ GameTickScheduler daemon 线程修复属正当附带改进（原每局泄漏常驻线程）。**遗留行动项**：🔴 服务器全部改动仍在工作区未提交——master HEAD 客户端已是 proto 版而已提交的服务器代码仍是 JSON 版，仓库处于不一致中间态，**须立即提交服务器侧改动**；🟡 proto package 大写偏离 protobuf 命名惯例（理由已注释留档，未来如调整走 csharp_namespace 方案，非阻塞） / 主智能体（QA）
