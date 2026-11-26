using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// Recursive-descent parser for one statement per line, and for Functions, which wrap lines of their own. A syntax error
/// skips the rest of its line, so every line still gets checked.
/// </summary>
internal sealed partial class Parser
{
    /// <summary>
    /// How deep an expression may nest, counting operators, calls, lists and parentheses; far more than a Script needs,
    /// and shallow enough that parsing and compiling it can't overflow the stack.
    /// </summary>
    public const int MaxDepth = 50;

    private readonly List<Token> _tokens;
    private readonly List<Diagnostic> _diagnostics;
    private int _index;
    private int _depth;

    private Parser(List<Token> tokens, List<Diagnostic> diagnostics)
    {
        _tokens = tokens;
        _diagnostics = diagnostics;
    }

    private Token Current => _tokens[_index];

    public static List<Statement> Parse(List<Token> tokens, List<Diagnostic> diagnostics)
    {
        return new Parser(tokens, diagnostics).ParseScript();
    }

    private List<Statement> ParseScript()
    {
        List<Statement> statements = [];

        while (Current.Kind != TokenKind.End)
        {
            if (Current.Kind == TokenKind.NewLine)
            {
                _index++;
            }
            else if (IsFunctionStart())
            {
                var function = ParseFunction();
                if (function != null)
                    statements.Add(function);
            }
            else if (IsEndLine())
            {
                _diagnostics.Add(new Diagnostic(Current.Position, $"'{FunctionDefinition.EndKeyword}' has no function to close"));
                _index++;
            }
            else
            {
                ParseLine(statements);
            }
        }

        return statements;
    }

    private void ParseLine(List<Statement> statements)
    {
        try
        {
            statements.Add(ParseStatement());
            if (Current.Kind != TokenKind.NewLine && Current.Kind != TokenKind.End)
                throw Error(Current, $"Expected the end of the line, found {Describe(Current)}");
        }
        catch (SyntaxError error)
        {
            _diagnostics.Add(error.Diagnostic);
            SkipLine();
            if (error.ContinuesOnNextLine)
                SkipContinuationLine();
        }
    }

    private Statement ParseStatement()
    {
        if (IsDefinitionStart())
        {
            var name = Current;
            _index += 2;
            return new Definition(name.Position, name.Text, ParseExpression());
        }

        if (Current.Kind == TokenKind.Name && Current.Text == DefaultStatement.Keyword && IsNamedArgument(1))
        {
            var keyword = Current;
            _index++;
            return new DefaultStatement(keyword.Position, ParseNamedArgument());
        }

        return new ExpressionStatement(ParseExpression());
    }

    private void Expect(TokenKind kind, string expected)
    {
        if (Current.Kind != kind)
            throw Error(Current, $"Expected {expected}, found {Describe(Current)}");

        _index++;
    }

    private Token Peek(int offset)
    {
        return _tokens[Math.Min(_index + offset, _tokens.Count - 1)];
    }
}
