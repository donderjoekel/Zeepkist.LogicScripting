using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;
using static TNRD.Zeepkist.LogicScripting.Compiler.Tests.ImportedBlocks;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Imports whose Wires and Toggler targets reach blocks outside the import.
/// </summary>
public class BlockImportOutsideTests
{
    [Fact]
    public void AWireFromOutside_ReadsAPlacedBlock()
    {
        var level = new ImportLevel([Trigger(), Logic()], new ImportedWire("t", 0, "l", 1));

        ImportAssert.Imports(level, "trigger_1 = placed(\"t\")\n\nlogic(b: trigger_1)", "l");
    }

    [Fact]
    public void AWireFromOneOfSeveralOutputs_NamesTheOutput()
    {
        var level = new ImportLevel([new ImportedBlock("s", SensorId), Logic()], new ImportedWire("s", 2, "l", 0));

        ImportAssert.Imports(level, "distance_sensor_1 = placed(\"s\")\n\nlogic(distance_sensor_1.y)", "l");
    }

    [Fact]
    public void AWireFromAnOutsideBlock_NamesTheOutputAsItsModeDoes()
    {
        var telemetry = new ImportedBlock("s", TelemetryId, ints: new Dictionary<string, int> { ["xbs"] = 2 });
        var level = new ImportLevel([telemetry, Logic()], new ImportedWire("s", 3, "l", 0));

        ImportAssert.Imports(level, "simple_telemetry_1 = placed(\"s\")\n\nlogic(simple_telemetry_1.heading)", "l");
    }

    [Fact]
    public void AWireToOutside_IsACall()
    {
        var level = new ImportLevel([Logic(), Lamp()], new ImportedWire("l", 3, "lamp", 1));

        ImportAssert.Imports(level, "lamp_1 = placed(\"lamp\")\n\nlamp_1(red: logic())", "l");
    }

    [Fact]
    public void LogicBetweenATriggerAndALamp_RefersToBoth()
    {
        var level = new ImportLevel(
            [Trigger(), Logic(), Lamp()],
            new ImportedWire("t", 0, "l", 0),
            new ImportedWire("l", 3, "lamp", 0));

        ImportAssert.Imports(
            level,
            "trigger_1 = placed(\"t\")\nlamp_1 = placed(\"lamp\")\n\nlamp_1(logic(trigger_1))",
            "l");
    }

    [Fact]
    public void AnOutsideBlockUsedSeveralTimes_IsReferredToOnce()
    {
        var level = new ImportLevel(
            [Trigger(), Logic("a"), Logic("b"), Generator(), Lamp()],
            new ImportedWire("t", 0, "a", 0),
            new ImportedWire("t", 0, "a", 1),
            new ImportedWire("t", 0, "b", 0),
            new ImportedWire("b", 3, "lamp", 4),
            new ImportedWire("a", 3, "lamp", 0),
            new ImportedWire("a", 3, "lamp", 1));

        // The call comes after the last block that feeds the lamp
        ImportAssert.Imports(
            level,
            """
            trigger_1 = placed("t")
            lamp_1 = placed("lamp")

            logic_1 = trigger_1 & trigger_1
            lamp_1(logic_1, logic_1, range: logic(trigger_1))

            generator()
            """.Replace("\r\n", "\n"),
            "a",
            "b",
            "g");
    }

    [Fact]
    public void OneOutputToSeveralOutsideBlocks_IsOneCallEach()
    {
        var level = new ImportLevel(
            [Generator(), Lamp("y"), Lamp("x")],
            new ImportedWire("g", 0, "y", 0),
            new ImportedWire("g", 0, "x", 0));

        ImportAssert.Imports(
            level,
            "lamp_1 = placed(\"x\")\nlamp_2 = placed(\"y\")\n\nlamp_1(0)\n\nlamp_2(0)",
            "g");
    }

    [Fact]
    public void AnOutsideBlockOfAnImportedType_HasANameOfItsOwn()
    {
        var level = new ImportLevel([Logic("out"), Logic("l")], new ImportedWire("out", 3, "l", 0));

        ImportAssert.Imports(level, "logic_1 = placed(\"out\")\n\nlogic(logic_1)", "l");
    }

