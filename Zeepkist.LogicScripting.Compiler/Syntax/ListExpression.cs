using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed class ListExpression : Expression
{
    public ListExpression(Position position, List<Expression> items)
        : base(position, items)
    {
        Items = items;
    }

    public List<Expression> Items { get; }
}
