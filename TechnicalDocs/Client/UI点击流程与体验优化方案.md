# UI 点击流程与体验优化方案

> 基于代码实况梳理（2026-08-16）。范围：`Client/Assets/Scripts/UI/` 全部 6 面板 + 对局内 UI 缺口。

---

## 一、现有 UI 面板清单

| 面板 | 层级/模式 | 职责 | 状态 |
|---|---|---|---|
| UILogin | Normal / FullScreen | 注册/登录，模式切换 | ✅ 可用 |
| UIHome | FullScreen | 账号信息 + 开始/开房/好友/退出 | ✅ 可用 |
| UIFriends | FullScreen | 好友列表/搜索/申请，5s 轮询 | ✅ 可用 |
| UIInvitePopup | **Top 常驻**（不进栈） | 全局邀请卡/申请卡对账 | ✅ 可用 |
| UIRoom | FullScreen | 房间码/邀请/房主开局/等待 | ✅ 可用 |
| UITest | FullScreen | 框架测试面板 | 🔧 遗留，未接线 |
| **游戏内 HUD** | — | **不存在** | ❌ 缺失 |
| **结算面板** | — | **不存在**（GameEndEvent 无订阅者） | ❌ 缺失 |
| **暂停菜单** | — | **不存在** | ❌ 缺失 |

---

## 二、当前点击流程（实况）

### 流程 1：注册 / 登录

```
启动 → UILogin
├─ [切换模式] 登录 ⇄ 注册（标题/按钮文案/昵称框切换）
├─ [提交]
│   ├─ 用户名空 → "请输入用户名"
│   ├─ 密码<6位 → "密码至少 6 位"
│   ├─ 成功 → Push UIHome
│   └─ 失败 → 显示服务端错误
└─ ⚠️ 不支持 Enter 提交，必须鼠标点按钮
```

### 流程 2：主界面 → 单机开始

```
UIHome
├─ [开始游戏] → Pop(UIHome) → Pop(UILogin) → GameManager.StartGame()
│   └─ ⚠️ 全部 UI 关闭 → 无 HUD 裸奔对局；结束无出口
├─ [联机开房] → UIRoomCtrl.Prepare("", host=true) → Push UIRoom
├─ [好友] → Push UIFriends
└─ [退出登录] → Logout → Pop → 回 UILogin
```

### 流程 3：联机 · 房主线

```
UIHome → [联机开房] → UIRoom
├─ 连接中："正在创建房间..." → ConnectAck → 显示房间码
├─ [邀请好友] → Push UIFriends（叠加，房间保持）
│   └─ 好友行 [邀请] → CreateInvite → 对方弹邀请卡
├─ 好友进房 → PlayerJoined → 状态变"好友已就位"，StartBtn 解锁
├─ [开始对局] → RequestStartGame → 广播 GameStart → 双方关全部面板 → StartGame
└─ [退出房间] → Disconnect → Pop → 回 UIHome
```

### 流程 4：联机 · 客方线

```
UIInvitePopup（任意界面顶部弹出邀请卡）
├─ [接受] → AcceptInvite → Prepare(roomCode) → Push UIRoom → "等待房主开始游戏"
└─ [拒绝] → 下次轮询卡片消失
收到 GameStart → 关闭全部面板 → StartGame（无 HUD）
```

### 流程 5：好友面板

```
UIFriends（Close 按钮 Pop 返回）
├─ 搜索框 + [搜索] → 结果行 [添加] → 发申请
├─ 好友行 [邀请]（未进房间时提示先开房）/ [删除]（⚠️ 无二次确认）
├─ 申请区（SocialNotifyService 驱动）[接受]/[拒绝]
└─ 5s 轮询 + 全局通知事件刷新
```

### 流程 6：对局内（当前实况）

```
对局开始 → 所有 UI 已关闭
├─ 无 HP/能量显示 → 玩家不知道自己状态
├─ 无阶段倒计时 → 不知道当前阶段/剩余时间
├─ 无碎片计数 → 不知道携带材料
├─ 无 ESC 暂停/退出 → 单机也无法退出
└─ 对局结束 → GameEndEvent 无人接 → 画面停住，只能重启客户端
```

---

## 三、问题清单（按严重度）

### 🔴 P0 — 体验闭环断裂

| # | 问题 | 位置 | 影响 |
|---|---|---|---|
| 1 | 对局内零 HUD：HP/能量/阶段/碎片全不可见 | 无 | 盲玩，核心数值系统形同虚设 |
| 2 | 对局结束无出口：GameEndEvent 无 UI 订阅 | 无 | 胜利/失败后卡死，只能重启 |
| 3 | UIHome 开始游戏靠连续 Pop×2 硬编码关栈 | UIHomeCtrl.OnStartGameClicked | 结束后无法恢复主界面（UILogin 已被 Pop） |
| 4 | 对局中无退出手段（ESC 菜单缺失） | 无 | 单机死了也要硬玩到底 |

