using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class CompletionTests
{
    [Fact]
    public void Items_AfterDefault_Hide()
    {
        Assert.Equal(["hide:"], Labels("default |"));
        Assert.Empty(Labels("default x|"));
    }

    [Theory]
    [InlineData("logic(a, o|", "op:", "logic(a, op: |")]
    [InlineData("logic(a, o|p", "op:", "logic(a, op: |")]
    [InlineData("logic(a, o|: and)", "op:", "logic(a, op|: and)")]
    [InlineData("logic(a, o| : and)", "op:", "logic(a, op| : and)")]
    [InlineData("default |", "hide:", "default hide: |")]
    public void Accept_ANamedArgument_WritesItsColon_UnlessOneFollows(string text, string label, string expected)
    {
        var context = At(text);
        var item = Completion.GetItems(context).First(i => i.Label == label);

        Assert.Equal(expected, Format(Completion.Accept(context, item)));
    }

    [Theory]
    [InlineData("logic(a, b, op: |")]
    [InlineData("logic(a, b, op:|")]
    [InlineData("logic(a, op: a|nd)")]
    [InlineData("x = logic(\n    a,\n    op: |")]
    [InlineData("x = logic(\n    a,\n    op:\n    |")]
    [InlineData("logic(a, hide: |")]
    [InlineData("passthrough(1, style: |")]
    [InlineData("death(1, player: 2|")]
    [InlineData("delay(1, ticks: |")]
    [InlineData("default hide: |")]
    [InlineData("x = 1\ndefault hide: tr|")]
    public void SettingValue_AfterASettingsColon(string text)
    {
        Assert.Equal(CaretContextKind.SettingValue, At(text).Kind);
    }

    [Theory]
    [InlineData("logic(a, b: |")]
    [InlineData("toggler(1, targets: |")]
    [InlineData("logic(a, nope: |")]
    [InlineData("logic(style: |")]
    [InlineData("default op: |")]
    [InlineData("default hide:\n|")]
    [InlineData("x = logic(a)\nop:\n|")]
    [InlineData("lamp_1 = placed(\"L\")\nlamp_1(red: |")]
    [InlineData("x = timing.logic(a, op: |")]
    public void Expression_AfterTheColonOfAnInputOrPlacedBlocksOrAnythingElse(string text)
    {
        Assert.Equal(CaretContextKind.Expression, At(text).Kind);
    }

    [Fact]
    public void SettingValue_KnowsTheCallAndTheSetting()
    {
        var context = At("x = logic(a, op: |");

        Assert.Equal("logic", context.Callee);
        Assert.Equal("op", context.SettingName);
        Assert.Null(At("default hide: |").Callee);
        Assert.Null(At("x = logic(a, |").SettingName);
    }

    [Theory]
    [InlineData("logic(a, b, op: |", new[] { "and", "or", "nand", "nor", "xor", "xnor" })]
    [InlineData("logic(a, hide: |", new[] { "true", "false" })]
    [InlineData("default hide: |", new[] { "true", "false" })]
    [InlineData("death(1, player: |", new[] { "all", "1", "2", "3", "4" })]
    [InlineData("player_input(player: |", new[] { "1", "2", "3", "4" })]
    [InlineData("death(1, player: 2|", new[] { "2" })]
    [InlineData("passthrough(1, style: |", new[] { "normal", "skinny_1h", "skinny_2h", "skinny_3h" })]
    [InlineData("logic(a, op: n|", new[] { "nand", "nor", "and", "xnor" })]
    public void Items_ASettingsValues_InTheOrderItStoresThem(string text, string[] expected)
    {
        Assert.Equal(expected, Labels(text));
    }

    [Theory]
    [InlineData("delay(1, ticks: |")]
    [InlineData("generator(power: |")]
    public void Items_NoneForANumberSetting(string text)
    {
        Assert.Empty(Labels(text));
    }

    [Fact]
    public void Items_ASettingsValues_AreValues()
    {
        Assert.All(Completion.GetItems(At("logic(a, op: |")), i => Assert.Equal(CompletionKind.Value, i.Kind));
    }

    [Theory]
    [InlineData("logic(a, op: n|", "nor", "logic(a, op: nor|")]
    [InlineData("logic(a, op: a|nd)", "nand", "logic(a, op: nand|)")]
    [InlineData("default hide: |", "true", "default hide: true|")]
    public void Accept_AValue_ReplacesTheWord(string text, string label, string expected)
    {
        var context = At(text);
        var item = Completion.GetItems(context).First(i => i.Label == label);

        Assert.Equal(expected, Format(Completion.Accept(context, item)));
    }
}
