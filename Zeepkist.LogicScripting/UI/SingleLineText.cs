using Imui.Core;
using Imui.Rendering;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal static class SingleLineText
{
    /// <summary>
    /// Left-aligned and centered vertically, cut off with an ellipsis when it doesn't fit.
    /// </summary>
    public static ImTextSettings GetSettings(ImGui gui)
    {
        return new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f, overflow: ImTextOverflow.Ellipsis);
    }
}