### 🟠 P1 — 交互缺陷

| # | 问题 | 位置 | 影响 |
|---|---|---|---|
| 5 | 删除好友无二次确认 | UIFriendsCtrl.OnDeleteFriendClicked | 误触直接删好友 |
| 6 | 登录不支持 Enter 提交 | UILoginCtrl | 输入法用户痛点 |
| 7 | 房间码无一键复制 | UIRoom | PC 端要肉眼抄码 |
| 8 | 断线只有文字提示，无 [重连]/[返回] 按钮 | UIRoom.OnServerDisconnected | 断线后卡在房间页 |
| 9 | 连接无超时处理 | UIRoom.ConnectToServer | 服务器挂了会一直"正在创建房间..." |
| 10 | 状态反馈仅 Console 日志（接受邀请失败等） | UIInvitePopup | 用户无感知 |
| 11 | 搜索结果复用好友行模板改按钮文案 | UIFriendsCtrl.RenderSearchResults | 维护隐患，按钮语义 hack |

### 🟡 P2 — 打磨项

| # | 问题 | 说明 |
|---|---|---|
| 12 | 无面板过渡动效 | 开关生硬 |
| 13 | 无按钮音效/悬停反馈 | 手感差 |
| 14 | 空状态无引导 | 好友 0 个时一片空白 |
| 15 | UITest 遗留面板 | 预制体+代码未清理 |

---

## 四、优化建议

### 方案 A：补齐对局闭环（P0，建议本迭代完成）

**A1. UIGameHUD（新建，Normal 层常驻，对局期间显示）**

```
┌──────────────────────────────────────────────┐
│ [水人HP条][能量条]   第1章1-1 · 收集阶段 12s   [火人HP条][能量条] │
│                                                │
│                    (游戏画面)                   │
│                                                │
│                              碎片: 冰×2 岩×1     │
└──────────────────────────────────────────────┘
```

- 数据源全部现成：`PhaseChangedEvent`（阶段名）、`GameStateMachine.PhaseRemainingTime`（倒计时）、`ShelterSystem`（HP/能量）、联机对方状态走 `NetworkSystem.OpponentHP/OpponentShelterEnergy`（M3 已实现）
- 事件驱动 + Update 刷新倒计时；GameStart 显示 / GameEnd 隐藏

**A2. UIGameOver 结算面板（新建，Top 层）**

- 订阅 `GameEndEvent`：胜利/失败标题 + 章节进度回顾
- 按钮：[再来一局]（单机重开 / 联机回房间）· [返回主界面]
- 返回主界面 = 恢复 UIHome（配合 A3 的栈改造）

**A3. UIHome 栈结构修正**

- StartGame 改为 `Hide` UIHome（保留在栈底）而非 Pop×2；对局结束 Pop 到 UIHome 为止
- 引入 `UIManager.SetGameMode(bool)` 或约定：对局期间隐藏全部栈面板，HUD 走独立层

**A4. ESC 暂停菜单（单机）**

- ESC → 暂停面板：[继续] [重新开始] [退出主界面]（联机模式禁用暂停，仅显示 [退出]）

### 方案 B：交互细节修复（P1，工作量小见效快）

1. UILogin：InputField `onEndEdit` 支持 Enter；密码框 `contentType = Password`
2. 删除好友 → 通用确认弹窗（新建 UIConfirm 小组件，后续复用）
3. 房间码旁加 [复制] 按钮：`GUIUtility.systemCopyWindow`
4. UIRoom 断线态显示 [重试连接] + [返回主界面] 两按钮
5. 连接加 10s 超时 → 超时提示 + 重试
6. UIInvitePopup 操作失败在卡片内显示错误文字（不再只打 Log）

### 方案 C：视觉打磨（P2，可排后期）

1. 面板开关动效：CanvasGroup alpha + scale（0.15s）
2. 按钮点击音效（AudioSystem 就绪后接入）
3. 空状态：好友 0 人 → "还没有好友，去搜索添加吧"引导块
4. 清理 UITest

---

## 五、建议实施顺序

| 批次 | 内容 | 依赖 |
|---|---|---|
| 1 | A3 栈结构修正 + A1 HUD（先本地单机数据） | 无 |
| 2 | A2 结算面板 + A4 暂停菜单 | A3 |
| 3 | B1-B6 交互细节 | 无，可穿插 |
| 4 | C 打磨 | AudioSystem |

> HUD 骨架先行（阶段/倒计时/HP/能量），碎片计数与联机对方状态第二梯队接入。
