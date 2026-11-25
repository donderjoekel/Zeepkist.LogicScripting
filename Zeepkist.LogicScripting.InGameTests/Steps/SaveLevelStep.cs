using System.Collections;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Saves the level to a file, as the editor's save panel does, for <see cref="LoadLevelStep"/>.
/// </summary>
internal sealed class SaveLevelStep : ScenarioStep
{
    private const float PanelSeconds = 5;

    public SaveLevelStep(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        central.tool.EnableSavePanel();
        yield return context.WaitUntil(() => TestLevels.IsPanelReady(central), PanelSeconds);
        if (!TestLevels.IsPanelReady(central))
        {
            context.Fail($"the save panel didn't open within {PanelSeconds} seconds");
            yield break;
        }

        var file = TestLevels.Save(central, Name);
        yield return context.WaitUntil(() => !TestLevels.IsPanelOpen(central), PanelSeconds);
        if (file == null)
        {
            context.Fail("the game saved no level file");
            yield break;
        }

        context.SavedLevels[Name] = file;
        context.Detail = $"{file.FullName}, {file.Length} bytes, in a folder with {string.Join(", ", file.Directory.GetFiles().Select(f => f.Name))}";
    }
}
