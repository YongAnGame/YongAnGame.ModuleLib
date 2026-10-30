# YongAnGame.ModuleLib

> **文档内容由AI编写，如有问题请反馈**

面向游戏 MOD 系统的通用模块库，提供配置加载、序列化、仓储管理等基础设施。

## 特性

- **多种格式支持**：JSON、YAML，通过统一接口 `IConfigSerializer` 抽象，换格式不改调用方
- **格式工厂**：`IConfigSerializerFactory` 按文件扩展名选择合适的序列化器
- **线程安全**：所有公开接口承诺并发安全；仓储实现读路径无锁，写路径依赖原子操作
- **结构化标识符**：`Id` 支持 `ModId.ItemId:SubId` 格式，内置多维度比较方法
- **面向 MOD 的配置仓储**：`IConfigRepository` 负责目录扫描、缓存、哈希，支持 `Unload` + `Load` 组合实现热重载
- **日志抽象**：仓储只依赖 `Microsoft.Extensions.Logging.ILogger`，不绑定具体日志实现
- **低框架兼容**：通过 Polyfill 为 `netstandard2.0` 等旧目标补齐新 API

## 快速开始

```bash
dotnet add package YongAnGame.ModuleLib
```

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YongAnGame.ModuleLib.Features;
using YongAnGame.ModuleLib.Features.Configs;
using YongAnGame.ModuleLib.Features.Serializers;

// 仓储依赖 Microsoft.Extensions.Logging.ILogger，不绑定具体日志框架
ILogger logger = NullLogger.Instance;

IConfigOptions options = new ConfigOptions(Path.Combine(AppContext.BaseDirectory, "Mods"));
IConfigSerializer serializer = new JsonConfigSerializer();

IConfigRepository repo = new ConfigRepository(logger, options, serializer, "example_mod");

// 分组类型必须是接口，目录名 = 接口名去掉首字母 I
repo.Add(typeof(ICharacterConfig), (typeof(CharacterData), isRoot: false));
repo.Load();

if (repo.TryGetValue<ICharacterConfig, CharacterData>("chestnut", out var data))
{
    Console.WriteLine(data.Name);
}
```

详见 [快速入门](articles/quickstart.md)。

## 文档导航

| 章节 | 内容 |
|------|------|
| [快速入门](articles/quickstart.md) | 从安装到跑通的完整流程 |
| [序列化器](articles/serialization.md) | JSON / YAML 序列化器的使用与扩展 |
| [配置仓储](articles/repository.md) | 从 MOD 目录加载、缓存、查询、卸载配置 |
| [标识符](articles/id.md) | `Id` 的格式、解析、比较方法 |
| [线程安全模型](articles/threading.md) | 并发设计，按实现分别说明 |
| [API 参考](api/YongAnGame.ModuleLib.Compatibility.html) | 所有公开类型的 API 文档 |
| [贡献指南](CONTRIBUTING.md) | 开发流程与代码规范 |

## 支持的目标框架

| 框架 | 说明 |
|------|------|
| `net10.0` | 主目标，完整功能 |
| `net8.0` | 除 `Lock` 类型外全部支持 |
| `netstandard2.1` | 需 Polyfill 补齐部分 API |
| `netstandard2.0` | 需 Polyfill + 兼容包 |
| `net47` | 需 Polyfill + 兼容包 |

## 许可证

LGPL-3.0-only License，见 [LICENSE](https://github.com/YongAnGame/YongAnGame.ModuleLib/blob/main/LICENSE)。