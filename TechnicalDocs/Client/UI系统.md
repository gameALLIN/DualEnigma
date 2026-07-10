# UI系统文档

> **文档版本**: v3.0  
> **最后更新**: 2026-07-10  
> **文档状态**: 草稿讨论中  
> **用途**: uGUI 界面架构、MVC 模式实现、UI 管理框架、Editor 自动生成工具、组件自动绑定

---

## 一、设计思想

采用 **MVC（Model-View-Controller）** 模式组织 UI 代码：

- **Model** — UI 数据状态（显示什么）
- **View** — UGUI 组件（怎么显示）
- **Controller** — 交互逻辑（用户操作怎么响应）

核心原则：View 不写逻辑，Controller 不碰 UGUI 组件，Model 不关心界面表现。

---

## 二、目录结构

### 2.1 代码目录

```
Scripts/UI/
├── Core/               # UI 框架基础设施
│   ├── UIManager.cs        # UI 总管理器（栈式面板管理）
│   ├── UIMode.cs           # UI 模式枚举（全屏 / 弹窗 / HUD）
│   ├── UILayer.cs         # Canvas 层级定义
│   └── IUIPanel.cs         # 面板生命周期接口
│
├── MVC/                # MVC 基类
│   ├── UICtrlBase.cs       # 面板基类（Controller 角色）
│   ├── UIViewBase.cs       # 视图基类（View 角色）
│   └── UIModelBase.cs      # 数据基类（Model 角色）
│
├── Views/              # 具体界面（每个面板一个文件夹）
│   ├── UIHUD/              # 游戏内 HUD
│   ├── UISkillSelect/      # 技能选择界面
│   ├── UITalentSelect/     # 天赋选择界面
│   ├── UIBuild/            # 建造界面
│   ├── UIPauseMenu/        # 暂停菜单
│   └── UIGameResult/       # 结算界面
│
└── Common/             # 通用 UI 组件
    └── ...
```

### 2.2 预制体目录

预制体统一存放在 `AssetPackage/Prefabs/UI/` 下，每个面板对应一个文件夹：

```
AssetPackage/Prefabs/UI/
├── UIHUD/
│   └── UIHUD.prefab
├── UISkillSelect/
│   └── UISkillSelect.prefab
├── UITalentSelect/
│   └── UITalentSelect.prefab
├── UIHome/
│   ├── UIHome.prefab              # 面板主预制体
│   └── Common/                    # 该面板的通用子物体预制体（如有）
│       ├── SkillCardItem.prefab
│       └── TalentOptionItem.prefab
└── ...
```

### 2.3 Editor 工具目录

```
Editor/
└── UI/
    ├── UIPanelGenerator.cs       # UI 面板自动生成工具
    └── UIBindingGenerator.cs     # UI 组件自动绑定工具
```

---

## 三、MVC 分层职责

### 3.1 Model — 数据层

| 职责 | 说明 |
|------|------|
| 持有 UI 显示数据 | HP、能量、碎片数量、轮次信息等 |
| 数据变更通知 | 通过事件通知 View 刷新 |
| 不引用任何 UGUI 类型 | 纯 C# 逻辑，可独立测试 |

### 3.2 View — 视图层

| 职责 | 说明 |
|------|------|
| 持有 UGUI 组件引用 | Text、Image、Button 等 |
| 监听 Model 变更刷新表现 | 被动刷新，不含判断逻辑 |
| 转发用户交互给 Controller | 按钮点击等事件转发 |
| 不持有游戏逻辑状态 | 只负责"怎么显示" |

### 3.3 Controller — 控制层

| 职责 | 说明 |
|------|------|
| 持有 Model 和 View 引用 | 连接两者 |
| 处理用户交互逻辑 | 按钮该做什么、界面该怎么切换 |
| 接收游戏事件驱动 UI 更新 | 监听 EventBus，更新 Model |
| 不直接操作 UGUI 组件 | 通过 Model → View 间接刷新 |

---

## 四、UIManager — 面板管理

### 4.1 设计思路

**全局唯一 Canvas**：场景中只有一个 Canvas，UIManager 在其下创建 4 个层级子节点（Empty GameObject），面板实例化到对应层级子节点下。

采用**栈结构**管理面板，同一时刻只有一个面板处于栈顶活跃状态。

```
场景
└── Canvas (全局唯一, Canvas + CanvasScaler + GraphicRaycaster)
    ├── Bottom       ← HUD 层
    ├── Normal       ← 普通面板层
    ├── Top          ← 弹窗层
    └── Loading      ← 加载/过渡层

UIManager
├── 全局 Canvas 引用（场景中唯一）
├── 栈式面板管理（打开/关闭/回退）
├── 4 层级子节点管理
├── 面板缓存（已打开的面板缓存，避免重复实例化）
└── 面板生命周期回调
```

