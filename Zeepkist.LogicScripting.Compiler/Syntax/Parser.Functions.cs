using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed partial class Parser
{
    /// <summary>
    /// A Function's first line, its body and its <c>end</c>; null when the first line has no name to call it by.
    /// </summary>
    private FunctionDefinition ParseFunction()
    {
        var keyword = Current;
        var bodyEnd = FindBodyEnd(_tokens, _index);
        var firstLine = ParseFirstLine();
        var name = firstLine.Name;
        var isComplete = firstLine.Error == null;
        if (!isComplete)
            _diagnostics.Add(firstLine.Error);

        // A line never runs past a line break, and a skipped continuation line is never a function or end line, so this
        // stops right at the body's end
        List<Statement> body = [];
        while (_index < bodyEnd.Index)
        {
            if (Current.Kind == TokenKind.NewLine)
                _index++;
            else
                ParseLine(body);
        }

        var shownName = name == null ? "This function" : $"Function '{name.Name}'";
        switch (bodyEnd.Reason)
        {
            case FunctionBodyEndReason.EndLine:
                _index++;
                break;
            case FunctionBodyEndReason.NextFunction:
                var message = $"{shownName} has no {FunctionDefinition.EndKeyword} before this one. Functions can't be defined inside each other";
                _diagnostics.Add(new Diagnostic(Current.Position, message));
                isComplete = false;
                break;
            case FunctionBodyEndReason.EndOfScript:
                _diagnostics.Add(new Diagnostic(keyword.Position, $"{shownName} has no {FunctionDefinition.EndKeyword}"));
                isComplete = false;
                break;
            default:
                throw new InvalidOperationException($"No diagnostic for a body ending at {bodyEnd.Reason}");
        }

        return name == null ? null : new FunctionDefinition(keyword.Position, name, firstLine.Inputs, firstLine.Outputs, body, isComplete);
    }

    /// <summary>
    /// Where and why the body of the Function at <paramref name="keyword"/> ends. Indentation is only style, so the
    /// body ends at the first <c>end</c> line. A Function can't be defined inside another, so a <c>function</c> line
    /// before it ends this one, to be read as the next Function. It parses nothing, so the editor can find bodies with
    /// it on each key press.
    /// </summary>
    public static FunctionBodyEnd FindBodyEnd(List<Token> tokens, int keyword)
    {
        var index = FindLineEnd(tokens, keyword);
        while (tokens[index].Kind != TokenKind.End)
        {
            if (tokens[index].Kind == TokenKind.NewLine)
                index++;
            else if (IsFunctionStart(tokens, index))
                return new FunctionBodyEnd(index, FunctionBodyEndReason.NextFunction);
            else if (IsEndLine(tokens, index))
                return new FunctionBodyEnd(index, FunctionBodyEndReason.EndLine);
            else
                index = FindLineEnd(tokens, index);
        }

        return new FunctionBodyEnd(index, FunctionBodyEndReason.EndOfScript);
    }

    /// <summary>
    /// The first line from the <c>function</c> keyword at <paramref name="keyword"/>, read the way
    /// <see cref="Parse"/> reads it.
    /// </summary>
    public static FunctionFirstLine ParseFunctionFirstLine(List<Token> tokens, int keyword)
    {
        return new Parser(tokens, []) { _index = keyword }.ParseFirstLine();
    }

    /// <summary>
    /// Reads from the keyword to the end of the line, even past a syntax error.
    /// </summary>
    private FunctionFirstLine ParseFirstLine()
    {
        _index++;
        FunctionParameter name = null;
        List<FunctionParameter> inputs = [];
        List<FunctionParameter> outputs = [];
        try
        {
            name = ParseParameter("the function's name");
            ExpectOpenParen();
            if (Current.Kind == TokenKind.CloseParen)
                _index++;
            else
                ParseParameters(inputs, "an input name", TokenKind.CloseParen, "',' or ')'");

            if (Current.Kind == TokenKind.Arrow)
            {
                _index++;
                ParseParameters(outputs, "an output name", null, "',' or the end of the line");
            }
            else if (Current.Kind != TokenKind.NewLine && Current.Kind != TokenKind.End)
            {
                throw Error(Current, $"Expected '->' or the end of the line, found {Describe(Current)}");
            }

            return new FunctionFirstLine(name, inputs, outputs, null);
        }
        catch (SyntaxError error)
        {
            SkipLine();
            return new FunctionFirstLine(name, inputs, outputs, error.Diagnostic);
        }
    }

    /// <summary>
    /// Names separated by commas, up to <paramref name="close"/>, or to the end of the line when it's null.
    /// </summary>
    private void ParseParameters(List<FunctionParameter> parameters, string expected, TokenKind? close, string expectedAfter)
    {
        while (true)
        {
            parameters.Add(ParseParameter(expected));
            if (Current.Kind == TokenKind.Comma)
            {
                _index++;
                continue;
            }

            if (close == null)
            {
                if (Current.Kind != TokenKind.NewLine && Current.Kind != TokenKind.End)
                    throw Error(Current, $"Expected {expectedAfter}, found {Describe(Current)}");

                return;
            }

            Expect(close.Value, expectedAfter);
            return;
        }
    }

    private FunctionParameter ParseParameter(string expected)
    {
        var token = Current;
        Expect(TokenKind.Name, expected);
        return new FunctionParameter(token.Position, token.Text);
    }

    private bool IsFunctionStart()
    {
        return IsFunctionStart(_tokens, _index);
    }

    /// <summary>
    /// <c>function = ...</c> is a definition, so it gets the reserved-word error instead.
    /// </summary>
    public static bool IsFunctionStart(List<Token> tokens, int index)
    {
        return tokens[index].Kind == TokenKind.Name
            && tokens[index].Text == FunctionDefinition.Keyword
            && tokens[index + 1].Kind != TokenKind.Equals;
    }

    private bool IsEndLine()
    {
        return IsEndLine(_tokens, _index);
    }

    private static bool IsEndLine(List<Token> tokens, int index)
    {
        return tokens[index].Kind == TokenKind.Name
            && tokens[index].Text == FunctionDefinition.EndKeyword
            && tokens[index + 1].Kind is TokenKind.NewLine or TokenKind.End;
    }
}
