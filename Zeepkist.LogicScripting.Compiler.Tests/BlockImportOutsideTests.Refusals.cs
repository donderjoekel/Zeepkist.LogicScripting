using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class BlockImportOutsideTests
{
    [Fact]
    public void AWireToAnotherScriptsGroupBlock_IsRefused()
    {
        var level = new ImportLevel(
            [Generator(), new ImportedBlock("p", PassthroughId, isInBlockGroup: true)],
            new ImportedWire("g", 0, "p", 0));

        AssertRefused(
            level,
            "generator \"g\" has a Wire to passthrough \"p\", but that one is in a Block Group, which a recompile replaces",
            "g");
    }

    [Fact]
    public void AWireFromAnotherScriptsGroupBlock_IsRefused()
    {
        var level = new ImportLevel(
            [Logic(), new ImportedBlock("g", GeneratorId, isInBlockGroup: true)],
            new ImportedWire("g", 0, "l", 0));

        AssertRefused(
            level,
            "logic \"l\" has a Wire from generator \"g\", but that one is in a Block Group, which a recompile replaces",
            "l");
    }

    [Fact]
    public void ATargetInAnotherScriptsGroup_IsRefused()
    {
        var level = new ImportLevel([Toggler("tog", "g"), new ImportedBlock("g", GeneratorId, isInBlockGroup: true)]);

        AssertRefused(
            level,
            "toggler \"tog\" targets generator \"g\", but that one is in a Block Group, which a recompile replaces",
            "tog");
    }

    [Theory]
    [InlineData("g")]
    [InlineData("g", "tog")]
    public void ASelectedBlockAnyTogglerTargets_IsRefused(params string[] selected)
    {
        var level = new ImportLevel([Generator(), Toggler("tog", "g")]);

        AssertRefused(level, "a Toggler targets generator \"g\", and would lose it at the next Compile", selected);
    }

    [Fact]
    public void ASelectedTogglersOwnTarget_IsRefusedWithoutTheLevelsList()
    {
        var import = BlockImport.Import([Generator(), Toggler("tog", "g")], []);

        Assert.Equal("a Toggler targets generator \"g\", and would lose it at the next Compile", import.Refusal);
    }

    [Theory]
    [InlineData("has no targets, which a Script has to give")]
    [InlineData("has a value for targets that a Script can't write", "", "lamp")]
    [InlineData("has a value for targets that a Script can't write", "lamp", "lamp")]
    public void ATogglerWithTargetsAScriptCantWrite_IsRefusedOnItsOwn(string reason, params string[] targets)
    {
        AssertRefused(new ImportLevel([Toggler("tog", targets), Lamp()]), $"toggler \"tog\" {reason}", "tog");
    }

    [Fact]
    public void ATargetThatIsGone_IsRefused()
    {
        AssertRefused(new ImportLevel([Toggler("tog", "gone")]), "toggler \"tog\" targets block \"gone\", which isn't in the level", "tog");
    }

    [Fact]
    public void AWireToABlockThatIsGone_IsRefused()
    {
        AssertRefused(
            new ImportLevel([Generator()], new ImportedWire("g", 0, "gone", 0)),
            "generator \"g\" has a Wire to block \"gone\", which isn't in the level",
            "g");
    }

    [Fact]
    public void AWireToABlockTheManifestDoesNotKnow_IsRefused()
    {
        AssertRefused(
            new ImportLevel([Generator(), new ImportedBlock("door", DoorId)], new ImportedWire("g", 0, "door", 0)),
            "generator \"g\" has a Wire to block \"door\", which a Script can't wire",
            "g");
    }

    [Fact]
    public void AWireToAnOutsideBlocksOutput_IsRefused()
    {
        AssertRefused(
            new ImportLevel([Generator(), Trigger()], new ImportedWire("g", 0, "t", 0)),
            "the Wire from generator \"g\" to trigger \"t\" doesn't go from an output to an input",
            "g");
    }

    [Fact]
    public void AnOutsideBlocksModeOutOfRange_IsRefused()
    {
        var telemetry = new ImportedBlock("s", TelemetryId, ints: new Dictionary<string, int> { ["xbs"] = 3 });

        AssertRefused(
            new ImportLevel([telemetry, Logic()], new ImportedWire("s", 0, "l", 0)),
            "logic \"l\" has a Wire from simple_telemetry \"s\", which has a value for mode that a Script can't read",
            "l");
    }

    [Fact]
    public void AnOutsideBlockTheStartupCheckTurnedOff_IsRefused()
    {
        var level = new ImportLevel([Generator(), Lamp()], new ImportedWire("g", 0, "lamp", 0));

        Assert.Equal(
            "generator \"g\" has a Wire to lamp \"lamp\", but 'lamp' doesn't match this game version and is turned off",
            level.Import(["g"], [LampId]).Refusal);
    }

    [Fact]
    public void AnInputWiredFromInsideAndOutside_IsRefused()
    {
        var level = new ImportLevel(
            [Generator(), Trigger(), Lamp()],
            new ImportedWire("g", 0, "lamp", 0),
            new ImportedWire("t", 0, "lamp", 0));

        AssertRefused(
            level,
            "its text doesn't compile at lamp \"lamp\": Input 'intensity' of 'lamp_1' already has a Wire in the level, and an input takes one Wire",
            "g");
    }
}
