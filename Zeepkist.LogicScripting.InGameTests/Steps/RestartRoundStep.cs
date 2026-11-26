using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Restarts the round in test mode and waits until it starts again, with the level still the one that was running.
/// </summary>
internal sealed class RestartRoundStep : ScenarioStep
{
    private const float StartSeconds = 60;

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireTestMode())
            yield break;

        var roundsBefore = context.Game.RoundStarts;
        var scenesBefore = context.Game.ScenesLoaded;
        yield return TestModeControls.Restart();
        yield return context.WaitUntil(() => context.Game.RoundStarts > roundsBefore, StartSeconds);
        if (context.Game.RoundStarts == roundsBefore)
        {
            context.Fail($"the round didn't start again within {StartSeconds} seconds");
            yield break;
        }

        // A level loaded anew starts clear whatever its blocks do on a restart, so it would show nothing about one
        var scenesLoaded = context.Game.ScenesLoaded - scenesBefore;
        if (scenesLoaded != 0)
            context.Fail("the game loaded the level anew instead of restarting it", "0 scenes loaded", $"{scenesLoaded} scenes loaded");
    }
}
