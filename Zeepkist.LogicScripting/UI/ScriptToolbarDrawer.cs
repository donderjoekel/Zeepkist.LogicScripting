using Imui.Controls;
using Imui.Core;
using ZeepSDK.LevelEditor;
using ZeepSDK.UI;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed class ScriptToolbarDrawer : IZeepToolbarDrawer
{
    private readonly ScriptWindow _window;

    public ScriptToolbarDrawer(ScriptWindow window)
    {
        _window = window;
    }

    public string MenuTitle => "Logic Script";

    public void DrawMenuItems(ImGui gui)
    {
        var available = LevelEditorApi.IsInLevelEditor && !LevelEditorApi.IsTestingLevel;
        gui.BeginReadOnly(!available);
        if (gui.Menu(_window.IsOpen ? "Close Script window" : "Open Script window") && available)
            _window.SetOpen(!_window.IsOpen);

        gui.EndReadOnly();
        if (!available)
            gui.TooltipAtLastControl("Only in the level editor");
    }
}
