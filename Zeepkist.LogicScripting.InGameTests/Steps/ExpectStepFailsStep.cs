using System;
using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Runs another step and passes when it fails for the given reason. An exception still fails the scenario.
/// Writes <c>{uid:&lt;name&gt;}</c> in the reason as that block's UID, for a message that names blocks by it.
/// </summary>
internal sealed class ExpectStepFailsStep : ScenarioStep
{
    public ExpectStepFailsStep(ScenarioStep step, string reason)
    {
        Step = step;
        Reason = reason;
    }

    public ScenarioStep Step { get; }
    public string Reason { get; }

    public override IEnumerator Run(StepContext context)
    {
        var reason = context.PlacedBlocks.Expand(Reason, out var unknown);
        if (reason == null)
        {
            context.FailUnknownUid(unknown);
            yield break;
        }

        yield return Step.Run(context);

        var failed = context.Failed;
        var detail = context.Detail;
        context.Reset();
        if (!failed)
            context.Fail($"the step passed: {detail}", reason, "(passed)");
        else if (detail.IndexOf(reason, StringComparison.Ordinal) < 0)
            context.Fail("the step failed for another reason", reason, detail);
        else
            context.Pass(detail, reason, detail);
    }
}
