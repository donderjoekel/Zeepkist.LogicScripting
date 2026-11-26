using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

internal static class ReferenceText
{
    public const string None = "–";

    public static string JoinNames(IEnumerable<string> names)
    {
        var joined = string.Join(", ", names);
        return joined.Length == 0 ? None : joined;
    }

    public static bool Contains(string text, string filter)
    {
        return text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
