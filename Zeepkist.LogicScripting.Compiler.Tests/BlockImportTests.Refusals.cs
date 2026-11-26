using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class BlockImportTests
{
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
    [InlineData("0", "has no ticks, which a Script has to give")]
    [InlineData("-1", "has a value for ticks that a Script can't write")]
    [InlineData("2147483648", "has a value for ticks that a Script can't write")]
    [InlineData(null, "has no ticks, which a Script has to give")]
    public void ADelayWithTicksAScriptCantWrite_IsRefusedOnItsOwn(string ticks, string reason)
    {
        AssertRefused(
            $"delay \"d\" {reason}",
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
    public void ARangeWithItsMinimumAboveItsMaximum_IsRefusedOnItsOwn_UnlessAnInputReplacesTheMaximum()
    {
        ImportedBlock[] blocks = [new ImportedBlock("r", RandomId, texts: Texts(("xv1", "5"))), new ImportedBlock("g", GeneratorId)];

        AssertRefused("random \"r\" has a min above its max, which a Script can't write", [blocks[0]]);
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
}
