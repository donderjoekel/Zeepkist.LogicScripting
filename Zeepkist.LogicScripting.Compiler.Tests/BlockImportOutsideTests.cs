using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Imports whose Wires and Toggler targets reach blocks outside the import.
/// </summary>
public class BlockImportOutsideTests
{
    private const int DoorId = 1;
    private const int TriggerId = 2413;
    private const int GeneratorId = 2414;
    private const int TogglerId = 2417;
    private const int LogicId = 2420;
    private const int RandomId = 2425;
    private const int DelayId = 2428;
    private const int TelemetryId = 2440;
    private const int SensorId = 2442;
    private const int LampId = 2448;
    private const int PassthroughId = 2426;

    private static ImportedBlock Trigger(string uid = "t") => new(uid, TriggerId);
    private static ImportedBlock Lamp(string uid = "lamp") => new(uid, LampId);
    private static ImportedBlock Logic(string uid = "l") => new(uid, LogicId);
    private static ImportedBlock Generator(string uid = "g") => new(uid, GeneratorId);
    private static ImportedBlock Delay(string uid = "d") => new(uid, DelayId, texts: new Dictionary<string, string> { ["ldl"] = "1" });

    private static ImportedBlock Toggler(string uid, params string[] targets)
    {
        return new ImportedBlock(
            uid,
            TogglerId,
            texts: Enumerable.Range(0, targets.Length).ToDictionary(i => $"xuid{i + 1}", i => targets[i]));
    }

    [Fact]
    public void AWireFromOutside_ReadsAPlacedBlock()
    {
        var level = new ImportLevel([Trigger(), Logic()], new ImportedWire("t", 0, "l", 1));

        AssertImports(level, "trigger_1 = placed(\"t\")\nlogic_1 = logic(b: trigger_1)", "l");
    }

    [Fact]
    public void AWireFromOneOfSeveralOutputs_NamesTheOutput()
    {
        var level = new ImportLevel([new ImportedBlock("s", SensorId), Logic()], new ImportedWire("s", 2, "l", 0));

        AssertImports(level, "distance_sensor_1 = placed(\"s\")\nlogic_1 = logic(distance_sensor_1.y)", "l");
    }

    [Fact]
    public void AWireFromAnOutsideBlock_NamesTheOutputAsItsModeDoes()
    {
        var telemetry = new ImportedBlock("s", TelemetryId, ints: new Dictionary<string, int> { ["xbs"] = 2 });
        var level = new ImportLevel([telemetry, Logic()], new ImportedWire("s", 3, "l", 0));

        AssertImports(level, "simple_telemetry_1 = placed(\"s\")\nlogic_1 = logic(simple_telemetry_1.heading)", "l");
    }

    [Fact]
    public void AWireToOutside_IsACall()
    {
        var level = new ImportLevel([Logic(), Lamp()], new ImportedWire("l", 3, "lamp", 1));

        AssertImports(level, "lamp_1 = placed(\"lamp\")\nlogic_1 = logic()\nlamp_1(red: logic_1)", "l");
    }