### 4.2 面板加载

使用 **Addressables** 异步加载预制体，预制体存放在 `AssetPackage/Prefabs/UI/{面板名}/{面板名}.prefab`，通过 Addressables 标签或地址引用。

### 4.3 Canvas 层级

| 层级 | 用途 | 示例 |
|------|------|------|
| Bottom | 常驻底层 HUD | 游戏内 HUD |
| Normal | 普通面板 | 技能选择、天赋选择 |
| Top | 弹窗 | 暂停菜单、确认框 |
| Loading | 加载/过渡 | 加载遮罩 |

---

## 五、面板生命周期

```
OnCreate()    → 面板实例化，绑定组件引用
    ↓
OnShow()      → 面板显示，注册事件监听
    ↓
OnHide()      → 面板隐藏，注销事件监听
    ↓
OnDestroy()   → 面板销毁，清理资源
```

---

## 六、数据流向示例

以 **HUD 显示 HP** 为例：

```
游戏逻辑层（角色受伤）
    → EventBus.Publish<PlayerDamagedEvent>()
        → HUDController.OnPlayerDamaged()
            → HUDModel.SetHP(currentHP)
                → HUDView.RefreshHP(hp)  → 屏幕更新
```

用户点击暂停按钮：

```
用户点击
    → HUDView 转发给 HUDController
        → HUDController.RequestPause()
            → UIManager.Push<PauseMenuPanel>()
```

---

## 七、Editor 自动生成工具

### 7.1 用途

开发者通过 Editor 菜单输入面板名称，自动生成完整的 MVC 三件套代码 + 预制体目录结构，避免手动创建。

### 7.2 入口

Unity 编辑器菜单栏：`DualEnigma > UI > 生成面板`，弹出创建窗口。

### 7.3 生成规则

以输入面板名 **UIHome** 为例，工具自动生成以下文件：

#### 代码文件（Scripts/UI/Views/UIHome/）

| 文件 | 类名 | 职责 |
|------|------|------|
| UIHomeCtrl.cs | UIHomeCtrl | Controller，继承 UICtrlBase |
| UIHomeModel.cs | UIHomeModel | Model，继承 UIModelBase |
| UIHomeView.cs | UIHomeView | View，继承 UIViewBase |

#### 预制体目录（AssetPackage/Prefabs/UI/UIHome/）

```
UIHome/
└── UIHome.prefab          # 面板主预制体
                            # Common/ 子目录按需手动添加
```

#### 文件头部模板

每个生成的 .cs 文件自动包含头部注释：

```csharp
/// ============================================================
/// 文件名: UIHomeCtrl.cs
/// 创建时间: 2026-07-10 14:30:00
/// 作者: <开发者名字>
/// 描述: UIHome 面板控制器，处理用户交互逻辑
/// ============================================================
```

### 7.4 命名规范

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| 面板名 | UI + PascalCase | UIHome, HUD, SkillSelect |
| Controller | 面板名 + Ctrl | UIHomeCtrl |
| Model | 面板名 + Model | UIHomeModel |
| View | 面板名 + View | UIHomeView |
| 预制体 | 面板名 + .prefab | UIHome.prefab |
| 代码文件夹 | 面板名 | UIHome/ |
| 预制体文件夹 | 面板名 | UIHome/ |

### 7.5 代码模板示例

以 UIHome 为例，生成的初始代码：

**UIHomeCtrl.cs**

```csharp
/// ============================================================
/// 文件名: UIHomeCtrl.cs
/// 创建时间: 2026-07-10 14:30:00
/// 作者: <开发者名字>
/// 描述: UIHome 面板控制器，处理用户交互逻辑
/// ============================================================

using UnityEngine;

namespace DualEnigma.UI
{
    public class UIHomeCtrl : UICtrlBase
    {
        private UIHomeModel _model;
        private UIHomeView _view;

        protected override void OnCreate()
        {
            _model = new UIHomeModel();
            _view = GetComponent<UIHomeView>();
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }
    }
}
```

**UIHomeModel.cs**

```csharp
/// ============================================================
/// 文件名: UIHomeModel.cs
/// 创建时间: 2026-07-10 14:30:00
/// 作者: <开发者名字>
/// 描述: UIHome 面板数据层，持有显示数据
/// ============================================================

namespace DualEnigma.UI
{
    public class UIHomeModel : UIModelBase
    {
    }
}
```

**UIHomeView.cs**

```csharp
/// ============================================================
/// 文件名: UIHomeView.cs
/// 创建时间: 2026-07-10 14:30:00
/// 作者: <开发者名字>
/// 描述: UIHome 面板视图层，持有 UGUI 组件引用
/// ============================================================

using UnityEngine;

namespace DualEnigma.UI
{
    public class UIHomeView : UIViewBase
    {
    }
}
```

---

