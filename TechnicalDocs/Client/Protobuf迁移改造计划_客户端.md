# Protobuf 迁移改造计划 — 客户端篇

> **制定日期**: 2026-08-22
> **执行者**: main_network（实现）+ main_qa（验收）
> **配套文档**: 《TechnicalDocs/Server/Protobuf迁移改造计划_服务器.md》（服务器篇——**完整 `game.proto` 草案、schema 演进规则、切换策略（big-bang 决策）均在该篇**，本篇不重复）
> **前置阅读**: `TechnicalDocs/网络框架重构计划.md`（R1–R5 已完成——Protocol 层独立、RequestTracker/回执体系正是为本迁移铺路）

**Goal（客户端范围）:** 游戏连接的编解码从「`NetJson`/`JsonUtility` 文本帧」迁移到「Google.Protobuf 生成类型 + 二进制帧」：运行时 DLL 入库 → `gen-proto.ps1` 生成 C#（产物入库）→ `WebSocketConnection` 二进制化 → `GameConnection` 切 oneof 分发。**消息语义零变化**：reqId/RequestTracker、进房看门狗、心跳 RTT、PhaseChange 时钟差值法全部保留；**RequestTracker / RoomSession / NetConnError / UI 层零改动**。

**Tech Stack:** Google.Protobuf 3.25.x（netstandard2.0 DLL，与服务器 pom 大版本锁定一致）· Unity 2022.3（AOT/IL2CPP 友好）

---

## 〇、铁律（客户端侧）

1. **`.proto` 是唯一事实来源**：禁止手改 `Generated/Game.cs`；改协议 = 改 `Protocol/proto/game.proto` → 跑 `gen-proto.ps1` → 与服务器 regen 产物同 PR。
2. **JsonUtility 不退场，只退协议**：REST（AuthService/FriendApiService/SocialNotifyService）继续 JSON，本篇不碰。
3. **行为语义零变化**：发送 API 签名（`ConnectToRoom/RequestStartGame/SendHighFreqState/SendFragmentCaught/Disconnect`）与回执处理逐行保留；验收以《网络框架重构计划》R5 的 10 行矩阵 + M1–M4 回归为准。
4. **与服务器篇同分支同 PR**（`refactor/proto-r1`）：二进制协议不兼容 JSON，双端必须同时发版（决策详见服务器篇 §七）。
5. **旧 JSON 路径保留到 PC-2** 再删，回滚 = 丢弃分支。

---

## 一、目标架构（客户端侧前后对比）

```
【现在】
C# DTO ──JsonUtility.ToJson──> 文本帧 ──WS──> 服务器 Jackson
NetMessageRegistry（字符串 type 注册表）分发

【目标】
Protocol/proto/game.proto（唯一事实来源，服务器篇 §四）
   └─> Client/tools/gen-proto.ps1 → Network/Protocol/Generated/Game.cs（入库）

C# 调用生成类型（Envelope oneof）──ToByteArray()──> 二进制帧 ──WS──> 服务器 parseFrom
GameConnection 按 env.BodyCase switch 分发（编译期枚举路由，注册表退役）
```

## 二、文件结构（客户端侧）

```
Client/
├── Assets/Plugins/Google.Protobuf/
│   ├── Google.Protobuf.dll               # 运行时（netstandard2.0）+ .meta
│   ├── System.Runtime.CompilerServices.Unsafe.dll  # Protobuf 依赖（4.5.3/netstandard2.0）+ .meta
│   └── linker.xml                        # IL2CPP stripping 保留配置
├── tools/
│   ├── gen-proto.ps1                     # C# 代码生成脚本
│   └── protoc/                           # protoc.exe（.gitignore，README 给下载指引）
└── Assets/Scripts/
    ├── Network/Protocol/
    │   ├── Generated/Game.cs             # ⬅ 生成产物（入库，禁止手改）
    │   ├── ProtoMapping.cs               # 手写：GamePhasePb↔GamePhase、AnimState 收口
    │   └── Tests/ProtoRoundTripTests.cs  # EditMode 单测
    └── Framework/Network/
        └── WebSocketConnection.cs        # 改：二进制收发（仅传输边界）

【PC-2 删除】Protocol/C2SMessages.cs、S2CMessages.cs、NetEnvelope.cs、NetJson.cs、
             NetMessageRegistry.cs（+Tests）；NetProtocolTypes.cs 仅删 NetProto 常量类
             （NetErrorCode / NetworkRole 枚举保留——RequestTracker/UI 仍引用）
```

