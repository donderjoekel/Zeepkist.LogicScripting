using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Reads what follows a step's name on its line, or throws <see cref="ScenarioFormatException"/> saying what the step
/// takes.
/// </summary>
internal static class StepArgument
{
    public static void None(string step, string argument)
    {
        if (argument.Length > 0)
            throw new ScenarioFormatException($"{step} takes nothing after it");
    }

    public static string Text(string step, string argument, string what)
    {
        return argument.Length > 0 ? argument : throw Takes(step, what);
    }

    public static ScenarioFormatException Takes(string step, string what)
    {
        return new ScenarioFormatException($"{step} takes {what}");
    }

    /// <summary>
    /// For <paramref name="written"/>, the whole argument or the one word of it that's wrong, when it isn't
    /// <paramref name="what"/>.
    /// </summary>
    public static ScenarioFormatException Takes(string step, string what, string written)
    {
        return new ScenarioFormatException($"{step} takes {what}, not '{written}'");
    }

    public static string[] Words(string text)
    {
        return text.Split([' '], StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Splits at the first space, with what follows it trimmed. Returns false, with all of <paramref name="text"/> in
    /// <paramref name="first"/> and nothing in <paramref name="rest"/>, when it has no space.
    /// </summary>
    public static bool TrySplitFirst(string text, out string first, out string rest)
    {
        var space = text.IndexOf(' ');
        first = space < 0 ? text : text.Substring(0, space);
        rest = space < 0 ? string.Empty : text.Substring(space + 1).Trim();
        return space >= 0;
    }

    /// <summary>
    /// A one-based line and column, such as <c>2:5</c>.
    /// </summary>
    public static bool TryPosition(string text, out Position position)
    {
        var parts = text.Split(':');
        if (parts.Length != 2 || !TryPositive(parts[0], out var line) || !TryPositive(parts[1], out var column))
        {
            position = default;
            return false;
        }

        position = new Position(line, column);
        return true;
    }

    public static int Count(string step, string argument)
    {
        const string what = "a whole number of 0 or more";
        Text(step, argument, what);
        return int.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out var count)
            ? count
            : throw Takes(step, what, argument);
    }

    /// <summary>
    /// One of the words in <paramref name="choices"/>.
    /// </summary>
    public static T Choice<T>(string step, string argument, IReadOnlyDictionary<string, T> choices)
    {
        var words = choices.Keys.ToList();
        var what = $"{string.Join(", ", words.Take(words.Count - 1))} or {words[words.Count - 1]}";
        Text(step, argument, what);
        return choices.TryGetValue(argument, out var choice) ? choice : throw Takes(step, what, argument);
    }

    public static int Pixels(string step, string argument)
    {
        const string what = "a whole number of pixels above 0";
        Text(step, argument, what);
        return TryPositive(argument, out var pixels) ? pixels : throw Takes(step, what, argument);
    }

    /// <summary>
    /// As <see cref="Pixels"/> reads it, or null for <c>screen</c>.
    /// </summary>
    public static int? PixelsOrScreen(string step, string argument)
    {
        const string what = "a whole number of pixels above 0, or screen";
        Text(step, argument, what);
        if (argument == "screen")
            return null;

        return TryPositive(argument, out var pixels) ? pixels : throw Takes(step, what, argument);
    }

    /// <summary>
    /// A width and a height, as <see cref="Pixels"/> reads each.
    /// </summary>
    public static (int Width, int Height) Size(string step, string argument)
    {
        const string what = "a width and a height, each a whole number of pixels above 0";
        var words = Words(Text(step, argument, what));
        return words.Length == 2 && TryPositive(words[0], out var width) && TryPositive(words[1], out var height)
            ? (width, height)
            : throw Takes(step, what, argument);
    }

    /// <summary>
    /// A whole number of 1 or more, such as a one-based line.
    /// </summary>
    public static bool TryPositive(string text, out int number)
    {
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number > 0;
    }

    public static float Seconds(string step, string argument)
    {
        const string what = "a number of seconds above 0";
        Text(step, argument, what);
        return float.TryParse(argument, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
            ? seconds
            : throw Takes(step, what, argument);
    }

    /// <summary>
    /// Keys separated by spaces, each a key's name with any of <c>shift+</c>, <c>ctrl+</c>, <c>alt+</c> and
    /// <c>cmd+</c> before it.
    /// </summary>
    public static IReadOnlyList<KeyPress> Keys(string step, string argument, int max)
    {
        var what = $"{step} takes 1 to {max} keys, such as enter or ctrl+space";
        var names = Words(argument);
        if (names.Length == 0 || names.Length > max)
            throw new ScenarioFormatException(argument.Length == 0 ? what : $"{what}, not {names.Length}");

        return [.. names.Select(name => KeyNames.Parse(name) ?? throw new ScenarioFormatException($"{step} doesn't know the key '{name}'"))];
    }

    /// <summary>
    /// A name that's also part of a file name.
    /// </summary>
    public static string FileName(string step, string argument)
    {
        const string what = "a name of letters, digits, '-' and '_'";
        Text(step, argument, what);
        return argument.All(c => (c < 128 && char.IsLetterOrDigit(c)) || c == '-' || c == '_')
            ? argument
            : throw Takes(step, what, argument);
    }
}
