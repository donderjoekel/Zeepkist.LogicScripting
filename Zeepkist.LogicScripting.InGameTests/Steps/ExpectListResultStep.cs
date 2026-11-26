using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the shown tab's result line, in its footer, contains the text, matching case.
/// </summary>
internal sealed class ExpectListResultStep : ScenarioStep
{
    public ExpectListResultStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        context.ExpectContains(Text, context.Window.ShownList.Message, "the tab's result line doesn't contain it", "(no result line)");

        yield return null;
    }
}
