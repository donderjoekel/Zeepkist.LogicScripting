using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// The text an import wrote, and which part of it stands for which block.
/// </summary>
internal sealed class ImportedText
{
    public ImportedText(
        IReadOnlyList<ImportLine> lines,
        IReadOnlyDictionary<(Position Position, BlockType Type), string> places,
        IReadOnlyDictionary<string, string> shared)
    {
        Lines = lines;
        Places = places;
        Shared = shared;
    }

    public IReadOnlyList<ImportLine> Lines { get; }

    /// <summary>
    /// The UID of the block each place in the text stands for. The type tells apart the two blocks of <c>-x</c>, which
    /// are both at the sign.
    /// </summary>
    public IReadOnlyDictionary<(Position Position, BlockType Type), string> Places { get; }

    /// <summary>
    /// The Generators the text leaves out, each with the UID of the one that stands for it, as a literal's Generator is
    /// shared.
    /// </summary>
    public IReadOnlyDictionary<string, string> Shared { get; }

    public string Text => string.Join("\n", Lines.Select(l => l.Text));
}
