# AssetGraph 打包配置文档

> **文档版本**: v1.0  
> **最后更新**: 2026-07-11  
> **文档状态**: 设计定稿  
> **用途**: AssetBundle 分组规则、多平台构建配置

---

## 一、职责

AssetGraph 负责配置 AB 的分组规则，将 `AssetPackage/` 下的资源按模块打包为 `.bundle` 文件。

---

## 二、分组规则

### 2.1 Bundle 划分

| Bundle 名称 | 源路径 | 包含内容 | 加载时机 |
|-------------|--------|----------|----------|
| `ui` | `AssetPackage/Prefabs/UI/**` | 所有 UI 预制体 | 游戏启动常驻 |
| `character` | `AssetPackage/Prefabs/Characters/**` | 角色预制体、动画 | 关卡开始 |
| `effect` | `AssetPackage/Prefabs/Effects/**` | 粒子特效、Shader | 关卡开始 |
| `atlas` | `AssetPackage/Atlases/**` | SpriteAtlas 图集 | 随 UI 常驻 |
| `audio` | `AssetPackage/Audio/**` | 音效、BGM | 游戏启动常驻 |
| `data` | `AssetPackage/Data/**` | ScriptableObject 配置 | 游戏启动常驻 |

### 2.2 AssetGraph 节点配置

```
[Source] AssetPackage/Prefabs/UI/**       → [Group] "ui"
[Source] AssetPackage/Prefabs/Characters/** → [Group] "character"
[Source] AssetPackage/Prefabs/Effects/**   → [Group] "effect"
[Source] AssetPackage/Atlases/**           → [Group] "atlas"
[Source] AssetPackage/Audio/**             → [Group] "audio"
[Source] AssetPackage/Data/**              → [Group] "data"
```

---

## 三、多平台构建

### 3.1 产出路径

```
AssetBundles/                   # AB 产出根目录
├── Windows/                    # StandaloneWindows64
│   ├── ui.bundle
│   ├── character.bundle
│   ├── effect.bundle
│   ├── atlas.bundle
│   ├── audio.bundle
│   ├── data.bundle
│   └── Windows.bundle           # Manifest 总包
├── OSX/
├── Android/
├── iOS/
└── WebGL/
```

### 3.2 构建配置

| 平台 | BuildTarget | AB 压缩格式 | 备注 |
|------|-------------|------------|------|
| Windows | `StandaloneWindows64` | LZ4 | — |
| macOS | `StandaloneOSX` | LZ4 | — |
| Android | `Android` | LZ4 | 注意 ABI 分包 |
| iOS | `iOS` | LZ4 | — |
| WebGL | `WebGL` | LZ4 | 禁用 Unity Caching 2022+ |

---

## 四、StreamingAssets 部署

构建后将 AB 复制到 StreamingAssets：

```
Client/Assets/StreamingAssets/
└── AssetBundles/
    └── {Platform}/
        ├── ui.bundle
        ├── character.bundle
        └── ...
```

**注意**：StreamingAssets 不参与 Unity 序列化，不会被处理。需要在 `.gitignore` 中忽略 AB 产出，仅保留源资源。

---

## 五、版本管理

### 5.1 Manifest 校验

每个平台的 `{Platform}.bundle` 是 Manifest 总包，包含：
- 所有 AB 的 CRC 和 Hash
- AB 依赖关系图
- 版本号

### 5.2 热更新（预留）

后续可扩展为从 CDN 下载 AB 版本清单，对比本地版本号，按需下载更新的 AB。当前阶段不实现。
