using System.Collections;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Wires an output of one named block to an input of another, as one undo step. Fails for a Wire a player couldn't make
/// by hand: into an input that already has one, or one that closes a Dependency Loop.
/// </summary>
internal sealed class WireBlocksStep : ScenarioStep
{
    private const string UndoLabel = "In-game test: wire blocks";

    public WireBlocksStep(WireArgument wire)
    {
        Wire = wire;
    }

    public WireArgument Wire { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        var wire = context.RequireWire(central, Wire, out var from, out var to);
        if (wire == null)
            yield break;

        // No Script has this id, so no Block Group is left out of the Wires it finds
        var placed = PlacedBlocks.Find(central, wire.ToUid, ScriptId.New());
        if (placed.WiredInputs.Contains(wire.ToConnector))
        {
            var wires = LevelWires.Describe(central, context.PlacedBlocks, to.ReadWires());
            context.Fail($"\"{to.Name}\"'s input {Wire.Input} already has a Wire", "no Wire", wires);
            yield break;
        }

        // Downstream stops at a Delay, but not at the block it starts from
        var isCycle = wire.ToUid == wire.FromUid || placed.Downstream.Contains(wire.FromUid);
        if (isCycle && from.Type != BlockManifest.Delay && to.Type != BlockManifest.Delay)
        {
            context.Fail($"{Wire} closes a Dependency Loop");
            yield break;
        }

        var before = central.undoRedo.currentHistoryPosition;
        BlockBatch.Apply(central, [], [], [wire], UndoLabel);
        yield return null;

        if (!LevelWires.IsStoredAtOutput(central, wire) || !LevelWires.IsStoredAtInput(central, wire))
        {
            var wires = LevelWires.Describe(central, context.PlacedBlocks, from.ReadWires().Concat(to.ReadWires()));
            context.Fail("the blocks didn't keep the Wire", Wire.ToString(), wires);
            yield break;
        }

        context.Detail = $"{from.Type.Function} \"{from.Name}\" to {to.Type.Function} \"{to.Name}\", "
            + $"history position {before} → {central.undoRedo.currentHistoryPosition}";
    }
}
