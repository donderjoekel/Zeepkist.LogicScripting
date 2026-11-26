using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class ExpectLibraryReadFailedStep : ScenarioStep
{
    public ExpectLibraryReadFailedStep(bool shown)
    {
        Shown = shown;
    }

    public bool Shown { get; }

    public override IEnumerator Run(StepContext context)
    {
        return WarningCheck.Run(
            context,
            Shown,
            "the libraries.json warning",
            w => w.LibraryReadFailedLayout,
            "the Libraries tab wasn't drawn: add show-tab libraries first, with the Scripts side shown");
    }
}
