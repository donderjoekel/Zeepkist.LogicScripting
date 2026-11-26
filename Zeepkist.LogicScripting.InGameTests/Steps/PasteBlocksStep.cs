using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class PasteBlocksStep : ScenarioStep
{
    public PasteBlocksStep(IReadOnlyList<string> names)
    {
        Names = names;
    }

    public IReadOnlyList<string> Names { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireSelectedBlocks();
        if (central == null)
            yield break;

        var originals = central.selection.list.Select(b => (b.blockID, b.UID)).ToList();
        if (originals.Count != Names.Count)
        {
            context.Fail("paste-blocks takes a name for each selected block", $"{originals.Count} names", $"{Names.Count} names");
            yield break;
        }

        var taken = Names.FirstOrDefault(n => context.PlacedBlocks.TryGetUid(n, out _));
        if (taken != null)
        {
            context.FailNameTaken(taken);
            yield break;
        }

        yield return context.PressEditorKey(EditorKey.Duplicate);
        if (context.Failed)
            yield break;

        var copies = central.selection.list.Select(b => (b.blockID, b.UID)).ToList();
        var pasted = central.gizmos.isGrabbing
            && copies.Select(c => c.blockID).SequenceEqual(originals.Select(o => o.blockID))
            && !copies.Select(c => c.UID).Intersect(originals.Select(o => o.UID)).Any();
        if (!pasted)
        {
            context.Fail("the editor didn't paste and grab a copy of each selected block", string.Join(", ", originals), string.Join(", ", copies));
            yield break;
        }

        for (var i = 0; i < Names.Count; i++)
        {
            context.PlacedBlocks.Remember(Names[i], copies[i].UID);
        }

        context.Detail = $"pasted {string.Join(", ", Names.Zip(copies, (name, copy) => $"\"{name}\" block {copy.blockID}, UID \"{copy.UID}\""))}";
    }
}
