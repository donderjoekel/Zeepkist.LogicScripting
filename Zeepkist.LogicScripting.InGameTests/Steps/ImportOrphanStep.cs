using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Makes a Script from an Orphaned Group's blocks and selects it, as Import on its row does. Fails, with why, when it
/// makes none.
/// </summary>
internal sealed class ImportOrphanStep : ScenarioStep
{
    public ImportOrphanStep(string name)
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

        if (context.Window.ImportOrphan(orphan) is { } error)
            context.Fail(error);

        yield return null;
    }
}
