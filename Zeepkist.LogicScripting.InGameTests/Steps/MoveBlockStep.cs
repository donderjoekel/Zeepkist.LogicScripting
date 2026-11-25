using System.Collections;
using TNRD.Zeepkist.LogicScripting.Blocks;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Moves a named block by an offset, as one undo step.
/// </summary>
internal sealed class MoveBlockStep : ScenarioStep
{
    private const string UndoLabel = "In-game test: move block";

    public MoveBlockStep(string name, float x, float y, float z)
    {
        Name = name;
        X = x;
        Y = y;
        Z = z;
    }

    public string Name { get; }
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        var named = context.RequireNamedBlock(central, Name);
        if (named == null)
            yield break;

        var before = BlockPose.Read(named.Block);
        BlockBatch.Change(central, [named.Block], b => b.transform.position += new Vector3(X, Y, Z), UndoLabel);
        yield return null;

        var after = BlockPose.Read(named.Block);
        var wanted = (before.Position.X + X, before.Position.Y + Y, before.Position.Z + Z);
        var expected = new BlockPose(wanted, before.Rotation, before.Scale);
        if (expected.FindChanges(after).Count > 0)
            context.Fail("the block didn't keep the move", expected.ToString(), after.ToString());
        else
            context.Pass($"\"{Name}\" moved", expected.ToString(), after.ToString());
    }
}
