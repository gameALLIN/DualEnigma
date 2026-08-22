# Protocol — 对局 WebSocket 协议（双端共享）

`proto/game.proto` 是 game-server WebSocket 通道的**唯一事实来源**（proto3 `Envelope` oneof 信封，二进制帧）。
服务器与客户端的全部消息定义、字段编号、演进规则以本目录为准。

## 生成物

| 端 | 生成方式 | 产物 | 入库 |
|----|---------|------|------|
| 服务器 (Java) | Maven `protobuf-maven-plugin`（protoc 自动下载，本机零安装） | `Server/network/target/generated-sources`（`com.dualenigma.v1.*`） | ❌ 不入库 |
| 客户端 (C#) | `Client/tools/gen-proto.ps1` | `Assets/.../Protocol/Generated/Game.cs` | ✅ 入库 |

## 重新生成

```bash
# 服务器：Protocol/proto 变更后
cd Server && mvn -pl network compile

# 客户端：见 TechnicalDocs/Client/Protobuf迁移改造计划_客户端.md
```

## 演进规则（铁律）

1. **只加不改**：新增字段用新编号，旧编号永不复用、永不改类型；
2. **oneof 新消息用编号段尾部**：C2S = 10-29，S2C = 30-59；
3. `.proto` 变更 PR 必须同时包含**双端 regen 产物与受影响调用方**（禁止单端绕过 schema 加字段，禁止手改生成物）；
4. 不兼容变更 → 升 `v2` 包名（`dualenigma.v2`），双端同步切换；
5. **双端 protobuf 大版本锁定 3.25.x**：服务器 pom `protobuf-java 3.25.5` 与客户端 Google.Protobuf DLL 大版本必须一致（防线格式分歧）。

## 帧语义速查

- 每个二进制帧 = 一个 `Envelope`；`oneof body` 的 case 即路由键；
- `timestamp`（信封）：S2C 必填；`PhaseChange` 剩余时间 = `phase_end_time - timestamp`（时钟差值法）；
- `req_id`（信封）：C2S 请求关联号，`S2C_Resp` 回显；心跳/高频流豁免（恒 0）；
- `player_id`（信封）：S2C 广播方（-1=系统广播；OpponentDisconnect=离开者）；C2S 不填；
- `S2C_Resp.code`：`NetErrorCode` 码值表（0 成功 / 1002 未知类型 / 2001-2003 房间 / 3001-3003 开局 / 4002 碎片）。

## 排障

- 坏包/半包：WebSocket 帧自带边界；`Envelope.parseFrom` 全兜底 → 服务器回 `S2C_Resp{code=1002}` 并记 `Malformed envelope` 日志；
- 客户端发文本帧：服务器 warn `Unexpected text frame` 并丢弃（协议已切换为二进制）。
