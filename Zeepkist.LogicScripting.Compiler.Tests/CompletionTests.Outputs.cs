using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class CompletionTests
{
    [Theory]
    [InlineData("x = p.ho|", "p")]
    [InlineData("x = p.|", "p")]
    [InlineData("x = p.|ho", "p")]
    [InlineData("x = 1 & p.|", "p")]
    [InlineData("p.|", "p")]
    [InlineData("x = logic(p.|", "p")]
    public void Output_AfterANameAndADot(string text, string name)
    {
        var context = At(text);

        Assert.Equal(CaretContextKind.Output, context.Kind);
        Assert.Equal(name, context.DottedName);
    }

    [Theory]
    [InlineData("x = f().|", "f", null)]
    [InlineData("x = f().ho|", "f", null)]
    [InlineData("x = f(g(1), [a]).|", "f", null)]
    [InlineData("x = f([a, (b)], g([c])).|", "f", null)]
    [InlineData("x = logic(simple_telemetry(mode: local_velocity).|", "simple_telemetry", "local_velocity")]
    [InlineData("x = simple_telemetry(a: logic(mode: b), mode: local_velocity).|", "simple_telemetry", "local_velocity")]
    public void Output_AfterACallAndADot(string text, string callee, string mode)
    {
        var context = At(text);

        Assert.Equal(CaretContextKind.Output, context.Kind);
        Assert.Equal(callee, context.DottedCallee);
        Assert.Equal(mode, context.DottedMode);
        Assert.Null(context.DottedName);
        Assert.Null(context.DottedLibrary);
    }

    [Theory]
    [MemberData(nameof(RejectedPlacedCalls.All), MemberType = typeof(RejectedPlacedCalls))]
    public void Items_AfterADot_NothingFromAPlacedCallTheCompilerRejects(string call)
    {
        var placed = string.Format(call, "T");
        var context = At($"t = {placed}\nx = t.|");

        Assert.Empty(Completion.GetItems(context, DefinedNames.Read(context.Text, Level(RejectedPlacedCalls.Level))));
        Assert.Empty(Labels($"x = {placed}.|", RejectedPlacedCalls.Level.GetValueOrDefault));
    }

    [Theory]
    [MemberData(nameof(RejectedPlacedCalls.All), MemberType = typeof(RejectedPlacedCalls))]
    public void Items_NoNamedArguments_InACallToAPlacedCallTheCompilerRejects(string call)
    {
        var context = At($"l = {string.Format(call, "L")}\nl(|");
        var items = Completion.GetItems(context, DefinedNames.Read(context.Text, Level(RejectedPlacedCalls.Level)));

        Assert.DoesNotContain(items, i => i.Kind == CompletionKind.Argument);
    }

    [Theory]
    [InlineData("x = timing.edge(1).|")]
    [InlineData("x = timing.edge(g(1), [a]).ho|")]
    [InlineData("x = logic(timing.edge(1).|")]
    public void Output_AfterALibraryFunctionsCallAndADot(string text)
    {
        var context = At(text);

        Assert.Equal(CaretContextKind.Output, context.Kind);
        Assert.Equal("timing", context.DottedLibrary);
        Assert.Equal("edge", context.DottedCallee);
        Assert.Null(context.DottedName);
    }

    [Theory]
    [InlineData("x = 1.|")]
    [InlineData("x = p .|")]
    [InlineData("# p.|")]
    [InlineData("x = f() .|")]
    [InlineData("x = (a).|")]
    [InlineData("x = f()(1).|")]
    [InlineData("x = a.t.f().|")]
    [InlineData("x = t .f().|")]
    [InlineData("x = t. f().|")]
    [InlineData("x = [a].|")]
    public void None_AfterADotThatDoesntFollowANameOrACall(string text)
    {
        Assert.Equal(CaretContextKind.None, At(text).Kind);
    }

    [Fact]
    public void Items_AfterADot_TheOutputsOfTheBlockTheNameIsDefinedAs_InConnectorOrder()
    {
        Assert.Equal(["speed", "yaw", "up", "state"], NameLabels("t = simple_telemetry()\nx = t.|"));
    }

    [Theory]
    [InlineData("t = simple_telemetry(mode: local_velocity)\nx = t.|", new[] { "yaw", "x", "y", "z" })]
    [InlineData("t = simple_telemetry(1, mode: global_velocity)\nx = t.|", new[] { "x", "y", "z", "heading" })]
    [InlineData("t = simple_telemetry(mode: basic)\nx = t.|", new[] { "speed", "yaw", "up", "state" })]
    public void Items_AfterADot_FollowTheWrittenMode(string text, string[] expected)
    {
        Assert.Equal(expected, NameLabels(text));
    }

    [Fact]
    public void Items_AfterADot_MatchThePrefix()
    {
        Assert.Equal(["yaw", "y"], NameLabels("t = simple_telemetry(mode: local_velocity)\nx = t.y|"));
    }

    [Fact]
    public void Items_AfterADot_AreOutputs()
    {
        Assert.All(Names("t = simple_telemetry()\nx = t.|"), i => Assert.Equal(CompletionKind.Output, i.Kind));
    }

    [Theory]
    [InlineData(0, new[] { "speed", "yaw", "up", "state" })]
    [InlineData(2, new[] { "x", "y", "z", "heading" })]
    public void Items_AfterAPlacedBlockReferencesDot_ItsOutputsInTheLevel(int mode, string[] expected)
    {
        var level = new Dictionary<string, PlacedBlock>
        {
            ["T"] = new PlacedBlock(
                BlockManifest.SimpleTelemetry.DefaultStyle.BlockId,
                [Setting.Int(BlockManifest.SimpleTelemetryMode.Key, mode)]),
            ["D"] = new PlacedBlock(BlockManifest.DistanceSensor.DefaultStyle.BlockId)
        };
        var context = At("t = placed(\"T\")\nd = placed(\"D\")\nx = t.|");
        var items = Completion.GetItems(context, DefinedNames.Read(context.Text, Level(level)));

        Assert.Equal(expected, items.Select(i => i.Label));

        context = At("t = placed(\"T\")\nd = placed(\"D\")\nx = d.|");
        items = Completion.GetItems(context, DefinedNames.Read(context.Text, Level(level)));
        Assert.Equal(["distance", "x", "y", "z"], items.Select(i => i.Label));
    }

    [Theory]
    [InlineData("x = a & b\ny = x.|")]
    [InlineData("x = 1\ny = x.|")]
    [InlineData("x = logic(1, 2)\ny = x.|")]
    [InlineData("x = simple_telemetry() & 1\ny = x.|")]
    [InlineData("x = placed(\"UNKNOWN\")\ny = x.|")]
    [InlineData("t = simple_telemetry(mode: sideways)\nx = t.|")]
    [InlineData("y = t.|\nt = simple_telemetry()")]
    [InlineData("y = nothing.|")]
    [InlineData("function f(t)\n  y = t.|\nend")]
    [InlineData("t = simple_telemetry()\nfunction f()\n  y = t.|\nend")]
    [InlineData("function f(a) -> hi\nend\nr = f(1)\nx = r.|")]
    [InlineData("function f(a)\nend\nr = f(1)\nx = r.|")]
    [InlineData("function f(a) -> hi, lo\nend\nr = f(1) & 1\nx = r.|")]
    [InlineData("r = g(1)\nx = r.|")]
    [InlineData("function f(a) -> hi, lo\nend\nfunction g()\n  r = f(1)\nend\nx = r.|")]
    public void Items_AfterADot_NothingWhenTheNameIsntAVisibleBlockOrFunctionCallWithSeveralOutputs(string text)
    {
        Assert.Empty(Names(text));
    }

    [Fact]
    public void Items_AfterADot_TheFirstDefinitionOfANameDefinedTwice()
    {
        const string Text = "t = simple_telemetry(mode: global_velocity)\nt = simple_telemetry()\nx = t.|";

        Assert.Equal(["x", "y", "z", "heading"], NameLabels(Text));
    }

    [Fact]
    public void Items_AfterADot_InsideDelay_ALaterDefinition()
    {
        Assert.Equal(["speed", "yaw", "up", "state"], NameLabels("x = delay(t.|, ticks: 1)\nt = simple_telemetry()"));
    }

    [Fact]
    public void Items_AfterADot_TheOutputsOfTheFunctionTheNameCalls_InTheOrderItWritesThem()
    {
        Assert.Equal(["lo", "hi"], NameLabels("function f(a) -> lo, hi\nend\nr = f(1)\nx = r.|"));
    }

    [Fact]
    public void Items_AfterADot_TheOutputsOfTheFirstDefinitionOfAFunctionDefinedTwice()
    {
        const string Text = "function f(a) -> lo, hi\nend\nfunction f(a) -> x, y, z\nend\nr = f(1)\nx = r.|";

        Assert.Equal(["lo", "hi"], NameLabels(Text));
    }

    [Fact]
    public void Items_AfterADot_TheOutputsOfAFunctionCallInsideAFunction()
    {
        Assert.Equal(["lo", "hi"], NameLabels("function f(a) -> lo, hi\nend\nfunction g()\n  r = f(1)\n  x = r.|\nend"));
    }

    [Theory]
    [InlineData("t = (simple_telemetry())\nx = t.|")]
    [InlineData("t = simple_telemetry()\nu = t\nx = u.|")]
    [InlineData("t = (simple_telemetry())\nu = (t)\nv = u\nx = v.|")]
    public void Items_AfterADot_TheOutputsOfTheBlockAParenthesizedValueOrANameDefinedAsAnotherStandsFor(string text)
    {
        Assert.Equal(["speed", "yaw", "up", "state"], NameLabels(text));
    }

    [Theory]
    [InlineData("function f(a) -> hi, lo\nend\nr = (f(1))\nx = r.|")]
    [InlineData("function f(a) -> hi, lo\nend\nr = f(1)\ns = r\nx = s.|")]
    [InlineData("function f(a) -> hi, lo\nend\nfunction g()\n  r = (f(1))\n  s = r\n  x = s.|\nend")]
    public void Items_AfterADot_TheOutputsOfTheFunctionAParenthesizedValueOrANameDefinedAsAnotherCalls(string text)
    {
        Assert.Equal(["hi", "lo"], NameLabels(text));
    }

    [Theory]
    [InlineData("t = simple_telemetry()\nu = t & 1\nx = u.|")]
    [InlineData("u = t\nt = simple_telemetry()\nx = u.|")]
    [InlineData("t = simple_telemetry()\nfunction f()\n  u = t\n  x = u.|\nend")]
    [InlineData("function f(a) -> hi\nend\nr = (f(1))\nx = r.|")]
    public void Items_AfterADot_NothingForAParenthesizedValueOrANameDefinedAsAnotherThatIsntABlockOrFunctionCallWithSeveralOutputs(string text)
    {
        Assert.Empty(Names(text));
    }

    [Theory]
    [InlineData("x = simple_telemetry().|", new[] { "speed", "yaw", "up", "state" })]
    [InlineData("x = simple_telemetry(mode: global_velocity).|", new[] { "x", "y", "z", "heading" })]
    [InlineData("x = simple_telemetry(hide: true, mode: local_velocity).|", new[] { "yaw", "x", "y", "z" })]
    public void Items_AfterACallsDot_TheOutputsOfTheBlockFunction_InItsWrittenMode(string text, string[] expected)
    {
        Assert.Equal(expected, Labels(text));
    }

    [Theory]
    [InlineData("x = simple_telemetry(mode: global_velocity, mode: local_velocity).|")]
    [InlineData("t = simple_telemetry(mode: global_velocity, mode: local_velocity)\nx = t.|")]
    public void Items_AfterADot_TheFirstModeOfACallThatWritesItTwice(string text)
    {
        Assert.Equal(["x", "y", "z", "heading"], NameLabels(text));
    }

    [Theory]
    [InlineData("function f(a) -> lo, hi\nend\nx = f(1).|")]
    [InlineData("x = f(1).|\nfunction f(a) -> lo, hi\nend")]
    [InlineData("function f(a) -> lo, hi\nend\nfunction f(a) -> x, y, z\nend\nx = f(1).|")]
    [InlineData("function f(a) -> lo, hi\nend\nfunction g()\n  x = f(1).|\nend")]
    public void Items_AfterACallsDot_TheOutputsOfTheFirstDefinitionOfTheFunction_InTheOrderItWritesThem(string text)
    {
        Assert.Equal(["lo", "hi"], Labels(text));
    }

    [Theory]
    [InlineData("x = logic(1, 2).|")]
    [InlineData("x = simple_telemetry(mode: sideways).|")]
    [InlineData("function f(a) -> hi\nend\nx = f(1).|")]
    [InlineData("x = g(1).|")]
    [InlineData("x = placed(\"T\").|")]
    public void Items_AfterACallsDot_NothingWhenItsNotToABlockOrFunctionWithSeveralOutputs(string text)
    {
        Assert.Empty(Labels(text));
    }

    [Theory]
    [InlineData(0, new[] { "speed", "yaw", "up", "state" })]
    [InlineData(2, new[] { "x", "y", "z", "heading" })]
    public void Items_AfterAPlacedCallsDot_TheOutputsOfTheBlockWithThatUidInTheLevel(int mode, string[] expected)
    {
        var level = new Dictionary<string, PlacedBlock>
        {
            ["T"] = new PlacedBlock(
                BlockManifest.SimpleTelemetry.DefaultStyle.BlockId,
                [Setting.Int(BlockManifest.SimpleTelemetryMode.Key, mode)]),
            ["D"] = new PlacedBlock(BlockManifest.DistanceSensor.DefaultStyle.BlockId)
        };
        Func<string, PlacedBlock> findPlacedBlock = level.GetValueOrDefault;

        Assert.Equal(expected, Labels("x = placed(\"T\").|", findPlacedBlock));
        Assert.Equal(["distance", "x", "y", "z"], Labels("x = logic(placed(\"D\").|", findPlacedBlock));
        Assert.Equal(["y"], Labels("x = placed(\"D\").y|", findPlacedBlock));
        Assert.Equal(["distance", "x", "y", "z"], Labels("x = placed(((\"D\"))).|", findPlacedBlock));
    }

    [Theory]
    [InlineData("x = placed(\"UNKNOWN\").|")]
    [InlineData("x = placed(\"L\").|")]
    [InlineData("x = placed(D).|")]
    public void Items_AfterAPlacedCallsDot_NothingWhenNoBlockInTheLevelHasThatUidOrItHasOneOutput(string text)
    {
        var level = new Dictionary<string, PlacedBlock>
        {
            ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId),
            ["D"] = new PlacedBlock(BlockManifest.DistanceSensor.DefaultStyle.BlockId)
        };

        Assert.Empty(Labels(text, level.GetValueOrDefault));
    }

    [Theory]
    [InlineData("x = timing.edge(1).|")]
    [InlineData("x = logic(timing.edge(1, b: 2).|")]
    [InlineData("function edge(a) -> x, y, z\nend\nx = timing.edge(1).|")]
    [InlineData("timing = simple_telemetry()\nx = timing.edge(1).|")]
    [InlineData("function g()\n  x = timing.edge(1).|\nend")]
    public void Items_AfterALibraryFunctionCallsDot_ItsOutputs_InTheOrderItWritesThem(string text)
    {
        Assert.Equal(["lo", "hi"], NameLabels(text, Library(EdgeLibrary)));
    }

    [Fact]
    public void Items_AfterADot_NoOutputsOfANameBeforeACall()
    {
        Assert.Empty(NameLabels("t = simple_telemetry()\nx = t.|(1)"));
    }

    [Theory]
    [MemberData(nameof(RejectedFirstLines.All), MemberType = typeof(RejectedFirstLines))]
    public void Items_AfterADot_NothingFromAFunctionWhoseFirstLineTheCompilerRejects(string firstLine)
    {
        Assert.Empty(NameLabels($"{firstLine}\nend\nfunction g()\nend\nr = f(1)\nx = r.|"));
        Assert.Empty(Labels($"{firstLine}\nend\nfunction g()\nend\nx = f(1).|"));
    }

    [Theory]
    [MemberData(nameof(RejectedFirstLines.All), MemberType = typeof(RejectedFirstLines))]
    public void Items_InACallToAFunctionWhoseFirstLineTheCompilerRejects_NoInputs(string firstLine)
    {
        var items = Completion.GetItems(At($"{firstLine}\nend\nfunction g()\nend\nf(|"));

        Assert.DoesNotContain(items, i => i.Kind == CompletionKind.Argument);
    }

    [Fact]
    public void Accept_AnOutput_WritesItsName()
    {
        var context = At("t = simple_telemetry()\nx = t.sp|");
        var item = Completion.GetItems(context, DefinedNames.Read(context.Text)).First();

        Assert.Equal("t = simple_telemetry()\nx = t.speed|", Format(Completion.Accept(context, item)));
    }
}
