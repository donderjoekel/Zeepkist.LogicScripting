using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

public sealed class BlockReferenceCategory
{
    public BlockReferenceCategory(BlockCategory category, IReadOnlyList<BlockReferenceEntry> entries)
    {
        Category = category;
        Entries = entries;
    }

    public BlockCategory Category { get; }

    /// <summary>
    /// Sorted by Block Function name.
    /// </summary>
    public IReadOnlyList<BlockReferenceEntry> Entries { get; }
}
