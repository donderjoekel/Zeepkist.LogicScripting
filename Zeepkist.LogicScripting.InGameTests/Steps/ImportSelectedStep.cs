using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class ImportSelectedStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        if (context.Window.ImportSelected() is { } error)
            context.Fail(error);
        else
            context.Detail = context.Window.ShownList.Message;

        yield return null;
    }
}
