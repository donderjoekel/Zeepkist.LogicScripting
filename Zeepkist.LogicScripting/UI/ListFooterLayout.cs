using Imui.Core;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Where a list tab drew its scroll region and footer last frame.
/// </summary>
internal readonly struct ListFooterLayout
{
    public ListFooterLayout(ImRect tab, ImRect list, ImRect prompts, ImRect buttons, float promptsHeight, float minListHeight, float rowHeight)
    {
        Tab = tab;
        List = list;
        Prompts = prompts;
        Buttons = buttons;
        PromptsHeight = promptsHeight;
        MinListHeight = minListHeight;
        RowHeight = rowHeight;
    }

    /// <summary>
    /// The whole tab, under its tab bar.
    /// </summary>
    public ImRect Tab { get; }

    public ImRect List { get; }

    /// <summary>
    /// The region the open prompt and the result line scroll in.
    /// </summary>
    public ImRect Prompts { get; }

    /// <summary>
    /// The rows under the prompts: New, and on the Scripts tab Import selected above it.
    /// </summary>
    public ImRect Buttons { get; }

    /// <summary>
    /// The height the open prompt and the result line take, scrolled out of view or not. Zero with neither.
    /// </summary>
    public float PromptsHeight { get; }

    public float MinListHeight { get; }
    public float RowHeight { get; }
}
