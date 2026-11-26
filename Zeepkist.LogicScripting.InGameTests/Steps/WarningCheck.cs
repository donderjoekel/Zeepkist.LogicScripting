using System;
using System.Collections;
using TNRD.Zeepkist.LogicScripting.UI;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// For the steps that check whether the window drew a warning, in the frame it was last drawn.
/// </summary>
internal static class WarningCheck
{
    /// <summary>
    /// Reads whether the window drew the warning with <paramref name="readLayout"/>, failing with
    /// <paramref name="notDrawn"/> when the part of the window it goes in wasn't drawn that frame.
    /// </summary>
    public static IEnumerator Run(StepContext context, bool shown, string warning, Func<ScriptWindow.Driver, WarningLayout> readLayout, string notDrawn)
    {
        if (!context.RequireOpenWindow())
            yield break;

        yield return context.WaitForWindowDrawn();
        if (context.Failed)
            yield break;

        var drawn = readLayout(context.Window);
        if (drawn.Frame != Time.frameCount)
        {
            context.Fail(notDrawn, $"Unity frame {Time.frameCount}", $"Unity frame {drawn.Frame}");
            yield break;
        }

        var expected = ChoiceNames.Of(ChoiceNames.OnOff, shown);
        var actual = ChoiceNames.Of(ChoiceNames.OnOff, drawn.Shown);
        if (drawn.Shown == shown)
            context.Pass($"{warning}: {DescribeDrawn(shown)}", expected, actual);
        else
            context.Fail($"{warning}: {DescribeDrawn(drawn.Shown)}", expected, actual);
    }

    private static string DescribeDrawn(bool shown)
    {
        return shown ? "shown" : "not shown";
    }
}
