using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class BlockImportTests
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
        var (blocks, wires) = CompiledSignatures.Read(compiled);

        foreach (var selection in new[] { blocks, Enumerable.Reverse(blocks).ToList() })
        {
            var import = BlockImport.Import(selection, wires);

            Assert.Null(import.Refusal);
            var again = ScriptCompiler.Compile(import.Text, 0);
            Assert.True(again.Succeeded);
            Assert.Equal(CompiledSignatures.Describe(compiled), CompiledSignatures.Describe(again));
            Assert.Equal(compiled.Wires.Count, again.Wires.Count);
        }
    }

    [Fact]
    public void ABlockWithAnInputAnOperatorDoesNotWire_IsACall()
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
            power_display(logic(5, op_in: 0, op: or), hide: true, style: compact)
            """.Replace("\r\n", "\n"),
            import.Text);
    }

    [Fact]
    public void AWireReadFromBothEnds_IsOne()
    {
        var import = BlockImport.Import(
            [new ImportedBlock("g", GeneratorId), new ImportedBlock("d", DisplayId)],
            [new ImportedWire("g", 0, "d", 0), new ImportedWire("g", 0, "d", 0)]);

        Assert.Equal("power_display(0)", import.Text);
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

        Assert.Equal("power_display()", import.Text);
    }

    [Fact]
    public void AnUnknownSettingAtItsDefault_IsNoReason()
    {
        var import = BlockImport.Import(
            [new ImportedBlock("d", DisplayId, ints: Ints(("new", 0)), texts: Texts(("oi2", "0"), ("oi3", "")), floats: Floats(("x1", 0)))],
            []);

        Assert.Equal("power_display()", import.Text);
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
    [InlineData(0, "state_1 = simple_telemetry().state\npower_display(state_1)")]
    [InlineData(1, "z_1 = simple_telemetry(mode: local_velocity).z\npower_display(z_1)")]
    [InlineData(2, "heading_1 = simple_telemetry(mode: global_velocity).heading\npower_display(heading_1)")]
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

        Assert.Equal("death(0, player: 2)\n\ndeath()", import.Text);
    }

    [Fact]
    public void BlocksThatAreAllHidden_ShareADefault()
    {
        var import = BlockImport.Import(
            [new ImportedBlock("g", GeneratorId, ints: Ints(("lbhd", 1))), new ImportedBlock("d", DisplayId, ints: Ints(("lbhd", 1)))],
            [new ImportedWire("g", 0, "d", 0)]);

        Assert.Equal("default hide: true\n\npower_display(0)", import.Text);
    }

    [Fact]
    public void OneHiddenBlockOnItsOwn_IsADefaultToo()
    {
        var import = BlockImport.Import([new ImportedBlock("d", DisplayId, ints: Ints(("lbhd", 1)))], []);

        Assert.Equal("default hide: true\n\npower_display()", import.Text);
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
        var (compiledBlocks, compiledWires) = CompiledSignatures.Read(compiled);
        Assert.Equal(CompiledSignatures.Describe(blocks, wires), CompiledSignatures.Describe(compiledBlocks, compiledWires));
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

    private static ImportedBlock Delay(string uid, int ticks = 5)
    {
        return new ImportedBlock(uid, DelayId, texts: Texts(("ldl", $"{ticks}")));
    }

    /// <summary>
    /// The import itself checks that the text compiles to the same Wires.
    /// </summary>
    private static void AssertText(string text, IReadOnlyList<ImportedBlock> blocks, params ImportedWire[] wires)
    {
        var import = BlockImport.Import(blocks, wires);

        Assert.Null(import.Refusal);
        Assert.Equal(text.Replace("\r\n", "\n"), import.Text);
        Assert.Equal(wires.Length, ScriptCompiler.Compile(import.Text, 0).Wires.Count);
    }

    private static void AssertRefused(string refusal, IReadOnlyList<ImportedBlock> blocks, params ImportedWire[] wires)
    {
        var import = BlockImport.Import(blocks, wires);

        Assert.False(import.Succeeded);
        Assert.Null(import.Text);
        Assert.Equal(refusal, import.Refusal);
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
