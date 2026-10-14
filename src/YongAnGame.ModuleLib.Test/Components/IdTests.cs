using YongAnGame.ModuleLib.Components;

namespace YongAnGame.ModuleLib.Tests.Components;

[TestClass]
public class IdTests
{
    // ──────────────────────────────────────────────
    // 构造
    // ──────────────────────────────────────────────

    [TestMethod]
    public void Constructor_ValidParts_SetsProperties()
    {
        var id = new Id("mod_a", "sword", 3);

        Assert.AreEqual("mod_a", id.ModId);
        Assert.AreEqual("sword", id.ItemId);
        Assert.AreEqual(3, id.SubId);
    }

    [TestMethod]
    public void Constructor_OmitsSubId_DefaultsToZero()
    {
        var id = new Id("mod_a", "sword");

        Assert.AreEqual(0, id.SubId);
    }

    [TestMethod]
    [DataRow(null, "sword", DisplayName = "ModId 为 null")]
    [DataRow("mod_a", null, DisplayName = "ItemId 为 null")]
    public void Constructor_NullPart_ThrowsArgumentNullException(string? modId, string? itemId)
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new Id(modId!, itemId!));
    }

    [TestMethod]
    [DataRow("", "sword", DisplayName = "ModId 为空字符串")]
    [DataRow("  ", "sword", DisplayName = "ModId 为空白")]
    [DataRow("mod_a", "", DisplayName = "ItemId 为空字符串")]
    [DataRow("mod_a", "  ", DisplayName = "ItemId 为空白")]
    public void Constructor_WhitespacePart_ThrowsArgumentException(string modId, string itemId)
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => new Id(modId, itemId));
    }

    [TestMethod]
    public void Constructor_NegativeSubId_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new Id("mod_a", "sword", -1));
    }

    // ──────────────────────────────────────────────
    // 字符串解析
    // ──────────────────────────────────────────────

    [TestMethod]
    [DataRow("mod_a.sword", "mod_a", "sword", 0)]
    [DataRow("mod_a.sword:0", "mod_a", "sword", 0)]
    [DataRow("mod_a.sword:5", "mod_a", "sword", 5)]
    [DataRow("mod_a.complex.name", "mod_a", "complex.name", 0)]
    public void Parse_ValidFormat_ReturnsExpected(
        string input, string expectedMod, string expectedItem, int expectedSub)
    {
        var id = new Id(input);

        Assert.AreEqual(expectedMod, id.ModId);
        Assert.AreEqual(expectedItem, id.ItemId);
        Assert.AreEqual(expectedSub, id.SubId);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("nodot")]
    [DataRow(".sword")]
    [DataRow("mod_a.")]
    [DataRow(" .sword")]
    [DataRow("mod_a. ")]
    [DataRow("mod_a.sword:")]
    [DataRow("mod_a.sword:abc")]
    [DataRow("mod_a.sword:-1")]
    public void Parse_InvalidFormat_Throws(string input)
    {
        Assert.ThrowsExactly<FormatException>(() => new Id(input));
    }

    [TestMethod]
    public void Parse_Null_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new Id((string)null!));
    }

    // ──────────────────────────────────────────────
    // TryParse 不抛异常
    // ──────────────────────────────────────────────

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("nodot")]
    [DataRow(".sword")]
    [DataRow("mod_a.")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        bool result = Id.TryParse(input, null, out _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryParse_ValidInput_ReturnsTrue()
    {
        bool result = Id.TryParse("mod_a.sword:3", null, out Id id);

        Assert.IsTrue(result);
        Assert.AreEqual("mod_a", id.ModId);
        Assert.AreEqual("sword", id.ItemId);
        Assert.AreEqual(3, id.SubId);
    }

    // ──────────────────────────────────────────────
    // 相等性
    // ──────────────────────────────────────────────

    [TestMethod]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new Id("mod_a", "sword", 0);
        var b = new Id("mod_a", "sword", 0);

        Assert.IsTrue(a == b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Equals_DifferentSubId_ReturnsFalse()
    {
        var a = new Id("mod_a", "sword", 0);
        var b = new Id("mod_a", "sword", 1);

        Assert.IsTrue(a != b);
    }

    // ──────────────────────────────────────────────
    // 比较方法
    // ──────────────────────────────────────────────

    [TestMethod]
    public void SameMod_DifferentItem_ReturnsTrue()
    {
        var a = new Id("mod_a", "sword");
        var b = new Id("mod_a", "shield");

        Assert.IsTrue(a.SameMod(b));
        Assert.IsFalse(a.SameItem(b));
    }

    [TestMethod]
    public void SameItem_CrossMod_ReturnsTrue()
    {
        var a = new Id("mod_a", "sword");
        var b = new Id("mod_b", "sword");

        Assert.IsTrue(a.SameItem(b));
        Assert.IsFalse(a.SameMod(b));
    }

    [TestMethod]
    public void SameItemAndSubId_CrossModSameSubId_ReturnsTrue()
    {
        var a = new Id("mod_a", "sword", 1);
        var b = new Id("mod_b", "sword", 1);

        Assert.IsTrue(a.SameItemAndSubId(b));
    }

    [TestMethod]
    public void SameAll_DifferentMod_ReturnsFalse()
    {
        var a = new Id("mod_a", "sword");
        var b = new Id("mod_b", "sword");

        Assert.IsFalse(a.SameAll(b));
    }

    // ──────────────────────────────────────────────
    // ToString
    // ──────────────────────────────────────────────

    [TestMethod]
    [DataRow("mod_a", "sword", 0, "mod_a.sword")]
    [DataRow("mod_a", "sword", 5, "mod_a.sword:5")]
    public void ToString_ExpectedFormat(
        string mod, string item, int sub, string expected)
    {
        var id = new Id(mod, item, sub);

        Assert.AreEqual(expected, id.ToString());
    }
}