namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

public sealed class ReferenceRow
{
    public ReferenceRow(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public string Value { get; }
}
