using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// Where else the name at the caret is written: a name the Script defines, or a Function's input, in the same scope,
/// the top level or one Function's body. Read from the tokens, like <see cref="DefinitionJump"/>, so half-typed text has
/// them too.
/// </summary>
/// <remarks>
/// <see cref="NameUsages"/> reads the parsed Script for rename, and needs one without errors. The editor underlines
/// these while the Script is being typed, so this one reads the tokens instead.
/// </remarks>
public static class NameOccurrences
{
    /// <summary>
    /// Every other place the name the caret is in or right after is written, its definitions included, in text order;
    /// empty when the caret isn't on a name the Script defines in that scope. Words spelled like the name don't count: a
    /// Function's name or a call to it, an output or a Library's Function after a dot, a Library's name, a named
    /// argument's name, or a Setting's option word.
    /// </summary>
    public static IReadOnlyList<TextSpan> At(string text, int caret)
    {
        var tokens = Lexer.Tokenize(text ?? string.Empty, []);
        var name = FindNameAt(tokens, caret);
        if (name < 0)
            return [];

        var functions = DefinedNames.ReadEveryFunction(tokens);
        var firstLines = ReadFirstLines(tokens, functions);
        if (!TryGetScope(tokens, name, functions, firstLines, out var scope))
            return [];

        var spelling = tokens[name].Text;
        var isDefined = DefinedNames.Read(text).Any(n => n.Name == spelling && IsSameScope(n.Function, scope));
        if (!isDefined)
            return [];

        List<TextSpan> usages = [];
        for (var i = 0; i < tokens.Count; i++)
        {
            if (i != name
                && tokens[i].Kind == TokenKind.Name
                && tokens[i].Text == spelling
                && TryGetScope(tokens, i, functions, firstLines, out var other)
                && IsSameScope(other, scope))
            {
                usages.Add(tokens[i].Span);
            }
        }

        return usages;
    }

    /// <summary>
    /// The name token the caret is in or right after; of two, the one that can be a defined name's. -1 when there's
    /// none.
    /// </summary>
    private static int FindNameAt(List<Token> tokens, int caret)
    {
        var found = -1;
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Kind != TokenKind.Name || tokens[i].Span.Start > caret || caret > tokens[i].Span.End)
                continue;

            if (found < 0 || !Lexer.IsAfterDot(tokens, i))
                found = i;
        }

        return found;
    }

    /// <summary>
    /// The Function each first line belongs to, by the index of its keyword, with the index of the token that ends the
    /// line. Matched by where its body starts, which is right after that line.
    /// </summary>
    private static List<(int Start, int End, DefinedFunction Function)> ReadFirstLines(List<Token> tokens, List<DefinedFunction> functions)
    {
        List<(int, int, DefinedFunction)> lines = [];
        for (var i = 0; i + 1 < tokens.Count; i++)
        {
            if (!DefinedNames.IsFunctionLineStart(tokens, i))
                continue;

            var end = Parser.FindLineEnd(tokens, i);
            var function = functions.FirstOrDefault(f => f.Body.Start == tokens[end].Span.End);
            if (function != null)
                lines.Add((i, end, function));
        }

        return lines;
    }

    /// <summary>
    /// Whether the name at <paramref name="index"/> can stand for a defined name or an input, and in which scope: null
    /// for the top level, or the Function whose body or first line it's in.
    /// </summary>
    private static bool TryGetScope(
        List<Token> tokens,
        int index,
        List<DefinedFunction> functions,
        List<(int Start, int End, DefinedFunction Function)> firstLines,
        out DefinedFunction scope)
    {
        var token = tokens[index];
        foreach (var (start, end, function) in firstLines)
        {
            if (index <= start || index >= end)
                continue;

            // The keyword's name is the Function's; after it come its inputs, and after the arrow its outputs
            scope = function;
            return index > start + 1 && !GraphBuilder.IsReserved(token.Text);
        }

        scope = functions.FirstOrDefault(f => f.Contains(token.Span.Start));
        return CanBeDefinedName(tokens, index, functions, scope);
    }

    private static bool CanBeDefinedName(List<Token> tokens, int index, List<DefinedFunction> functions, DefinedFunction scope)
    {
        var token = tokens[index];
        var next = tokens[index + 1].Kind;
        if (GraphBuilder.IsReserved(token.Text) || IsDefaultKeyword(tokens, index) || Lexer.IsAfterDot(tokens, index))
            return false;

        // A named argument's name, or a Setting's name after default
        if (next == TokenKind.Colon)
            return false;

        // timing in timing.edge(x)
        if (next == TokenKind.Dot
            && tokens[index + 2].Kind == TokenKind.Name
            && tokens[index + 3].Kind == TokenKind.OpenParen
            && Lexer.FindLibraryName(tokens, index + 2) == index)
        {
            return false;
        }

        // A call to the Script's own Function; a call to a Placed Block Reference is a name's
        if (next == TokenKind.OpenParen && functions.Any(f => f.Name == token.Text))
            return false;

        return index < 2 || tokens[index - 1].Kind != TokenKind.Colon || IsArgumentValue(tokens, index, scope);
    }

    /// <summary>
    /// Whether the name after a named argument's colon is a value rather than a word: anything given to a Script's own
    /// Function, a Library's Function or a Placed Block Reference, or to a Block Function's input or a Setting that takes
    /// placed blocks. A word given to any other Setting is an option, unless it spells an input of the Function it's in,
    /// <c>delay(x, ticks: ticks)</c>.
    /// </summary>
    private static bool IsArgumentValue(List<Token> tokens, int index, DefinedFunction scope)
    {
        var argument = tokens[index - 2];
        if (argument.Kind != TokenKind.Name)
            return false;

        var call = OpenCall.FindStartedAt(tokens, argument.Span.Start);
        if (call == null)
            return false;

        if (call.Callee == null || !BlockManifest.TryGetFunction(call.Callee, out var type))
            return true;

        if (type.IndexOfInput(argument.Text) >= 0 || type.FindSetting(argument.Text)?.Type == SettingType.Targets)
            return true;

        return scope != null && scope.Inputs.Contains(tokens[index].Text);
    }

    /// <summary>
    /// Each read of the Functions makes them anew, so the same one is told by where its body starts.
    /// </summary>
    private static bool IsSameScope(DefinedFunction a, DefinedFunction b)
    {
        return a == null ? b == null : b != null && a.Body.Start == b.Body.Start;
    }

    private static bool IsDefaultKeyword(List<Token> tokens, int index)
    {
        return tokens[index].Text == DefaultStatement.Keyword
            && Lexer.IsFirstOnLine(tokens, index)
            && tokens[index + 1].Kind != TokenKind.Equals;
    }
}
