using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using TNRD.Zeepkist.LogicScripting.UI;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

internal static partial class StepList
{
    /// <summary>
    /// A label has no spaces, so the rest of the line is the detail.
    /// </summary>
    private static ScenarioStep ExpectCompletion(string argument)
    {
        var space = argument.IndexOf(' ');
        return space < 0
            ? new ExpectCompletionStep(argument, null)
            : new ExpectCompletionStep(argument.Substring(0, space), argument.Substring(space + 1).Trim());
    }

    /// <summary>
    /// A completion row's label, which has no spaces. <c>expect-completion-no-detail</c> takes only the label, as the
    /// detail can't be left empty on <c>expect-completion</c>'s line: the line's trailing spaces are taken off.
    /// </summary>
    private static string ReadLabel(string step, string argument)
    {
        var what = "a row's label";
        var label = StepArgument.Text(step, argument, what);
        return label.IndexOf(' ') < 0 ? label : throw new ScenarioFormatException($"{step} takes {what}, with no spaces, not '{argument}'");
    }

    /// <summary>
    /// Headings separated by spaces, each a name with <see cref="ReferenceHeading.ClosedSuffix"/> after it for a closed
    /// one, or <see cref="NoHeadings"/> alone. A heading's name has no spaces, nor a colon.
    /// </summary>
    private static ScenarioStep ExpectReferenceHeadings(string step, string argument)
    {
        var what = $"the headings, such as Inputs Libraries{ReferenceHeading.ClosedSuffix}, or {NoHeadings}";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1 && words[0] == NoHeadings)
            return new ExpectReferenceHeadingsStep([]);

        var headings = new List<ReferenceHeading>();
        foreach (var word in words)
        {
            var open = !word.EndsWith(ReferenceHeading.ClosedSuffix, StringComparison.Ordinal);
            var name = open ? word : word.Substring(0, word.Length - ReferenceHeading.ClosedSuffix.Length);
            if (name.Length == 0 || name == NoHeadings || name.IndexOf(':') >= 0)
                throw new ScenarioFormatException($"{step} takes {what}, not '{word}'");

            headings.Add(new ReferenceHeading(name, open));
        }

        return new ExpectReferenceHeadingsStep(headings);
    }

    /// <summary>
    /// <c>&lt;line&gt;:&lt;column&gt; &lt;text&gt;</c>, where the text is the rest of the line.
    /// </summary>
    private static ScenarioStep ExpectError(string step, string argument)
    {
        var what = $"{step} takes a line and column, such as 2:5, then the text to look for";
        var space = argument.IndexOf(' ');
        var text = space < 0 ? string.Empty : argument.Substring(space + 1).Trim();
        if (text.Length == 0)
            throw new ScenarioFormatException(what);

        var written = argument.Substring(0, space);
        var position = written.Split(':');
        if (position.Length != 2 || !StepArgument.TryPositive(position[0], out var line) || !StepArgument.TryPositive(position[1], out var column))
            throw new ScenarioFormatException($"{what}, not '{written}'");

        return new ExpectErrorStep(new Position(line, column), text);
    }

    private static ScenarioStep ExpectUsages(string step, string argument)
    {
        var what = $"the one-based line and column of each underline, such as 1:1 3:12, or {ExpectUsagesStep.NoUsages}";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1 && words[0] == ExpectUsagesStep.NoUsages)
            return new ExpectUsagesStep([]);

        List<Position> positions = [];
        foreach (var word in words)
        {
            var position = word.Split(':');
            if (position.Length != 2 || !StepArgument.TryPositive(position[0], out var line) || !StepArgument.TryPositive(position[1], out var column))
                throw new ScenarioFormatException($"{step} takes {what}, not '{word}'");

            positions.Add(new Position(line, column));
        }

        return new ExpectUsagesStep(positions);
    }

    /// <summary>
    /// <c>&lt;step&gt; because &lt;text&gt;</c>, split at the last <c>because</c>, so the step's own text can hold one.
    /// </summary>
    private static ScenarioStep ExpectStepFails(string step, string argument)
    {
        const string separator = " because ";
        var what = "a step of one line, 'because' and text its failure contains, such as expect-blocks 1 because 0 Logic Blocks";
        var at = StepArgument.Text(step, argument, what).LastIndexOf(separator, StringComparison.Ordinal);
        if (at < 0)
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        var inner = CreateInner(step, argument, argument.Substring(0, at).Trim(), what);
        return new ExpectStepFailsStep(inner, argument.Substring(at + separator.Length).Trim());
    }

    /// <summary>
    /// A step written inside another's line, so it takes one line, and isn't that step itself.
    /// </summary>
    private static ScenarioStep CreateInner(string step, string argument, string inner, string what)
    {
        var space = inner.IndexOf(' ');
        var name = space < 0 ? inner : inner.Substring(0, space);
        if (name == SetText || name == step)
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        return Create(name, space < 0 ? string.Empty : inner.Substring(space + 1).Trim())
            ?? throw new ScenarioFormatException($"unknown step '{name}'");
    }
}
