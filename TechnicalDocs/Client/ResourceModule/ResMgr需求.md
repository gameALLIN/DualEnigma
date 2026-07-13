# ResMgr 需求文档

> **文档版本**: v1.0  
> **最后更新**: 2026-07-11  
> **文档状态**: 设计定稿  
> **用途**: 对外统一资源加载接口，屏蔽底层 AB / AssetDatabase 差异

---

## 一、职责

ResMgr 是资源管理模块的**对外接口层**，负责：

- 提供统一的资源加载 API
- 屏蔽 Editor（AssetDatabase）和 Runtime（AssetBundle）的差异
- 路径到 Bundle 的自动映射
- 同步/异步加载入口
- 常驻 Bundle 标记转发

**调用方**：所有业务代码（UIManager、游戏逻辑等）。

---

## 二、双模式

### 2.1 Editor 模式

```csharp
#if UNITY_EDITOR
    T asset = AssetDatabase.LoadAssetAtPath<T>("Assets/AssetPackage/" + path);
#endif
```

- 直接从 AssetDatabase 加载，零配置
- 不走 AB，不需要打包
- 开发效率最高

### 2.2 Runtime 模式

```csharp
#if !UNITY_EDITOR
    AssetBundleMgr.Instance.LoadBundle(bundleName);
    T asset = AssetBundleMgr.Instance.LoadAsset<T>(bundleName, assetName);
#endif
```

- 通过 AssetBundleMgr 加载
- 自动解析依赖、引用计数
- 支持多平台

---

## 三、路径映射

### 3.1 路径格式

所有加载路径相对于 `AssetPackage/`：

```
ResMgr.LoadPrefab("Prefabs/UI/UITest/UITest")
ResMgr.Load<Sprite>("Atlases/Icons/icon_fire")
ResMgr.Load<AudioClip>("Audio/BGM/bgm_main")
```

### 3.2 映射表

路径前缀 → Bundle 名称：

```csharp
static readonly Dictionary<string, string> s_PathToBundle = new Dictionary<string, string> {
    { "Prefabs/UI",         "ui" },
    { "Prefabs/Characters", "character" },
    { "Prefabs/Effects",    "effect" },
    { "Atlases",            "atlas" },
    { "Audio",              "audio" },
    { "Data",               "data" },
};
```

### 3.3 映射流程

```
ResMgr.LoadPrefab("Prefabs/UI/UITest/UITest")
    ↓ 解析路径
    路径前缀 "Prefabs/UI" → Bundle: "ui"
    资产名: "UITest"（去掉路径前缀和扩展名）
    ↓ Editor
    AssetDatabase.LoadAssetAtPath("Assets/AssetPackage/Prefabs/UI/UITest/UITest.prefab")
    ↓ Runtime
    AssetBundleMgr.LoadBundle("ui")
    AssetBundleMgr.LoadAsset<GameObject>("ui", "UITest")
```

---

## 四、核心接口

### 4.1 同步加载

| 方法 | 说明 |
|------|------|
| `Load<T>(path)` | 同步加载任意资源 |
| `LoadPrefab(path)` | 加载预制体（自动补 .prefab） |

### 4.2 异步加载

| 方法 | 说明 |
|------|------|
| `LoadAsync<T>(path, callback)` | 异步加载资源 |
| `LoadPrefabAsync(path, callback)` | 异步加载预制体 |

### 4.3 引用管理

| 方法 | 说明 |
|------|------|
| `AddRef(bundleName, holder)` | 增加引用计数（转发 AssetBundleMgr） |
| `ReleaseRef(bundleName, holder)` | 减少引用计数（转发 AssetBundleMgr） |

### 4.4 卸载

| 方法 | 说明 |
|------|------|
| `UnloadUnused()` | 触发延迟卸载队列执行 |
| `SetPersistentBundle(name)` | 标记常驻 AB |

---

## 五、使用示例

### 5.1 UI 面板加载

```csharp
// UIManager 中
GameObject prefab = ResMgr.Instance.LoadPrefab("Prefabs/UI/UITest/UITest");
GameObject panelObj = Instantiate(prefab, parent, false);
```

### 5.2 异步加载角色

```csharp
ResMgr.Instance.LoadPrefabAsync("Prefabs/Characters/Aqua", (prefab) => {
    GameObject character = Instantiate(prefab);
});
```

### 5.3 引用管理

```csharp
// 关卡开始
ResMgr.Instance.AddRef("character", this);
ResMgr.Instance.AddRef("effect", this);

// 关卡结束
ResMgr.Instance.ReleaseRef("character", this);
ResMgr.Instance.ReleaseRef("effect", this);
// 2秒后自动卸载
```

---

## 六、初始化

```csharp
// GameLaunch.Awake() 中
ResMgr.Instance.Init();  // 初始化 AssetBundleMgr（Runtime 模式）

// Runtime 模式下标记常驻
ResMgr.Instance.SetPersistentBundle("ui");
ResMgr.Instance.SetPersistentBundle("audio");
ResMgr.Instance.SetPersistentBundle("atlas");
ResMgr.Instance.SetPersistentBundle("data");
```

---

## 七、与 AssetBundleMgr 的关系

```
ResMgr（对外）
    │
    ├── Editor 模式 → AssetDatabase（不经过 AssetBundleMgr）
    │
    └── Runtime 模式 → AssetBundleMgr（转发所有 AB 操作）
                         ├── LoadBundle / LoadAsset
                         ├── AddRef / ReleaseRef
                         ├── SetPersistent
                         └── UnloadUnused / UnloadAll
```

**ResMgr 不持有任何 AB 状态**，所有状态由 AssetBundleMgr 管理。ResMgr 只做路径映射和模式分发。
