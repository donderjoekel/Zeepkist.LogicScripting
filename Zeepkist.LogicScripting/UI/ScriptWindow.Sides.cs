using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptWindow
{
    private static void DrawDivider(ImGui gui, ImRect edge)
    {
        gui.Canvas.Rect(edge, gui.Style.Window.Box.BorderColor);
    }

    private static ImRect GetSideBody(ImGui gui, ImRect side)
    {
        side.TakeTop(GetPanelHeaderHeight(gui), out var body);
        return body;
    }

    private static float GetPanelHeaderHeight(ImGui gui)
    {
        return gui.GetRowHeight() + (gui.Style.Layout.Spacing * 2);
    }

    /// <summary>
    /// Returns whether the collapse button was clicked. A collapsed side is only that button, which is grayed out while
    /// the window is too narrow to show the side.
    /// </summary>
    private static bool DrawSideHeader(ImGui gui, ImRect side, string label, bool collapsed, bool buttonOnRight, bool canToggle)
    {
        var size = gui.GetRowHeight();
        var row = side.TakeTop(GetPanelHeaderHeight(gui)).WithPadding(gui.Style.Layout.Spacing);
        ImRect labelRect;
        var buttonRect = buttonOnRight ? row.TakeRight(size, out labelRect) : row.TakeLeft(size, out labelRect);
        if (!collapsed)
            gui.Text(label, new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f), labelRect);

        gui.BeginReadOnly(!canToggle);
        var clicked = gui.Button(collapsed == buttonOnRight ? "»" : "«", buttonRect) && canToggle;
        gui.EndReadOnly();
        if (!canToggle)
            gui.TooltipAtLastControl($"Make the window wider to show the {label} panel");
        else
            gui.TooltipAtLastControl(collapsed ? $"Show the {label} panel" : $"Hide the {label} panel");

        return clicked;
    }

    /// <summary>
    /// The window keeps which tab is shown rather than Imui's tabs pane, whose choice is lost when the Blocks side is
    /// collapsed and doesn't draw it.
    /// </summary>
    private BlockProperties DrawBlocksTabs(ImGui gui, ImRect rect)
    {
        var bar = rect.WithPadding(gui.Style.Layout.Spacing).TakeTop(ImTabsPane.GetTabBarHeight(gui), out var content);
        var placedRect = bar.TakeLeft(bar.W / 2, out var referenceRect);
        if (ImTabsPane.TabBarButton(gui, gui.GetNextControlId(), !_showReference, PlacedBlocksTab, placedRect))
            _showReference = false;

        if (ImTabsPane.TabBarButton(gui, gui.GetNextControlId(), _showReference, ReferenceTab, referenceRect))
            _showReference = true;

        gui.Box(content, in gui.Style.Tabs.ContainerBox);

        // A scope per tab, so their scroll areas don't share one id, and with it one scroll position
        if (_showReference)
        {
            _referenceDrawnFrame = Time.frameCount;
            gui.PushId(ReferenceTab);
            var spacing = gui.Style.Layout.Spacing;
            var filter = content.WithPadding(spacing).TakeTop(gui.GetRowHeight(), out _);
            content.TakeTop(gui.GetRowHeight() + spacing, out var entries);
            _referencePanel.DrawFilter(gui, filter);
            WindowRegion.Begin(gui, entries);
            _referencePanel.DrawEntries(gui, _state.CollapsedReferenceCategories);
            WindowRegion.End(gui);
            gui.PopId();
            return null;
        }

        gui.PushId(PlacedBlocksTab);
        BlockProperties hovered = null;
        WindowRegion.Begin(gui, content);
        if (_edited == null)
        {
            gui.Text("No Script selected", wrap: true);
        }
        else
        {
            hovered = _edited.DrawPlacedBlocks(gui, _placedBlocksPanel, out var edited);
            if (edited)
                OnEdited();
        }

        WindowRegion.End(gui);
        gui.PopId();
        return hovered;
    }

    /// <summary>
    /// Like the Blocks side's tabs. Switching tabs leaves the editor as it is, so a dot marks the tab that holds what
    /// it shows, and a warning mark the Scripts tab while the level has Orphaned Groups.
    /// </summary>
    private void DrawScriptsTabs(ImGui gui, ImRect rect)
    {
        var bar = rect.WithPadding(gui.Style.Layout.Spacing).TakeTop(ImTabsPane.GetTabBarHeight(gui), out var content);
        var scriptsRect = bar.TakeLeft(bar.W / 2, out var librariesRect);
        var hasOrphans = _scripts.OrphanedGroups.Count > 0;
        if (SideTab.Draw(gui, !_state.ShowLibraries, ScriptsTab, scriptsRect, CurrentScript != null, hasOrphans))
            _state.ShowLibraries = false;

        gui.TooltipAtLastControl(hasOrphans ? $"{ScriptListPanel.Help}. The level has Orphaned Groups, listed under its Scripts" : ScriptListPanel.Help);

        if (SideTab.Draw(gui, _state.ShowLibraries, LibrariesTab, librariesRect, CurrentLibrary != null, false))
            _state.ShowLibraries = true;

        gui.TooltipAtLastControl(LibraryListPanel.Help);

        gui.Box(content, in gui.Style.Tabs.ContainerBox);

        // A scope per tab, so their scroll areas don't share one id, and with it one scroll position
        gui.PushId(_state.ShowLibraries ? LibrariesTab : ScriptsTab);
        if (_state.ShowLibraries)
            DrawLibrariesTab(gui, content);
        else
            DrawScriptsTab(gui, content);

        gui.PopId();
    }
}
