using System.Collections;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the named blocks have the position, rotation and scale <see cref="RememberBlocksStep"/> remembered.
/// </summary>
internal sealed class ExpectBlocksUnmovedStep : ScenarioStep
{
    public ExpectBlocksUnmovedStep(IReadOnlyList<string> names)
    {
        Names = names;
    }

    public IReadOnlyList<string> Names { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        List<string> remembered = [];
        List<string> found = [];
        List<string> moved = [];
        foreach (var name in Names)
        {
            if (!context.RememberedPoses.TryGetValue(name, out var pose))
            {
                context.Fail($"\"{name}\" wasn't remembered: add remember-blocks {name} first", name, string.Join(", ", context.RememberedPoses.Keys));
                yield break;
            }

            var named = context.RequireNamedBlock(central, name);
            if (named == null)
                yield break;

            var now = BlockPose.Read(named.Block);
            remembered.Add($"\"{name}\" {pose}");
            found.Add($"\"{name}\" {now}");
            var changes = pose.FindChanges(now);
            if (changes.Count > 0)
                moved.Add($"\"{name}\" has another {string.Join(", ", changes)}");
        }

        var expected = string.Join(", ", remembered);
        var actual = string.Join(", ", found);
        if (moved.Count == 0)
            context.Pass($"{Names.Count} unmoved", expected, actual);
        else
            context.Fail(string.Join(", ", moved), expected, actual);
    }
}
