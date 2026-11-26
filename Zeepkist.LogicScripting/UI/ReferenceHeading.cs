using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal readonly struct ReferenceHeading
{
    public const string ClosedSuffix = ":closed";

    public ReferenceHeading(string name, bool open)
    {
        Name = name;
        Open = open;
    }

    public string Name { get; }
    public bool Open { get; }

    /// <summary>
    /// The headings separated by spaces, a closed one with <see cref="ClosedSuffix"/> after its name, as the in-game
    /// tests write them.
    /// </summary>
    public static string Describe(IEnumerable<ReferenceHeading> headings)
    {
        return string.Join(" ", headings.Select(h => h.Open ? h.Name : h.Name + ClosedSuffix));
    }
}
