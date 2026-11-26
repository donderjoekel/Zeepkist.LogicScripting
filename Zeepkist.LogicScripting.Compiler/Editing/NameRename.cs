using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// Renames a name a Script defines, at its definition and every use of it in that Script: a name defined at the top
/// level or in a Function's body, a Placed Block Reference, a Function, or a Function's input or output. Names in
/// other Scripts and in Libraries, labels of Block Functions and placed blocks, and words given to Settings stay.
/// </summary>
/// <remarks>
/// It reads the parsed Script, so it only renames one without errors: the parser drops a broken line, which could hold
/// a use. As a check, the renamed Script has to compile to the same blocks and Wires as before.
/// </remarks>
public static class NameRename
{
    public const string FixErrorsFirst = "Fix the errors first, so every use of the name can be found";
    public const string NoNameHere = "Put the caret on a name the Script defines to rename it";

    /// <summary>
    /// The name the character at <paramref name="index"/> is in or right after, or null with
    /// <paramref name="error"/> saying why it can't be renamed.
    /// </summary>
    public static string Find(string text, int index, EditorLookups lookups, out string error)
    {
        var usages = Read(text ?? string.Empty, lookups ?? EditorLookups.None, out error);
        if (usages == null)
            return null;

        var usage = usages.Find(index);
        if (usage == null)
        {
            error = NoNameHere;
            return null;
        }

        return usage.Name;
    }

    /// <summary>
    /// Renames the name the character at <paramref name="index"/> is in or right after to
    /// <paramref name="newName"/>, everywhere the Script uses it.
    /// </summary>
    /// <param name="text">The Script.</param>
    /// <param name="index">The caret, in or right after a use of the name.</param>
    /// <param name="newName">Trimmed before it's checked.</param>
    /// <param name="lookups">The level's blocks and the Libraries, to compile the Script with.</param>
    public static RenameResult Rename(string text, int index, string newName, EditorLookups lookups = null)
    {
        text ??= string.Empty;
        lookups ??= EditorLookups.None;
        newName = newName?.Trim() ?? string.Empty;
        var usages = Read(text, lookups, out var error);
        if (usages == null)
            return RenameResult.Refused(text, index, error);

        var usage = usages.Find(index);
        if (usage == null)
            return RenameResult.Refused(text, index, NoNameHere);

        if (newName == usage.Name)
            return RenameResult.Renamed(text, index, 0);

        if (GetNewNameError(newName, usages) is { } nameError)
            return RenameResult.Refused(text, index, nameError);

        if (usages.FindUnclearOutput(usage.Symbol) is { } unclear)
        {
            var message = $"Can't tell whether '.{unclear.Output}' on line {unclear.Position.Line} is an output of '{usage.Symbol.Function.Name.Name}', so nothing was renamed";
            return RenameResult.Refused(text, index, message);
        }

        var spans = usages.All.Where(u => u.Symbol.Equals(usage.Symbol)).Select(u => u.Span).OrderBy(s => s.Start).ToList();
        var renamed = Replace(text, spans, newName, index, out var caret);
        if (!CompilesTheSame(usages.Compiled, Compile(renamed, lookups)))
            return RenameResult.Refused(text, index, $"Renaming '{usage.Name}' to '{newName}' would change what the Script places, so nothing was renamed");

        return RenameResult.Renamed(renamed, caret, spans.Count);
    }

    private static NameUsages Read(string text, EditorLookups lookups, out string error)
    {
        var compiled = Compile(text, lookups);
        if (!compiled.Succeeded)
        {
            error = FixErrorsFirst;
            return null;
        }

        error = null;
        List<Diagnostic> diagnostics = [];
        var tokens = Lexer.Tokenize(text, diagnostics);
        var statements = Parser.Parse(tokens, diagnostics);
        return NameUsages.Read(tokens, statements, compiled);
    }

    private static CompileResult Compile(string text, EditorLookups lookups)
    {
        return ScriptCompiler.Compile(text, 0, null, lookups.FindPlacedBlock, lookups.FindLibrary);
    }

    private static string GetNewNameError(string name, NameUsages usages)
    {
        if (name.Length == 0)
            return "Type the new name";

        if (!ScriptText.IsNameStart(name[0]) || !name.All(ScriptText.IsNamePart))
            return $"'{name}' isn't a name. Use letters, digits and _, and don't start with a digit";

        if (GraphBuilder.GetReservedNameError(name) is { } reserved)
            return reserved;

        return usages.DefinedNames.Contains(name) ? $"'{name}' is already defined in this Script" : null;
    }

    private static string Replace(string text, List<TextSpan> spans, string newName, int index, out int caret)
    {
        caret = index;
        for (var i = spans.Count - 1; i >= 0; i--)
        {
            var span = spans[i];
            text = text.Substring(0, span.Start) + newName + text.Substring(span.End);
            if (index >= span.End)
                caret += newName.Length - span.Length;
            else if (index > span.Start)
                caret = span.Start + newName.Length + (caret - index);
        }

        return text;
    }

    /// <summary>
    /// Whether both place the same blocks, with the same Settings and in the same cells, and the same Wires. Where in
    /// the text each block is written may differ, as names change length.
    /// </summary>
    private static bool CompilesTheSame(CompileResult before, CompileResult after)
    {
        if (!after.Succeeded || before.Blocks.Count != after.Blocks.Count)
            return false;

        for (var i = 0; i < before.Blocks.Count; i++)
        {
            var a = before.Blocks[i];
            var b = after.Blocks[i];
            if (a.Type != b.Type || a.Style != b.Style || a.Yaw != b.Yaw || a.Cell.X != b.Cell.X || a.Cell.Z != b.Cell.Z
                || !a.Settings.Select(Describe).SequenceEqual(b.Settings.Select(Describe)))
                return false;
        }

        return before.Wires.Select(Describe).SequenceEqual(after.Wires.Select(Describe))
            && before.PlacedWires.Select(Describe).SequenceEqual(after.PlacedWires.Select(Describe));
    }

    private static string Describe(Setting setting)
    {
        return $"{setting.Key}={setting.Kind}:{setting.IntValue}:{setting.TextValue}";
    }

    private static string Describe(CompiledWire wire)
    {
        return $"{wire.From.IsPlaced}:{wire.From}:{wire.FromConnector}>{wire.To.IsPlaced}:{wire.To}:{wire.ToConnector}";
    }
}
