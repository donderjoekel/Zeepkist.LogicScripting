using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Storage;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The window itself: three regions side by side, Scripts on the left, the current Script in the center and Blocks
/// on the right (those in the level and those a Script can use), whose sides collapse to a strip. The sides and the
/// error panel under the editor are resized by dragging their inner edge, and the error panel hides to its header.
/// Open state, size, position, panel sizes, collapsed panels, collapsed Reference categories, the Scripts side's shown
/// tab and whether Orphaned Groups is collapsed are kept in the <see cref="WindowState"/>.
/// </summary>
internal sealed partial class ScriptWindow
{
    private const string Title = "Logic Script";
    private const float DefaultWidth = 1120;
    private const float DefaultHeight = 640;
    private const float DefaultScriptsWidth = 220;
    private const float MinScriptsWidth = 140;
    private const float MinBlocksWidth = 300;
    private const string PlacedBlocksTab = "Placed blocks";
    private const string ReferenceTab = "Reference";
    private const string ScriptsTab = "Scripts";
    private const string LibrariesTab = "Libraries";
    private const string NoScriptText = "No Script selected. Make one with New Script.";

    /// <summary>
    /// Narrower than this, the sides draw collapsed whatever the state says, so the editor stays usable.
    /// </summary>
    private const float MinCenterWidth = 280;

    private const int DefaultErrorRows = 5;
    private const int MinErrorRows = 1;
    private const int MinFieldRows = 3;

    private readonly ResizeHandle _scriptsEdge = new(ImAxis.Horizontal, 1);
    private readonly ResizeHandle _blocksEdge = new(ImAxis.Horizontal, -1);
    private readonly ResizeHandle _errorsEdge = new(ImAxis.Vertical, 1);

    private WindowState _savedState;
    private bool _showReference;

    /// <summary>
    /// For the in-game tests, which check the tab is on screen before typing into its filter.
    /// </summary>
    private int _referenceDrawnFrame = -1;

    // Kept for the in-game tests
    private float _drawnScriptsWidth;

    /// <summary>
    /// The storage warning as the frame began, which its minimum height counts, and the Script or Library it's for. The
    /// center shows it while that's still the one selected: one selected while drawing, such as by clicking it in the
    /// list, shows its warning from the next frame, at any height.
    /// </summary>
    private string _storageWarning;

    private EditedItem _storageWarningOf;

    /// <summary>
    /// Opening tries again after a failure, and logs the next one. A failure in the work before the toggle key is
    /// read, such as saving Drafts, leaves only ZeepSDK's menu bar to reopen the window.
    /// </summary>
    public void SetOpen(bool open)
    {
        _state.Open = open;
        if (open)
            _failed = false;
    }

    /// <summary>
    /// Call when the mod unloads.
    /// </summary>
    public void SaveState()
    {
        if (_state.IsSameAs(_savedState))
            return;

        _stateStore.Save(_state);
        _savedState = _state.Copy();
    }

    /// <summary>
    /// Waits for the mouse to be up, so dragging or resizing the window writes the file once, at the end.
    /// </summary>
    private void SaveStateWhenSettled()
    {
        if (!Input.GetMouseButton(0))
            SaveState();
    }

    /// <summary>
    /// Returns the block under the hovered row of the Placed blocks tab, or null.
    /// </summary>
    private BlockProperties DrawWindow(ImGui gui)
    {
        BlockProperties hovered = null;
        var open = true;

        // A field sets whether it has focus as it's drawn, so one in a hidden tab or a collapsed side has none
        ReleasePanelFocus();

        // First, so the minimum height counts the warnings of Scripts that a Compile or a change to the level marked
        RefreshScripts();
        _storageWarning = _edited?.StorageWarning;
        _storageWarningOf = _edited;
        var minSize = new Vector2(GetMinWidth(gui), GetMinHeight(gui));
        ApplyRequestedRect(gui, minSize);
        var resizeHandleId = GetResizeHandleId(gui);
        HoldResizeAt(gui, resizeHandleId, minSize);
        if (gui.BeginWindow(
                Title,
                ref open,
                ref _mouseOver,
                GetInitialRect(gui, minSize),
                mouseEntered: () => SetMouseBlocked(true),
                mouseExited: () => SetMouseBlocked(false)))
        {
            ref var window = ref gui.WindowManager.GetWindowState(gui.PeekId());
            StoreRect(window.Rect);
            _windowLayout = new WindowLayout(
                Time.frameCount,
                minSize.x,
                minSize.y,
                ImWindow.GetResizeHandleRect(gui, window.Rect),
                gui.IsControlActive(resizeHandleId),
                gui.Canvas.SafeScreenRect);
            hovered = DrawRegions(gui);
        }

        gui.EndWindow();
        if (!open)
            SetOpen(false);

        return hovered;
    }

