using System;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal static class TextPoint
{
    /// <summary>
    /// The index the text is first at, or null with the step failed when the editor's text doesn't have it.
    /// </summary>
    public static int? Find(StepContext context, string text)
    {
        var index = context.Window.Text.IndexOf(text, StringComparison.Ordinal);
        if (index >= 0)
            return index;

        context.Fail("the editor's text doesn't have it", text, context.Window.Text);
        return null;
    }

    /// <summary>
    /// The point on the character at <paramref name="index"/> as the editor drew it this frame, or null with the step
    /// failed when it didn't draw the editor this frame or the character isn't in view in it. Call after
    /// <see cref="StepContext.WaitForWindowDrawn"/>.
    /// </summary>
    public static Vector2? OnCharacter(StepContext context, int index)
    {
        if (context.Window.FindDrawnCharacter(index) is not { } drawn || drawn.Frame != Time.frameCount)
        {
            context.Fail("the window didn't draw the editor");
            return null;
        }

        // A quarter of the way across, so the character is the one under the mouse and not the one before it
        var point = new Vector2(drawn.Character.X + (drawn.Character.W / 4), drawn.Character.Y + (drawn.Character.H / 2));
        if (drawn.TextBounds.Contains(point))
            return point;

        context.Fail("it isn't in view in the editor", RectCheck.Describe(drawn.TextBounds), Describe(point));
        return null;
    }

    public static string Describe(Vector2 point)
    {
        return $"the mouse at x {RectCheck.Pixels(point.x)}, y {RectCheck.Pixels(point.y)}";
    }
}
