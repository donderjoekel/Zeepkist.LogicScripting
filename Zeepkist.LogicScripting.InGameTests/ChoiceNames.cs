using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.UI;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// The words a scenario writes for a step's choice, by what each stands for. A step's results name what it found by
/// the same words, through <see cref="Of{T}"/>. The order is the one a step's error lists them in.
/// </summary>
internal static class ChoiceNames
{
    /// <summary>
    /// Whether the tab is the Libraries tab.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, bool> Tabs = new Dictionary<string, bool>
    {
        ["scripts"] = false,
        ["libraries"] = true,
    };

    /// <summary>
    /// Whether the Blocks side's tab is the Reference tab.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, bool> BlocksTabs = new Dictionary<string, bool>
    {
        ["placed"] = false,
        ["reference"] = true,
    };

    public static readonly IReadOnlyDictionary<string, bool> OnOff = new Dictionary<string, bool>
    {
        ["on"] = true,
        ["off"] = false,
    };

    /// <summary>
    /// A list tab's prompts.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, ListPrompt> Prompts = new Dictionary<string, ListPrompt>
    {
        ["none"] = ListPrompt.None,
        ["new"] = ListPrompt.New,
        ["rename"] = ListPrompt.Rename,
        ["delete"] = ListPrompt.Delete,
    };

    /// <summary>
    /// The buttons of a list tab's prompts.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, PromptButton> Buttons = new Dictionary<string, PromptButton>
    {
        ["ok"] = PromptButton.Ok,
        ["cancel"] = PromptButton.Cancel,
        ["delete"] = PromptButton.Delete,
        ["keep-blocks"] = PromptButton.KeepBlocks,
    };

    /// <summary>
    /// The word in <paramref name="names"/> for <paramref name="value"/>.
    /// </summary>
    public static string Of<T>(IReadOnlyDictionary<string, T> names, T value)
    {
        return names.First(n => EqualityComparer<T>.Default.Equals(n.Value, value)).Key;
    }
}
