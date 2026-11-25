using System.Collections;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Deletes the Drafts of the level's Orphaned Groups, as on a machine that never had their Scripts.
/// </summary>
internal sealed class DeleteDraftsStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        var withDraft = context.Window.Orphans.Count(o => o.Draft != null);
        if (withDraft == 0)
        {
            context.Fail("no Orphaned Group has a Draft", "a Draft", context.DescribeOrphans());
            yield break;
        }

        context.Window.DeleteOrphanDrafts();
        yield return null;
        yield return context.WaitForWindowDrawn();
        if (context.Failed)
            yield break;

        if (context.Window.Orphans.Any(o => o.Draft != null))
            context.Fail("an Orphaned Group still has a Draft", "none", context.DescribeOrphans());
        else
            context.Detail = $"deleted {withDraft}, leaving {context.DescribeOrphans()}";
    }
}
