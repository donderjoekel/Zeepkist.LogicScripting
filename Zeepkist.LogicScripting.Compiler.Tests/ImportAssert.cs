using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Checks on importing blocks selected in an <see cref="ImportLevel"/>.
/// </summary>
internal static class ImportAssert
{
    /// <summary>
    /// The selected blocks import into the text, none of them both imported and kept, and compiling the text over the
    /// imported blocks gives as many blocks.
    /// </summary>
    public static ImportResult Imports(ImportLevel level, string text, params string[] selected)
    {
        var import = level.Import(selected);

        Assert.Null(import.Refusal);
        Assert.Equal(text.Replace("\r\n", "\n"), import.Text);
        Assert.DoesNotContain(import.Kept, k => import.ImportedUids.Contains(k.Uid));
        var compiled = level.Compile(import);
        Assert.Empty(compiled.Errors);
        Assert.Equal(import.ImportedUids.Count, compiled.Blocks.Count);
        return import;
    }

    /// <summary>
    /// The import of the selected blocks is refused, and imports and keeps nothing.
    /// </summary>
    public static void Refused(ImportLevel level, string refusal, params string[] selected)
    {
        var import = level.Import(selected);

        Assert.Equal(refusal, import.Refusal);
        Assert.Null(import.Text);
        Assert.Empty(import.ImportedUids);
        Assert.Empty(import.Kept);
    }
}
