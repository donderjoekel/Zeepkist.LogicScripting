namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// The Script after <see cref="NameRename.Rename"/>, or why it's left as it was.
/// </summary>
public sealed class RenameResult
{
    private RenameResult(string text, int caret, int count, string error)
    {
        Text = text;
        Caret = caret;
        Count = count;
        Error = error;
    }

    /// <summary>
    /// The renamed Script; the Script as it was when the rename is refused or changes nothing.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Where the caret goes: in the same place relative to the text around it, at the end of the new name when it was
    /// in the old one.
    /// </summary>
    public int Caret { get; }

    /// <summary>
    /// How many places the name was changed in.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Why the rename was refused, or null.
    /// </summary>
    public string Error { get; }

    public bool Succeeded => Error == null;

    internal static RenameResult Renamed(string text, int caret, int count)
    {
        return new RenameResult(text, caret, count, null);
    }

    internal static RenameResult Refused(string text, int caret, string error)
    {
        return new RenameResult(text, caret, 0, error);
    }
}
