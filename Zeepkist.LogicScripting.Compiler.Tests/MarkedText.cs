using TNRD.Zeepkist.LogicScripting.Compiler.Editing;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// A text with the caret marked by <c>|</c>. Only the first <c>|</c> is the caret; later ones are the OR operator.
/// </summary>
internal static class MarkedText
{
    /// <summary>
    /// The text without its caret mark, and the caret where the mark was. Throws when there is no mark.
    /// </summary>
    public static TextSnapshot Parse(string marked)
    {
        var caret = marked.IndexOf('|');
        return new TextSnapshot(marked.Remove(caret, 1), caret);
    }

    /// <summary>
    /// The text with <c>|</c> at its caret. The selection is left out.
    /// </summary>
    public static string Format(TextSnapshot snapshot)
    {
        return snapshot.Text.Insert(snapshot.Caret, "|");
    }
}
