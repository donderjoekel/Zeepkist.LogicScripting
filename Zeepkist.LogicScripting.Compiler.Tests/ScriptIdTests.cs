using System;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class ScriptIdTests
{
    [Fact]
    public void IdsWithTheSameText_AreEqual()
    {
        Assert.True(new ScriptId("a") == new ScriptId("a"));
        Assert.Equal(new ScriptId("a").GetHashCode(), new ScriptId("a").GetHashCode());
    }

    [Fact]
    public void IdsWithOtherText_Differ()
    {
        Assert.True(new ScriptId("a") != new ScriptId("b"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AnEmptyId_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new ScriptId(value));
    }
}