---

## 三、里程碑 PC-0：运行时与代码生成

### Task C0.1：Google.Protobuf DLL 入库

**Files:**
- New: `Client/Assets/Plugins/Google.Protobuf/Google.Protobuf.dll`（+ .meta）
- New: `Client/Assets/Plugins/Google.Protobuf/System.Runtime.CompilerServices.Unsafe.dll`（+ .meta）
- New: `Client/Assets/Plugins/Google.Protobuf/linker.xml`

1. 从 NuGet 包 `Google.Protobuf 3.25.5` 提取 `lib/netstandard2.0/Google.Protobuf.dll`；
1a. 从 NuGet 包 `System.Runtime.CompilerServices.Unsafe 4.5.3` 提取 `lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll`（程序集版本 4.0.4.1，与 Protobuf 引用令牌精确匹配；netstandard2.1 profile 不含此程序集，缺它 Unity 报 `Unable to resolve reference` 拒载 Protobuf）；
2. **版本锁**：与服务器 pom 的 protobuf-java 大版本一致（3.25.x），写入 `Protocol/README.md`；
3. IL2CPP：生成代码为纯 C# 无动态编译，AOT 友好；stripping 开启时 linker.xml 保留：

```xml
<linker>
  <assembly fullname="Google.Protobuf" preserve="all"/>
  <assembly fullname="System.Runtime.CompilerServices.Unsafe" preserve="all"/>
</linker>
```

- [ ] Step C0.1.1 DLL + linker.xml 入库，Unity 编译通过。

### Task C0.2：`gen-proto.ps1` 生成 C#

**Files:**
- New: `Client/tools/gen-proto.ps1`
- New（生成）: `Client/Assets/Scripts/Network/Protocol/Generated/Game.cs`

```powershell
# 在 Client/tools/ 下执行；protoc.exe 放 tools/protoc/（gitignore，版本 25.x，README 注明）
$protoc = "$PSScriptRoot/protoc/protoc.exe"
& $protoc `
  --proto_path="$PSScriptRoot/../../Protocol/proto" `
  --csharp_out="$PSScriptRoot/../Assets/Scripts/Network/Protocol/Generated" `
  --csharp_opt=file_extension=.cs,base_namespace=DualEnigma.Network.Proto `
  "$PSScriptRoot/../../Protocol/proto/game.proto"
```

- [ ] Step C0.2.1 前置：服务器篇 Task S0.1 的 `game.proto` 已入库；本脚本执行成功生成 `Game.cs`，Unity 编译 0 error。
- [ ] Step C0.2.2 `.gitignore` 追加 `Client/tools/protoc/`；生成物 `Game.cs` 入库（文件头加"禁止手改"注释由脚本追加或 code review 把关）。

**PC-0 验收**：编译通过；此阶段未接线，线上行为零变化。

---

## 四、里程碑 PC-1：客户端切换

### Task C1.1：`WebSocketConnection` 二进制化（Framework 层，仅传输边界）

**Files:** Modify `Framework/Network/WebSocketConnection.cs`

| 改动点 | 说明 |
|--------|------|
| `SendAsync(string)` → `SendAsync(byte[] payload)` | `ArraySegment<byte>` 二进制帧；`_sendLock` 串行化保留 |
| 接收循环 | `result.MessageType == Text` → log warn 丢弃（协议错误）；Binary 正常拼包入队；Close 语义不变 |
| `OnMessageReceived` | `Action<string>` → `Action<byte[]>`（断线 null 标记语义不变） |
| 心跳 | `StartHeartbeat(float, Func<byte[]>)`——payloadFactory 由上层包 proto 信封 |

超时/锁/主线程泵/主动关闭语义/`NotifyHeartbeatAck()` RTT **零改动**——传输层不感知协议内容。

### Task C1.2：`GameConnection` 组装切换

**Files:** Modify `Network/Session/GameConnection.cs`

- **发送侧**（五个 API 签名不变，内部构造 Envelope）：

```csharp
public void RequestStartGame()
{
    if (!_conn.IsConnected) { /* 现逻辑 */ }
    var env = new Envelope
    {
        ReqId = _tracker.Register(NetProto.StartGame, REQUEST_TIMEOUT, OnStartGameResp),
        StartGame = new C2S_StartGame(),
    };
    _ = _conn.SendAsync(env.ToByteArray());
}
```

（`NetProto.StartGame` 等字符串常量在 PC-2 前保留，仅作 RequestTracker 的 source 标识，不再进线协议。）

