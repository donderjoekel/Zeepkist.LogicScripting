using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Imports whose Wires and Toggler targets reach blocks outside the import.
/// </summary>
public partial class BlockImportOutsideTests
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

        AssertImports(level, "trigger_1 = placed(\"t\")\n\nlogic(b: trigger_1)", "l");
    }

    [Fact]
    public void AWireFromOneOfSeveralOutputs_NamesTheOutput()
    {
        var level = new ImportLevel([new ImportedBlock("s", SensorId), Logic()], new ImportedWire("s", 2, "l", 0));

        AssertImports(level, "distance_sensor_1 = placed(\"s\")\n\nlogic(distance_sensor_1.y)", "l");
    }

    [Fact]
    public void AWireFromAnOutsideBlock_NamesTheOutputAsItsModeDoes()
    {
        var telemetry = new ImportedBlock("s", TelemetryId, ints: new Dictionary<string, int> { ["xbs"] = 2 });
        var level = new ImportLevel([telemetry, Logic()], new ImportedWire("s", 3, "l", 0));

        AssertImports(level, "simple_telemetry_1 = placed(\"s\")\n\nlogic(simple_telemetry_1.heading)", "l");
    }

    [Fact]
    public void AWireToOutside_IsACall()
    {
        var level = new ImportLevel([Logic(), Lamp()], new ImportedWire("l", 3, "lamp", 1));

        AssertImports(level, "lamp_1 = placed(\"lamp\")\n\nlamp_1(red: logic())", "l");
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
        AssertImports(
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

        AssertImports(
            level,
            "lamp_1 = placed(\"x\")\nlamp_2 = placed(\"y\")\n\nlamp_1(0)\n\nlamp_2(0)",
            "g");
    }

    [Fact]
    public void AnOutsideBlockOfAnImportedType_HasANameOfItsOwn()
    {
        var level = new ImportLevel([Logic("out"), Logic("l")], new ImportedWire("out", 3, "l", 0));

        AssertImports(level, "logic_1 = placed(\"out\")\n\nlogic(logic_1)", "l");
    }

    [Fact]
    public void AllHidden_TheDefaultStaysFirst()
    {
        var level = new ImportLevel(
            [Trigger(), new ImportedBlock("l", LogicId, ints: new Dictionary<string, int> { ["lbhd"] = 1 })],
            new ImportedWire("t", 0, "l", 0));

        AssertImports(level, "default hide: true\n\ntrigger_1 = placed(\"t\")\n\nlogic(trigger_1)", "l");
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

        AssertImports(level, "trigger_1 = placed(\"t\")\n\nrandom(max_in: trigger_1, min: 5)", "r");
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

        AssertImports(level, "toggler_1 = placed(\"tog\")\n\ntoggler_1(0)", "g");
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
