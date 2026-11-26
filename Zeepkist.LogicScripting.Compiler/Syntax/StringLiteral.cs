namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed class StringLiteral : Expression
{
    public StringLiteral(Position position, string value)
        : base(position, [])
    {
        Value = value;
    }

    public string Value { get; }
}