- **接收侧**：`OnRawMessage(byte[])` → `Envelope.Parser.ParseFrom(bytes)`（try/catch → log 丢弃）→ 按 `env.BodyCase` switch 分发。**11 个 handler 的处理逻辑逐行搬运自现 `RegisterHandlers()`，仅取值方式变化**（对照速查表见 §六）；
- **心跳**：`StartHeartbeat(HEARTBEAT_INTERVAL, () => new Envelope { Heartbeat = new C2S_Heartbeat() }.ToByteArray())`；`S2C_HeartbeatAck` case → `_conn.NotifyHeartbeatAck()`；
- **PhaseChange**：`remaining = (env.PhaseChange.PhaseEndTime - env.Timestamp) / 1000f`——时钟差值法语义不变（信封 timestamp 仍是服务器时钟）；
- `NetMessageRegistry` 调用退役（oneof case 是编译期枚举，switch 即路由）。

### Task C1.3：`S2C_MidFreqState` 精度适配

proto 侧 `shelter_energy` 为 float（JSON 时代服务器 `Math.round` 成 int）→ `RoomSession.UpdateOpponentStats(int hp, float shelterEnergy)` 签名不变，HUD 对手能量显示出现小数——**预期行为变化**（服务器篇 Task S1.5 同步去 round），验收确认 `UIGameHudCtrl.ApplyVitals` 无 int 假设（已核对：本就按 float 处理）。

### Task C1.4：`ProtoMapping` 枚举映射

**Files:** New: `Network/Protocol/ProtoMapping.cs`（手写）

```csharp
public static class ProtoMapping
{
    /// <summary>GamePhasePb → 本地 GamePhase（UNSPECIFIED → Preview + LogWarning）</summary>
    public static GamePhase ToGamePhase(GamePhasePb pb);
    /// <summary>AnimState 字符串收口（现 Enum.TryParse 逻辑迁移至此）</summary>
    public static AnimState ToAnimState(string animState);
}
```

### Task C1.5：Fragment 消息适配

`S2C_FragmentDropPlan / S2C_FragmentResult` 两个 handler 内的"DTO → 本地结构"转换改为"生成类型 → 本地结构"：`FragmentDropPlan{FragmentId, Type, Position, DropTime, Seed}` 字段名不变仅取值来源变；`FragmentSystem.ExecuteDropPlan / OnFragmentCollected` **零改动**。

### Task C1.6：EditMode 单测

**Files:** New: `Network/Protocol/Tests/ProtoRoundTripTests.cs`

- 与服务器篇 `ProtoRoundTripTest` **镜像**：16 种消息构造 → ToByteArray → ParseFrom → 字段断言；重点覆盖信封字段（reqId/timestamp/playerId）与 oneof 互斥性；
- **体积断言**：`C2S_HighFreqState` 满字段序列化 < 100 字节（JSON 时代 ~200B——带宽收益的量化锚点）。

**PC-1 验收**：Unity 编译 0 error；EditMode 全绿；与服务器篇 PS-1 合并后双开联调（见 §五）。

---

## 五、联调验收（与服务器篇同分支共同执行）

| # | 验收项 | 方法 |
|---|--------|------|
| 1 | R5 回执矩阵 10 行 | 双开（1002 场景：调试端发坏字节帧） |
| 2 | M1–M4 双开回归 | 阶段同步/互见移动/中频快照/碎片掉落拾取 |
| 3 | 心跳与 RTT | HUD PING 正常量级；长连 10 分钟 |
| 4 | MidFreq 精度 | 对手能量小数位出现（预期） |
| 5 | 帧体积与带宽 | 单测断言 <100B；双开 60s 高频收发字节统计 vs JSON 基线留档 |
| 6 | 兼容兜底 | 故意连 JSON 老客户端（如有）→ 服务器拒文本帧 → 客户端 5s 超时提示（RequestTracker 现有机制） |

---

## 六、客户端消息契约速查表（旧 DTO → proto 访问器）

完整 schema 见服务器篇 Task S0.1；本表为客户端实现时的高频对照（`env` 为 `Envelope`）：

