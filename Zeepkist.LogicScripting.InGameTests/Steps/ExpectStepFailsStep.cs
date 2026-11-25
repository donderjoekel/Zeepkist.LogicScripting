using System;
using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Runs another step and passes when it fails for the given reason. An exception still fails the scenario.
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
        yield return Step.Run(context);

        var failed = context.Failed;
        var detail = context.Detail;
        context.Reset();
        if (!failed)
            context.Fail($"the step passed: {detail}", Reason, "(passed)");
        else if (detail.IndexOf(Reason, StringComparison.Ordinal) < 0)
            context.Fail("the step failed for another reason", Reason, detail);
        else
            context.Pass(detail, Reason, detail);
    }
}
