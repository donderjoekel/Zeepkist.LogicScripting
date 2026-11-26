using System.Collections;
using System.Globalization;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the window lists this many Orphaned Groups.
/// </summary>
internal sealed class ExpectOrphansStep : ScenarioStep
{
    public ExpectOrphansStep(int count)
    {
        Count = count;
    }

    public int Count { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        yield return null;
        yield return context.WaitForWindowDrawn();
        if (context.Failed)
            yield break;

        var expected = Count.ToString(CultureInfo.InvariantCulture);
        var actual = context.Window.Orphans.Count.ToString(CultureInfo.InvariantCulture);
        context.Check(context.Window.Orphans.Count == Count, context.DescribeOrphans(), expected, actual);
    }
}
