using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class LoadLevelStep : ScenarioStep
{
    private const float PanelSeconds = 5;
    private const float LoadSeconds = 60;

    public LoadLevelStep(string name, ScenarioStep meanwhile = null)
    {
        Name = name;
        Meanwhile = meanwhile;
    }

    public string Name { get; }

    /// <summary>
    /// Null when nothing runs while the level loads. It starts in the frame the load does, so it's never too late for a
    /// small level.
    /// </summary>
    public ScenarioStep Meanwhile { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        if (!context.SavedLevels.TryGetValue(Name, out var file))
        {
            context.Fail($"no level was saved as \"{Name}\": add save-level {Name} first", Name, string.Join(", ", context.SavedLevels.Keys));
            yield break;
        }

        TestLevels.OpenLoadPanel(central);
        yield return context.WaitUntil(() => TestLevels.IsPanelReady(central), PanelSeconds);
        if (!TestLevels.IsPanelReady(central))
        {
            context.Fail($"the load panel didn't open within {PanelSeconds} seconds");
            yield break;
        }

        var loadsBefore = context.Game.LevelLoads;
        TestLevels.Load(central, file);
        if (!central.saveload.isLoading)
        {
            context.Fail("the editor didn't start loading the level");
            yield break;
        }

        if (Meanwhile != null)
            yield return Meanwhile.Run(context);

        yield return context.WaitUntil(() => TestModeControls.IsLevelBack(context.Game, loadsBefore), LoadSeconds);
        if (!TestModeControls.IsLevelBack(context.Game, loadsBefore))
        {
            context.Fail($"the editor didn't load the level within {LoadSeconds} seconds");
            yield break;
        }

        // The window reads the level's Scripts as it's next drawn
        yield return null;
        yield return null;
        if (Meanwhile == null)
            context.Detail = file.FullName;
    }
}
