using System.Collections;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Remembers where the named blocks are, for <see cref="ExpectBlocksUnmovedStep"/>.
/// </summary>
internal sealed class RememberBlocksStep : ScenarioStep
{
    public RememberBlocksStep(IReadOnlyList<string> names)
    {
        Names = names;
    }

    public IReadOnlyList<string> Names { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        Dictionary<string, BlockPose> poses = [];
        List<string> described = [];
        foreach (var name in Names)
        {
            var named = context.RequireNamedBlock(central, name);
            if (named == null)
                yield break;

            poses[name] = BlockPose.Read(named.Block);
            described.Add($"\"{name}\" {poses[name]}");
        }

        // Only once every block was found, so a failing step remembers none
        foreach (var pose in poses)
        {
            context.RememberedPoses[pose.Key] = pose.Value;
        }

        context.Detail = string.Join(", ", described);
    }
}
