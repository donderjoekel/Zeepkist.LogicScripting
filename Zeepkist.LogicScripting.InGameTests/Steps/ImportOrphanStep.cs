using System.Collections;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class ImportOrphanStep : ScenarioStep
{
    private const float DrawSeconds = 2;

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

        var window = context.Window;
        window.PressImportOrphan(orphan);
        yield return context.WaitUntil(() => !window.IsOrphanInputPending, DrawSeconds);
        if (window.IsOrphanInputPending)
        {
            window.CancelOrphanInput();
            context.Fail("the group's row wasn't drawn: the Scripts side or the Orphaned Groups are collapsed");
            yield break;
        }

        if (window.Orphans.Any(o => o.Id == orphan.Id))
            context.Fail(window.OrphanMessage);
        else
            context.Detail = window.OrphanMessage;
    }
}
