using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class RestoreOrphanStep : ScenarioStep
{
    public RestoreOrphanStep(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        var orphan = context.RequireOrphan(Name);
        if (orphan == null)
            yield break;

        if (orphan.Draft == null)
        {
            context.Fail($"\"{Name}\" has no Draft, so Restore from Draft is grayed out");
            yield break;
        }

        context.Window.RestoreOrphan(orphan);
        yield return null;
    }
}
