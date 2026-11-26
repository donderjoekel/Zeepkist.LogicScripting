using System.Collections;
using System.Globalization;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the window is drawn this tall, to within <see cref="RectCheck.Tolerance"/>.
/// </summary>
internal sealed class ExpectWindowHeightStep : ScenarioStep
{
    public ExpectWindowHeightStep(int height)
    {
        Height = height;
    }

    public int Height { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        yield return context.WaitForWindowDrawn();
        if (context.Failed)
            yield break;

        var height = context.Window.WindowRect.H;
        var expected = Height.ToString(CultureInfo.InvariantCulture);
        var actual = RectCheck.Pixels(height);
        context.Check(Mathf.Abs(height - Height) <= RectCheck.Tolerance, $"{actual} px tall", expected, actual);
    }
}
