using System;
using System.Collections.Generic;
using System.Globalization;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Where a block is, how it's turned and how big, as the level saves it. Holds plain numbers, so it's testable without
/// the game.
/// </summary>
internal sealed class BlockPose
{
    // Positions far from the origin are only this exact
    public const float Tolerance = 0.001f;

    public BlockPose((float X, float Y, float Z) position, (float X, float Y, float Z) rotation, (float X, float Y, float Z) scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    public (float X, float Y, float Z) Position { get; }

    /// <summary>
    /// In degrees.
    /// </summary>
    public (float X, float Y, float Z) Rotation { get; }

    public (float X, float Y, float Z) Scale { get; }

    public static BlockPose Read(BlockProperties block)
    {
        var json = block.ConvertBlockToJSON_v15();
        return new BlockPose((json.p.x, json.p.y, json.p.z), (json.r.x, json.r.y, json.r.z), (json.s.x, json.s.y, json.s.z));
    }

    public IReadOnlyList<string> FindChanges(BlockPose other)
    {
        List<string> changes = [];
        if (!Matches(Position, other.Position, Difference))
            changes.Add("position");

        if (!Matches(Rotation, other.Rotation, AngleDifference))
            changes.Add("rotation");

        if (!Matches(Scale, other.Scale, Difference))
            changes.Add("scale");

        return changes;
    }

    public override string ToString()
    {
        return $"position {Describe(Position)}, rotation {Describe(Rotation)}, scale {Describe(Scale)}";
    }

    private static bool Matches((float X, float Y, float Z) a, (float X, float Y, float Z) b, Func<float, float, float> difference)
    {
        return difference(a.X, b.X) <= Tolerance && difference(a.Y, b.Y) <= Tolerance && difference(a.Z, b.Z) <= Tolerance;
    }

    private static float Difference(float a, float b)
    {
        return Math.Abs(a - b);
    }

    /// <summary>
    /// The short way around, as 0 and 360 degrees are the same.
    /// </summary>
    private static float AngleDifference(float a, float b)
    {
        var difference = Math.Abs(a - b) % 360;
        return Math.Min(difference, 360 - difference);
    }

    private static string Describe((float X, float Y, float Z) value)
    {
        return $"({Number(value.X)}, {Number(value.Y)}, {Number(value.Z)})";
    }

    private static string Number(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