    [Fact]
    public void AllHidden_TheDefaultStaysFirst()
    {
        var level = new ImportLevel(
            [Trigger(), new ImportedBlock("l", LogicId, ints: new Dictionary<string, int> { ["lbhd"] = 1 })],
            new ImportedWire("t", 0, "l", 0));

        ImportAssert.Imports(level, "default hide: true\n\ntrigger_1 = placed(\"t\")\n\nlogic(trigger_1)", "l");
    }

    [Fact]
    public void SelectedBlocksAScriptCantCreate_StayOutside()
    {
        var level = new ImportLevel(
            [Trigger(), Logic(), Lamp(), Lamp("unwired"), new ImportedBlock("door", DoorId)],
            new ImportedWire("t", 0, "l", 0),
            new ImportedWire("l", 3, "lamp", 0),
            new ImportedWire("t", 0, "lamp", 1));

        ImportAssert.Imports(
            level,
            "trigger_1 = placed(\"t\")\nlamp_1 = placed(\"lamp\")\n\nlamp_1(logic(trigger_1))",
            "t",
            "l",
            "lamp",
            "unwired",
            "door");
        Assert.Equal(["l"], level.Import("t", "l", "lamp", "unwired", "door").ImportedUids);
    }

    [Fact]
    public void ASelectedGroupBlockAScriptCantCreate_IsNoReason_UnlessItIsReached()
    {
        var grouped = new ImportedBlock("lamp", LampId, isInBlockGroup: true);

        Assert.True(new ImportLevel([Generator(), grouped]).Import("g", "lamp").Succeeded);
        ImportAssert.Refused(
            new ImportLevel([Generator(), grouped], new ImportedWire("g", 0, "lamp", 0)),
            "generator \"g\" has a Wire to lamp \"lamp\", but that one is in a Block Group, which a recompile replaces",
            "g",
            "lamp");
    }

    [Fact]
    public void ARangesMaximumFromOutside_CountsAsWired()
    {
        var random = new ImportedBlock("r", RandomId, texts: new Dictionary<string, string> { ["xv1"] = "5" });
        var level = new ImportLevel([Trigger(), random], new ImportedWire("t", 0, "r", 1));

        ImportAssert.Imports(level, "trigger_1 = placed(\"t\")\n\nrandom(max_in: trigger_1, min: 5)", "r");
    }

    [Fact]
    public void ATogglersTargets_AreReferences()
    {
        var level = new ImportLevel(
            [Trigger(), Toggler("tog", "door", "lamp", "out"), new ImportedBlock("door", DoorId), Lamp(), Logic("out")],
            new ImportedWire("t", 0, "tog", 0));

        ImportAssert.Imports(
            level,
            """
            trigger_1 = placed("t")
            block_1 = placed("door")
            lamp_1 = placed("lamp")
            logic_1 = placed("out")

            toggler(trigger_1, targets: [block_1, lamp_1, logic_1])
            """.Replace("\r\n", "\n"),
            "tog",
            "lamp");
    }

    [Fact]
    public void ATargetThatIsWiredToo_IsReferredToOnce()
    {
        var level = new ImportLevel([Generator(), Toggler("tog", "lamp"), Lamp()], new ImportedWire("g", 0, "lamp", 0));

        ImportAssert.Imports(
            level,
            "lamp_1 = placed(\"lamp\")\n\nlamp_1(0)\n\ntoggler(targets: [lamp_1])",
            "g",
            "tog");
    }

    [Fact]
    public void AnUnselectedToggler_IsAnOutsideBlockLikeAnother()
    {
        var level = new ImportLevel(
            [Generator(), Toggler("tog", "door"), new ImportedBlock("door", DoorId)],
            new ImportedWire("g", 0, "tog", 0));

        ImportAssert.Imports(level, "toggler_1 = placed(\"tog\")\n\ntoggler_1(0)", "g");
    }

