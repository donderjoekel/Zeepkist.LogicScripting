using System.Collections;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Counts the Wires of a named block, into its inputs and out of its outputs, and checks that the block at each one's
/// other end stores it too.
/// </summary>
internal sealed class ExpectWiresStep : ScenarioStep
{
    public ExpectWiresStep(string name, int count)
    {
        Name = name;
        Count = count;
    }

    public string Name { get; }
    public int Count { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        var named = context.RequireNamedBlock(central, Name);
        if (named == null)
            yield break;

        var wires = named.ReadWires();
        var oneEnded = wires.Where(w => !LevelWires.IsStoredAtOutput(central, w) || !LevelWires.IsStoredAtInput(central, w)).ToList();
        var detail = LevelWires.Describe(central, context.PlacedBlocks, wires);
        if (oneEnded.Count > 0)
            context.Fail($"stored on only one block: {LevelWires.Describe(central, context.PlacedBlocks, oneEnded)}", "both blocks store each Wire", detail);
        else if (wires.Count == Count)
            context.Pass(detail, Count.ToString(), wires.Count.ToString());
        else
            context.Fail(detail, Count.ToString(), wires.Count.ToString());
    }
}
