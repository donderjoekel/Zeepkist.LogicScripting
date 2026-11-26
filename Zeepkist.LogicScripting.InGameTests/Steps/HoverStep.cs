using System.Collections;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Moves the mouse, as <see cref="ImuiMouse"/> hands it to Imui, onto the first character of the first time the text
/// is in the selected Script's or Library's editor, and waits until it has rested there long enough for the hover box.
/// The mouse stays there for the steps after it, until the Scenario ends.
/// </summary>
internal sealed class HoverStep : ScenarioStep
{
    // The hover box needs half a second's rest, so this leaves it plenty
    private const float RestSeconds = 3;

    public HoverStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireText())
            yield break;

        if (TextPoint.Find(context, Text) is not { } index)
            yield break;

        yield return context.WaitForWindowDrawn();
        if (context.Failed)
            yield break;

        if (TextPoint.OnCharacter(context, index) is not { } point)
            yield break;

        var where = TextPoint.Describe(point);
        ImuiMouse.MoveTo(point);

        // The rest starts the frame Imui first finds the mouse there, so one from before can't count
        var movedAt = Time.frameCount;
        yield return context.WaitUntil(() => context.Window.HoverRestingFrame > movedAt, RestSeconds);
        if (context.Window.HoverRestingFrame <= movedAt)
        {
            context.Fail("the mouse didn't rest on the editor, as when something covers it", "the editor hovered", where);
            yield break;
        }

        context.Pass(where, null, null);
    }
}
