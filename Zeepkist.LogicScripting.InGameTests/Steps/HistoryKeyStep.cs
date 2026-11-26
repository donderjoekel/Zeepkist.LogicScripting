using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal abstract class HistoryKeyStep : ScenarioStep
{
    private readonly EditorKey _key;

    protected HistoryKeyStep(EditorKey key)
    {
        _key = key;
    }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        var before = central.undoRedo.currentHistoryPosition;
        yield return context.PressEditorKey(_key);
        if (context.Failed)
            yield break;

        var after = central.undoRedo.currentHistoryPosition;
        if (after == before)
            context.Fail($"nothing to {_key.ToString().ToLowerInvariant()}, or the editor was busy");
        else
            context.Detail = $"history position {before} → {after}";
    }
}
