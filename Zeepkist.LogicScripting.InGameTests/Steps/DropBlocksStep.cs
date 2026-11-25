using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Presses the editor's grab key again, which lets go of the grabbed blocks where they are, as one undo step.
/// </summary>
internal sealed class DropBlocksStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        if (!central.gizmos.isGrabbing)
        {
            context.Fail("no blocks are grabbed: add grab-blocks or paste-blocks first");
            yield break;
        }

        var before = central.undoRedo.currentHistoryPosition;
        yield return context.PressEditorKey(EditorKey.Grab);
        if (context.Failed)
            yield break;

        var after = central.undoRedo.currentHistoryPosition;
        if (central.gizmos.isGrabbing)
            context.Fail("the editor didn't let go of the grabbed blocks");
        else if (after == before)
            context.Fail("letting go of the blocks made no undo step");
        else
            context.Detail = $"history position {before} → {after}";
    }
}
