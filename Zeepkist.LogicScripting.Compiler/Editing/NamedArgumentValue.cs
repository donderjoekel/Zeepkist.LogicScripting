using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// Tells a name written after a named argument's colon that is a Setting's or style's word (<c>op: lerp</c>) from one
/// that uses a name the Script defines (<c>logic(a: x)</c>). Read from the tokens, so half-typed text has it too.
/// </summary>
internal static class NamedArgumentValue
{
    /// <summary>
    /// Whether the name at <paramref name="index"/> comes after a colon and is a word rather than a name the Script
    /// defines. Anything given to a Script's own Function, a Library's Function or a Placed Block Reference, or to a
    /// Block Function's input or a Setting that takes placed blocks, is a name. A word given to any other Setting, to
    /// <c>style:</c>, or after a colon outside a call (<c>default hide: x</c>) is a word, unless it spells an input of
    /// the Function it's in, <c>delay(x, ticks: ticks)</c>. A call left open while it's typed carries on to the next
    /// line, so the colon can be on the line above.
    /// </summary>
    public static bool IsWord(List<Token> tokens, int index)
    {
        var colon = FindColon(tokens, index);
        if (colon < 0)
            return false;

        if (colon == 0 || tokens[colon - 1].Kind != TokenKind.Name)
            return true;

        var argument = tokens[colon - 1];
        var call = OpenCall.FindStartedAt(tokens, argument.Span.Start);
        if (call == null)
            return true;

        if (call.Callee == null || !BlockManifest.TryGetFunction(call.Callee, out var type))
            return false;

        if (type.IndexOfInput(argument.Text) >= 0 || type.FindSetting(argument.Text)?.Type == SettingType.Targets)
            return false;

        var token = tokens[index];
        var scope = DefinedNames.ReadFunctions(tokens).Select(f => f.Function).FirstOrDefault(f => f.Contains(token.Span.Start));
        return scope == null || !scope.Inputs.Contains(token.Text);
    }

    /// <summary>
    /// The colon right before the name, past the line breaks a call carries on over; -1 when there's none.
    /// </summary>
    private static int FindColon(List<Token> tokens, int index)
    {
        if (Lexer.IsStatementStart(tokens, index))
            return -1;

        var previous = index - 1;
        while (previous >= 0 && tokens[previous].Kind == TokenKind.NewLine)
        {
            previous--;
        }

        return previous >= 0 && tokens[previous].Kind == TokenKind.Colon ? previous : -1;
    }
}
