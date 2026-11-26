using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the Blocks side shows this tab.
/// </summary>
internal sealed class ExpectBlocksTabStep : ScenarioStep
{
    public ExpectBlocksTabStep(bool reference)
    {
        Reference = reference;
    }

    public bool Reference { get; }

    public override IEnumerator Run(StepContext context)
    {
        context.ExpectTab(ChoiceNames.BlocksTabs, Reference, () => context.Window.ShowsReference);
        yield return null;
    }
}
