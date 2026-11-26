using System.Collections;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Ctrl+clicks the first character of the first time the text is in the selected Script's or Library's editor, as a
/// player does to jump to a name's definition: the mouse, as <see cref="ImuiMouse"/> hands it to Imui, moves there and
/// clicks with the jump modifier held for the editor, which Unity's <c>Input</c> never reports for the steps. Passes when
/// the click jumped, leaving the caret at the definition. Imui has the player's mouse again after it.
/// </summary>
internal sealed class CtrlClickStep : ScenarioStep
{
    // The jump happens as the editor takes the press, the frame after it's queued
    private const float JumpSeconds = 1;

    public CtrlClickStep(string text)
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

        var window = context.Window;
        var where = TextPoint.Describe(point);
        var pressed = false;
        int pressedAt;
        try
        {
            window.HoldSentJumpModifier(true);
            ImuiMouse.MoveTo(point);

            // Imui finds the hovered control as it draws, for the frame after, and the editor then finds the jump under it
            yield return null;
            yield return null;
            pressedAt = Time.frameCount;
            if (!ImuiMouse.Press())
            {
                context.Fail("Imui has no backend to send the mouse to");
                yield break;
            }

            pressed = true;
            yield return context.WaitUntil(() => window.JumpedFrame >= pressedAt, JumpSeconds);
        }
        finally
        {
            // Also when the step fails or throws, so the next step finds the mouse let go, the player's again, and the
            // jump modifier only where the player holds it
            if (pressed)
                ImuiMouse.Release();

            window.HoldSentJumpModifier(false);
            ImuiMouse.GiveBack();
        }

        // A frame for Imui to take the release, and for the field to take the focus the jump gave it
        yield return null;
        yield return null;
        if (window.JumpedFrame < pressedAt)
        {
            context.Fail("the click didn't jump, as on a name with no definition to go to, or something over the editor", "a jump to a definition", where);
            yield break;
        }

        var caret = ExpectUsagesStep.ToPosition(window.Text, window.Caret);
        context.Pass($"{where}, the caret at {caret}", null, null);
    }
}
