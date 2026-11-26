using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Texts mark the caret with <c>@</c>.
/// </summary>
public class NameRenameTests
{
    private const string Latch = "function latch(a, b) -> q, not_q\n    q = delay(!b & (a | q), ticks: 1)\n    not_q = !q\nend\n";

    private static readonly Dictionary<string, PlacedBlock> _level = new()
    {
        ["T"] = new PlacedBlock(BlockManifest.Trigger.DefaultStyle.BlockId),
        ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId)
    };

    private static readonly Dictionary<string, string> _libraries = new()
    {
        ["timing"] = "function edge(x) -> rising\n    rising = x & !delay(x, ticks: 1)\nend",
        ["util"] = "function id(v) -> w\n    w = v\nend"
    };

    private static readonly EditorLookups _lookups = new(uid => _level.GetValueOrDefault(uid), _libraries.GetValueOrDefault);

    [Fact]
    public void TopLevelName_RenamesItsDefinitionAndEveryUse()
    {
        var result = Rename("a = 1\nx = logic(@a, a)\npower_display(x)", "b");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("b = 1\nx = logic(b, b)\npower_display(x)", result.Text);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void CaretRightAfterTheName_Renames()
    {
        Assert.Equal("b = 1\npower_display(b)", Rename("a = 1\npower_display(a@)", "b").Text);
    }

    [Fact]
    public void CaretInsideTheName_GoesToTheEndOfTheNewName()
    {
        var result = Rename("abc = 1\npower_display(a@bc)", "longer");

        Assert.Equal("longer = 1\npower_display(longer)", result.Text);
        Assert.Equal("longer = 1\npower_display(longer".Length, result.Caret);
    }

    [Fact]
    public void CaretAtTheStartOfALaterUse_StaysAtItsStart()
    {
        var result = Rename("a = 1\npower_display(@a)", "bb");

        Assert.Equal("bb = 1\npower_display(".Length, result.Caret);
    }

    [Fact]
    public void NamedArgumentValue_IsRenamedButItsLabelIsNot()
    {
        var result = Rename("x = 1\ny = logic(a: @x, b: x)\npower_display(y)", "a");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("a = 1\ny = logic(a: a, b: a)\npower_display(y)", result.Text);
    }

    [Fact]
    public void SettingWord_SpelledLikeTheName_IsNotRenamed()
    {
        var result = Rename("or = 1\nx = logic(@or, 2, op: or)\npower_display(x)", "y");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("y = 1\nx = logic(y, 2, op: or)\npower_display(x)", result.Text);
    }

    [Fact]
    public void OutputPickedFromABlock_IsNotRenamed()
    {
        var result = Rename("horn = 1\np = player_input()\npower_display(p.horn & @horn)", "h");

        Assert.Equal("h = 1\np = player_input()\npower_display(p.horn & h)", result.Text);
    }

    [Fact]
    public void PlacedBlockReference_RenamesWiringCallsAndTargets()
    {
        var text = "t = placed(\"T\")\n@lamp_1 = placed(\"L\")\nlamp_1(intensity: t)\ntoggler(1, targets: [lamp_1, t])";

        var result = Rename(text, "lamp");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("t = placed(\"T\")\nlamp = placed(\"L\")\nlamp(intensity: t)\ntoggler(1, targets: [lamp, t])", result.Text);
    }

    [Fact]
    public void Function_RenamesItsFirstLineAndEveryCall()
    {
        var text = "function @same(v) -> w\n    w = v\nend\nfunction twice(v) -> w\n    w = same(same(v))\nend\npower_display(same(1) & twice(2))";

        var result = Rename(text, "copy");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("function copy(v) -> w\n    w = v\nend\nfunction twice(v) -> w\n    w = copy(copy(v))\nend\npower_display(copy(1) & twice(2))", result.Text);
    }

    [Fact]
    public void FunctionInput_RenamesItsBodyUsesAndNamedArgumentsAtCalls()
    {
        var text = Latch + "l = latch(b: 0, a: 1)\npower_display(l.q)";

        var result = Rename(text.Replace("latch(a, b)", "latch(a, @b)"), "reset");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(
            "function latch(a, reset) -> q, not_q\n    q = delay(!reset & (a | q), ticks: 1)\n    not_q = !q\nend\nl = latch(reset: 0, a: 1)\npower_display(l.q)",
            result.Text);
    }

    [Fact]
    public void FunctionOutput_RenamesItsDefinitionAndPicksThroughAliases()
    {
        var text = Latch + "l = latch(1, 0)\nm = l\npower_display(m.q & latch(0, 1).q & l.not_q)";

        var result = Rename(text.Replace("-> q,", "-> @q,"), "set");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(
            "function latch(a, b) -> set, not_q\n    set = delay(!b & (a | set), ticks: 1)\n    not_q = !set\nend\nl = latch(1, 0)\nm = l\npower_display(m.set & latch(0, 1).set & l.not_q)",
            result.Text);
    }

    [Fact]
    public void FunctionOutput_PickedFromAnInput_IsRefused()
    {
        var text = Latch + "function show(l)\n    power_display(l.q)\nend\nshow(latch(1, 0))";

        var result = Rename(text.Replace("-> q,", "-> @q,"), "set");

        Assert.False(result.Succeeded);
        Assert.Contains("'.q' on line 6", result.Error);
        Assert.Equal(Unmarked(text), result.Text);
    }

    [Fact]
    public void TopLevelName_LeavesTheSameNameInAFunctionBody()
    {
        var text = "@x = 1\nfunction f(x) -> y\n    y = x\nend\nfunction g() -> x\n    x = 2\nend\npower_display(f(x) & g())";

        var result = Rename(text, "top");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("top = 1\nfunction f(x) -> y\n    y = x\nend\nfunction g() -> x\n    x = 2\nend\npower_display(f(top) & g())", result.Text);
    }

    [Fact]
    public void BodyName_LeavesTheTopLevelAndOtherBodies()
    {
        var text = "t = 1\nfunction f(v) -> w\n    @t = v\n    w = t\nend\nfunction g(v) -> w\n    t = v\n    w = t\nend\npower_display(f(t) & g(t))";

        var result = Rename(text, "kept");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("t = 1\nfunction f(v) -> w\n    kept = v\n    w = kept\nend\nfunction g(v) -> w\n    t = v\n    w = t\nend\npower_display(f(t) & g(t))", result.Text);
    }

    [Fact]
    public void SettingInput_RenamesItsUseAsASettingValue()
    {
        var text = "function wait(x, @n) -> y\n    y = delay(x, ticks: n)\nend\npower_display(wait(1, n: 3))";

        var result = Rename(text, "ticks_");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("function wait(x, ticks_) -> y\n    y = delay(x, ticks: ticks_)\nend\npower_display(wait(1, ticks_: 3))", result.Text);
    }

    [Fact]
    public void NameUsedAhead_InDelay_IsRenamed()
    {
        var result = Rename("x = delay(@later, ticks: 2)\nlater = passthrough(x)\npower_display(later)", "next");

        Assert.Equal("x = delay(next, ticks: 2)\nnext = passthrough(x)\npower_display(next)", result.Text);
    }

    [Fact]
    public void CallOverSeveralLines_WithComments_KeepsLayout()
    {
        var text = "@a = 1 # the a\nx = logic(\n    a, # first a\n    2\n)\npower_display(x)";

        var result = Rename(text, "b");

        Assert.Equal("b = 1 # the a\nx = logic(\n    b, # first a\n    2\n)\npower_display(x)", result.Text);
    }

    [Fact]
    public void LibraryNames_AreNotRenamed()
    {
        var text = "@edge = 1\npower_display(timing.edge(edge))";

        var result = Rename(text, "e");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("e = 1\npower_display(timing.edge(e))", result.Text);
    }

    [Fact]
    public void SameName_ChangesNothing()
    {
        var result = Rename("@a = 1\npower_display(a)", "a");

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.Count);
        Assert.Equal("a = 1\npower_display(a)", result.Text);
    }

    [Theory]
    [InlineData("", "Type the new name")]
    [InlineData("1a", "isn't a name")]
    [InlineData("a b", "isn't a name")]
    [InlineData("a-b", "isn't a name")]
    [InlineData("logic", "Block Function")]
    [InlineData("placed", "reserved word")]
    [InlineData("end", "reserved word")]
    [InlineData("b", "already defined")]
    [InlineData("f", "already defined")]
    [InlineData("v", "already defined")]
    [InlineData("w", "already defined")]
    [InlineData("local", "already defined")]
    public void BadNewName_IsRefusedAndTheTextKept(string newName, string error)
    {
        var text = "@a = 1\nb = 2\nfunction f(v) -> w\n    local = v\n    w = local\nend\npower_display(f(a) & b)";

        var result = Rename(text, newName);

        Assert.False(result.Succeeded);
        Assert.Contains(error, result.Error);
        Assert.Equal(Unmarked(text), result.Text);
    }

    [Fact]
    public void ScriptWithErrors_IsRefused()
    {
        var result = Rename("@a = 1\npower_display(nope)", "b");

        Assert.Equal(NameRename.FixErrorsFirst, result.Error);
        Assert.Equal("a = 1\npower_display(nope)", result.Text);
    }

    [Theory]
    [InlineData("a = 1\npower_display(a & @1)")]
    [InlineData("a = 1\n@power_display(a)")]
    [InlineData("a = logic(1, 2, op: @or)\npower_display(a)")]
    [InlineData("p = player_input()\npower_display(p.ho@rn)")]
    [InlineData("power_display(tim@ing.edge(1))")]
    [InlineData("# a com@ment\na = 1\npower_display(a)")]
    public void NoNameAtTheCaret_IsRefused(string text)
    {
        var result = Rename(text, "b");

        Assert.Equal(NameRename.NoNameHere, result.Error);
    }

    [Fact]
    public void Find_GivesTheNameAtTheCaret()
    {
        Assert.Equal("lamp_1", NameRename.Find("lamp_1 = placed(\"L\")\nlamp_1(intensity: 1)", 25, _lookups, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Renamed_CompilesToTheSameBlocks()
    {
        var text = Latch + "t = placed(\"T\")\n@l = latch(t, 0)\nlamp = placed(\"L\")\nlamp(intensity: l.q, red: l.not_q)";
        var before = ScriptCompiler.Compile(Unmarked(text), 0, findPlacedBlock: _level.GetValueOrDefault);

        var result = Rename(text, "flipflop");
        var after = ScriptCompiler.Compile(result.Text, 0, findPlacedBlock: _level.GetValueOrDefault);

        Assert.True(result.Succeeded, result.Error);
        Assert.True(after.Succeeded);
        Assert.Equal(before.Blocks.Select(DescribeBlock), after.Blocks.Select(DescribeBlock));
        Assert.Equal(before.Wires.Select(WireText.Describe), after.Wires.Select(WireText.Describe));
        Assert.Equal(before.PlacedWires.Select(WireText.Describe), after.PlacedWires.Select(WireText.Describe));
    }

    [Fact]
    public void FunctionOutput_PickedThroughAnotherFunctionsOutput_IsRenamed()
    {
        var text = Latch + "function wrap() -> l, m\n    l = latch(1, 0)\n    m = 1\nend\nx = wrap().l\npower_display(x.q & wrap().m)";

        var result = Rename(text.Replace("-> q,", "-> @q,"), "set");

        Assert.True(result.Succeeded, result.Error);
        Assert.EndsWith("x = wrap().l\npower_display(x.set & wrap().m)", result.Text);
    }

    [Fact]
    public void FunctionOutput_PickedThroughAOneOutputFunction_IsRenamed()
    {
        var text = Latch + "function make() -> l\n    l = latch(1, 0)\nend\npower_display(make().q)";

        var result = Rename(text.Replace("-> q,", "-> @q,"), "set");

        Assert.True(result.Succeeded, result.Error);
        Assert.EndsWith("power_display(make().set)", result.Text);
    }

    [Fact]
    public void FunctionOutput_PickedFromALibraryCall_IsRefusedNamingThePick()
    {
        var text = Latch + "x = util.id(latch(1, 0))\npower_display(x.q)";

        var result = Rename(text.Replace("-> q,", "-> @q,"), "set");

        Assert.False(result.Succeeded);
        Assert.Contains("'.q' on line 6", result.Error);
        Assert.Equal(Unmarked(text), result.Text);
    }

    private static string DescribeBlock(CompiledBlock block)
    {
        var settings = string.Join(",", block.Settings.Select(s => $"{s.Key}={s.Kind}:{s.IntValue}:{s.TextValue}"));
        return $"{block.Type}/{block.Style}/{block.Yaw}/{block.Cell.X},{block.Cell.Z}/{settings}";
    }

    private static RenameResult Rename(string marked, string newName)
    {
        var index = marked.IndexOf('@');
        return NameRename.Rename(Unmarked(marked), index, newName, _lookups);
    }

    private static string Unmarked(string marked)
    {
        var index = marked.IndexOf('@');
        return index < 0 ? marked : marked.Remove(index, 1);
    }
}
