using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

internal static partial class StepList
{
    /// <summary>
    /// <c>&lt;function&gt;</c>, or <c>&lt;function&gt; as &lt;name&gt;</c> to remember the block by the name.
    /// </summary>
    private static ScenarioStep PlaceBlock(string step, string argument)
    {
        var what = "a Block Function's name, and optionally 'as <name>'";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
            return new PlaceBlockStep(words[0]);

        if (words[1] != "as")
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        var name = StepArgument.FileName($"{step} ... as", string.Join(" ", words.Skip(2)));
        return new PlaceBlockStep(words[0], name);
    }

    /// <summary>
    /// <c>&lt;name&gt; &lt;setting&gt; &lt;value&gt;</c>. Whether the block has the Setting and it takes the value is
    /// only known once the block is placed, so the step checks that when it runs.
    /// </summary>
    private static ScenarioStep SetBlockSetting(string step, string argument)
    {
        var what = "a placed block's name, a Setting's name and its value, such as t mode global_velocity";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 3)
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        return new SetBlockSettingStep(StepArgument.FileName(step, words[0]), words[1], words[2]);
    }

    /// <summary>
    /// <c>&lt;name&gt; int|float|text &lt;key&gt; &lt;value&gt;</c>. A number's value is read here, as the step takes
    /// it as it's written, and writes it the same way.
    /// </summary>
    private static ScenarioStep SetStoredSetting(string step, string argument)
    {
        var what = "a placed block's name, int, float or text, the key it's stored under and its value, such as m text oi2 5";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        StoredKind? kind = words.Length != 4 ? null : words[1] switch
        {
            "int" => StoredKind.Int,
            "float" => StoredKind.Float,
            "text" => StoredKind.Text,
            _ => null
        };
        if (kind == null)
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        var isNumber = kind switch
        {
            StoredKind.Int => int.TryParse(words[3], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _),
            StoredKind.Float => float.TryParse(words[3], NumberStyles.Float, CultureInfo.InvariantCulture, out _),
            _ => true
        };
        if (!isNumber)
            throw new ScenarioFormatException($"{step} takes a number for {words[1]}, not '{words[3]}'");

        return new SetStoredSettingStep(StepArgument.FileName(step, words[0]), kind.Value, words[2], words[3]);
    }

    /// <summary>
    /// <c>&lt;script&gt; &lt;function&gt; as &lt;name&gt;</c>. A Script's name can have spaces, so the last three words
    /// are the rest.
    /// </summary>
    private static ScenarioStep NameGroupBlock(string step, string argument)
    {
        var what = "a Script's name, a Block Function's name and 'as <name>', such as Lights logic as l";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 4 || words[words.Length - 2] != "as")
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        var name = StepArgument.FileName($"{step} ... as", words[words.Length - 1]);
        return new NameGroupBlockStep(string.Join(" ", words.Take(words.Length - 3)), words[words.Length - 3], name);
    }

    /// <summary>
    /// <c>&lt;name&gt; &lt;output&gt; to &lt;name&gt; &lt;input&gt;</c>. Whether the blocks have the output and input is
    /// only known once they're placed, so the step checks that when it runs.
    /// </summary>
    private static WireArgument ReadWire(string step, string argument)
    {
        var what = "a placed block's name and output, 'to', and a placed block's name and input, such as g out to l a";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 5 || words[2] != "to")
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        return new WireArgument(StepArgument.FileName(step, words[0]), words[1], StepArgument.FileName(step, words[3]), words[4]);
    }

    private static IReadOnlyList<string> ReadBlockNames(string step, string argument)
    {
        var what = "placed blocks' names, each once, such as g l";
        var names = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (names.Distinct().Count() != names.Length)
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        return [.. names.Select(name => StepArgument.FileName(step, name))];
    }

    private static ScenarioStep MoveBlock(string step, string argument)
    {
        var what = "a placed block's name and how far to move it along x, y and z, such as l 16 0 -8";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 4 || !TryReadOffset(words[1], out var x) || !TryReadOffset(words[2], out var y) || !TryReadOffset(words[3], out var z))
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        return new MoveBlockStep(StepArgument.FileName(step, words[0]), x, y, z);
    }

    private static bool TryReadOffset(string text, out float offset)
    {
        return float.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out offset);
    }

    /// <summary>
    /// <c>&lt;name&gt; &lt;input&gt; &lt;signal&gt;</c>. Whether the block has the input is only known once it's placed,
    /// so the step checks that when it runs.
    /// </summary>
    private static ScenarioStep ExpectSignal(string step, string argument)
    {
        var what = "a placed block's name, its input and a whole number, such as l a 1";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 3 || !long.TryParse(words[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var signal))
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        return new ExpectSignalStep(StepArgument.FileName(step, words[0]), words[1], signal);
    }

    /// <summary>
    /// <c>&lt;name&gt; &lt;text&gt;</c>, where the text is the rest of the line.
    /// </summary>
    private static ScenarioStep ExpectDisplay(string step, string argument)
    {
        var what = "a placed Power Display's name and what it shows, such as p 1";
        var space = StepArgument.Text(step, argument, what).IndexOf(' ');
        if (space < 0)
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        return new ExpectDisplayStep(StepArgument.FileName(step, argument.Substring(0, space)), argument.Substring(space + 1).Trim());
    }

    private static ScenarioStep ExpectWires(string step, string argument)
    {
        var what = "a placed block's name and how many Wires it has, such as l 2";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2)
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        return new ExpectWiresStep(StepArgument.FileName(step, words[0]), StepArgument.Count(step, words[1]));
    }

    /// <summary>
    /// <c>as &lt;name&gt;...</c>, a name for each copy.
    /// </summary>
    private static ScenarioStep PasteBlocks(string step, string argument)
    {
        const string prefix = "as ";
        if (!argument.StartsWith(prefix, StringComparison.Ordinal))
            throw new ScenarioFormatException($"{step} takes 'as' and a name for each selected block's copy, such as 'as g2 l2'");

        return new PasteBlocksStep(ReadBlockNames(step, argument.Substring(prefix.Length).Trim()));
    }

    /// <summary>
    /// <c>&lt;name&gt;</c>, or <c>&lt;name&gt; while &lt;step&gt;</c> to run a step of one line as the level loads.
    /// </summary>
    private static ScenarioStep LoadLevel(string step, string argument)
    {
        const string separator = " while ";
        var what = "a saved level's name, and optionally 'while' and a step of one line to run as it loads";
        var at = StepArgument.Text(step, argument, what).IndexOf(separator, StringComparison.Ordinal);
        if (at < 0)
            return new LoadLevelStep(StepArgument.FileName(step, argument));

        var meanwhile = CreateInner(step, argument, argument.Substring(at + separator.Length).Trim(), what);
        return new LoadLevelStep(StepArgument.FileName(step, argument.Substring(0, at).Trim()), meanwhile);
    }

    /// <summary>
    /// <c>&lt;function&gt;</c>, or <c>&lt;function&gt; &lt;style&gt;</c>. Whether the type has the style is only
    /// checked when the step runs, as for <c>place-block</c>'s Block Function.
    /// </summary>
    private static ScenarioStep TurnOffStyle(string step, string argument)
    {
        var what = "a Block Function's name, and optionally one of its styles";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            1 => new TurnOffStyleStep(words[0]),
            2 => new TurnOffStyleStep(words[0], words[1]),
            _ => throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'"),
        };
    }
}
