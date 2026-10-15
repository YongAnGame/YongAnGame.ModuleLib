# YongAnGame.ModuleLib

> **文档内容由AI编写，如有问题请反馈**

面向游戏 MOD 系统的通用模块库，提供配置加载、序列化、仓储管理等基础设施。

## 特性

- **多种格式支持**：JSON、YAML，通过统一接口 `IConfigSerializer` 抽象，换格式不改调用方
- **自动格式识别**：`IConfigSerializerFactory` 按文件扩展名自动选择合适的序列化器
- **线程安全**：所有公开接口承诺并发安全，读路径无锁，写路径采用不可变快照替换
- **结构化标识符**：`Id` 支持 `ModId.ItemId:SubId` 格式，内置多维度比较方法
- **面向 MOD 的配置仓储**：`IConfigRepository` 负责目录扫描、缓存、哈希，支持热加载
- **低框架兼容**：通过 Polyfill 为 `netstandard2.0` 等旧目标补齐新 API

## 快速开始

```bash
dotnet add package YongAnGame.ModuleLib
```

```csharp
IConfigSerializerFactory factory = new ConfigSerializerFactory().WithYaml();
IConfigSerializer serializer = factory.ForFile("config.yaml");

IConfigRepository repo = new ConfigRepository(logger, options, serializer, "example_mod");
repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
repo.Load();

if (repo.TryGetValue<CharacterConfig, CharacterData>("char_001", out var data))
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
| [配置仓储](articles/repository.md) | 从 MOD 目录加载、缓存、查询配置 |
| [标识符](articles/id.md) | `Id` 的格式、解析、比较方法 |
| [线程安全模型](articles/threading.md) | 并发设计、快照替换机制 |
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