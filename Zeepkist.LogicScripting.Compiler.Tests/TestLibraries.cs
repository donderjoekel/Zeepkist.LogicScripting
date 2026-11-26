using System;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Libraries for tests that look one up.
/// </summary>
internal static class TestLibraries
{
    /// <summary>
    /// A lookup with one Library, <c>timing</c>.
    /// </summary>
    public static Func<string, string> Timing(string text)
    {
        return name => name == "timing" ? text : null;
    }

    /// <summary>
    /// A list with one Library.
    /// </summary>
    public static LibraryList WithLibrary(string name, string text)
    {
        var list = new LibraryList();
        list.SetText(list.Add(name), text);
        return list;
    }
}