| 消息 | 旧取值 | 新取值（生成访问器） |
|------|--------|---------------------|
| ConnectAck | `msg.data.playerId/roomCode` | `env.ConnectAck.PlayerId / .RoomCode` |
| PlayerJoined | `msg.data.playerId/playerCount` | `env.PlayerJoined.PlayerId / .PlayerCount` |
| GameStart | `msg.data.chapter/section/round` | `env.GameStart.Chapter / .Section / .Round` |
| PhaseChange | `msg.timestamp`、`msg.data.phase(string)/phaseEndTime` | `env.Timestamp`（long）、`env.PhaseChange.Phase`（GamePhasePb→ProtoMapping）、`.PhaseEndTime` |
| HighFreqState | `msg.data.playerId/position/velocity/animState/facing` | `env.HighFreqStateS2c.PlayerId / .Position / .Velocity / .AnimState / .Facing` |
| MidFreqState | `msg.data.players[i].hp/shelterEnergy(int)` | `env.MidFreqState.Players[i].Hp / .ShelterEnergy`（**float**） |
| OpponentDisconnect | `msg.playerId`（顶层）、`msg.data.state` | `env.PlayerId`（信封）、`env.OpponentDisconnect.State` |
| FragmentDropPlan | `item.fragmentId/type/position/dropTime/seed(long→uint截断)` | `item.FragmentId / .Type / .Position / .DropTime / .Seed`（long，截断逻辑保留） |
| FragmentResult | `msg.data.playerId/fragmentId` | `env.FragmentResult.PlayerId / .FragmentId` |
| HeartbeatAck | —（仅触发 RTT） | `env.HeartbeatAck` case → `NotifyHeartbeatAck()` |
| Resp | `msg.data.reqId/code/message` | `env.Resp.ReqId / .Code / .Message` |
| C2S_Connect 发送 | `data.roomCode/token` | `new Envelope{ ReqId=…, Connect = new C2S_Connect{ RoomCode=…, Token=… } }` |
| C2S_HighFreqState 发送 | `data.position/velocity/animState/facing/hp/shelterEnergy` | `HighFreqState = new C2S_HighFreqState{ Position=new Vec2{X=,Y=}, … }` |
| C2S_FragmentCaught 发送 | `data.fragmentId/posX/posY` | `FragmentCaught = new C2S_FragmentCaught{ FragmentId=, PosX=, PosY= }` |

> 生成 C# 的字段为 PascalCase（proto snake_case 自动转），Vec2 为 `X/Y`；数值类型一一对应（int32→int / int64→long / float→float）。

---

## 七、里程碑 PC-2：客户端清理（联调验收通过后）

- [ ] 删除：`C2SMessages.cs`、`S2CMessages.cs`、`NetEnvelope.cs`、`NetJson.cs`、`NetMessageRegistry.cs`（+ Tests）；`NetProtocolTypes.cs` 仅删 `NetProto` 常量类
- [ ] 全库 grep：`Network/` 下无 `JsonUtility` 协议残留（REST 层保留）；无 `NetJson` 引用
- [ ] （可选，建议同批）Editor 调试菜单：`DualEnigma > 网络 > 帧转储`——选最近 N 帧打印 `env.ToString()`（protogen 自带文本格式），弥补二进制不可读

---

## 八、风险与回滚（客户端侧）

| 风险 | 概率 | 缓解 |
|------|------|------|
| IL2CPP/裁剪剥掉 protobuf 运行时 | 中 | linker.xml preserve（Task C0.1）；Editor + IL2CPP 双构建设备 |
| 生成物被手改 | 中 | 文件头"禁止手改"注释 + README 规则；code review 盯 |
| protoc 本机缺失阻塞他人 | 低 | 生成物入库（改协议才需要 protoc）；README 给下载指引 |
| 解析坏帧崩溃 | 低 | `ParseFrom` try/catch → log 丢弃；WS 帧自带边界无半包 |
| 回滚 | — | 丢弃 `refactor/proto-r1` 分支；JSON 路径 PC-2 前完好 |

---

## 九、工作量（客户端侧）

| 阶段 | 内容 | 预估 |
|------|------|------|
| PC-0 | DLL 入库 + 生成脚本 + 编译验证 | 1.5h |
| PC-1 | 传输二进制化 + GameConnection 切换 + 映射/适配 + 单测 | 5h |
| 联调 | §五 6 项（与服务器篇分摊） | 1h |
| PC-2 | 删除清单 + grep + 调试菜单 | 0.5h |
| **合计** | | **~8h**（服务器篇 ~9h，总计 ~17h） |

---

## 回归记录

