using System.Collections.Generic;
using System.Globalization;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed partial class Parser
{
    private Expression ParseExpression()
    {
        return ParseBinary(0);
    }

    /// <summary>
    /// Precedence climbing over C's binary precedence levels; every operator is left-associative.
    /// </summary>
    private Expression ParseBinary(int minPrecedence)
    {
        var left = ParseUnary();
        while (Current.BinaryOperator is { } op && op.Precedence >= minPrecedence)
        {
            var token = Current;
            _index++;
            if (Current.Kind == TokenKind.NewLine)
                throw DanglingOperator(Current);

            var right = ParseBinary(op.Precedence + 1);
            left = CheckDepth(new BinaryExpression(token.Position, op, left, right), token);
        }

        return left;
    }

    private Expression ParseUnary()
    {
        var token = Current;
        if (++_depth > MaxDepth)
            throw TooDeep(token);

        try
        {
            return CheckDepth(ParseUnaryUnchecked(), token);
        }
        finally
        {
            _depth--;
        }
    }

    private Expression ParseUnaryUnchecked()
    {
        var token = Current;
        if (token.UnaryOperator is not { } op)
            return ParsePostfix();

        _index++;

        if (op.SignsInteger && IsTouchingInteger(token, Current))
            return ParseInteger(Current, token, op.Symbol);

        return new UnaryExpression(token.Position, op, ParseUnary());
    }

    /// <summary>
    /// The parser's own recursion only bounds how deep the parentheses go; a long run of operators builds a deep
    /// expression in a loop.
    /// </summary>
    private static Expression CheckDepth(Expression expression, Token token)
    {
        if (expression.Depth > MaxDepth)
            throw TooDeep(token);

        return expression;
    }

    private static bool IsTouchingInteger(Token sign, Token next)
    {
        return next.Kind == TokenKind.Integer && next.IsDirectlyAfter(sign);
    }

    private Expression ParsePostfix()
    {
        var expression = ParsePrimary();
        while (Current.Kind == TokenKind.Dot)
        {
            var beforeDot = Peek(-1);
            var dot = Current;
            _index++;
            if (expression is not (NameExpression or CallExpression))
                throw Error(dot, "Only names and calls have outputs to pick with .");

            if (!dot.IsDirectlyAfter(beforeDot))
                throw Error(dot, $"Write the dot right after '{beforeDot.Text}', without a space");

            var output = Current;
            Expect(TokenKind.Name, "an output name");
            if (!output.IsDirectlyAfter(dot))
                throw Error(dot, $"Write '{output.Text}' right after the dot, without a space");

            expression = expression is NameExpression library && Current.Kind == TokenKind.OpenParen
                ? ParseCall(library, output)
                : new MemberExpression(output.Position, expression, output.Text);
        }

        return expression;
    }

    private Expression ParsePrimary()
    {
        var token = Current;
        switch (token.Kind)
        {
            case TokenKind.Integer:
                return ParseInteger(token, token, string.Empty);

            case TokenKind.String:
                _index++;
                return new StringLiteral(token.Position, token.Text);

            case TokenKind.OpenBracket:
                return ParseList();

            case TokenKind.Name:
                _index++;
                if (Current.Kind == TokenKind.OpenParen)
                    return ParseCall(null, token);

                return new NameExpression(token.Position, token.Text);

            case TokenKind.OpenParen:
                ExpectOpenParen();
                var inner = ParseExpression();
                Expect(TokenKind.CloseParen, "')'");
                return inner;

            default:
                throw Error(token, $"Expected a value, found {Describe(token)}");
        }
    }

    /// <param name="digits">The integer token.</param>
    /// <param name="start">Where the literal starts: the digits, or the sign before them.</param>
    /// <param name="sign">The sign's symbol, or empty.</param>
    private IntegerLiteral ParseInteger(Token digits, Token start, string sign)
    {
        _index++;
        var text = sign + digits.Text;
        if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            throw Error(start, $"{text} doesn't fit in a Signal");

        return new IntegerLiteral(start.Position, value);
    }

    /// <summary>
    /// <paramref name="library"/> is the name before the dot of a qualified call, <c>timing.edge(x)</c>, or null.
    /// </summary>
    private CallExpression ParseCall(NameExpression library, Token function)
    {
        var position = library?.Position ?? function.Position;
        ExpectOpenParen();
        List<Expression> arguments = [];
        List<NamedArgument> namedArguments = [];
        if (Current.Kind == TokenKind.CloseParen)
        {
            _index++;
            return new CallExpression(position, library?.Name, function.Text, arguments, namedArguments);
        }

        while (true)
        {
            if (IsNamedArgument(0))
            {
                namedArguments.Add(ParseNamedArgument());
            }
            else
            {
                var start = Current;
                var argument = ParseExpression();
                if (namedArguments.Count > 0)
                    throw Error(start, "Positional inputs go before named arguments");

                arguments.Add(argument);
            }

            if (Current.Kind == TokenKind.Comma)
            {
                _index++;
                continue;
            }

            Expect(TokenKind.CloseParen, "',' or ')'");
            return new CallExpression(position, library?.Name, function.Text, arguments, namedArguments);
        }
    }

    private ListExpression ParseList()
    {
        var open = Current;
        _index++;
        List<Expression> items = [];
        if (Current.Kind == TokenKind.CloseBracket)
        {
            _index++;
            return new ListExpression(open.Position, items);
        }

        while (true)
        {
            items.Add(ParseExpression());
            if (Current.Kind == TokenKind.Comma)
            {
                _index++;
                continue;
            }

            Expect(TokenKind.CloseBracket, "',' or ']'");
            return new ListExpression(open.Position, items);
        }
    }

    private bool IsNamedArgument(int offset)
    {
        return Peek(offset).Kind == TokenKind.Name && Peek(offset + 1).Kind == TokenKind.Colon;
    }

    private NamedArgument ParseNamedArgument()
    {
        var name = Current;
        _index += 2;
        var valueStart = Current;
        var value = ParseExpression();
        return new NamedArgument(name.Position, name.Text, value, valueStart.Position);
    }

    private void ExpectOpenParen()
    {
        if (Current.IsUnclosed)
            throw Error(Current, "'(' isn't closed");

        Expect(TokenKind.OpenParen, "'('");
    }
}
