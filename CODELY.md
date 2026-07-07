# DualEnigma 《双生迷城》

## 项目概述

双人联网 2D 合作解密建造游戏。水人与火人利用元素材料建造防御掩体，保护彼此免受自然灾害的侵袭。

- **引擎**: Unity 2022.3+ (2D, URP-ready, C#)
- **平台**: PC (键鼠)，强制双人联网
- **美术风格**: 矢量几何 + 迷雾废墟 + 纯特效（零外部美术资源依赖，全部程序化生成）
- **PPU**: 32 (1 格 = 32px = 1 Unity 单位)

## 目录结构

```
DualEnigma/
├── Client/                          # Unity 客户端工程
│   ├── Assets/
│   │   └── Scenes/                   # 场景文件（当前仅保留场景，代码已清空待重建）
│   ├── Packages/manifest.json        # Unity 包依赖
│   ├── ProjectSettings/              # Unity 项目配置（TagManager 含 Ground 层 + Player 标签）
│   └── Client.sln                    # Visual Studio 解决方案
│
├── GameDesign/                       # 策划工作区
│   ├── DesignDocuments/
│   │   └── GDD_核心设计文档.md       # 核心设计文档 v3.0（游戏概述/角色/材料/灾害/建造/流程/地图）
│   ├── LevelDesign/                  # 关卡设计（待基于 GDD 重写）
│   ├── NumericalDesign/              # 数值设计（待基于 GDD 重写）
│   ├── ArtRequirements/              # 美术需求（待基于 GDD 重写）
│   ├── AudioRequirements/            # 音频需求
│   └── VersionHistory/               # 版本历史
│
└── .codely-cli/                     # Codely CLI 配置（gitignored）
    ├── agents/                       # 子智能体配置
    │   ├── 00.toml                   # 主智能体（任务分派调度器）
    │   ├── main_coder.toml           # 主程子智能体
    │   ├── main_art.toml             # 美术子智能体
    │   └── main_designer.toml        # 主策划子智能体
    ├── extensions/                   # Codely 扩展（unity-lsp, TJGenerators, superpowers 等）
    └── skills/                       # 自定义技能
        └── dualenigma-dispatcher/    # 任务分派 skill
```

## 智能体架构

项目使用主智能体 + 三子智能体的层级分派模式：

```
00 (主智能体) — 接收需求 → 识别类型 → 拆解任务 → 分派 → 汇报
├── main_coder     — 程序：Unity C# 代码、架构、Bug修复
├── main_art       — 美术：精灵、Shader、预制体、特效、UI视觉
└── main_designer  — 策划：GDD、数值、关卡、需求文档
```

子智能体均配置了 `task` 工具权限，可在需求较大时继续向下拆分给 `general-purpose` 子智能体并行执行。

## 核心设计要点（GDD v3.0 摘要）

- **角色**: 水人(HP100, 移速4格/s) + 火人(HP100, 可二段跳)
- **材料**: 5种（水砖/冰砖/火砖/岩浆砖/石砖），各有抗性/弱点/护盾加成
- **灾害**: 5种（山火/洪水/地震/暴风雪/陨石雨），每种20秒
- **建造**: 32×32px网格，12×10格区域，3块搬运上限
- **流程**: 灾害预告(5s) → 建造(45s) → 灾害(20s) → 修整(10s) → 升级(15s) = 95s/轮 × 3轮
- **结构**: 3章 × 12节 = 36关

## 美术风格规范

- **角色**: 森林冰火人风格 — Q版大头小身(头占75%)，头部即元素本体，粗黑描边3px，径向渐变
- **水人配色**: 中心#E1F5FE → 中段#4FC3F7 → 边缘#0277BD，描边#050505
- **火人配色**: 中心#FFE082 → 中段#FF6F00 → 边缘#BF360C，描边#050505
- **背景**: 深青灰#263238，渐变天空#1A237E→#283593
- **灾害**: 纯 Particle System + Shader，零贴图依赖

## 开发约定

- **C# 命名空间**: `DualEnigma.{Module}`（如 `DualEnigma.Core`, `DualEnigma.Player`）
- **字段命名**: `_camelCase`（私有序列化字段）
- **单例模式**: 继承 `Singleton<T>`，重写 `OnSingletonInitialized()`
- **事件总线**: `EventBus.Instance.Subscribe<T>()` / `Publish<T>()`（MonoBehaviour 单例）
- **服务定位器**: `ServiceLocator.Register<IBuildSystem>(this)` / `Get<IBuildSystem>()`
- **Sprite 生成**: `SpriteMeshType.FullRect`，`filterMode = FilterMode.Point`，PPU = 32
- **Ground 层**: Layer 7 = Ground（TagManager.asset 已定义）
- **Player 标签**: Tag = Player（TagManager.asset 已定义）

## 构建

```bash
# Unity 命令行构建（需安装 Unity 2022.3+）
# 当前项目处于初始阶段，尚无构建脚本
```

## 当前状态

- ✅ GDD v3.0 设计迭代中
- ✅ 智能体架构配置完成（4个 agent toml）
- ✅ Unity 工程框架已清空，保留 Scenes，准备从零重建
- 🎯 下一步：基于 GDD 重新设计工程化代码架构