    private BlockProperties DrawRegions(ImGui gui)
    {
        var area = gui.Layout.AddRect(gui.GetLayoutWidth(), gui.GetLayoutHeight());
        var collapsedWidth = GetCollapsedSideWidth(gui);
        var minScriptsWidth = GetMinScriptsWidth(gui);
        var minBlocksWidth = GetMinBlocksWidth(gui);

        // The Blocks side gives way first; the Scripts side only when the center still doesn't fit without both
        var blocksFits = area.W - (_state.Scripts.Collapsed ? collapsedWidth : minScriptsWidth) - minBlocksWidth >= MinCenterWidth;
        var scriptsFits = area.W - minScriptsWidth - collapsedWidth >= MinCenterWidth;
        var scriptsCollapsed = _state.Scripts.Collapsed || !scriptsFits;
        var blocksCollapsed = _state.Blocks.Collapsed || !blocksFits;

        // Before collapsing, the sides shrink to make room for the center, the Blocks side first. The state keeps the
        // size they were given, so they grow back when the window does.
        var maxScriptsWidth = area.W - MinCenterWidth - (blocksCollapsed ? collapsedWidth : minBlocksWidth);
        var scriptsWidth = scriptsCollapsed
            ? collapsedWidth
            : GetPanelSize(_state.Scripts.Size, DefaultScriptsWidth, minScriptsWidth, maxScriptsWidth);
        _drawnScriptsWidth = scriptsWidth;
        var maxBlocksWidth = area.W - MinCenterWidth - scriptsWidth;
        var blocksWidth = blocksCollapsed
            ? collapsedWidth
            : GetPanelSize(_state.Blocks.Size, minBlocksWidth, minBlocksWidth, maxBlocksWidth);

        var scriptsEdgeId = gui.GetControlId("scripts edge");
        var blocksEdgeId = gui.GetControlId("blocks edge");
        if (!scriptsCollapsed && _scriptsEdge.Drag(gui, scriptsEdgeId, scriptsWidth) is { } draggedScriptsWidth)
            _state.Scripts.Size = ClampSize(draggedScriptsWidth, minScriptsWidth, maxScriptsWidth);

        if (!blocksCollapsed && _blocksEdge.Drag(gui, blocksEdgeId, blocksWidth) is { } draggedBlocksWidth)
            _state.Blocks.Size = ClampSize(draggedBlocksWidth, minBlocksWidth, maxBlocksWidth);

        var scripts = area.TakeLeft(scriptsWidth, out var rest);
        var blocks = rest.TakeRight(blocksWidth, out var center);
        var scriptsEdge = new ImRect(scripts.Right - 1, area.Y, 1, area.H);
        var blocksEdge = new ImRect(blocks.X, area.Y, 1, area.H);
        DrawDivider(gui, scriptsEdge);
        DrawDivider(gui, blocksEdge);

        // Imui numbers controls in drawing order, so each region gets its own scope: a new error row in the center
        // must not change the ids, and so the focus and state, of the controls drawn after it
        gui.PushId("scripts");
        if (DrawSideHeader(gui, scripts, "Scripts", scriptsCollapsed, buttonOnRight: true, canToggle: scriptsFits))
            _state.Scripts.ToggleCollapsed();

        if (!scriptsCollapsed)
            DrawScriptsTabs(gui, GetSideBody(gui, scripts));

        gui.PopId();
        gui.PushId("center");
        DrawCenter(gui, center);
        gui.PopId();

        gui.PushId("blocks");
        if (DrawSideHeader(gui, blocks, "Blocks", blocksCollapsed, buttonOnRight: false, canToggle: blocksFits))
            _state.Blocks.ToggleCollapsed();

        var hovered = blocksCollapsed ? null : DrawBlocksTabs(gui, GetSideBody(gui, blocks));
        gui.PopId();

        // Each grab strip lies right of its edge, off the scrollbar of the region on the left
        if (!scriptsCollapsed)
            _scriptsEdge.Draw(gui, scriptsEdgeId, scriptsEdge, ResizeHandle.GrabRightOf(scriptsEdge));

        if (!blocksCollapsed)
            _blocksEdge.Draw(gui, blocksEdgeId, blocksEdge, ResizeHandle.GrabRightOf(blocksEdge));

        return hovered;
    }
}
