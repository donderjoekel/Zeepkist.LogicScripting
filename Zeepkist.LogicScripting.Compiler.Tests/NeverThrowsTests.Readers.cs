using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class NeverThrowsTests
{
    private static CompileResult AssertNeverThrows(string text)
    {
        try
        {
            return SmallStack.Run(() => Read(text));
        }
        catch (Exception exception)
        {
            Assert.Fail($"{Escape(text)}\n{exception}");
            return null;
        }
    }

    /// <summary>
    /// Everything the editor does with the text while the player types.
    /// </summary>
    private static CompileResult Read(string text)
    {
        var result = Compile(text);
        ScriptCompiler.CheckLibrary(text, _libraries.GetValueOrDefault);
        AssertSpansInOrder(text, ScriptHighlighter.Highlight(text, _libraryFunctions));
        var oddErrors = _oddPositions.Select(p => new Diagnostic(p, "odd"));
        var underlines = ScriptText.GetErrorUnderlines(text, result.Errors.Concat(oddErrors));
        foreach (var position in _oddPositions)
        {
            ScriptText.GetIndex(text, position);
        }

        foreach (var index in new[] { -1, 0, text.Length / 2, text.Length - 1, text.Length, text.Length + 1 })
        {
            Hover(text, index, underlines);
        }

        var definedNames = DefinedNames.Read(text, _lookups);
        Complete(new TextSnapshot(text, -1), definedNames);
        Complete(new TextSnapshot(text, text.Length + 1), definedNames);

        foreach (var snapshot in GetSnapshots(text))
        {
            Edit(snapshot);
            Complete(snapshot, definedNames);
        }

        return result;
    }

    /// <summary>
    /// The editor draws the text run by run between the spans, so they have to follow each other inside the text.
    /// </summary>
    private static void AssertSpansInOrder(string text, IReadOnlyList<HighlightSpan> spans)
    {
        var end = 0;
        foreach (var span in spans)
        {
            Assert.True(
                span.Start >= end && span.Length >= 0 && span.Start + span.Length <= text.Length,
                $"Span {span.Start}+{span.Length} after {end} in {text.Length} characters");
            end = span.Start + span.Length;
        }
    }

    private static IEnumerable<TextSnapshot> GetSnapshots(string text)
    {
        foreach (var caret in new[] { 0, text.Length / 2, text.Length })
        {
            yield return new TextSnapshot(text, caret);
            yield return new TextSnapshot(text, caret, -caret);
            yield return new TextSnapshot(text, caret, text.Length - caret);
        }
    }

    private static void Edit(TextSnapshot snapshot)
    {
        EditCommands.ToggleComment(snapshot);
        EditCommands.Indent(snapshot);
        EditCommands.Outdent(snapshot);
        EditCommands.NewLine(snapshot);
        EditCommands.MoveTo(snapshot, 0, false);
        EditCommands.MoveTo(snapshot, snapshot.Text.Length, true);
        EditCommands.Home(snapshot, false);
        EditCommands.Home(snapshot, true);
        EditCommands.End(snapshot, false);
        EditCommands.End(snapshot, true);
        EditCommands.DeleteTo(snapshot, 0);
        EditCommands.DeleteTo(snapshot, snapshot.Text.Length);
        EditCommands.MoveLines(snapshot, -1);
        EditCommands.MoveLines(snapshot, 1);
        EditCommands.Duplicate(snapshot);
        EditCommands.DeleteLines(snapshot);
    }

    /// <summary>
    /// Accepts the first item too, so a word span that doesn't fit the text would fail here. Reads the call's help too.
    /// </summary>
    private static void Complete(TextSnapshot snapshot, IReadOnlyList<DefinedName> definedNames)
    {
        var context = CaretContext.At(snapshot.Text, snapshot.Caret);
        var items = Completion.GetItems(context, definedNames, _lookups);
        if (items.Count > 0)
            Completion.Accept(context, items[0]);

        CallHelp.At(snapshot.Text, snapshot.Caret, _lookups);
    }

    private static void Hover(string text, int index, IReadOnlyList<ErrorUnderline> underlines)
    {
        HoverInfo.At(text, index, underlines, _lookups);
        DefinitionJump.At(text, index, _lookups);
        NameOccurrences.At(text, index);
    }

    private static CompileResult Compile(string text)
    {
        return ScriptCompiler.Compile(text, 1, [], uid => _level.GetValueOrDefault(uid), _libraries.GetValueOrDefault);
    }

    private static string Escape(string text)
    {
        var shown = text.Length > 200 ? text.Substring(0, 200) + "…" : text;
        return shown.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}
