# 配置仓储

`IConfigRepository` 负责从 MOD 目录加载配置、按类型分组缓存、计算哈希、按需卸载。

## 核心概念

- **分组**：按配置类型分组，**分组类型必须是接口**，如 `ICharacterConfig` 对应 `Character/` 目录
- **项**：分组内的单个配置文件，用 `itemId` 标识
- **键**：`(分组类型, itemId)` 唯一确定一份配置

### ⚠️ 分组类型必须是接口

仓储用 `classType.Name[1..]` 推导子目录名，即**接口名去掉首字母 `I`**。例如：

| 分组接口 | 目录名 |
|---------|--------|
| `ICharacterConfig` | `Character` |
| `IWeaponConfig` | `Weapon` |
| `IGlobalConfig` | `Global` |

传入具体类（如 `CharacterConfig`）会得到 `haracterConfig` 这样的错误目录名，导致加载静默失败。这是库的既定约定，调用方必须遵守。

## 注册与加载

```csharp
// 注册分组，分组类型必须是接口
repo.Add(typeof(ICharacterConfig), (typeof(CharacterData), isRoot: false));

// 扫描所有已注册分组并加载
repo.Load();
```

`Add` 只声明分组的存在和读取方式，**不触发加载**。`Load` 才会扫描目录。

## 目录约定

```
Mods/
└── {modId}/
    ├── Character/              # 分组目录 = 接口名去掉首字母 I
    │   ├── chestnut.json
    │   └── chestnut.yaml
    └── Weapon/
        └── 83079.json
```

`isRoot: true` 时从 `Mods/{modId}/` 根目录直接查找，不进入子目录。

## 序列化器限制

`ConfigRepository` 的构造函数接收**单个** `IConfigSerializer`，仓储只处理该序列化器声明的扩展名。如果 MOD 同时有 `.json` 和 `.yaml` 配置：

- 当前版本建议为每种格式创建独立仓储实例（各自 `Add` 对应分组）
- 未匹配 `Serializer.Extensions` 的文件会被静默跳过

`IConfigSerializerFactory` 的作用是为**调用方**按扩展名选择序列化器，它不直接接入仓储。工厂与仓储的组合留待后续版本。

## 查询

```csharp
// 单个查询
var data = repo.GetValue<ICharacterConfig, CharacterData>("chestnut");

// 尝试查询
if (repo.TryGetValue<ICharacterConfig, CharacterData>("chestnut", out var data))
{
    // 使用 data
}

// 遍历某分组所有项
foreach (Id id in repo.GetAllId<ICharacterConfig>())
{
    Console.WriteLine(id.ItemId);
}

// 所有分组类型
foreach (Type t in repo.GetAllConfigType())
{
    Console.WriteLine(t.Name);
}
```

## 卸载

用于热重载、Mod 卸载、资源释放等场景。**卸载只影响内存缓存，不删除磁盘文件。**

```csharp
// 卸载单个项
repo.Unload<ICharacterConfig>("chestnut");

// 卸载整个分组的全部项，分组注册保留
repo.Unload(typeof(ICharacterConfig));

// 卸载所有分组的全部项，所有分组注册保留
repo.UnloadAll();
```

`Unload` 系列方法的语义：

| 方法 | 返回值 | 说明 |
|------|--------|------|
| `Unload(Type, string)` | 该项存在并卸载成功返回 `true`，否则 `false` | 移除单个配置项 |
| `Unload<T>(string)` | 同上 | 泛型版本 |
| `Unload(Type)` | 分组已注册返回 `true`，未注册返回 `false` | 移除该分组全部项 |
| `Unload<T>()` | 同上 | 泛型版本 |
| `UnloadAll()` | 无 | 移除所有项 |

**哈希清理**：卸载项对应的文件路径若不再被其他分组引用，其哈希从 `Sha256` 中一并移除。`isRoot: true` 下不同分组可能共享同一文件，此时哈希保留到最后一个引用被卸载。

**分组注册保留**：`Unload(Type)` 和 `UnloadAll()` 只清空配置项，不注销分组。卸载后可以再次 `Load()` 或 `Load<TClass, TConfig>` 重新填充。

## 热重载

`Unload` + `Load` 组合实现单项重载：

```csharp
repo.Unload<ICharacterConfig>("chestnut");
repo.Load<ICharacterConfig, CharacterData>("chestnut");
```

## 哈希

`Sha256` 属性提供所有已加载配置文件的整体哈希：

```csharp
string hash = repo.Sha256;
```

将所有文件的 SHA256 排序后拼接再取哈希，**与文件加载顺序无关**。

用途：

- 热重载时判断配置是否有变化
- 存档时记录配置版本
- 客户端 / 服务器一致性校验

**并发语义**：加载或卸载进行中读取 `Sha256`，得到的是“调用瞬间”已加载文件的哈希集合，不保证对应某一完整时刻。建议在 `Load()` / `Unload` 系列完成后读取。

## 线程安全

- 内部使用三个 `ConcurrentDictionary` 保存状态：分组→表、表内 `itemId`→对象、`itemId`→路径
- 加载与查询可并发进行，读路径无锁
- 同一项被多个线程同时加载时，只有一个线程成功，其余返回 `false`
- 同一项被多个线程同时卸载时，只有一个线程成功
- 枚举结果是调用瞬间的快照

**卸载与加载的并发建议**：`Unload` 系列方法的“路径是否仍被引用”检查不是原子的。在极端的并发卸载场景下（多个线程同时卸载共享根路径的不同分组），可能残留或重复删除哈希。Mod 场景下卸载通常发生在 Mod 卸载或热重载的串行时机，这一假设可接受；若需严格并发安全，调用方应自行串行化 `Unload` 与 `Load`。

## 生命周期

典型用法：

```
启动阶段（可并发）
├─ Add 注册所有分组
└─ Load 扫描并加载

运行阶段（只读）
├─ GetValue / TryGetValue
└─ GetAllId / GetAllConfigType

卸载阶段
├─ Unload<T>(itemId)         单权卸载
├─ Unload<T>()               整组卸载
└─ UnloadAll()               全部卸载
```