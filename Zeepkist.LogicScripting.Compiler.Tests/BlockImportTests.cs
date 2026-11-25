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

    [Fact]
    public void OnlyBlocksAScriptCantCreate_AreRefused()
    {
        AssertRefused(
            "a Script can't create any of the selected blocks",
            [new ImportedBlock("l", LampId), new ImportedBlock("b", 1)]);
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
    public void ADelayThatFeedsItself_IsWrittenWithItsOwnName()
    {
        AssertText(
            "delay_1 = delay(delay_1, ticks: 5)",
            [Delay("d")],
            new ImportedWire("d", 1, "d", 0));
    }

    [Fact]
    public void TwoDelaysFeedingEachOther_OnlyTheFirstHasAName()
    {
        AssertText(
            """
            delay_1 = delay(delay(delay_1, ticks: 6), ticks: 5)
            """,
            [Delay("d", 5), Delay("e", 6)],
            new ImportedWire("d", 1, "e", 0),
            new ImportedWire("e", 1, "d", 0));
    }

    [Fact]
    public void ALatchThroughSeveralBlocks_RefersAheadOnlyInItsDelay()
    {
        AssertText(
            """
            delay_1 = delay(logic_2, ticks: 5)
            logic_1 = 0 | delay_1
            logic_2 = logic_1 & logic_1
            power_display(logic_2)
            """,
            [
                new ImportedBlock("p", DisplayId),
                new ImportedBlock("m", LogicId),
                new ImportedBlock("l", LogicId, ints: Ints(("xmo", 1))),
                Delay("d"),
                new ImportedBlock("g", GeneratorId)
            ],
            new ImportedWire("g", 0, "l", 0),
            new ImportedWire("d", 1, "l", 1),
            new ImportedWire("l", 3, "m", 0),
            new ImportedWire("l", 3, "m", 1),
            new ImportedWire("m", 3, "d", 0),
            new ImportedWire("m", 3, "p", 0));
    }

    [Fact]
    public void OneDelayFeedingSeveralLoops_RefersAheadOnce()
    {
        AssertText(
            """
            delay_1 = delay(logic(delay_1) & logic(delay_1, op: or), ticks: 5)
            """,
            [
                new ImportedBlock("a", LogicId),
                new ImportedBlock("b", LogicId, ints: Ints(("xmo", 1))),
                new ImportedBlock("c", LogicId),
                Delay("d")
            ],
            new ImportedWire("d", 1, "a", 0),
            new ImportedWire("d", 1, "b", 0),
            new ImportedWire("a", 3, "c", 0),
            new ImportedWire("b", 3, "c", 1),
            new ImportedWire("c", 3, "d", 0));
    }

    [Fact]
    public void ADelayFedByALoop_ComesAfterIt()
    {
        AssertText(
            """
            delay_1 = delay(logic_1, ticks: 5)
            logic_1 = logic(delay_1)
            delay(delay(logic_1, ticks: 6), ticks: 7)
            """,
            [Delay("f", 7), Delay("e", 6), Delay("d"), new ImportedBlock("l", LogicId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("l", 3, "e", 0),
            new ImportedWire("e", 1, "f", 0));
    }

    [Fact]
    public void ADelayThatAnotherOneTakesOutOfItsLoops_NeedsNoName()
    {
        AssertText(
            """
            delay_1 = delay(delay(logic(delay_1), ticks: 6), ticks: 5)
            """,
            [Delay("d"), Delay("e", 6), new ImportedBlock("l", LogicId)],
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("l", 3, "e", 0),
            new ImportedWire("e", 1, "d", 0));
    }

    [Fact]
    public void AnOutputOfSeveral_CanBeReferredAheadTo()
    {
        Assert.True(BlockManifest.TryGetFunction("demultiplexer", out var type));
        var output = type.Outputs[1];
        AssertText(
            $"""
            delay_1 = delay({output.Name}_1, ticks: 5)
            {output.Name}_1 = demultiplexer(delay_1).{output.Name}
            """,
            [new ImportedBlock("m", type.DefaultStyle.BlockId), Delay("d")],
            new ImportedWire("d", 1, "m", type.Inputs[0].Index),
            new ImportedWire("m", output.Index, "d", 0));
    }

    [Fact]
    public void LoopsWithWiresInAnyOrder_GiveTheSameText()
    {
        ImportedBlock[] blocks = [Delay("d"), Delay("e", 6), new ImportedBlock("l", LogicId), new ImportedBlock("m", LogicId)];
        ImportedWire[] wires =
        [
            new("d", 1, "l", 0), new("e", 1, "l", 1), new("l", 3, "m", 0), new("m", 3, "d", 0), new("l", 3, "e", 0)
        ];

        var import = BlockImport.Import(blocks, wires);

        Assert.Null(import.Refusal);
        for (var i = 1; i < wires.Length; i++)
            Assert.Equal(import.Text, BlockImport.Import(blocks, [.. wires.Skip(i), .. wires.Take(i)]).Text);

        Assert.Equal(import.Text, BlockImport.Import(blocks, [.. Enumerable.Reverse(wires)]).Text);
    }

    [Fact]
    public void ADependencyLoop_IsRefused_NamingItsBlocksInTheDirectionOfTheWires()
    {
        AssertRefused(
            "a loop of Wires has no Delay in it: logic \"l\", power_display \"d\", power_display \"e\"",
            [new ImportedBlock("e", DisplayId), new ImportedBlock("l", LogicId), new ImportedBlock("d", DisplayId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "e", 0),
            new ImportedWire("e", 1, "l", 0));
    }

    [Fact]
    public void ABlockThatFeedsItself_IsADependencyLoop()
    {
        AssertRefused(
            "a loop of Wires has no Delay in it: logic \"l\"",
            [new ImportedBlock("l", LogicId)],
            new ImportedWire("l", 3, "l", 0));
    }

    [Fact]
    public void ADependencyLoopsRefusal_LeavesOutWhatIsOnlyFedByIt()
    {
        AssertRefused(
            "a loop of Wires has no Delay in it: power_display \"p\", logic \"l\"",
            [Delay("d"), new ImportedBlock("q", DisplayId), new ImportedBlock("l", LogicId), new ImportedBlock("p", DisplayId)],
            new ImportedWire("l", 3, "p", 0),
            new ImportedWire("p", 1, "l", 0),
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "q", 0));
    }

    [Fact]
    public void ADependencyLoopNextToALoopThroughADelay_IsRefusedWithoutTheDelay()
    {
        AssertRefused(
            "a loop of Wires has no Delay in it: logic \"m\", logic \"l\"",
            [Delay("d"), new ImportedBlock("l", LogicId), new ImportedBlock("m", LogicId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("l", 3, "m", 0),
            new ImportedWire("m", 3, "l", 1));
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
            "random(max_in: 0, min: 5)",
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
    [InlineData("a = generator()\nb = power_display(a)", null)]
    [InlineData("a = generator()", "its text compiles to 1 blocks, not 2")]
    [InlineData("a = generator()\nb = generator()", "its text doesn't give power_display \"d\" its own type and style")]
    [InlineData("a = generator()\nb = power_display(a, style: compact)", "its text doesn't give power_display \"d\" its own type and style")]
    [InlineData("a = generator(power: 2)\nb = power_display(a)", "generator \"g\" has a value for power that a Script can't write")]
    [InlineData("a = generator()\nb = power_display()", "its text misses a Wire into power_display \"d\"")]
    [InlineData("a = generator()\nb = power_display(b)", "its text doesn't compile: 'b' can't refer to itself outside delay(...)")]
    [InlineData("a = generator()\nelsewhere = power_display(a)", "its text compiles to a power_display that isn't one of the blocks")]
    public void TheCompare_CatchesTextThatCompilesToSomethingElse(string text, string difference)
    {
        ImportedBlock[] blocks = [new ImportedBlock("g", GeneratorId), new ImportedBlock("d", DisplayId)];
        var graph = new ImportGraph(blocks);
        Assert.Null(graph.AddWire(new ImportedWire("g", 0, "d", 0)));

        Assert.Equal(difference, Compare(graph, blocks, text, [(1, "g"), (2, "d")], []));
    }

    [Fact]
    public void TheCompare_CatchesAWireTheBlocksDoNotHave()
    {
        ImportedBlock[] blocks = [new ImportedBlock("g", GeneratorId), new ImportedBlock("d", DisplayId)];

        Assert.Equal(
            "its text gives power_display \"d\" a Wire it doesn't have",
            Compare(new ImportGraph(blocks), blocks, "a = generator()\nb = power_display(a)", [(1, "g"), (2, "d")], []));
    }

    [Fact]
    public void TheCompare_CatchesTwoBlocksForOne()
    {
        ImportedBlock[] blocks = [new ImportedBlock("g", GeneratorId), new ImportedBlock("h", GeneratorId)];

        Assert.Equal(
            "its text compiles to several blocks for one",
            Compare(new ImportGraph(blocks), blocks, "a = generator()\nb = generator()", [(1, "g"), (2, "g")], []));
    }

    [Fact]
    public void TheCompare_CatchesTwinsWithEachOthersWires()
    {
        // Each twin alone looks the same, so only the Wires tell them apart
        ImportedBlock[] blocks = [Delay("d"), Delay("e"), new ImportedBlock("p", DisplayId)];
        var graph = new ImportGraph(blocks);
        Assert.Null(graph.AddWire(new ImportedWire("d", 1, "e", 0)));
        Assert.Null(graph.AddWire(new ImportedWire("e", 1, "d", 0)));
        Assert.Null(graph.AddWire(new ImportedWire("d", 1, "p", 0)));
        (int, string)[] places = [(1, "d"), (2, "e"), (3, "p")];

        Assert.Null(Compare(graph, blocks, "a = delay(b, ticks: 5)\nb = delay(a, ticks: 5)\nc = power_display(a)", places, []));
        Assert.Equal(
            "its text gives delay \"d\" a Wire it doesn't have",
            Compare(graph, blocks, "a = delay(a, ticks: 5)\nb = delay(b, ticks: 5)\nc = power_display(a)", places, []));
        Assert.Equal(
            "its text gives power_display \"p\" a Wire it doesn't have",
            Compare(graph, blocks, "a = delay(b, ticks: 5)\nb = delay(a, ticks: 5)\nc = power_display(b)", places, []));
    }

    [Theory]
    [InlineData("5", "5", "a = generator(power: 5)\nb = logic(a, a)", null)]
    [InlineData("5", "6", "a = generator(power: 5)\nb = logic(a, a)", "generator \"h\" has a value for power that a Script can't write")]
    [InlineData("5", "5", "a = generator(power: 5)\nb = logic(a)", "its text misses a Wire into logic \"l\"")]
    [InlineData("5", "5", "a = generator(power: 5)\nb = logic(a, a, a)", "its text gives logic \"l\" a Wire it doesn't have")]
    public void TheCompare_AllowsSharedGeneratorsOfTheSamePower(string power, string otherPower, string text, string difference)
    {
        ImportedBlock[] blocks =
        [
            new ImportedBlock("g", GeneratorId, texts: Texts(("lbp", power))),
            new ImportedBlock("h", GeneratorId, texts: Texts(("lbp", otherPower))),
            new ImportedBlock("l", LogicId)
        ];
        var graph = new ImportGraph(blocks);
        Assert.Null(graph.AddWire(new ImportedWire("g", 0, "l", 0)));
        Assert.Null(graph.AddWire(new ImportedWire("h", 0, "l", 1)));

        Assert.Equal(difference, Compare(graph, blocks, text, [(1, "g"), (2, "l")], new Dictionary<string, string> { ["h"] = "g" }));
    }

    [Fact]
    public void TheCompare_AllowsNoOtherBlockToBeShared()
    {
        ImportedBlock[] blocks = [new ImportedBlock("d", DisplayId), new ImportedBlock("e", DisplayId)];

        Assert.Equal(
            "its text shares power_display \"e\", and only Generators can be shared",
            Compare(new ImportGraph(blocks), blocks, "a = power_display()", [(1, "d")], new Dictionary<string, string> { ["e"] = "d" }));
    }

    /// <summary>
    /// Compares the text as if an import wrote it, each place being where a line's one-letter name is given a block.
    /// </summary>
    private static string Compare(
        ImportGraph graph,
        IReadOnlyList<ImportedBlock> blocks,
        string text,
        IReadOnlyList<(int Line, string Uid)> places,
        Dictionary<string, string> shared)
    {
        var imported = new ImportedText(
            [.. text.Split('\n').Select(l => new ImportLine(null, l))],
            places.ToDictionary(p => (new Position(p.Line, 5), graph.GetType(p.Uid)), p => p.Uid),
            shared);
        return ImportCompare.FindDifference(graph, blocks, imported, ScriptCompiler.Compile(text, 0));
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
