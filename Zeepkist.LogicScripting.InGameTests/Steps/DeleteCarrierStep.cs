using System.Collections;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Blocks;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Removes the block of a Script's Block Group that carries its text, and its Wires, as one undo step, which leaves what
/// deleting it by hand does.
/// </summary>
internal sealed class DeleteCarrierStep : ScenarioStep
{
    private const string UndoLabel = "In-game test: delete carrier";

    public DeleteCarrierStep(string script)
    {
        Script = script;
    }

    public string Script { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        var scriptId = context.Window.FindScriptId(Script);
        if (scriptId == null)
        {
            context.FailNoScript(Script);
            yield break;
        }

        var group = BlockGroups.Find(central, scriptId);
        var carrier = group.FirstOrDefault(LevelScripts.IsCarrier);
        if (carrier == null)
        {
            context.Fail($"the level has no block that carries \"{Script}\": add compile first", "a block with its text", $"{group.Count} blocks without it");
            yield break;
        }

        var uid = carrier.UID;
        BlockBatch.Apply(central, [], [carrier], [], UndoLabel);
        yield return null;

        if (LevelBlocks.Find(central, uid) == null)
            context.Detail = $"removed block \"{uid}\", leaving {group.Count - 1} of the group";
        else
            context.Fail("the block is still in the level");
    }
}
