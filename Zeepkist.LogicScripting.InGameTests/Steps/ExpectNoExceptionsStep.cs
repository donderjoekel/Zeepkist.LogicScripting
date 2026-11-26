using System.Collections;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class ExpectNoExceptionsStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        // Unity logs an exception from the frame it's thrown in, so one from the step before has been logged by now
        yield return null;
        var logged = context.Exceptions.TakeSinceCheck();
        if (logged.Count == 0)
        {
            context.Pass("no exceptions", "0", "0");
            yield break;
        }

        var count = logged.Sum(e => e.Count);
        var listed = string.Join("; ", logged.ConvertAll(e => e.ToString()));
        context.Fail(count == 1 ? "1 exception logged" : $"{count} exceptions logged", "0", $"{count}: {listed}");
    }
}
