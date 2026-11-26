using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Blocks;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class SelectBlocksStep : ScenarioStep
{
    public SelectBlocksStep(IReadOnlyList<string> names)
    {
        Names = names;
    }

    public IReadOnlyList<string> Names { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        List<BlockProperties> blocks = [];
        foreach (var name in Names)
        {
            var named = context.RequireNamedBlock(central, name);
            if (named == null)
                yield break;

            blocks.Add(named.Block);
        }

        BlockGroups.Select(central, blocks);
        yield return null;

        var expected = string.Join(", ", Names);
        var selected = string.Join(", ", central.selection.list.Select(b => context.PlacedBlocks.FindName(b.UID) ?? b.UID));
        // By block, as two names can name the same one
        if (central.selection.list.SequenceEqual(blocks))
            context.Pass($"selected {selected}", expected, selected);
        else
            context.Fail("the editor doesn't have exactly these blocks selected", expected, selected);
    }
}
