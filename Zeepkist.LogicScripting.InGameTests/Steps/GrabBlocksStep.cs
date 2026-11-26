using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// The grabbed blocks follow the mouse until <see cref="DropBlocksStep"/>.
/// </summary>
internal sealed class GrabBlocksStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireSelectedBlocks();
        if (central == null)
            yield break;

        yield return context.PressEditorKey(EditorKey.Grab);
        if (context.Failed)
            yield break;

        if (central.gizmos.isGrabbing)
            context.Detail = $"grabbed {central.selection.list.Count} blocks";
        else
            context.Fail("the editor didn't grab the selected blocks");
    }
}
