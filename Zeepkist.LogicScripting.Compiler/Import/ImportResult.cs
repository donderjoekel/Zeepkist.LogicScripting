namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// The text of the Script that blocks import into, or why they can't be imported.
/// </summary>
public sealed class ImportResult
{
    private ImportResult(string text, string refusal)
    {
        Text = text;
        Refusal = refusal;
    }

    /// <summary>
    /// Null when the import is refused.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Which block or Wire is the reason, or null when the import isn't refused.
    /// </summary>
    public string Refusal { get; }

    public bool Succeeded => Refusal == null;

    public static ImportResult Written(string text)
    {
        return new ImportResult(text, null);
    }

    public static ImportResult Refused(string refusal)
    {
        return new ImportResult(null, refusal);
    }
}
