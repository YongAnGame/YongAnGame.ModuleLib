# 贡献指南

感谢你考虑为 YongAnGame.ModuleLib 做贡献。本文档说明开发流程、代码规范和贡献者许可要求。

> **版权声明**  
> Copyright (C) 2026 **YongAnGame**。  
> 本项目原始代码版权归 **YongAnGame** 所有。  
> 贡献者保留其贡献的版权，并授予 **YongAnGame** 一份永久的、不可撤销的、可再许可的许可，允许团队以任何许可证（包括专有许可证）分发其贡献。

## 开发环境

| 项 | 要求 |
|----|------|
| .NET SDK | 10.0 或更高 |
| IDE | Visual Studio 2022+  |
| 目标框架 | `net10.0` / `net8.0` / `netstandard2.1` / `netstandard2.0` / `net47` |

## 拉取代码

```bash
git clone https://github.com/YongAnGame/YongAnGame.ModuleLib.git
cd YongAnGame.ModuleLib
dotnet restore
dotnet build
```

## 贡献者许可协议（CLA）

在提交任何代码之前，你必须签署 **贡献者许可协议（CLA）**。

签署 CLA 后，你同意：

1. **你保留你贡献的版权**；
2. 你授予 **YongAnGame** 及其关联方一项永久的、全球范围的、非独占的、免费的、免版税的、不可撤销的、可再许可的版权许可，允许其复制、修改、准备衍生作品、公开展示、公开表演、分发、再许可和以任何许可证条款（包括商业专有许可证）分发你的贡献及衍生作品；
3. 对于你拥有或有权许可的、且被你的贡献所必然侵犯的专利，你授予 **YongAnGame** 一项永久的、全球范围的、非独占的、免费的、免版税的、不可撤销的专利许可；
4. **YongAnGame** 有权以任何许可证条款（包括但不限于本项目当前采用的开源许可证 **LGPL-3.0-only**、商业许可证或其他开源/专有许可证）使用、修改、分发和再许可你的贡献及本项目。

未签署 CLA 的 Pull Request 将无法被合并。  
签署方式：请使用 CLA Assistant，或在本仓库指定 Issue/PR 中按模板回复并确认。项目方会保留签署记录。

## YongAnGame 团队特别许可

为便于项目维护、商业合作和许可证管理，贡献者额外授予 **YongAnGame 团队** 一项永久的、全球范围的、非独占的、免费的、免版税的、不可撤销的、可再许可的许可，允许其以任何许可证条款（包括商业专有许可证）复制、修改、准备衍生作品、公开展示、公开表演、分发、再许可和分发贡献及衍生作品。

“**YongAnGame 团队**”指：

- YongAnGame 及其关联方；
- YongAnGame 的员工、董事、承包商和代理人；
- 由 YongAnGame 书面指定的维护者、合作方或后续权利继受方。

该特别许可不限制贡献者对其贡献的版权，也不影响项目对外发布的 **LGPL-3.0-only** 版本对第三方的约束。已按 LGPL-3.0-only 发布的副本，其接收者依据 LGPL 获得的权力不可撤销。

## 分支策略

| 分支 | 用途 |
|------|------|
| `main` | 稳定分支 |
| `dev` | 开发分支 |

从 `dev` 拉分支，完成后发起 PR：

## 提交信息规范

