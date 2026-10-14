# 配置仓储

`IConfigRepository` 负责从 MOD 目录加载配置、按类型分组缓存、计算哈希。

## 核心概念

- **分组**：按配置类型分组，如 `CharacterConfig` 对应 `Character/` 目录
- **项**：分组内的单个配置文件，用 `itemId` 标识
- **键**：`(分组类型, itemId)` 唯一确定一份配置

## 注册与加载

```csharp
// 注册分组
repo.Add(typeof(CharacterConfig), (typeof(CharacterData), isRoot: false));

// 扫描所有已注册分组并加载
repo.Load();
```

`Add` 只声明分组的存在和读取方式，**不触发加载**。`Load` 才会扫描目录。

## 目录约定

```
Mods/
└── {modId}/
    ├── Character/              # 分组目录 = 类型名去掉前缀
    │   ├── chestnut.json
    │   └── chestnut.yaml
    └── Weapon/
        └── 83079.json
```

`isRoot: true` 时从 `Mods/{modId}/` 根目录直接查找，不进入子目录。

## 查询

```csharp
// 单个查询
var data = repo.GetValue<CharacterConfig, CharacterData>("chestnut");

// 尝试查询
if (repo.TryGetValue<CharacterConfig, CharacterData>("chestnut", out var data))
{
    // 使用 data
}

// 遍历某分组所有项
foreach (Id id in repo.GetAllId<CharacterConfig>())
{
    Console.WriteLine(id.ItemId);
}

// 所有分组类型
foreach (Type t in repo.GetAllConfigType())
{
    Console.WriteLine(t.Name);
}
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

## 线程安全

- 内部使用 `ConcurrentDictionary` 保存状态
- 加载与查询可并发进行
- 同一项被多个线程同时加载时，只有一个线程成功，其余返回 `false`
- 枚举结果是调用瞬间的快照

## 生命周期

典型用法：

```
启动阶段（可并发）
├─ Add 注册所有分组
└─ Load 扫描并加载

运行阶段（只读）
├─ GetValue / TryGetValue
└─ GetAllId / GetAllConfigType
```

如果运行期需要热加载，可以再次调用 `Load` 或 `Load(Type, Type, string)` 加载单项。