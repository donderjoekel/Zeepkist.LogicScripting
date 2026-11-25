using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Stores a Wire that closes a Dependency Loop, as a level file can hold, though the editor refuses a player one. Fails
/// for a Wire that closes none. The level mustn't run with it, so test-mode refuses it.
/// </summary>
internal sealed class WireDependencyLoopStep : ScenarioStep
{
    public WireDependencyLoopStep(WireArgument wire)
    {
        Wire = wire;
    }

    public WireArgument Wire { get; }

    public override IEnumerator Run(StepContext context)
    {
        return WireBlocksStep.StoreWire(context, Wire, closesLoop: true);
    }
}
