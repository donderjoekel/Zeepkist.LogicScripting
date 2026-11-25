using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockImportTests
{
    private const int GeneratorId = 2414;
    private const int DisplayId = 2415;
    private const int CompactDisplayId = 2513;
    private const int MathId = 2416;
    private const int LogicId = 2420;

    [Theory]
    [InlineData("power_display(generator(power: 5) | generator(power: -3))")]
    [InlineData("power_display(1 & 1)")]
    [InlineData("power_display(7)\npower_display(7)\npower_display(7)")]
    [InlineData("power_display(logic(1, 2, op: xnor), style: compact)")]
    [InlineData("x = generator(power: 9223372036854775807)\npower_display(x, hide: true)")]
    [InlineData("a = generator()\nb = logic(a, a, a)\nc = logic(b, a)\nd = logic(b, a)\npower_display(c)\npower_display(d)")]
    [InlineData("power_display(logic(op_in: 1))\npower_display(logic(b: 2, op: nand))\npower_display(logic())")]
    [InlineData("power_display(power_display(power_display(3, style: compact)))")]
    [InlineData("generator(power: 5)\nlogic()\npower_display()")]
    [InlineData("default hide: true\npower_display(1 & 2)")]
    public void CompiledText_ImportsIntoTextThatCompilesToTheSame(string text)
    {
        var compiled = ScriptCompiler.Compile(text, 0);
        Assert.True(compiled.Succeeded);
        var (blocks, wires) = Read(compiled);

        foreach (var selection in new[] { blocks, Enumerable.Reverse(blocks).ToList() })
        {
            var import = BlockImport.Import(selection, wires);

            Assert.Null(import.Refusal);
            var again = ScriptCompiler.Compile(import.Text, 0);
            Assert.True(again.Succeeded);
            Assert.Equal(Describe(compiled), Describe(again));
            Assert.Equal(compiled.Wires.Count, again.Wires.Count);
        }
    }

    [Fact]
    public void TheText_HasOneLinePerBlock_EachNameAboveItsUse()
    {
        var import = BlockImport.Import(
            [
                new ImportedBlock("d", CompactDisplayId, ints: Ints(("lbhd", 1))),
                new ImportedBlock("l", LogicId, ints: Ints(("xmo", 1))),
                new ImportedBlock("g2", GeneratorId, texts: Texts(("lbp", "0"))),
                new ImportedBlock("g1", GeneratorId, texts: Texts(("lbp", "5")))
            ],
            [new ImportedWire("g1", 0, "l", 0), new ImportedWire("g2", 0, "l", 2), new ImportedWire("l", 3, "d", 0)]);

        Assert.Null(import.Refusal);
        Assert.Equal(
            """
            generator_1 = generator()
            generator_2 = generator(power: 5)
            logic_1 = logic(generator_2, op_in: generator_1, op: or)
            power_display_1 = power_display(logic_1, hide: true, style: compact)
            """.Replace("\r\n", "\n"),
            import.Text);
    }

    [Fact]
    public void AWireReadFromBothEnds_IsOne()
    {
        var import = BlockImport.Import(
            [new ImportedBlock("g", GeneratorId), new ImportedBlock("d", DisplayId)],
            [new ImportedWire("g", 0, "d", 0), new ImportedWire("g", 0, "d", 0)]);

        Assert.Equal("generator_1 = generator()\npower_display_1 = power_display(generator_1)", import.Text);
    }

    [Fact]
    public void WiresPaintAndTheModsOwnKeys_AreNoSettings()
    {
        var import = BlockImport.Import(
            [
                new ImportedBlock(
                    "d",
                    DisplayId,
                    ints: Ints(("p0", 12), ("p13", 4), ("id0", 1), (ScriptCarrier.LanguageVersionKey, 1)),
                    texts: Texts((BlockGroupTag.Key, "a:b"), (ScriptCarrier.TextKey, "x"), (ScriptCarrier.NameKey, "y"), ("id0-0", "z")))
            ],
            []);

        Assert.Equal("power_display_1 = power_display()", import.Text);
    }

    [Fact]
    public void AnUnknownSettingAtItsDefault_IsNoReason()
    {
        var import = BlockImport.Import(
            [new ImportedBlock("d", DisplayId, ints: Ints(("new", 0)), texts: Texts(("oi2", "0"), ("oi3", "")), floats: Floats(("x1", 0)))],
            []);

        Assert.Equal("power_display_1 = power_display()", import.Text);
    }

    [Fact]
    public void ABlockOfAnotherType_IsRefused()
    {
        AssertRefused(
            "math \"m\" isn't a type Import takes: generator, logic, power_display",
            [new ImportedBlock("g", GeneratorId), new ImportedBlock("m", MathId)]);
    }

    [Fact]
    public void ABlockThatIsNoLogicBlock_IsRefused()
    {
        AssertRefused("block \"b\" isn't a type Import takes: generator, logic, power_display", [new ImportedBlock("b", 1)]);
    }

    [Fact]
    public void ABlockInABlockGroup_IsRefused()
    {
        AssertRefused(
            "generator \"g\" is in a Block Group, which a recompile replaces",
            [new ImportedBlock("g", GeneratorId, isInBlockGroup: true)]);
    }

    [Fact]
    public void ATogglersTarget_IsRefused()
    {
        var import = BlockImport.Import([new ImportedBlock("g", GeneratorId)], [], ["other", "g"]);

        Assert.Null(import.Text);
        Assert.Equal("a Toggler targets generator \"g\", and would lose it at the next Compile", import.Refusal);
    }

    [Fact]
    public void AWireToABlockOutside_IsRefused()
    {
        AssertRefused(
            "generator \"g\" has a Wire to block \"out\", which isn't selected",
            [new ImportedBlock("g", GeneratorId)],
            new ImportedWire("g", 0, "out", 0));
    }

    [Fact]
    public void AWireFromABlockOutside_IsRefused()
    {
        AssertRefused(
            "power_display \"d\" has a Wire from block \"out\", which isn't selected",
            [new ImportedBlock("d", DisplayId)],
            new ImportedWire("out", 0, "d", 0));
    }

    [Fact]
    public void AWireToAnOutput_IsRefused()
    {
        AssertRefused(
            "the Wire from generator \"g\" to power_display \"d\" doesn't go from an output to an input",
            [new ImportedBlock("g", GeneratorId), new ImportedBlock("d", DisplayId)],
            new ImportedWire("g", 0, "d", 1));
    }

    [Fact]
    public void TwoWiresIntoOneInput_AreRefused()
    {
        AssertRefused(
            "power_display \"d\" has several Wires into one input",
            [new ImportedBlock("g", GeneratorId), new ImportedBlock("h", GeneratorId), new ImportedBlock("d", DisplayId)],
            new ImportedWire("g", 0, "d", 0),
            new ImportedWire("h", 0, "d", 0));
    }

    [Fact]
    public void ALoop_IsRefused()
    {
        AssertRefused(
            "logic \"l\" is in a loop of Wires, or fed by one",
            [new ImportedBlock("l", LogicId), new ImportedBlock("d", DisplayId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "l", 0));
    }

    [Fact]
    public void AStyleTheStartupCheckTurnedOff_IsRefused()
    {
        var import = BlockImport.Import([new ImportedBlock("d", CompactDisplayId)], [], null, [CompactDisplayId]);

        Assert.Equal(
            "its text doesn't compile: 'power_display style compact' doesn't match this game version and is turned off",
            import.Refusal);
    }

    [Theory]
    [InlineData("lbp", "five")]
    [InlineData("lbp", "1.5")]
    [InlineData("lbp", "99999999999999999999")]
    public void ASettingsValueAScriptCantWrite_IsRefused(string key, string value)
    {
        AssertRefused(
            "generator \"g\" has a value for power that a Script can't write",
            [new ImportedBlock("g", GeneratorId, texts: Texts((key, value)))]);
    }

    [Fact]
    public void AnOptionOutOfRange_IsRefused()
    {
        AssertRefused(
            "logic \"l\" has a value for op that a Script can't write",
            [new ImportedBlock("l", LogicId, ints: Ints(("xmo", 6)))]);
    }

    [Fact]
    public void ASettingTheManifestDoesNotKnow_IsRefused()
    {
        AssertRefused(
            "logic \"l\" has a Setting a Script can't write, \"oi2\"",
            [new ImportedBlock("l", LogicId, texts: Texts(("oi2", "3")))]);
    }

    [Fact]
    public void AFloatTheManifestDoesNotKnow_IsRefused()
    {
        AssertRefused(
            "logic \"l\" has a Setting a Script can't write, \"x1\"",
            [new ImportedBlock("l", LogicId, floats: Floats(("x1", 0.5f)))]);
    }

    [Theory]
    [InlineData("generator_1 = generator()\npower_display_1 = power_display(generator_1)", null)]
    [InlineData("generator_1 = generator()", "its text compiles to 1 blocks, not 2")]
    [InlineData("generator_1 = generator()\ngenerator_2 = generator()", "its text doesn't give power_display \"d\" its own type and style")]
    [InlineData("generator_1 = generator()\npower_display_1 = power_display(generator_1, style: compact)", "its text doesn't give power_display \"d\" its own type and style")]
    [InlineData("generator_1 = generator(power: 2)\npower_display_1 = power_display(generator_1)", "generator \"g\" has a value for power that a Script can't write")]
    [InlineData("generator_1 = generator()\npower_display_1 = power_display()", "its text misses a Wire into power_display \"d\"")]
    [InlineData("generator_1 = generator()\npower_display_1 = power_display(power_display_1)", "its text doesn't compile: 'power_display_1' can't refer to itself outside delay(...)")]
    public void TheCompare_CatchesTextThatCompilesToSomethingElse(string text, string difference)
    {
        ImportedBlock[] blocks = [new ImportedBlock("g", GeneratorId), new ImportedBlock("d", DisplayId)];
        var graph = new ImportGraph(blocks);
        Assert.Null(graph.AddWire(new ImportedWire("g", 0, "d", 0)));

        Assert.Equal(difference, ImportCompare.FindDifference(graph, blocks, ScriptCompiler.Compile(text, 0)));
    }

    [Fact]
    public void TheCompare_CatchesAWireTheBlocksDoNotHave()
    {
        ImportedBlock[] blocks = [new ImportedBlock("g", GeneratorId), new ImportedBlock("d", DisplayId)];
        var compiled = ScriptCompiler.Compile("generator_1 = generator()\npower_display_1 = power_display(generator_1)", 0);

        Assert.Equal(
            "its text gives power_display \"d\" a Wire it doesn't have",
            ImportCompare.FindDifference(new ImportGraph(blocks), blocks, compiled));
    }

    private static void AssertRefused(string refusal, IReadOnlyList<ImportedBlock> blocks, params ImportedWire[] wires)
    {
        var import = BlockImport.Import(blocks, wires);

        Assert.False(import.Succeeded);
        Assert.Null(import.Text);
        Assert.Equal(refusal, import.Refusal);
    }

    /// <summary>
    /// The compiled blocks as the level would hold them.
    /// </summary>
    private static (List<ImportedBlock> Blocks, List<ImportedWire> Wires) Read(CompileResult compiled)
    {
        var blocks = compiled.Blocks.Select((block, i) => new ImportedBlock(
            Uid(i),
            block.Style.BlockId,
            ints: block.Settings.Where(s => s.Kind == SettingKind.Int).ToDictionary(s => s.Key, s => s.IntValue),
            texts: block.Settings.Where(s => s.Kind == SettingKind.Text).ToDictionary(s => s.Key, s => s.TextValue)));
        var wires = compiled.Wires.Select(w => new ImportedWire(Uid(w.From.Block), w.FromConnector, Uid(w.To.Block), w.ToConnector));
        return (blocks.ToList(), wires.ToList());
    }

    /// <summary>
    /// Each block with everything wired into it, sorted, so it doesn't depend on the order of the lines. A Setting at
    /// its default counts as left out.
    /// </summary>
    private static List<string> Describe(CompileResult compiled)
    {
        return Enumerable.Range(0, compiled.Blocks.Count).Select(i => Describe(compiled, i)).OrderBy(d => d, System.StringComparer.Ordinal).ToList();
    }

    private static string Describe(CompileResult compiled, int index)
    {
        var block = compiled.Blocks[index];
        var settings = block.Settings
            .Select(s => (s.Key, Value: s.Kind == SettingKind.Int ? s.IntValue.ToString(CultureInfo.InvariantCulture) : s.TextValue))
            .Where(s => s.Value != "0")
            .Select(s => $"{s.Key}={s.Value}")
            .OrderBy(s => s, System.StringComparer.Ordinal);
        var inputs = compiled.Wires
            .Where(w => w.To.Block == index)
            .OrderBy(w => w.ToConnector)
            .Select(w => $"{w.ToConnector}<-{w.FromConnector}.{Describe(compiled, w.From.Block)}");
        return $"{block.Style.BlockId}[{string.Join(" ", settings)}]({string.Join(" ", inputs)})";
    }

    private static string Uid(int index)
    {
        return $"uid{index}";
    }

    private static Dictionary<string, int> Ints(params (string Key, int Value)[] values)
    {
        return values.ToDictionary(v => v.Key, v => v.Value);
    }

    private static Dictionary<string, string> Texts(params (string Key, string Value)[] values)
    {
        return values.ToDictionary(v => v.Key, v => v.Value);
    }

    private static Dictionary<string, float> Floats(params (string Key, float Value)[] values)
    {
        return values.ToDictionary(v => v.Key, v => v.Value);
    }
}
