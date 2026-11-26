using Imui.Controls;
using Imui.Core;
using Imui.IO.Events;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptWindow
{
    /// <summary>
    /// Imui only takes this rect when it first makes the window and keeps its own rect after that, through test mode
    /// too. So it's where the window was in an earlier session, kept on screen in case the resolution changed since.
    /// </summary>
    private ImRect GetInitialRect(ImGui gui, Vector2 minSize)
    {
        var screen = gui.Canvas.SafeScreenRect;
        if (_state.Width > 0 && _state.Height > 0)
        {
            var stored = new ImRect(_state.X, _state.Y, _state.Width, _state.Height);
            return ImRectUtility.Clamp(screen, WithSize(stored, Mathf.Max(stored.W, minSize.x), Mathf.Max(stored.H, minSize.y)));
        }

        var width = Mathf.Min(DefaultWidth, screen.W);
        var height = Mathf.Min(DefaultHeight, screen.H);
        return new ImRect(screen.X + ((screen.W - width) / 2), screen.Y + ((screen.H - height) / 2), width, height);
    }

    /// <summary>
    /// Like dragging the window's resize handle: the left edge stays where it is.
    /// </summary>
    private static ImRect WithWidth(ImRect rect, float width)
    {
        return WithSize(rect, width, rect.H);
    }

    /// <summary>
    /// Like dragging the window's resize handle: the top edge stays where it is.
    /// </summary>
    private static ImRect WithHeight(ImRect rect, float height)
    {
        return WithSize(rect, rect.W, height);
    }

    /// <summary>
    /// Like dragging the window's resize handle: the top left corner stays where it is.
    /// </summary>
    private static ImRect WithSize(ImRect rect, float width, float height)
    {
        return new ImRect(rect.X, rect.Top - height, width, height);
    }

    /// <summary>
    /// The window is no narrower than the center's minimum with both sides collapsed.
    /// </summary>
    private static float GetMinWidth(ImGui gui)
    {
        return gui.Style.Window.ContentPadding.Horizontal + MinCenterWidth + (GetCollapsedSideWidth(gui) * 2);
    }

    /// <summary>
    /// A collapsed side is a strip as wide as its header is tall.
    /// </summary>
    private static float GetCollapsedSideWidth(ImGui gui)
    {
        return GetPanelHeaderHeight(gui);
    }

    /// <summary>
    /// The window is no shorter than the Scripts side's tab bar and either tab's minimum: two list rows, a row of the
    /// open prompt and New, with a row for Import selected or the libraries.json warning while it shows. Nor is it shorter than the center's
    /// minimum: the name row and the editor's minimum, with a row of the startup check's and the storage warning while
    /// they show.
    /// </summary>
    private float GetMinHeight(ImGui gui)
    {
        var spacing = gui.Style.Layout.Spacing;
        var minTabHeight = Mathf.Max(ScriptListPanel.GetMinHeightWithImport(gui), LibraryListPanel.GetMinHeightWithWarning(gui, ShowsLibraryReadFailed));
        var minScriptsHeight = GetPanelHeaderHeight(gui) + ImTabsPane.GetTabBarHeight(gui) + (spacing * 2) + minTabHeight;

        // The center's padding, and a row of the startup check's warnings with the spacing under it
        var minStartupHeight = GetStartupWarnings().Count > 0 ? WindowRegion.GetRowHeight(gui) + spacing : 0;
        var minCenterHeight = (spacing * 2) + minStartupHeight + GetMinScriptHeight(gui, _storageWarning != null, _renamePrompt.GetHeight(gui));
        return ImWindow.GetTitleBarHeight(gui) + gui.Style.Window.ContentPadding.Vertical + Mathf.Max(minScriptsHeight, minCenterHeight);
    }

    /// <summary>
    /// The name row, a row of the storage warning while it shows, the rename prompt's <paramref name="renameHeight"/>,
    /// and the editor's minimum.
    /// </summary>
    private static float GetMinScriptHeight(ImGui gui, bool hasStorageWarning, float renameHeight)
    {
        var minStorageHeight = hasStorageWarning ? gui.Style.Layout.Spacing + WindowRegion.GetRowHeight(gui) : 0;
        return gui.GetRowHeight() + minStorageHeight + renameHeight + GetMinEditorHeight(gui);
    }

    private static float GetMinEditorHeight(ImGui gui)
    {
        return gui.GetRowsHeightWithSpacing(MinFieldRows) + GetPanelHeaderHeight(gui) + (gui.Style.Layout.Spacing * 2);
    }

    /// <summary>
    /// Gives the window the size and position the in-game tests asked for before it begins: Imui lays out its content in
    /// the rect it has then, so a rect changed afterwards shows a frame late. The size is no bigger than the screen, as a
    /// first open is, and at least <paramref name="minSize"/>, keeping the top left corner as dragging the resize handle
    /// does. A window Imui hasn't made yet starts through <see cref="GetInitialRect"/>, and takes the requests the frame
    /// after.
    /// </summary>
    private void ApplyRequestedRect(ImGui gui, Vector2 minSize)
    {
        var windowId = gui.GetControlId(Title);
        if (gui.WindowManager.TryFindWindow(windowId) < 0)
            return;

        ref var window = ref gui.WindowManager.GetWindowState(windowId);
        var screen = gui.Canvas.SafeScreenRect;
        var request = _requestedRect;
        var width = Mathf.Max(request.Width is { } requestedWidth ? Mathf.Min(requestedWidth, screen.W) : window.Rect.W, minSize.x);
        var height = Mathf.Max(request.Height is { } requestedHeight ? Mathf.Min(requestedHeight, screen.H) : window.Rect.H, minSize.y);
        var rect = WithSize(window.Rect, width, height);
        if (request.TopLeft is { } topLeft)
            rect = ImRectUtility.Clamp(screen, new ImRect(topLeft.x, topLeft.y - rect.H, rect.W, rect.H));
        else if (request.Center)
            rect = new ImRect(screen.X + ((screen.W - rect.W) / 2), screen.Y + ((screen.H - rect.H) / 2), rect.W, rect.H);

        request.Clear();
        if (rect != window.Rect)
            window.Rect = rect;
    }

    /// <summary>
    /// Imui's resize handle takes a drag off the window's width and height as the window begins, down to Imui's own
    /// 64 px, so clamping afterwards would draw the window too small for that frame. A drag that would take it below
    /// <paramref name="minSize"/> first makes the window as much wider or taller as the drag takes off it past that.
    /// </summary>
    private static void HoldResizeAt(ImGui gui, uint resizeHandleId, Vector2 minSize)
    {
        ref readonly var evt = ref gui.Input.MouseEvent;
        if (evt.Type != ImMouseEventType.Drag || gui.GetActiveControl() != resizeHandleId)
            return;

        ref var window = ref gui.WindowManager.GetWindowState(gui.GetControlId(Title));
        if (window.Rect.W + evt.Delta.x < minSize.x)
            window.Rect = WithWidth(window.Rect, minSize.x - evt.Delta.x);

        if (window.Rect.H - evt.Delta.y < minSize.y)
            window.Rect = WithHeight(window.Rect, minSize.y + evt.Delta.y);
    }

    /// <summary>
    /// The id Imui gives the window's resize handle, before the window begins.
    /// </summary>
    private static uint GetResizeHandleId(ImGui gui)
    {
        // The ids Imui gives the window and, in its own scope, the title bar and then the resize handle
        var windowId = gui.GetControlId(Title);
        return gui.GetControlId(2u, gui.GetControlId("win_ctrl", windowId));
    }

    private void StoreRect(ImRect rect)
    {
        _state.X = rect.X;
        _state.Y = rect.Y;
        _state.Width = rect.W;
        _state.Height = rect.H;
    }

    /// <summary>
    /// A stored size of zero means the panel was never resized. A drag never stores one: it stores the size it shows,
    /// clamped with <see cref="ClampSize"/>.
    /// </summary>
    private static float GetPanelSize(float stored, float defaultSize, float min, float max)
    {
        return ClampSize(stored > 0 ? stored : defaultSize, min, max);
    }

    /// <summary>
    /// Unlike <see cref="Mathf.Clamp(float, float, float)"/>, the minimum wins when there's less room than that.
    /// </summary>
    private static float ClampSize(float size, float min, float max)
    {
        return Mathf.Max(min, Mathf.Min(size, max));
    }

    /// <summary>
    /// Wide enough for both tabs, each half the strip, to show their whole label in the theme's font.
    /// </summary>
    private static float GetMinBlocksWidth(ImGui gui)
    {
        var settings = ImTabsPane.GetTextSettings(gui);
        var widestLabel = Mathf.Max(gui.MeasureTextSize(PlacedBlocksTab, settings).x, gui.MeasureTextSize(ReferenceTab, settings).x);
        return GetMinTabsWidth(gui, widestLabel, MinBlocksWidth);
    }

    /// <summary>
    /// Like <see cref="GetMinBlocksWidth"/>, with room in each tab for both its marks.
    /// </summary>
    private static float GetMinScriptsWidth(ImGui gui)
    {
        var widestLabel = Mathf.Max(SideTab.GetLabelWidth(gui, ScriptsTab), SideTab.GetLabelWidth(gui, LibrariesTab));
        return GetMinTabsWidth(gui, widestLabel, MinScriptsWidth);
    }

    /// <summary>
    /// A side with two tabs, each half its width, and at least <paramref name="min"/>.
    /// </summary>
    private static float GetMinTabsWidth(ImGui gui, float widestLabel, float min)
    {
        var tabWidth = widestLabel + (gui.Style.Layout.Indent * 2);
        return Mathf.Max(min, (tabWidth * 2) + (gui.Style.Layout.Spacing * 4));
    }
}