<!-- 每阶段验收后追加：日期 / 阶段 / 结果 / 执行人 -->
- 2026-08-22 PC-0：✅ 分支 `refactor/proto-r1`；Google.Protobuf 3.25.5（netstandard2.0 DLL 462KB + linker.xml）入库；`Protocol/proto/game.proto` 按服务器篇 S0.1 草案逐字落盘 + `Protocol/README.md`（演进规则/版本锁定/排障）；`gen-proto.ps1` 修正 base_namespace 计划笔误（原计划 `DualEnigma.Network.Proto` 与 proto package `dualenigma.v1` 不兼容，实为不传 base_namespace → 产物命名空间 `Dualenigma.V1` 平铺无子目录）；`Game.cs`（273KB）生成入库含禁改头。Unity 编译验证待所有者执行。
- 2026-08-22 PC-1：✅ `WebSocketConnection` 二进制化（`SendAsync(byte[])`/`OnMessageReceived(byte[])`/MemoryStream 分片拼包/文本帧 warn 丢弃/心跳 payloadFactory 二进制化，超时锁泵语义零改动）；`GameConnection` 切 Envelope oneof（5 发送 API 构造 Envelope、`ParseFrom` 坏帧 try/catch 丢弃、`BodyCase` switch 11 handler 逐行搬运；生成代码 case 名实测为 `HighFreqStateS2C` 大写 C 已对齐）；`ProtoMapping`（GamePhasePb/AnimState/Vec2 映射收口）；MidFreq `ShelterEnergy` float 精度直通；`ProtoRoundTripTests` 16 用例（信封字段/oneof 互斥/16 消息 round-trip/坏帧抛出断言/高频帧 <100B 体积断言）。EditMode 全绿与双开联调待所有者执行。
- 2026-08-22 PC-2：✅ 删除 `C2SMessages/S2CMessages/NetEnvelope/NetJson/NetMessageRegistry/INetMessage`（+ Registry Tests，共 7 组文件）；`NetProtocolTypes` 的 NetProto 常量类瘦身为"RequestTracker source 标识"（C2S 5 条，S2C 常量删除）；全库 grep `NetJson|NetEnvelope|NetMessageRegistry|INetMessage|NetVec2` 零残留（Generated 除外）。**联调验收 §五 6 项待所有者双开执行后回填**。
- 2026-08-22 / **独立质检（客户端侧全量复核，提交 95c90a1..75d15b5）** / ✅ **合格**。① PC-0：DLL 3.25.5 + **System.Runtime.CompilerServices.Unsafe.dll 依赖补入库**（好于计划）+ linker.xml preserve + protoc gitignore + Game.cs 禁改头齐全，命名空间 `DualEnigma.V1` 全局一致（Pb 别名引用）；双端版本锁核实（pom protobuf-java/protoc 3.25.5 = DLL 3.25.5，README 留档）。② PC-1：WebSocketConnection 二进制化仅动传输边界（byte[] 队列/Binary 帧/文本帧丢弃/心跳 factory 二进制，超时锁泵语义不变）；GameConnection 11 个 BodyCase handler 与 JSON 版逐行对齐——PhaseChange 时钟差值法（env.Timestamp 信封 + PhaseEndTime）、ConnectAck 取消看门狗、FragmentResult 本地玩家过滤、DropPlan seed long→uint 截断保留、MidFreq float 直通，全部语义正确；发送侧 5 API 签名不变，reqId 登记/回执派发/看门狗/DisconnectWithReason 与 R5 版零差异。③ **零改动承诺核实**：RequestTracker/RoomSession/NetConnError/Rest/ 自 946cad8 起 diff 为空（git 验证）；REST JsonUtility ×14 处保留。④ PC-2：JSON 协议层删除后全库 grep 零残留；NetErrorCode/NetworkRole 枚举按计划保留。⑤ ProtoRoundTripTests 16 用例含 `Assert.Less(bytes.Length, 100)` 高频帧体积断言。⑥ **编译验证**：ScriptAssemblies 四程序集（DualEnigma/DualEnigma.Framework/DualEnigma.Tests/DualEnigma.Network.Tests）15:13-15:20 重编译成功；asmdef 三连修（预编译引用/独立测试程序集/移除 optionalUnityReferences）过程曲折但终态结构正确。**遗留**：🟡 联调 §五 6 项中 #1/#2/#6 可由服务器侧集成测试部分背书，#3（M1-M4 双开）/ #4（10 分钟长连 RTT）/ #5（MidFreq 小数位观察）/ 带宽对比留档仍需所有者在 Unity 双开环境执行后回填；🔴 依赖服务器侧工作区改动尽快提交（详见服务器篇质检记录） / 主智能体（QA）