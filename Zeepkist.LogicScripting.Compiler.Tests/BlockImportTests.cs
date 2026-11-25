using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockImportTests
{
    private const int GeneratorId = 2414;
    private const int DisplayId = 2415;
    private const int CompactDisplayId = 2513;
    private const int LogicId = 2420;
    private const int DigitizerId = 2418;
    private const int DelayId = 2428;
    private const int RandomId = 2425;
    private const int CounterId = 2434;
    private const int TelemetryId = 2440;
    private const int TogglerId = 2417;
    private const int DeathId = 2427;
    private const int LampId = 2448;
    private const int MultiplierId = 2449;

    public static IEnumerable<object[]> CreatableTypes()
    {
        return BlockManifest.All
            .Where(t => !t.IsPlacedOnly && t.Settings.All(s => s.Type != SettingType.Targets))
            .Select(t => new object[] { t.Function });
    }

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

    [Theory]
    [MemberData(nameof(CreatableTypes))]
    public void EachCreatableType_ImportsWithEachStyleModeSettingAndWire(string function)
    {
        Assert.True(BlockManifest.TryGetFunction(function, out var type));
        foreach (var style in type.Styles)
        {
            foreach (var settings in GetSettingCases(type))
                AssertImports(type, style, settings, type.Inputs);

            // Each input alone leaves a gap before it, and none leaves them all out
            AssertImports(type, style, GetLastValues(type), []);
            foreach (var input in type.Inputs)
                AssertImports(type, style, GetLastValues(type), [input]);
        }
    }

    [Fact]
    public void TheManifestWalk_CoversEveryBlockFunctionButTheToggler()
    {
        Assert.Equal(
            BlockManifest.All.Where(t => !t.IsPlacedOnly && t != BlockManifest.Toggler).Select(t => new object[] { t.Function }),
            CreatableTypes());
    }

    [Theory]
    [InlineData(0, "simple_telemetry_1 = simple_telemetry()\npower_display_1 = power_display(simple_telemetry_1.state)")]
    [InlineData(1, "simple_telemetry_1 = simple_telemetry(mode: local_velocity)\npower_display_1 = power_display(simple_telemetry_1.z)")]
    [InlineData(2, "simple_telemetry_1 = simple_telemetry(mode: global_velocity)\npower_display_1 = power_display(simple_telemetry_1.heading)")]
    public void AnOutputOfSeveral_IsNamedAsTheModeNamesIt(int mode, string text)
    {
        var import = BlockImport.Import(
            [new ImportedBlock("t", TelemetryId, ints: Ints(("xbs", mode))), new ImportedBlock("d", DisplayId)],
            [new ImportedWire("t", 3, "d", 0)]);

        Assert.Equal(text, import.Text);
    }

    [Fact]
    public void ABlockWithoutOutputs_HasNoName()
    {
        var import = BlockImport.Import(
            [new ImportedBlock("g", GeneratorId), new ImportedBlock("d1", DeathId, ints: Ints(("xpr", 2))), new ImportedBlock("d2", DeathId)],
            [new ImportedWire("g", 0, "d1", 0)]);

        Assert.Equal("generator_1 = generator()\ndeath(generator_1, player: 2)\ndeath()", import.Text);
    }

    [Fact]
    public void BlocksThatAreAllHidden_ShareADefault()
    {
        var import = BlockImport.Import(
            [new ImportedBlock("g", GeneratorId, ints: Ints(("lbhd", 1))), new ImportedBlock("d", DisplayId, ints: Ints(("lbhd", 1)))],
            [new ImportedWire("g", 0, "d", 0)]);

        Assert.Equal("default hide: true\ngenerator_1 = generator()\npower_display_1 = power_display(generator_1)", import.Text);
    }

    [Fact]
    public void OneHiddenBlockOnItsOwn_IsADefaultToo()
    {
        var import = BlockImport.Import([new ImportedBlock("d", DisplayId, ints: Ints(("lbhd", 1)))], []);

        Assert.Equal("default hide: true\npower_display_1 = power_display()", import.Text);
    }

    [Fact]
    public void APlacedOnlyBlock_IsRefused()
    {
        AssertRefused("a Script can't create lamp \"l\"", [new ImportedBlock("g", GeneratorId), new ImportedBlock("l", LampId)]);
    }

    [Fact]
    public void ABlockThatIsNoLogicBlock_IsRefused()
    {
        AssertRefused("a Script can't create block \"b\"", [new ImportedBlock("b", 1)]);
    }

    [Fact]
    public void AToggler_IsRefused()
    {
        AssertRefused(
            "toggler \"t\" has targets, which Import can't write",
            [new ImportedBlock("t", TogglerId, texts: Texts(("xuid1", "g")))]);
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
            "power_display \"d\" can't be imported: 'power_display style compact' doesn't match this game version and is turned off",
            import.Refusal);
    }

    [Fact]
    public void AnotherStyleTheStartupCheckTurnedOff_IsNoReason()
    {
        var import = BlockImport.Import([new ImportedBlock("d", DisplayId)], [], null, [CompactDisplayId]);

        Assert.True(import.Succeeded);
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
    public void WhatAFreshBlockStores_IsNoReason()
    {
        Assert.All(BlockManifest.All, t => Assert.DoesNotContain(t.FreshSettings, f => t.Settings.Any(s => s.Key == f.Key)));
        var import = BlockImport.Import([new ImportedBlock("p", MultiplierId, ints: Ints(("x47", 1), ("x48", 1)))], []);

        Assert.Equal("property_multiplier()", import.Text);
    }

    [Theory]
    [InlineData(1, 0, "x48")]
    [InlineData(0, 1, "x47")]
    [InlineData(2, 1, "x47")]
    public void AValueOtherThanWhatAFreshBlockStores_IsRefused(int x47, int x48, string key)
    {
        AssertRefused(
            $"property_multiplier \"p\" has a Setting a Script can't write, \"{key}\"",
            [new ImportedBlock("p", MultiplierId, ints: Ints(("x47", x47), ("x48", x48)))]);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("2147483648")]
    [InlineData(null)]
    public void ADelayWithTicksAScriptCantWrite_IsRefused(string ticks)
    {
        AssertRefused(
            "delay \"d\" has a value for ticks that a Script can't write",
            [new ImportedBlock("d", DelayId, texts: ticks == null ? null : Texts(("ldl", ticks)))]);
    }

    [Theory]
    [InlineData("ld1", "-5", "max_reset")]
    [InlineData("ld2", "5", "min_reset")]
    public void ANumberTheBlockWouldNotKeep_IsRefused(string key, string value, string setting)
    {
        AssertRefused(
            $"counter \"c\" has a value for {setting} that a Script can't write",
            [new ImportedBlock("c", CounterId, texts: Texts((key, value)))]);
    }

    [Fact]
    public void AWordStoredForANumberOutOfRange_IsRefused()
    {
        AssertRefused(
            "digitizer \"d\" has a value for invert that a Script can't write",
            [new ImportedBlock("d", DigitizerId, ints: Ints(("xt1", 2)))]);
        AssertRefused(
            "death \"d\" has a value for player that a Script can't write",
            [new ImportedBlock("d", DeathId, ints: Ints(("xpr", 5)))]);
        AssertRefused(
            "power_display \"d\" has a value for hide that a Script can't write",
            [new ImportedBlock("d", DisplayId, ints: Ints(("lbhd", 2)))]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void AModeOutOfRange_IsRefused(int mode)
    {
        AssertRefused(
            "simple_telemetry \"t\" has a value for mode that a Script can't write",
            [new ImportedBlock("t", TelemetryId, ints: Ints(("xbs", mode))), new ImportedBlock("d", DisplayId)],
            new ImportedWire("t", 0, "d", 0));
    }

    [Fact]
    public void ARangeWithItsMinimumAboveItsMaximum_IsRefused_UnlessAnInputReplacesTheMaximum()
    {
        ImportedBlock[] blocks = [new ImportedBlock("r", RandomId, texts: Texts(("xv1", "5"))), new ImportedBlock("g", GeneratorId)];

        AssertRefused("random \"r\" has a min above its max, which a Script can't write", blocks);
        Assert.Equal(
            "generator_1 = generator()\nrandom_1 = random(max_in: generator_1, min: 5)",
            BlockImport.Import(blocks, [new ImportedWire("g", 0, "r", 1)]).Text);
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

    /// <summary>
    /// Imports the block with a Generator of its own wired into each of the inputs and a Power Display wired to each
    /// output of its mode, and compiles the text to the same blocks.
    /// </summary>
    private static void AssertImports(
        BlockType type,
        BlockStyle style,
        IReadOnlyList<Setting> settings,
        IReadOnlyList<BlockConnector> inputs)
    {
        List<ImportedBlock> blocks =
        [
            new ImportedBlock(
                "subject",
                style.BlockId,
                ints: settings.Where(s => s.Kind == SettingKind.Int).ToDictionary(s => s.Key, s => s.IntValue),
                texts: settings.Where(s => s.Kind == SettingKind.Text).ToDictionary(s => s.Key, s => s.TextValue))
        ];
        List<ImportedWire> wires = [];
        for (var i = 0; i < inputs.Count; i++)
        {
            blocks.Add(new ImportedBlock($"in{i}", GeneratorId, texts: Texts(("lbp", $"{i + 10}"))));
            wires.Add(new ImportedWire($"in{i}", 0, "subject", inputs[i].Index));
        }

        foreach (var output in type.GetOutputs(settings))
        {
            blocks.Add(new ImportedBlock($"out{output.Index}", DisplayId));
            wires.Add(new ImportedWire("subject", output.Index, $"out{output.Index}", 0));
        }

        var import = BlockImport.Import(blocks, wires);

        Assert.Null(import.Refusal);
        var compiled = ScriptCompiler.Compile(import.Text, 0);
        Assert.True(compiled.Succeeded);
        var (compiledBlocks, compiledWires) = Read(compiled);
        Assert.Equal(Describe(blocks, wires), Describe(compiledBlocks, compiledWires));
    }

    /// <summary>
    /// Every value of each Setting and <c>hide:</c>, one at a time and then the last of each together.
    /// </summary>
    private static IEnumerable<IReadOnlyList<Setting>> GetSettingCases(BlockType type)
    {
        List<Setting> required = [.. type.Settings.Where(s => s.IsRequired).Select(s => GetValues(s)[0])];
        yield return required;
        foreach (var setting in type.Settings.Append(BlockSetting.Hide))
        {
            foreach (var value in GetValues(setting))
                yield return [.. required.Where(r => r.Key != setting.Key), value];
        }

        yield return GetLastValues(type);
    }

    private static List<Setting> GetLastValues(BlockType type)
    {
        return [.. type.Settings.Append(BlockSetting.Hide).Select(s => GetValues(s).Last())];
    }

    /// <summary>
    /// As a block stores them: every option, and a few numbers the Setting accepts, among them both its limits.
    /// </summary>
    private static List<Setting> GetValues(BlockSetting setting)
    {
        if (setting.Type != SettingType.Number)
            return [.. Enumerable.Range(0, setting.Values.Count).Select(i => Setting.Int(setting.Key, i))];

        return
        [
            .. new[] { setting.Minimum, -7, 0, 7, setting.Maximum }
                .Where(n => n >= setting.Minimum && n <= setting.Maximum)
                .Distinct()
                .Select(n => Setting.Text(setting.Key, n.ToString(CultureInfo.InvariantCulture)))
        ];
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
        var (blocks, wires) = Read(compiled);
        return Describe(blocks, wires);
    }

    private static List<string> Describe(List<ImportedBlock> blocks, List<ImportedWire> wires)
    {
        return blocks.Select(b => Describe(b, blocks, wires)).OrderBy(d => d, System.StringComparer.Ordinal).ToList();
    }

    private static string Describe(ImportedBlock block, List<ImportedBlock> blocks, List<ImportedWire> wires)
    {
        var settings = block.Settings
            .Select(s => (s.Key, Value: s.Kind == SettingKind.Int ? s.IntValue.ToString(CultureInfo.InvariantCulture) : s.TextValue))
            .Where(s => s.Value != "0")
            .Select(s => $"{s.Key}={s.Value}")
            .OrderBy(s => s, System.StringComparer.Ordinal);
        var inputs = wires
            .Where(w => w.ToUid == block.Uid)
            .OrderBy(w => w.ToConnector)
            .Select(w => $"{w.ToConnector}<-{w.FromConnector}.{Describe(blocks.Single(b => b.Uid == w.FromUid), blocks, wires)}");
        return $"{block.BlockId}[{string.Join(" ", settings)}]({string.Join(" ", inputs)})";
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
