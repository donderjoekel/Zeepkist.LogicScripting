using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// A level file can hold a Wire that closes a Dependency Loop, though the editor refuses a player one. The level
/// mustn't run with it, so test-mode refuses it.
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
