using Imui.Controls;
using Imui.Core;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal static class ButtonWidth
{
    /// <summary>
    /// The width of a button that fits <paramref name="label"/>.
    /// </summary>
    public static float For(ImGui gui, string label)
    {
        return gui.MeasureTextSize(label).x + (gui.Style.Layout.Indent * 2);
    }
}
