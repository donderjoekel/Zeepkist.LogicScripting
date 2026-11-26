namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed class ExpressionStatement : Statement
{
    public ExpressionStatement(Expression expression)
        : base(expression.Position)
    {
        Expression = expression;
    }

    public Expression Expression { get; }
}
