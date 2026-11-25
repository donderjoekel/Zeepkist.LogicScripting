using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Makes a Script from the blocks selected in the level, as Import selected on the Scripts tab does. Fails, with why,
/// when it makes none.
/// </summary>
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