    [Fact]
    public void ALoopThroughAnOutsideBlock_WithADelay_IsWritten()
    {
        var level = new ImportLevel(
            [Logic(), new ImportedBlock("p", PassthroughId), Delay("d", 1)],
            new ImportedWire("l", 3, "p", 0),
            new ImportedWire("p", 1, "d", 0),
            new ImportedWire("d", 1, "l", 0));

        ImportAssert.Imports(
            level,
            "passthrough_1 = placed(\"p\")\n\npassthrough_1(logic(delay(passthrough_1, ticks: 1)))",
            "l",
            "d");
    }

    [Fact]
    public void ALoopThroughAnOutsideDelay_IsWritten()
    {
        var level = new ImportLevel([Logic(), Delay("d", 1)], new ImportedWire("l", 3, "d", 0), new ImportedWire("d", 1, "l", 0));

        ImportAssert.Imports(level, "delay_1 = placed(\"d\")\n\ndelay_1(logic(delay_1))", "l");
    }

    [Fact]
    public void ADependencyLoopThroughAnOutsideBlock_IsRefused()
    {
        var level = new ImportLevel(
            [Logic(), new ImportedBlock("p", PassthroughId)],
            new ImportedWire("l", 3, "p", 0),
            new ImportedWire("p", 1, "l", 0));

        ImportAssert.Refused(
            level,
            "its text doesn't compile at passthrough \"p\": This Wire makes a Dependency Loop, which stops all logic in play. Put a delay(...) in the loop",
            "l");
    }

    [Fact]
    public void ADependencyLoopThroughWiresBetweenOutsideBlocks_IsRefused()
    {
        ImportedBlock[] blocks =
        [
            Logic("a"), new ImportedBlock("p", PassthroughId), new ImportedBlock("mid", PassthroughId),
            new ImportedBlock("q", PassthroughId), Logic("b")
        ];
        var level = new ImportLevel(
            blocks,
            new ImportedWire("a", 3, "p", 0),
            new ImportedWire("p", 1, "mid", 0),
            new ImportedWire("mid", 1, "q", 0),
            new ImportedWire("q", 1, "b", 0),
            new ImportedWire("b", 3, "a", 0));

        ImportAssert.Refused(
            level,
            "its text doesn't compile at passthrough \"p\": This Wire makes a Dependency Loop, which stops all logic in play. Put a delay(...) in the loop",
            "a",
            "b");
    }

    [Fact]
    public void ALatchBetweenOutsideBlocks_RefersAheadOnlyInItsDelay()
    {
        var level = new ImportLevel(
            [Trigger(), Logic(), Delay("d", 1), Lamp()],
            new ImportedWire("t", 0, "l", 0),
            new ImportedWire("d", 1, "l", 1),
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("l", 3, "lamp", 0),
            new ImportedWire("d", 1, "lamp", 1));

        ImportAssert.Imports(
            level,
            """
            trigger_1 = placed("t")
            lamp_1 = placed("lamp")

            delay_1 = delay(logic_1, ticks: 1)
            logic_1 = trigger_1 & delay_1
            lamp_1(logic_1, delay_1)
            """.Replace("\r\n", "\n"),
            "l",
            "d");
    }

