namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// A selected block that stays in the level as it is, with the Script referring to it, because it stores something a
/// Script can't write.
/// </summary>
public sealed class KeptBlock
{
    public KeptBlock(string uid, string reason)
    {
        Uid = uid;
        Reason = reason;
    }

    public string Uid { get; }

    /// <summary>
    /// Names the block and what a Script can't write, on one line.
    /// </summary>
    public string Reason { get; }
}
