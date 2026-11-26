using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Texts mark the caret with <c>|</c>.
/// </summary>
public partial class CompletionTests
{
    /// <summary>
    /// With the names the text itself defines.
    /// </summary>
    private static IReadOnlyList<CompletionItem> Names(string text, EditorLookups lookups = null)
    {
        var context = At(text);
        return Completion.GetItems(context, DefinedNames.Read(context.Text), lookups);
    }

    private static List<CompletionItem> Arguments(string text, EditorLookups lookups = null)
    {
        return Names(text, lookups).Where(i => i.Kind == CompletionKind.Argument).ToList();
    }

    private static List<string> NameLabels(string text, EditorLookups lookups = null)
    {
        return Names(text, lookups).Select(i => i.Label).ToList();
    }

    private static List<string> Labels(string text, Func<string, PlacedBlock> findPlacedBlock = null)
    {
        return Completion.GetItems(At(text), lookups: new EditorLookups(findPlacedBlock)).Select(i => i.Label).ToList();
    }

    /// <summary>
    /// A Library with a Function of two outputs.
    /// </summary>
    private const string EdgeLibrary = "function edge(a) -> lo, hi\n  lo = a\n  hi = !a\nend";

    /// <summary>
    /// A Library whose Function takes two inputs, as the issue's <c>timing</c> does.
    /// </summary>
    private const string TwoInputLibrary = "function edge(a, b) -> lo\n  lo = a & b\nend";

    /// <summary>
    /// A Library with two Functions, written out of alphabetical order.
    /// </summary>
    private const string TwoFunctionLibrary = "function pulse(a, ticks) -> b\n  b = delay(a, ticks: ticks)\nend\n" + EdgeLibrary;

    private static EditorLookups Level(IReadOnlyDictionary<string, PlacedBlock> level)
    {
        return new EditorLookups(uid => level.GetValueOrDefault(uid));
    }

    /// <summary>
    /// A lookup with one Library, <c>timing</c>.
    /// </summary>
    private static EditorLookups Library(string text)
    {
        return new EditorLookups(findLibrary: name => name == "timing" ? text : null);
    }

    private static CaretContext At(string marked)
    {
        var caret = marked.IndexOf('|');
        return CaretContext.At(marked.Remove(caret, 1), caret);
    }

    private static string Format(TextSnapshot snapshot)
    {
        return snapshot.Text.Insert(snapshot.Caret, "|");
    }
}