    [Fact]
    public void ALoopThroughAnOutsideBlock_AndOneInsideTheImport_AreWritten()
    {
        var level = new ImportLevel(
            [Logic(), Delay("d", 1), new ImportedBlock("p", PassthroughId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("d", 1, "p", 0),
            new ImportedWire("p", 1, "l", 1));

        ImportAssert.Imports(
            level,
            """
            passthrough_1 = placed("p")

            delay_1 = delay(delay_1 & passthrough_1, ticks: 1)
            passthrough_1(delay_1)
            """.Replace("\r\n", "\n"),
            "l",
            "d");
    }

    [Fact]
    public void ADependencyLoopThroughAnOutsideBlock_NextToALoopThroughADelay_IsRefused()
    {
        var level = new ImportLevel(
            [Logic(), Delay("d", 1), new ImportedBlock("p", PassthroughId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("l", 3, "p", 0),
            new ImportedWire("p", 1, "l", 1));

        ImportAssert.Refused(
            level,
            "its text doesn't compile at passthrough \"p\": This Wire makes a Dependency Loop, which stops all logic in play. Put a delay(...) in the loop",
            "l",
            "d");
    }

    [Fact]
    public void ADependencyLoopInsideTheImport_IsRefusedWithoutTheOutsideBlocks()
    {
        var level = new ImportLevel(
            [Trigger(), Logic("a"), Logic("b"), Lamp()],
            new ImportedWire("t", 0, "a", 0),
            new ImportedWire("a", 3, "b", 0),
            new ImportedWire("b", 3, "a", 1),
            new ImportedWire("b", 3, "lamp", 0));

        ImportAssert.Refused(level, "a loop of Wires has no Delay in it: logic \"b\", logic \"a\"", "a", "b", "lamp");
    }

    [Fact]
    public void AWireToAnotherScriptsGroupBlock_IsRefused()
    {
        var level = new ImportLevel(
            [Generator(), new ImportedBlock("p", PassthroughId, isInBlockGroup: true)],
            new ImportedWire("g", 0, "p", 0));

        ImportAssert.Refused(
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

        ImportAssert.Refused(
            level,
            "logic \"l\" has a Wire from generator \"g\", but that one is in a Block Group, which a recompile replaces",
            "l");
    }

    [Fact]
    public void ATargetInAnotherScriptsGroup_IsRefused()
    {
        var level = new ImportLevel([Toggler("tog", "g"), new ImportedBlock("g", GeneratorId, isInBlockGroup: true)]);

        ImportAssert.Refused(
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

        ImportAssert.Refused(level, "a Toggler targets generator \"g\", and would lose it at the next Compile", selected);
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
        ImportAssert.Refused(new ImportLevel([Toggler("tog", targets), Lamp()]), $"toggler \"tog\" {reason}", "tog");
    }

    [Fact]
    public void ATargetThatIsGone_IsRefused()
    {
        ImportAssert.Refused(new ImportLevel([Toggler("tog", "gone")]), "toggler \"tog\" targets block \"gone\", which isn't in the level", "tog");
    }

    [Fact]
    public void AWireToABlockThatIsGone_IsRefused()
    {
        ImportAssert.Refused(
            new ImportLevel([Generator()], new ImportedWire("g", 0, "gone", 0)),
            "generator \"g\" has a Wire to block \"gone\", which isn't in the level",
            "g");
    }

    [Fact]
    public void AWireToABlockTheManifestDoesNotKnow_IsRefused()
    {
        ImportAssert.Refused(
            new ImportLevel([Generator(), new ImportedBlock("door", DoorId)], new ImportedWire("g", 0, "door", 0)),
            "generator \"g\" has a Wire to block \"door\", which a Script can't wire",
            "g");
    }

    [Fact]
    public void AWireToAnOutsideBlocksOutput_IsRefused()
    {
        ImportAssert.Refused(
            new ImportLevel([Generator(), Trigger()], new ImportedWire("g", 0, "t", 0)),
            "the Wire from generator \"g\" to trigger \"t\" doesn't go from an output to an input",
            "g");
    }

    [Fact]
    public void AnOutsideBlocksModeOutOfRange_IsRefused()
    {
        var telemetry = new ImportedBlock("s", TelemetryId, ints: new Dictionary<string, int> { ["xbs"] = 3 });

        ImportAssert.Refused(
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

        ImportAssert.Refused(
            level,
            "its text doesn't compile at lamp \"lamp\": Input 'intensity' of 'lamp_1' already has a Wire in the level, and an input takes one Wire",
            "g");
    }

    [Fact]
    public void TheCompare_CatchesAMissingWireToOutside()
    {
        ImportedBlock[] blocks = [Generator()];
        PlacedBlock Find(string uid) => uid == "lamp" ? new PlacedBlock(LampId) : null;
        var graph = new ImportGraph(blocks, Find);
        Assert.Null(graph.AddWire(new ImportedWire("g", 0, "lamp", 0)));

        Assert.Equal(
            "its text misses a Wire into lamp \"lamp\"",
            ImportCompare.FindDifference(
                graph,
                blocks,
                new ImportedText(
                    [new ImportLine("g", "generator()")],
                    new Dictionary<(Position, BlockType), string> { [(new Position(1, 1), BlockManifest.Generator)] = "g" },
                    new Dictionary<string, string>()),
                ScriptCompiler.Compile("generator()", 0, null, Find)));
    }
}
