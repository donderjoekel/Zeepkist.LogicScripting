using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed partial class Parser
{
    /// <summary>
    /// Whether a line read from <paramref name="index"/> starts a definition or a Function, or is a Function's
    /// <c>end</c>. It doesn't check that a line starts there. Each check reads the token after a name, which is
    /// always there, since the tokens end with <see cref="TokenKind.End"/>.
    /// </summary>
    public static bool StartsStatement(List<Token> tokens, int index)
    {
        return IsDefinitionStart(tokens, index) || IsFunctionStart(tokens, index) || IsEndLine(tokens, index);
    }

    private bool IsDefinitionStart()
    {
        return IsDefinitionStart(_tokens, _index);
    }

    private static bool IsDefinitionStart(List<Token> tokens, int index)
    {
        return tokens[index].Kind == TokenKind.Name && tokens[index + 1].Kind == TokenKind.Equals;
    }

    private void SkipLine()
    {
        _index = FindLineEnd(_tokens, _index);
    }

    private static int FindLineEnd(List<Token> tokens, int index)
    {
        while (tokens[index].Kind != TokenKind.NewLine && tokens[index].Kind != TokenKind.End)
        {
            index++;
        }

        return index;
    }

    /// <summary>
    /// Skips the line after a statement that broke off mid-expression, since it most likely holds the rest of that
    /// expression and would only add a second error. A line that starts a definition or a Function, or ends one, is
    /// kept.
    /// </summary>
    private void SkipContinuationLine()
    {
        if (Current.Kind != TokenKind.NewLine)
            return;

        _index++;
        if (StartsStatement(_tokens, _index))
            return;

        SkipLine();
    }

    private static string Describe(Token token)
    {
        return token.Kind switch
        {
            TokenKind.NewLine => "the end of the line",
            TokenKind.End => "the end of the Script",
            TokenKind.String => $"\"{token.Text}\"",
            _ => $"'{token.Text}'"
        };
    }

    private static SyntaxError Error(Token token, string message)
    {
        return new SyntaxError(new Diagnostic(token.Position, message), false);
    }

    private static SyntaxError TooDeep(Token token)
    {
        return Error(token, $"This nests more than {MaxDepth} deep. Split it into definitions");
    }

    /// <param name="lineEnd">The line break right after a binary operator.</param>
    private static SyntaxError DanglingOperator(Token lineEnd)
    {
        var message = "Expected a value, found the end of the line. To continue on the next line, put the expression in ( )";
        return new SyntaxError(new Diagnostic(lineEnd.Position, message), true);
    }
}
