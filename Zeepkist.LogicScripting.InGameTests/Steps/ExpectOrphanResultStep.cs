using System;
using System.Collections;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the Scripts tab draws the Orphaned Groups' result line and it contains the text, matching case, or with
/// <see cref="NoResult"/> when it draws none.
/// </summary>
internal sealed class ExpectOrphanResultStep : ScenarioStep
{
    public const string NoResult = "none";

    public ExpectOrphanResultStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        yield return null;
        yield return context.WaitForWindowDrawn();
        if (context.Failed)
            yield break;

        var (frame, drawn) = context.Window.OrphanResult;
        var actual = drawn.Length == 0 ? "(no result line)" : drawn;
        var matches = Text == NoResult ? drawn.Length == 0 : drawn.IndexOf(Text, StringComparison.Ordinal) >= 0;
        if (frame != Time.frameCount)
            context.Fail("the Scripts tab wasn't drawn: add show-tab scripts first, with the Scripts side shown");
        else if (matches)
            context.Pass(actual, Text, actual);
        else
            context.Fail("the Orphaned Groups' result line doesn't match", Text, actual);
    }
}
