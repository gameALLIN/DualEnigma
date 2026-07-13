# AssetBundleMgr 需求文档

> **文档版本**: v1.0  
> **最后更新**: 2026-07-11  
> **文档状态**: 设计定稿  
> **用途**: AssetBundle 加载/卸载、依赖解析、引用计数、延迟卸载、多平台适配

---

## 一、职责

AssetBundleMgr 是资源管理模块的内部实现层，负责：

- AB 的加载与卸载
- AB 依赖链自动解析
- 引用计数追踪
- 延迟卸载（避免抖动）
- 多平台加载适配
- 缓存策略控制
- 常驻 AB 管理

**调用方**：仅 ResMgr 直接调用，业务代码不接触此类。

---

## 二、多平台支持

### 2.1 平台适配接口

```csharp
interface IBundleLoader {
    AssetBundle LoadFromFile(string bundleName);
    IEnumerator LoadFromFileAsync(string bundleName, Action<AssetBundle> callback);
}
```

### 2.2 各平台实现

| 平台 | 实现类 | AB 位置 | 加载方式 | 缓存策略 |
|------|--------|---------|----------|----------|
| Windows | `StandaloneBundleLoader` | StreamingAssets | `LoadFromFile` | 无需缓存 |
| macOS | `StandaloneBundleLoader` | StreamingAssets | `LoadFromFile` | 无需缓存 |
| Android | `AndroidBundleLoader` | StreamingAssets / OBB | `LoadFromFile` | Unity Caching |
| iOS | `IOSBundleLoader` | StreamingAssets | `LoadFromFile` | Unity Caching |
| WebGL | `WebGLBundleLoader` | 远程服务器 | `UnityWebRequestAssetBundle` | 浏览器 IndexedDB |

### 2.3 AB 路径解析

```
AB 根路径 = {Application.streamingAssetsPath}/AssetBundles/{Platform}/

Platform 映射:
  StandaloneWindows64 → "Windows"
  StandaloneOSX       → "OSX"
  Android             → "Android"
  iOS                 → "iOS"
  WebGL               → "WebGL"

完整路径示例（Windows）:
  {Application.streamingAssetsPath}/AssetBundles/Windows/ui.bundle
```

---

## 三、下载与缓存

### 3.1 DownloadSettings

```csharp
[Flags]
enum DownloadSettings {
    Default          = 0,    // 使用 Unity Caching 系统缓存
    DoNotUseCache    = 1,    // 不使用缓存，每次重新下载
    ForceDownload    = 2,    // 强制下载最新版本（忽略本地缓存版本）
}
```

### 3.2 缓存规则

- **Manifest 文件**：始终不缓存，每次启动重新加载
- **普通 AB**：默认走 Unity Caching
- **WebGL 2022+**：禁用 Unity Caching，使用浏览器原生缓存
- 可通过 `DownloadSettings.DoNotUseCache` 对特定 AB 禁用缓存

---

## 四、核心数据结构

### 4.1 ABRefItem — 引用追踪

```csharp
class ABRefItem {
    public string BundleName;           // AB 名称
    public AssetBundle Bundle;          // AB 实例
    public int RefCount;                // 当前引用计数
    public List<Object> Holders;        // 持有该 AB 引用的对象列表
    public string[] Dependencies;       // 该 AB 的依赖 AB 列表
    public DownloadSettings Settings;   // 下载设置
    public bool IsPersistent;           // 是否常驻（不卸载）
}
```

### 4.2 DelayUnloadItem — 延迟卸载

```csharp
class DelayUnloadItem {
    public string BundleName;
    public float UnloadTime;            // 预定卸载时间（Time.time + 2s）
}
```

### 4.3 内部容器

```csharp
// 已加载的 AB 清单
Dictionary<string, ABRefItem> m_LoadedBundles;

// 延迟卸载队列
List<DelayUnloadItem> m_DelayUnloadList;

// 常驻 AB（不卸载）
HashSet<string> m_PersistentBundles;

// AssetBundleManifest（依赖解析 + 版本校验）
AssetBundleManifest m_Manifest;

// 平台加载器
IBundleLoader m_Loader;
```

---

## 五、核心接口

### 5.1 初始化

| 方法 | 说明 |
|------|------|
| `Init()` | 确定平台路径，创建对应 IBundleLoader，加载 AssetBundleManifest |

### 5.2 Bundle 加载

| 方法 | 说明 |
|------|------|
| `LoadBundle(bundleName, settings)` | 同步加载 AB 及其所有依赖，引用计数 +1 |
| `LoadBundleAsync(bundleName, callback, settings)` | 异步加载（协程） |

### 5.3 资产加载

| 方法 | 说明 |
|------|------|
| `LoadAsset<T>(bundleName, assetName)` | 从指定 AB 同步加载资产 |
| `LoadAssetAsync<T>(bundleName, assetName, callback)` | 异步加载资产（协程） |

### 5.4 引用计数

| 方法 | 说明 |
|------|------|
| `AddRef(bundleName, holder)` | 增加引用计数，记录持有者 |
| `ReleaseRef(bundleName, holder)` | 减少引用计数，归零后加入延迟卸载 |

### 5.5 卸载

| 方法 | 说明 |
|------|------|
| `SetPersistent(bundleName)` | 标记为常驻 AB，不参与卸载 |
| `UnloadUnused()` | 立即卸载所有引用归零的 AB |
| `UnloadAll()` | 卸载所有 AB（游戏退出时调用） |

### 5.6 查询

