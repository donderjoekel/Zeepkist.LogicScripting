using System.Collections;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when both blocks store the Wire.
/// </summary>
internal sealed class ExpectWireStep : ScenarioStep
{
    public ExpectWireStep(WireArgument wire)
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

        var wires = LevelWires.Describe(central, context.PlacedBlocks, from.ReadWires().Concat(to.ReadWires()));
        var isAtOutput = LevelWires.IsStoredAtOutput(central, wire);
        var isAtInput = LevelWires.IsStoredAtInput(central, wire);
        if (isAtOutput && isAtInput)
            context.Pass(wires, Wire.ToString(), wires);
        else if (isAtOutput || isAtInput)
            context.Fail($"only \"{(isAtOutput ? from : to).Name}\" stores the Wire {Wire}", Wire.ToString(), wires);
        else
            context.Fail($"no Wire {Wire}", Wire.ToString(), wires);
    }
}
