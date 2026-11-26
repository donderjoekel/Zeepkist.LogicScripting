using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Looks up Block Reference entries and reads their rows, for tests.
/// </summary>
internal static class TestReferences
{
    /// <summary>
    /// The Block Reference's one entry for the Block Function.
    /// </summary>
    public static BlockReferenceEntry Find(string function)
    {
        return BlockReference.Categories.SelectMany(c => c.Entries).Single(e => e.Function == function);
    }

    public static List<(string, string)> Rows(IReadOnlyList<ReferenceRow> rows)
    {
        return rows.Select(r => (r.Name, r.Value)).ToList();
    }
}