使用 [Conventional Commits](https://www.conventionalcommits.org/)：

```
<type>(<scope>): <subject>

<body>

<footer>
```

| type | 用途 |
|------|------|
| `feat` | 新功能 |
| `fix` | Bug 修复 |
| `refactor` | 重构 |
| `docs` | 文档 |
| `test` | 测试 |
| `chore` | 构建 / 工具链 |

示例：

```
feat(serializer): 支持 YAML 注释保留

在 YamlConfigSerializer 中启用 EmitDefaults 以外的选项，
使注释在序列化时保留。

Closes #42
```

## 代码规范

### 命名

- 类型和公开成员用 `PascalCase`
- 私有字段用 `_camelCase`
- 接口以 `I` 开头

### 注释

所有公开成员必须有 XML 文档注释。注释应说明**为什么**，而非重复**是什么**：

```csharp
// 好的注释：解释并发模型
// 只保护“重建 + 替换”这段临界区，查询路径不加锁
private readonly Lock _registerLock = new();

// 冗余的注释：重复方法名
// 获取值
public TData? GetValue(TKey key) { ... }
```

### 线程安全

所有面向调用方的公开接口必须承诺线程安全，并在 XML 注释中说明保证机制。

实现推荐使用**不可变快照替换**模式：

```csharp
private volatile SomeImmutableState _state;
private readonly Lock _writeLock = new();

public void Modify()
{
    lock (_writeLock)
    {
        var next = BuildNewState(_state);
        _state = next;
    }
}

public Result Query()
{
    var snapshot = _state;
    // 基于 snapshot 查询，不加锁
}
```

参考实现：

- `ConfigSerializerFactory`
- `JsonConfigSerializer`
- `YamlConfigSerializer`

### 异常

- 底层库异常（`JsonException`、`YamlException`）必须包装为 `SerializationException`
- 参数校验用 `ArgumentException.ThrowIfNullOrWhiteSpace` 等方法
- 不要在 `TryXxx` 方法中抛异常

### 多目标框架

代码需兼容所有目标框架。使用 `#if` 条件编译处理缺失的 API：

```csharp
#if NET7_0_OR_GREATER
    , IParsable<Id>
#endif

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
```

优先使用 Polyfill 提供的兼容层，避免大量 `#if`。

## 测试

### 运行现有测试

```bash
dotnet test
```

### 编写测试

- 每个公开成员至少覆盖：正常路径、null 参数、边界值、异常场景
- 并发相关代码需编写多线程测试
- 测试命名：`MethodName_Scenario_ExpectedBehavior`

示例：

```csharp
[Fact]
public void TryParse_NullInput_ReturnsFalse()
{
    bool result = Id.TryParse(null, null, out _);
    Assert.False(result);
}

[Fact]
public void AddTypeConverter_Concurrent_AllApplied()
{
    var serializer = new JsonConfigSerializer();
    Parallel.For(0, 10, _ => serializer.AddTypeConverter(new TestConverter()));

    // 验证不崩溃、转换器数量正确
}
```

## 提交 Pull Request

PR 应满足：

- [ ] 已签署 **贡献者许可协议**
- [ ] 通过 `dotnet build` 无警告
- [ ] 通过 `dotnet test`
- [ ] 新增或修改的公开成员有 XML 注释
- [ ] 线程安全相关改动附带说明
- [ ] 提交信息符合 Conventional Commits
- [ ] 更新相关文档（README / docfx / 接口注释）
- [ ] 所有目标框架均能编译

PR 描述模板：

```markdown
## 改动内容

简要说明改了什么、为什么改

## 影响范围

- [ ] 接口变更（破坏性 / 非破坏性）
- [ ] 行为变更
- [ ] 仅文档

## 测试

说明如何验证，附测试命令或截图

## 关联 Issue

Closes #xxx
```

## 代码审查要点

审查者关注：

1. **线程安全**：新增公开成员是否有并发契约？实现是否兑现？
2. **多目标兼容**：是否所有 TFM 都能编译？`#if` 使用是否正确？
3. **接口一致性**：泛型约束、命名、命名空间是否与现有成员一致
4. **异常约定**：是否统一包装为 `SerializationException`？
5. **注释质量**：是否解释“为什么”，而非重复代码
6. **API 边界**：是否引入不必要的新接口 / 新依赖

## 报告 Bug

提 Issue 时请包含：

- 使用的版本（`git log` 或 NuGet 版本号）
- 目标框架
- 最小复现步骤
- 期望行为与实际行为
- 完整异常堆栈
- 运行环境（.NET 版本、OS）

## 讨论

- 重大改动请先开 Issue 讨论，避免做完发现方向不对
- 涉及接口变更的，先说明兼容性影响
- 不确定实现方式时，可以先提交 RFC 类型的 Issue

## 许可证

本项目以 **GNU Lesser General Public License v3.0 only** 发布：

- **GNU Lesser General Public License v3.0 only**

SPDX 标识符：`LGPL-3.0-only`

许可证全文见项目根目录：

- `LICENSE`

**所有贡献者必须签署 CLA，授予 YongAnGame 一份永久的、不可撤销的、可再许可的版权许可。贡献者保留其贡献的版权。**  
签署 CLA 表示同意 **YongAnGame** 以 **LGPL-3.0-only** 或团队自行决定的其他许可证（包括专有许可证）分发你的贡献。

> 说明：项目对外发布的社区版继续按 **LGPL-3.0-only** 授权；同时，依据 CLA 和团队特别许可，YongAnGame 可将贡献用于商业专有版本。但若项目中包含第三方代码，必须确保该第三方代码的许可证允许相应使用和再许可。已按 LGPL-3.0-only 发布的副本，其接收者依据 LGPL 获得的权力不可撤销。