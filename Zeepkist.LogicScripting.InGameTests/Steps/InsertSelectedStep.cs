using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class InsertSelectedStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null || !context.RequireScript())
            yield break;

        if (!context.Window.InsertSelected(central))
            context.Fail("inserted nothing: add place-block first");

        yield return null;
    }
}
