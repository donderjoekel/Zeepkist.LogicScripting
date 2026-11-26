using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the Scripts side shows this tab.
/// </summary>
internal sealed class ExpectTabStep : ScenarioStep
{
    public ExpectTabStep(bool libraries)
    {
        Libraries = libraries;
    }

    public bool Libraries { get; }

    public override IEnumerator Run(StepContext context)
    {
        context.ExpectTab(ChoiceNames.Tabs, Libraries, () => context.Window.ShowsLibraries);
        yield return null;
    }
}