    [Fact]
    public void LogicBetweenATriggerAndALamp_RefersToBoth()
    {
        var level = new ImportLevel(
            [Trigger(), Logic(), Lamp()],
            new ImportedWire("t", 0, "l", 0),
            new ImportedWire("l", 3, "lamp", 0));

        AssertImports(
            level,
            "trigger_1 = placed(\"t\")\nlamp_1 = placed(\"lamp\")\nlogic_1 = logic(trigger_1)\nlamp_1(logic_1)",
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
        AssertImports(
            level,
            """
            trigger_1 = placed("t")
            lamp_1 = placed("lamp")
            logic_1 = logic(trigger_1, trigger_1)
            logic_2 = logic(trigger_1)
            lamp_1(logic_1, logic_1, range: logic_2)
            generator_1 = generator()
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

        AssertImports(
            level,
            "lamp_1 = placed(\"x\")\nlamp_2 = placed(\"y\")\ngenerator_1 = generator()\nlamp_1(generator_1)\nlamp_2(generator_1)",
            "g");
    }

    [Fact]
    public void AnOutsideBlockOfAnImportedType_HasANameOfItsOwn()
    {
        var level = new ImportLevel([Logic("out"), Logic("l")], new ImportedWire("out", 3, "l", 0));

        AssertImports(level, "logic_2 = placed(\"out\")\nlogic_1 = logic(logic_2)", "l");
    }

    [Fact]
    public void AllHidden_TheDefaultStaysFirst()
    {
        var level = new ImportLevel(
            [Trigger(), new ImportedBlock("l", LogicId, ints: new Dictionary<string, int> { ["lbhd"] = 1 })],
            new ImportedWire("t", 0, "l", 0));

        AssertImports(level, "default hide: true\ntrigger_1 = placed(\"t\")\nlogic_1 = logic(trigger_1)", "l");
    }

    [Fact]
    public void SelectedBlocksAScriptCantCreate_StayOutside()
    {
        var level = new ImportLevel(
            [Trigger(), Logic(), Lamp(), Lamp("unwired"), new ImportedBlock("door", DoorId)],
            new ImportedWire("t", 0, "l", 0),
            new ImportedWire("l", 3, "lamp", 0),
            new ImportedWire("t", 0, "lamp", 1));

        AssertImports(
            level,
            "trigger_1 = placed(\"t\")\nlamp_1 = placed(\"lamp\")\nlogic_1 = logic(trigger_1)\nlamp_1(logic_1)",
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
        AssertRefused(
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

        AssertImports(level, "trigger_1 = placed(\"t\")\nrandom_1 = random(max_in: trigger_1, min: 5)", "r");
    }

    [Fact]
    public void ATogglersTargets_AreReferences()
    {
        var level = new ImportLevel(
            [Trigger(), Toggler("tog", "door", "lamp", "out"), new ImportedBlock("door", DoorId), Lamp(), Logic("out")],
            new ImportedWire("t", 0, "tog", 0));

        AssertImports(
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

        AssertImports(
            level,
            "lamp_1 = placed(\"lamp\")\ngenerator_1 = generator()\nlamp_1(generator_1)\ntoggler(targets: [lamp_1])",
            "g",
            "tog");
    }

    [Fact]
    public void AnUnselectedToggler_IsAnOutsideBlockLikeAnother()
    {
        var level = new ImportLevel(
            [Generator(), Toggler("tog", "door"), new ImportedBlock("door", DoorId)],
            new ImportedWire("g", 0, "tog", 0));

        AssertImports(level, "toggler_1 = placed(\"tog\")\ngenerator_1 = generator()\ntoggler_1(generator_1)", "g");
    }

    [Fact]
    public void ALoopThroughAnOutsideBlock_WithADelay_IsWritten()
    {
        var level = new ImportLevel(
            [Logic(), new ImportedBlock("p", PassthroughId), Delay()],
            new ImportedWire("l", 3, "p", 0),
            new ImportedWire("p", 1, "d", 0),
            new ImportedWire("d", 1, "l", 0));

        AssertImports(
            level,
            "passthrough_1 = placed(\"p\")\ndelay_1 = delay(passthrough_1, ticks: 1)\nlogic_1 = logic(delay_1)\npassthrough_1(logic_1)",
            "l",
            "d");
    }

    [Fact]
    public void ALoopThroughAnOutsideDelay_IsWritten()
    {
        var level = new ImportLevel([Logic(), Delay()], new ImportedWire("l", 3, "d", 0), new ImportedWire("d", 1, "l", 0));

        AssertImports(level, "delay_1 = placed(\"d\")\nlogic_1 = logic(delay_1)\ndelay_1(logic_1)", "l");
    }

    [Fact]
    public void ADependencyLoopThroughAnOutsideBlock_IsRefused()
    {
        var level = new ImportLevel(
            [Logic(), new ImportedBlock("p", PassthroughId)],
            new ImportedWire("l", 3, "p", 0),
            new ImportedWire("p", 1, "l", 0));

        AssertRefused(
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

        AssertRefused(
            level,
            "its text doesn't compile at passthrough \"p\": This Wire makes a Dependency Loop, which stops all logic in play. Put a delay(...) in the loop",
            "a",
            "b");
    }

    [Fact]
    public void ALatchBetweenOutsideBlocks_RefersAheadOnlyInItsDelay()
    {
        var level = new ImportLevel(
            [Trigger(), Logic(), Delay(), Lamp()],
            new ImportedWire("t", 0, "l", 0),
            new ImportedWire("d", 1, "l", 1),
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("l", 3, "lamp", 0),
            new ImportedWire("d", 1, "lamp", 1));

        AssertImports(
            level,
            """
            trigger_1 = placed("t")
            lamp_1 = placed("lamp")
            delay_1 = delay(logic_1, ticks: 1)
            logic_1 = logic(trigger_1, delay_1)
            lamp_1(logic_1, delay_1)
            """.Replace("\r\n", "\n"),
            "l",
            "d");
    }

    [Fact]
    public void ALoopThroughAnOutsideBlock_AndOneInsideTheImport_AreWritten()
    {
        var level = new ImportLevel(
            [Logic(), Delay(), new ImportedBlock("p", PassthroughId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("d", 1, "p", 0),
            new ImportedWire("p", 1, "l", 1));

        AssertImports(
            level,
            """
            passthrough_1 = placed("p")
            delay_1 = delay(logic_1, ticks: 1)
            passthrough_1(delay_1)
            logic_1 = logic(delay_1, passthrough_1)
            """.Replace("\r\n", "\n"),
            "l",
            "d");
    }

    [Fact]
    public void ADependencyLoopThroughAnOutsideBlock_NextToALoopThroughADelay_IsRefused()
    {
        var level = new ImportLevel(
            [Logic(), Delay(), new ImportedBlock("p", PassthroughId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("l", 3, "p", 0),
            new ImportedWire("p", 1, "l", 1));

        AssertRefused(
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

        AssertRefused(level, "a loop of Wires has no Delay in it: logic \"b\", logic \"a\"", "a", "b", "lamp");
    }

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
    [InlineData]
    [InlineData("", "lamp")]
    [InlineData("lamp", "lamp")]
    public void ATogglerWithTargetsAScriptCantWrite_IsRefused(params string[] targets)
    {
        AssertRefused(
            new ImportLevel([Toggler("tog", targets), Lamp()]),
            "toggler \"tog\" has a value for targets that a Script can't write",
            "tog");
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

    [Fact]
    public void TheCompare_CatchesAMissingWireToOutside()
    {
        ImportedBlock[] blocks = [Generator()];
        PlacedBlock Find(string uid) => uid == "lamp" ? new PlacedBlock(LampId) : null;
        var graph = new ImportGraph(blocks, Find);
        Assert.Null(graph.AddWire(new ImportedWire("g", 0, "lamp", 0)));

        Assert.Equal(
            "its text misses a Wire into lamp \"lamp\"",
            ImportCompare.FindDifference(graph, blocks, ScriptCompiler.Compile("generator()", 0, null, Find)));
    }

    /// <summary>
    /// Also that compiling the text over the imported blocks gives as many blocks.
    /// </summary>
    private static void AssertImports(ImportLevel level, string text, params string[] selected)
    {
        var import = level.Import(selected);

        Assert.Null(import.Refusal);
        Assert.Equal(text, import.Text);
        var compiled = level.Compile(import);
        Assert.Empty(compiled.Errors);
        Assert.Equal(import.ImportedUids.Count, compiled.Blocks.Count);
    }

    private static void AssertRefused(ImportLevel level, string refusal, params string[] selected)
    {
        var import = level.Import(selected);

        Assert.Equal(refusal, import.Refusal);
        Assert.Null(import.Text);
        Assert.Empty(import.ImportedUids);
    }
}
