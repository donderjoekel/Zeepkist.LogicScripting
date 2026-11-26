using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Compiles a Script unturned, with no block turned off.
/// </summary>
internal static class TestCompile
{
    /// <summary>
    /// Compiles with the Libraries by name and the level's placed blocks by uid, each none when null.
    /// </summary>
    public static CompileResult Compile(
        string text,
        Dictionary<string, string> libraries = null,
        Dictionary<string, PlacedBlock> level = null)
    {
        return ScriptCompiler.Compile(text, 0, [], uid => level?.GetValueOrDefault(uid), name => libraries?.GetValueOrDefault(name));
    }
}
