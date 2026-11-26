using System;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class CompletionTests
{
    [Theory]
    [InlineData("lo|")]
    [InlineData("|")]
    [InlineData("x = lo|")]
    [InlineData("x = 1 & lo|")]
    [InlineData("x = !lo|")]
    [InlineData("x = logic(op_in: lo|")]
    [InlineData("toggler(1, targets: [pl|")]
    [InlineData("x = 1\nlo|")]
    [InlineData("x = (lo|")]
    [InlineData("x = lo|gic")]
    [InlineData("# a comment\nlo|")]
    [InlineData("x = \"a\" & lo|")]
    [InlineData("x = \"#\" & lo|")]
    public void Expression_WhereAnExpressionCanStart(string text)
    {
        Assert.Equal(CaretContextKind.Expression, At(text).Kind);
    }

    [Theory]
    [InlineData("# lo|")]
    [InlineData("x = 1 # lo|")]
    [InlineData("x = placed(\"lo|")]
    [InlineData("x = placed(\"lo|\")")]
    [InlineData("x = 12|")]
    [InlineData("x = 1|2")]
    [InlineData("x = 1lo|")]
    [InlineData("x = p ho|")]
    [InlineData("x = f() lo|")]
    [InlineData("lo| = 1")]
    [InlineData("x = lo|: 1")]
    [InlineData("x = default hi|")]
    public void None_InCommentsStringsNumbersAndWhereNoExpressionGoes(string text)
    {
        Assert.Equal(CaretContextKind.None, At(text).Kind);
    }

    [Fact]
    public void Word_IsTheWholeNameAroundTheCaret_AndThePrefixThePartBeforeIt()
    {
        var context = At("x = lo|gic(1)");

        Assert.Equal(4, context.Word.Start);
        Assert.Equal(5, context.Word.Length);
        Assert.Equal("lo", context.Prefix);
    }

    [Theory]
    [InlineData("power_display(lo|")]
    [InlineData("power_display(lo|)")]
    [InlineData("x = logic(1, lo|")]
    [InlineData("x = logic(\n    1,\n    lo|\n)")]
    [InlineData("x = logic(\n    1,\n    lo|")]
    [InlineData("x = logic(1, logic(|), 2)")]
    public void Argument_WhereAnArgumentOfACallStarts(string text)
    {
        Assert.Equal(CaretContextKind.Argument, At(text).Kind);
    }

    [Theory]
    [InlineData("x = logic(1, o|p: and)")]
    [InlineData("x = logic(1, |: and)")]
    public void ArgumentName_BeforeANamedArgumentsColon(string text)
    {
        Assert.Equal(CaretContextKind.ArgumentName, At(text).Kind);
    }

    [Theory]
    [InlineData("default |")]
    [InlineData("default hi|")]
    [InlineData("x = 1\ndefault hi|: true")]
    public void DefaultSetting_AfterDefault(string text)
    {
        Assert.Equal(CaretContextKind.DefaultSetting, At(text).Kind);
    }

    [Theory]
    [InlineData("x = placed(\"T\").|", "T")]
    [InlineData("x = placed(T).|", null)]
    [InlineData("x = placed().|", null)]
    [InlineData("x = f(\"T\").|", null)]
    public void DottedUid_IsTheUidOfAPlacedCallBeforeTheDot(string text, string uid)
    {
        Assert.Equal(uid, At(text).DottedUid);
    }

    [Theory]
    [InlineData("function f(a) -> hi, lo")]
    [MemberData(nameof(RejectedFirstLines.All), MemberType = typeof(RejectedFirstLines))]
    public void RejectedFirstLines_AreTheOnesWhoseCallTheCompilerDoesntExpand(string firstLine)
    {
        var text = $"{firstLine}\n    hi = delay(a, ticks: 1)\n    lo = !hi\nend\nfunction g()\nend\nr = f(1)";
        var isRejected = RejectedFirstLines.Lines.Contains(firstLine);

        var result = ScriptCompiler.Compile(text, 0, [], _ => null);

        Assert.Equal(!isRejected, result.Blocks.Any(b => b.Type == BlockManifest.Delay));
    }
}
