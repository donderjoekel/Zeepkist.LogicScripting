using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class CompletionTests
{
    [Theory]
    [InlineData("function f(a, b)\nend\nf(|", new[] { "a:", "b:" })]
    [InlineData("function f(a, b)\nend\nf(1, |", new[] { "b:" })]
    [InlineData("function f(a, b)\nend\nf(b: 1, |", new[] { "a:" })]
    [InlineData("f(|\nfunction f(a, b)\nend", new[] { "a:", "b:" })]
    [InlineData("function f(a, b) -> c\nend\nx = f(1, |", new[] { "b:" })]
    [InlineData("function f(a, b) -> c\nend\nf(1, b: 2, |", new string[0])]
    [InlineData("function f(a, b)\nend\nfunction g(x)\n    y = f(x, |\nend", new[] { "b:" })]
    public void Items_InACallToAFunction_ItsInputsItDoesntHaveYet(string text, string[] arguments)
    {
        var items = Completion.GetItems(At(text)).Where(i => i.Kind == CompletionKind.Argument).ToList();

        Assert.Equal(arguments, items.Select(i => i.Label));
        Assert.All(items, i => Assert.Equal("input", i.Detail));
    }

    [Theory]
    [InlineData("function f(a, b) -> c\nend\nx = timing.f(|")]
    [InlineData("function f(a, b) -> c\nend\nx = timing.f(1, |")]
    [InlineData("x = timing.logic(|")]
    public void Items_InAQualifiedCall_NoneOfTheScriptsFunctionsInputs(string text)
    {
        Assert.DoesNotContain(Completion.GetItems(At(text)), i => i.Kind == CompletionKind.Argument);
    }

    [Fact]
    public void Accept_AFunctionsInput_AddsItsColon()
    {
        var context = At("function f(a, b) -> c\nend\nx = f(1, |");
        var item = Completion.GetItems(context).First(i => i.Label == "b:");

        Assert.Equal("function f(a, b) -> c\nend\nx = f(1, b: |", Format(Completion.Accept(context, item)));
    }

    [Fact]
    public void Accept_AFunction_OpensItsCall()
    {
        var context = At("function edge(x)\nend\nz = ed|");
        var item = Completion.GetItems(context).First(i => i.Label == "edge");

        Assert.Equal("function edge(x)\nend\nz = edge(|", Format(Completion.Accept(context, item)));
    }

    [Theory]
    [InlineData("x = lo|", "logic", "x = logic(|")]
    [InlineData("x = lo|gic", "logic", "x = logic(|")]
    [InlineData("x = lo|(1, 2)", "logic", "x = logic(|1, 2)")]
    [InlineData("x = lo| (1, 2)", "logic", "x = logic (|1, 2)")]
    [InlineData("lo|\ny = 1", "logic", "logic(|\ny = 1")]
    [InlineData("x = tr|", "true", "x = true|")]
    [InlineData("x = |", "placed", "x = placed(|")]
    public void Accept_ReplacesTheWord_AndOpensAFunctionsCall(string text, string label, string expected)
    {
        var context = At(text);
        var item = Completion.GetItems(context).First(i => i.Label == label);

        Assert.Equal(expected, Format(Completion.Accept(context, item)));
    }

    [Fact]
    public void Accept_AName_WritesItAlone()
    {
        var context = At("abc = 1\nx = ab|");
        var item = Completion.GetItems(context, DefinedNames.Read(context.Text)).First(i => i.Label == "abc");

        Assert.Equal("abc = 1\nx = abc|", Format(Completion.Accept(context, item)));
    }

    [Fact]
    public void Items_InACall_NamedInputsAndSettings_BeforeTheExpressions()
    {
        var labels = Labels("logic(a, |");

        Assert.Equal(["b:", "hide:", "op:", "op_in:"], labels.Take(4));
        Assert.Contains("logic", labels);
    }

    [Fact]
    public void Items_InACall_LeaveOutTheNamedArgumentsItAlreadyHas()
    {
        Assert.DoesNotContain("op:", Labels("logic(a, op: and, |"));
        Assert.DoesNotContain("op:", Labels("logic(a, |, op: and)"));
        Assert.DoesNotContain("op:", Labels("logic(a, |\n    op: and)"));
        Assert.DoesNotContain("op:", Labels("x = logic(\n    a,\n    op: and,\n    |"));
        Assert.Contains("op:", Labels("logic(a, o|p: and)"));
    }

    [Theory]
    [InlineData("logic(|", "a:")]
    [InlineData("logic(1, |", "b:")]
    [InlineData("logic(op: and, 1, |", "b:")]
    [InlineData("logic(\n    1,\n    |", "b:")]
    [InlineData("logic(1, b|", "b:")]
    [InlineData("logic(f(1, 2), |", "b:")]
    public void Items_InACall_LeaveOutTheInputsItsPositionalArgumentsGive(string text, string first)
    {
        Assert.Equal(first, Labels(text).First(l => l.EndsWith(':')));
    }

    [Theory]
    [InlineData("logic(1, 2, |")]
    [InlineData("logic(1, |, 2)")]
    [InlineData("logic(1, | , 2)")]
    public void Items_InACall_NoInputsLeft_OnlyItsSettings(string text)
    {
        Assert.Equal(["hide:", "op:", "op_in:"], Labels(text).Where(l => l.EndsWith(':')));
    }

    [Fact]
    public void Items_InACall_OnlyItsOwnNamedArguments()
    {
        Assert.Contains("op:", Labels("logic(a, delay(1, ticks: 2, op: and), |"));
        Assert.Contains("ticks:", Labels("x = logic(op: and, delay(|"));
        Assert.DoesNotContain("op:", Labels("x = logic(op: and, delay(|"));
    }

    [Fact]
    public void Items_InACall_DescribeTheArguments()
    {
        var items = Completion.GetItems(At("logic(|"));

        Assert.Equal("input", items.First(i => i.Label == "a:").Detail);
        Assert.Equal("and, or, nand, nor, xor, xnor", items.First(i => i.Label == "op:").Detail);
        Assert.Equal("true, false", items.First(i => i.Label == "hide:").Detail);
        Assert.All(items.Where(i => i.Label.EndsWith(':')), i => Assert.Equal(CompletionKind.Argument, i.Kind));
    }

    [Fact]
    public void Items_InACallWithStyles_OfferStyle()
    {
        var style = Completion.GetItems(At("passthrough(|")).First(i => i.Label == "style:");

        Assert.Equal("normal, skinny_1h, skinny_2h, skinny_3h", style.Detail);
        Assert.DoesNotContain("style:", Labels("logic(|"));
    }

    [Fact]
    public void Items_BeforeAColon_OnlyNamedArguments()
    {
        Assert.Equal(["op:", "op_in:"], Labels("logic(a, o|: and)"));
    }

    [Fact]
    public void Items_InACallToAPlacedBlockReference_OnlyItsInputs()
    {
        var level = new Dictionary<string, PlacedBlock>
        {
            ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId)
        };
        var context = At("lamp_1 = placed(\"L\")\nlamp_1(1, |");
        var labels = Completion.GetItems(context, DefinedNames.Read(context.Text, Level(level)))
            .Where(i => i.Kind == CompletionKind.Argument)
            .Select(i => i.Label);

        Assert.Equal(["angle:", "blue:", "green:", "range:", "red:"], labels);
    }

    [Fact]
    public void Items_InACallToANameDefinedTwice_TheFirstDefinitionsInputs()
    {
        var level = new Dictionary<string, PlacedBlock>
        {
            ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId)
        };
        var context = At("lamp_1 = placed(\"L\")\nlamp_1 = logic(1, 2)\nlamp_1(|");
        var labels = Completion.GetItems(context, DefinedNames.Read(context.Text, Level(level)))
            .Where(i => i.Kind == CompletionKind.Argument)
            .Select(i => i.Label);

        Assert.Equal(["angle:", "blue:", "green:", "intensity:", "range:", "red:"], labels);
    }

    [Theory]
    [InlineData("a = logic(1, 2)\na(|")]
    [InlineData("a = placed(\"UNKNOWN\")\na(|")]
    [InlineData("a(|\na = placed(\"L\")")]
    [InlineData("x = (|")]
    [InlineData("x = |")]
    [InlineData("toggler(targets: [|")]
    public void Items_NoNamedArguments_OutsideACallThatTakesThem(string text)
    {
        Assert.DoesNotContain(Names(text), i => i.Kind == CompletionKind.Argument);
    }
}
