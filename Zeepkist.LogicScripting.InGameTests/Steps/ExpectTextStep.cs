using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the selected Script's or Library's text contains the text, matching case.
/// </summary>
internal sealed class ExpectTextStep : ScenarioStep
{
    public ExpectTextStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireText())
            context.ExpectContains(Text, context.Window.Text, "the text doesn't contain it", "(no text)");

        yield return null;
    }
}
