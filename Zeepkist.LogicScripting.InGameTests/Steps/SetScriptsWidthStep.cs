using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class SetScriptsWidthStep : ScenarioStep
{
    public SetScriptsWidthStep(int width)
    {
        Width = width;
    }

    public int Width { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        context.Window.SetScriptsWidth(Width);
        yield return null;
        yield return context.WaitForWindowDrawn();
        if (context.Failed)
            yield break;

        // A width below the side's minimum gives the narrowest side
        var actual = RectCheck.Pixels(context.Window.DrawnScriptsWidth);
        context.Pass($"{actual} px wide", RectCheck.Pixels(Width), actual);
    }
}