## 八、UI 组件自动绑定

### 8.1 用途

在 UI 预制体上挂载一个绑定脚本，执行后自动扫描预制体下所有符合命名规范的 UGUI 组件，完成引用绑定。免去手动拖拽引用的工作。

### 8.2 绑定脚本

挂载在每个 UI 面板预制体根节点上：

```
UIHome (Prefab)
├── UIHomeView.cs              # View 脚本
├── UIAutoBinder.cs            # 自动绑定脚本（Editor 工具组件）
├── m_StartBtn (Button)
├── m_TitleText (Text)
├── m_BgImage (Image)
└── mi_ItemRoot (Transform)
```

### 8.3 命名规范

组件节点命名必须以 `m_Xxx` 或 `mi_Xxx` 开头，`_` 后第一个字母**必须大写**：

| 前缀 | 含义 | 示例 | 说明 |
|------|------|------|------|
| `m_` | 成员变量 | `m_StartBtn`、`m_TitleText` | 通用 UI 组件 |
| `mi_` | UI 控件变量 | `mi_ItemRoot`、`mi_SkillCard` | UI 容器 / 自定义控件 |

**命名规则：**
- `m_` 后第一个字符必须大写：`m_StartBtn` ✅ / `m_startBtn` ❌
- `mi_` 后第一个字符必须大写：`mi_ItemRoot` ✅ / `mi_itemRoot` ❌
- 不符合上述规范的节点自动跳过，不绑定

### 8.4 工作流程

```
1. 开发者搭建预制体层级，按 m_Xxx / mi_Xxx 规范命名组件节点
       ↓
2. 在预制体根节点挂载 UIAutoBinder 脚本
       ↓
3. 点击 Inspector 上的「Auto Bind」按钮
       ↓
4. 脚本递归扫描所有子节点，匹配命名规范
       ↓
5. 自动在对应的 View 脚本中生成/更新 [SerializeField] 字段
       ↓
6. 将组件引用自动写入 View 脚本的序列化字段
       ↓
7. 保存预制体和脚本
```

### 8.5 生成规则

以 **UIHome** 预制体为例：

```
UIHome (Prefab)
├── m_StartBtn         (Button)
├── m_SettingsBtn      (Button)
├── m_TitleText        (Text)
├── m_BgImage          (Image)
└── mi_ItemRoot        (Transform)
```

执行自动绑定后，**UIHomeView.cs** 自动更新为：

```csharp
/// ============================================================
/// 文件名: UIHomeView.cs
/// 创建时间: 2026-07-10 14:30:00
/// 作者: <开发者名字>
/// 描述: UIHome 面板视图层，持有 UGUI 组件引用
/// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace DualEnigma.UI
{
    public class UIHomeView : UIViewBase
    {
        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====
        [SerializeField] private Button     m_StartBtn;
        [SerializeField] private Button     m_SettingsBtn;
        [SerializeField] private Text       m_TitleText;
        [SerializeField] private Image      m_BgImage;
        [SerializeField] private Transform  m_ItemRoot;
        // ===== Auto Bind End =====
    }
}
```

**字段命名映射：**
- 节点名 `m_StartBtn` → 字段名 `m_StartBtn`（保持原名，去重确认）
- 节点名 `mi_ItemRoot` → 字段名 `m_ItemRoot`（`mi_` 前缀统一转为 `m_`，因为字段都是成员变量）

### 8.6 同步更新策略

工具采用**增量更新**，多次执行不会丢失已有代码：

| 场景 | 处理方式 |
|------|----------|
| 预制体新增了 `m_Xxx` / `mi_Xxx` 节点 | 在自动绑定区域内追加新字段 |
| 预制体删除了某个节点 | 自动移除对应字段，清除无效引用 |
| 预制体节点改名 | 旧字段移除，新字段添加 |
| 开发者手动写的字段 | 不受影响（在自动绑定区域之外） |

### 8.7 自动绑定区域标记

View 脚本中用注释标记自动绑定区域，区域内由工具维护，区域外开发者自由编写：

```csharp
public class UIHomeView : UIViewBase
{
    // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====
    [SerializeField] private Button m_StartBtn;
    [SerializeField] private Text   m_TitleText;
    // ===== Auto Bind End =====

    // 以下区域开发者自由编写
    public void RefreshTitle(string title)
    {
        m_TitleText.text = title;
    }
}
```

---

## 九、待讨论

- [ ] MVVM vs MVC：是否需要数据绑定替代手动刷新？
- [ ] 与输入系统的集成方式
- [ ] 网络双人状态下的 UI 同步策略
- [ ] 是否需要 UI 动画系统（Tween 方案选型）
- [ ] Editor 工具的作者名来源（全局配置 vs 每次输入）

---

> **附注**: 本文档为草稿，待讨论敲定后更新为定稿。
