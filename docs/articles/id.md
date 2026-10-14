# 标识符

`Id` 是形如 `ModId.ItemId:SubId` 的结构化标识符，用于跨 MOD 引用。

## 格式

| 形式 | 含义 |
|------|------|
| `example_mod.example` | 无 SubId，视为 0 |
| `example_mod.example:0` | 显式 SubId 为 0，与上面等价 |
| `example_mod.example:5` | SubId 为 5 |

## 构造

```csharp
// 从三部分构造
var id = new Id("example_mod", "example", subId: 0);

// 从字符串构造
var id2 = new Id("example_mod.example");
var id3 = new Id("example_mod.example:5");

// 使用 IParsable
var id4 = Id.Parse("example_mod.example", null);
Id.TryParse("example_mod.example", null, out var id5);
```

## 属性

```csharp
id.ModId;      // "example_mod"
id.ItemId;     // "example"
id.SubId;      // 0
```

## 比较方法

针对不同维度的比较需求：

| 方法 | 比较的字段 | 用途 |
|------|-----------|------|
| `SameMod(other)` | ModId | 判断是否同一 MOD |
| `SameItem(other)` | ItemId | 跨 MOD 识别同一物品 |
| `SameModAndItem(other)` | ModId + ItemId | 同一 MOD 下的同一物品 |
| `SameItemAndSubId(other)` | ItemId + SubId | 跨 MOD 的同一物品同一序号 |
| `SameAll(other)` | 全部字段 | 完全相等，等价 `Equals` |

示例：

```csharp
var a = new Id("mod_a", "sword");
var b = new Id("mod_b", "sword");
var c = new Id("mod_a", "sword", 1);

a.SameMod(b);           // false，MOD 不同
a.SameItem(b);          // true，ItemId 都是 "sword"
a.SameModAndItem(b);    // false
a.SameItemAndSubId(c);  // false，SubId 不同
a.SameAll(c);           // false
```

## 相等性

```csharp
new Id("a", "b") == new Id("a", "b", 0);    // true
new Id("a", "b", 0) == new Id("a", "b", 1); // false
```

`SubId` 参与相等性判断。可以用作字典键。

## ToString 格式

```csharp
new Id("a", "b", 0).ToString();    // "a.b"
new Id("a", "b", 5).ToString();    // "a.b:5"
```

`SubId` 为 0 时省略 `:0`，其他值显式输出。

## 线程安全

`Id` 是不可变值类型，天然线程安全。可作为字典键跨线程共享。