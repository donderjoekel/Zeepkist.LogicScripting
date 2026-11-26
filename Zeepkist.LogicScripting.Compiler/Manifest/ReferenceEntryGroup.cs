using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

public sealed class ReferenceEntryGroup
{
    public ReferenceEntryGroup(string heading, IReadOnlyList<ReferenceRow> rows)
    {
        Heading = heading;
        Rows = rows;
    }

    public string Heading { get; }

    public IReadOnlyList<ReferenceRow> Rows { get; }
}
