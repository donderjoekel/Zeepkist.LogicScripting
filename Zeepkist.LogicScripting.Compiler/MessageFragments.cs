using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// Wording that several errors and window texts share, so that rewording a rule changes every copy.
/// </summary>
public static class MessageFragments
{
    public const string OnlyDelayCanReferAhead = "only delay(...) can refer ahead";

    public const string InBlockGroup = "in a Block Group, which a recompile replaces";

    public const string TurnedOff = "doesn't match this game version and is turned off";

    /// <summary>
    /// The count and the noun, with an s unless the count is 1: <c>1 error</c>, <c>3 errors</c>.
    /// </summary>
    public static string Count(int count, string noun)
    {
        return count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    }

    public static string DescribeTurnedOff(BlockType type, BlockStyle style)
    {
        return $"'{type.Describe(style)}' {TurnedOff}";
    }
}