| 方法 | 说明 |
|------|------|
| `IsLoaded(bundleName)` | 判断 AB 是否已加载 |
| `IsAllDependenciesLoaded(bundleName)` | 检查目标 AB 及所有依赖是否已加载 |
| `GetLoadedBundleNames()` | 获取所有已加载 AB 名称列表 |

---

## 六、加载流程

### 6.1 Bundle 加载

```
LoadBundle("ui")
    → 检查 m_LoadedBundles 是否已加载
        → 已加载 → RefCount++ → 返回
        → 未加载 → 继续
    → m_Manifest.GetAllDependencies("ui") 获取依赖
    → 遍历依赖：
        → 依赖 AB 未加载 → m_Loader.LoadFromFile → 创建 ABRefItem → RefCount = 1
        → 依赖 AB 已加载 → RefCount++
    → 加载目标 AB 本身
    → 创建 ABRefItem，记录引用
    → 返回
```

### 6.2 多依赖示例

```
LoadBundle("character")  // 假设依赖 atlas + effect
    → 获取依赖：["character", "atlas", "effect"]
    → character 未加载 → LoadFromFile → RefCount = 1
    → atlas 已加载 → RefCount: 1 → 2
    → effect 未加载 → LoadFromFile → RefCount = 1
    → 返回
```

### 6.3 异步加载（WebGL / 大 AB）

```
LoadBundleAsync("character", callback)
    → 协程启动
    → 遍历依赖：
        → 未加载 → m_Loader.LoadFromFileAsync → yield return
        → 已加载 → RefCount++
    → callback(bundle)
```

---

## 七、引用计数机制

### 7.1 计数规则

```
LoadBundle(name)           → RefCount = 1（首次）或 RefCount++（已加载）
AddRef(name, holder)        → RefCount++，记录持有者
ReleaseRef(name, holder)    → 移除持有者，RefCount--
                              → RefCount == 0 → 加入延迟卸载队列
```

### 7.2 完整示例

```
场景开始时：
    LoadBundle("character")       → AB character: RefCount = 1
    AddRef("character", 角色A)    → RefCount = 1

    LoadBundle("effect")          → AB effect: RefCount = 1, 依赖 atlas: 1→2
    AddRef("effect", 特效管理器)   → RefCount = 1

场景结束时：
    ReleaseRef("character", 角色A)      → RefCount: 1 → 0 → 加入延迟卸载
    ReleaseRef("effect", 特效管理器)     → RefCount: 1 → 0 → 加入延迟卸载
                                         atlas: 2 → 1

2秒后：
    卸载 AB character
    卸载 AB effect
    atlas: RefCount = 1, 不卸载（UI 仍在使用）
```

---

## 八、延迟卸载

```
RefCount 归零
    → 不立即卸载
    → 加入 m_DelayUnloadList，UnloadTime = Time.time + 2
    → Update() 轮询：
        → UnloadTime 已到 → 执行 AssetBundle.Unload(true)
        → 期间有新引用 → RefCount > 0 → 从队列移除，不卸载
```

**延迟时间**：2 秒（可配置）

**目的**：避免同一 AB 在短时间内反复加载/卸载（如滚动列表频繁复用图片）。

---

## 九、无效持有者自动清理

```
Update() 每 1 秒检查：
    遍历 m_LoadedBundles
        → 遍历 AB 的 Holders 列表
            → 持有者为 null（GameObject 已销毁）
            → 自动 ReleaseRef，计数 -1
```

**目的**：开发者忘记手动 ReleaseRef 时，靠 Unity 销毁对象的 null 检查兜底。

---

## 十、常驻 AB

```csharp
// 游戏启动时标记
AssetBundleMgr.Instance.SetPersistent("ui");
AssetBundleMgr.Instance.SetPersistent("audio");
AssetBundleMgr.Instance.SetPersistent("atlas");
AssetBundleMgr.Instance.SetPersistent("data");

// 这些 AB 即使 RefCount 归零也不卸载
// 仅在游戏退出时 UnloadAll() 统一释放
```

---

## 十一、Manifest 管理

```
游戏启动时（Init）：
    1. 加载平台根目录的总 AB（不缓存）
    2. 从中获取 AssetBundleManifest
    3. Manifest 包含：
       - 所有 AB 的 CRC 和 Hash（版本校验）
       - 所有 AB 的依赖关系图（GetAllDependencies）
    4. 后续 LoadBundle 时通过 Manifest 解析依赖
```

---

## 十二、同步/异步使用规则

| 平台 | 推荐方式 | 说明 |
|------|----------|------|
| Standalone (Windows/Mac) | 同步为主 | 本地磁盘读取，延迟可忽略 |
| Android / iOS | 同步为主 | 本地文件读取，延迟可忽略 |
| WebGL | 必须异步 | 网络下载，同步会阻塞 |

```csharp
// 同步（Standalone/Mobile）
AssetBundleMgr.Instance.LoadBundle("ui");
GameObject prefab = AssetBundleMgr.Instance.LoadAsset<GameObject>("ui", "UIHome");

// 异步（WebGL / 大 AB）
AssetBundleMgr.Instance.LoadBundleAsync("character", (bundle) => {
    // AB 加载完成
});
```

---

## 十三、生命周期

```
游戏启动
    → AssetBundleMgr.Init()
        → 确定平台路径
        → 创建 IBundleLoader
        → 加载 AssetBundleManifest
    → SetPersistent("ui", "audio", "atlas", "data")
    → LoadBundle("ui")
    → LoadBundle("data")

进入关卡
    → LoadBundle("character")
    → LoadBundle("effect")

关卡结束
    → ReleaseRef("character", ...)
    → ReleaseRef("effect", ...)
    → 2秒后自动卸载

游戏退出
    → UnloadAll()
```
