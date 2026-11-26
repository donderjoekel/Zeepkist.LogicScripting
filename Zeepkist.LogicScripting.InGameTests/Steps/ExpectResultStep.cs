using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the result line contains the text, matching case.
/// </summary>
internal sealed class ExpectResultStep : ScenarioStep
{
    public ExpectResultStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        context.ExpectContains(Text, context.Window.Result, "the result line doesn't contain it", "(no result line)");

        yield return null;
    }
}
