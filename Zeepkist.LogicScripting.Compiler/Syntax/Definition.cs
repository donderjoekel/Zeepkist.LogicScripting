namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed class Definition : Statement
{
    public Definition(Position position, string name, Expression value)
        : base(position)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }
    public Expression Value { get; }
}
