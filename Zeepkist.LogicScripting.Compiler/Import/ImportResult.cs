using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// The text of the Script that blocks import into, or why they can't be imported.
/// </summary>
public sealed class ImportResult
{
    private ImportResult(
        string text,
        IReadOnlyCollection<string> importedUids,
        IReadOnlyList<KeptBlock> kept,
        string refusal)
    {
        Text = text;
        ImportedUids = importedUids;
        Kept = kept;
        Refusal = refusal;
    }

    /// <summary>
    /// Null when the import is refused.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// The UIDs of the selected blocks that become the Script's Block Group, none when the import is refused.
    /// </summary>
    public IReadOnlyCollection<string> ImportedUids { get; }

    /// <summary>
    /// The selected blocks of types a Script can create that stay outside it, none when the import is refused.
    /// </summary>
    public IReadOnlyList<KeptBlock> Kept { get; }

    /// <summary>
    /// Which block or Wire is the reason, or null when the import isn't refused.
    /// </summary>
    public string Refusal { get; }

    public bool Succeeded => Refusal == null;

    public static ImportResult Written(string text, IReadOnlyCollection<string> importedUids, IReadOnlyList<KeptBlock> kept)
    {
        return new ImportResult(text, importedUids, kept, null);
    }

    public static ImportResult Refused(string refusal)
    {
        return new ImportResult(null, [], [], refusal);
    }
}
