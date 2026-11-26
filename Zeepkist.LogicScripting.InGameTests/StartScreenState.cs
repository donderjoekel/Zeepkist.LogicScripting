using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// What the game's start screen showed when a run gave up waiting for it, so a run that stays there names the screen it
/// saw and which of the things it waits for never came.
/// </summary>
internal sealed class StartScreenState
{
    // Keeps the detail to one readable line when the screen shows more than its status
    private const int MaxTexts = 5;
    private const int MaxTextLength = 80;

    /// <summary>
    /// False while the game hasn't shown its start screen yet, when the two waits don't apply.
    /// </summary>
    public bool ScreenFound { get; set; } = true;

    /// <summary>
    /// True once the game has destroyed the start screen it showed, as it does when it moves on to the main menu.
    /// </summary>
    public bool ScreenLeft { get; set; }

    public bool IntroPanelActive { get; set; }
    public bool PlayerManagerExists { get; set; }

    /// <summary>
    /// The texts the screen showed, in the order they were found.
    /// </summary>
    public List<string> Texts { get; set; } = [];

    /// <summary>
    /// One line for the failed step's detail: the texts first, as they're what a person saw, then the two waits.
    /// </summary>
    public string Describe()
    {
        var shown = Shown(Texts);
        if (!ScreenFound)
            return $"the game showed no start screen; on screen: {shown}";

        if (ScreenLeft)
            return $"the game left its start screen, player manager {(PlayerManagerExists ? "there" : "missing")}; on screen: {shown}";

        return $"the game stayed on its start screen, showing {shown}; "
               + $"intro panel {(IntroPanelActive ? "active" : "not active")}, "
               + $"player manager {(PlayerManagerExists ? "there" : "missing")}";
    }

    /// <summary>
    /// The texts as the detail lists them: one line each, without empty or repeated ones, the first few only.
    /// </summary>
    public static string Shown(IEnumerable<string> texts)
    {
        var cleaned = texts
            .Select(Clean)
            .Where(text => text.Length > 0)
            .Distinct()
            .ToList();
        return cleaned.Count == 0
            ? "no text"
            : string.Join(", ", cleaned.Take(MaxTexts).Select(text => $"\"{text}\""))
              + (cleaned.Count > MaxTexts ? $" and {cleaned.Count - MaxTexts} more" : string.Empty);
    }

    private static string Clean(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var oneLine = string.Join(" ", text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        // Rich-text tags say how it looks, not what it says
        oneLine = Regex.Replace(oneLine, "<[^>]*>", string.Empty).Trim();
        return oneLine.Length > MaxTextLength ? oneLine.Substring(0, MaxTextLength) + "…" : oneLine;
    }
}
