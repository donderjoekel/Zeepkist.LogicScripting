using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using TNRD.Zeepkist.LogicScripting.Blocks;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptWindow
{
    /// <summary>
    /// The startup check's warnings, the header with Compile, the storage warning, the editor, then the error panel: a
    /// header with the error count, and the error list and result line in a region of their own, so a long list scrolls
    /// instead of pushing the editor away. The editor takes the height the panel doesn't, and each warning scrolls in a
    /// region of its own where there isn't room for it.
    /// </summary>
    private void DrawCenter(ImGui gui, ImRect rect)
    {
        var spacing = gui.Style.Layout.Spacing;
        gui.Layout.Push(ImAxis.Vertical, rect.WithPadding(spacing));
        var storageWarning = _edited == _storageWarningOf ? _storageWarning : null;

        var settings = WrappedText.GetSettings(gui);
        var minUnderStartupHeight = spacing + (_edited == null
            ? WrappedText.GetHeight(gui, NoScriptText, in settings, gui.GetLayoutWidth(), minHeight: 0)
            : GetMinScriptHeight(gui, storageWarning != null, _renamePrompt.GetHeight(gui)));
        var startupWarnings = GetStartupWarnings();
        _startupWarningLayout = new WarningLayout(Time.frameCount, startupWarnings.Count > 0);
        DrawWarnings(gui, startupWarnings, minUnderStartupHeight, "startup check");
        if (_edited == null)
        {
            gui.Text(NoScriptText, in settings);
            gui.Layout.Pop();
            return;
        }

        var compileClicked = _edited.DrawHeader(gui);
        if (storageWarning != null)
            DrawWarnings(gui, [storageWarning], GetMinEditorHeight(gui), "storage warning");

        DrawRenamePrompt(gui);
        _edited.RefreshCheck();

        var available = gui.Layout.GetAvailableHeight() - (spacing * 2);
        var minFieldHeight = gui.GetRowsHeightWithSpacing(MinFieldRows);
        var headerHeight = GetPanelHeaderHeight(gui);
        var defaultErrorsHeight = gui.GetRowsHeightWithSpacing(DefaultErrorRows);
        var minErrorsHeight = gui.GetRowsHeightWithSpacing(MinErrorRows);
        var maxErrorsHeight = available - headerHeight - minFieldHeight;
        var errorsBodyHeight = GetPanelSize(_state.Errors.Size, defaultErrorsHeight, minErrorsHeight, maxErrorsHeight);
        var errorsHeight = headerHeight + (_state.Errors.Collapsed ? 0 : errorsBodyHeight);

        var errorsEdgeId = gui.GetControlId("errors edge");
        if (!_state.Errors.Collapsed && _errorsEdge.Drag(gui, errorsEdgeId, errorsBodyHeight) is { } draggedErrorsHeight)
            _state.Errors.Size = ClampSize(draggedErrorsHeight, minErrorsHeight, maxErrorsHeight);

        var fieldHeight = Mathf.Max(minFieldHeight, available - errorsHeight);
        var field = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), fieldHeight);
        var compileRequested = DrawField(gui, field);
        if (_edited.Editor.RenameRequested)
            StartRename();

        ShowResult(compileClicked ? _edited.Compile() : compileRequested ? _edited.CompileFromKeyboard() : null);

        var errors = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), Mathf.Max(0, gui.Layout.GetAvailableHeight() - spacing));
        gui.Layout.Pop();

        gui.Box(errors, in gui.Style.List.Box);
        var header = errors.TakeTop(headerHeight, out var body);
        _editorLayout = new EditorLayout(Time.frameCount, rect, field, header, minFieldHeight, storageWarning != null);
        if (DrawErrorsHeader(gui, header))
            _state.Errors.ToggleCollapsed();

        // A window too short for the editor's minimum leaves no room under the header
        if (_state.Errors.Collapsed || body.H <= 0)
            return;

        var hasErrors = _edited.HasCurrentErrors;
        var columns = default(ErrorColumns);
        if (hasErrors)
        {
            columns = ErrorTable.Measure(gui, _edited.Errors);
            var columnsHeader = body.TakeTop(ErrorTable.GetHeaderHeight(gui), out body);
            ErrorTable.DrawHeader(gui, columnsHeader.WithPadding(left: spacing, right: spacing), columns);
            if (body.H <= 0)
                return;
        }

        WindowRegion.Begin(gui, body);
        if (hasErrors && ErrorTable.DrawRows(gui, _edited.Errors, columns) is { } clicked)
            _edited.Editor.JumpTo(clicked);

        if (_result.Length > 0)
            gui.Text(_result, wrap: true);

        WindowRegion.End(gui);

        // Centered in the gap above the panel, so the grab strip takes as little as it can of the editor's edge
        var edge = new ImRect(errors.X, errors.Top + (spacing / 2), errors.W, 0);
        _errorsEdge.Draw(gui, errorsEdgeId, edge, _errorsEdge.GrabAround(edge));
    }

    /// <summary>
    /// Returns whether the hide button was clicked. A hidden panel is only this row, which shows the result line in
    /// place of the error count while there is one, as the list it's drawn under isn't shown.
    /// </summary>
    private bool DrawErrorsHeader(ImGui gui, ImRect header)
    {
        var row = header.WithPadding(gui.Style.Layout.Spacing);
        var buttonRect = row.TakeRight(gui.GetRowHeight(), gui.Style.Layout.Spacing, out var labelRect);

        var (text, color) = _state.Errors.Collapsed && _result.Length > 0 ? (_result, gui.Style.Text.Color) : GetErrorsSummary();
        var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f, overflow: ImTextOverflow.Ellipsis);
        gui.Text(text, settings, color, labelRect);
        if (labelRect.Contains(gui.Input.MousePosition) && gui.MeasureTextSize(text, settings).x > labelRect.W)
            gui.Tooltip(text, labelRect);

        var clicked = gui.Button(_state.Errors.Collapsed ? "▲" : "▼", buttonRect);
        gui.TooltipAtLastControl(_state.Errors.Collapsed ? "Show the error panel" : "Hide the error panel");
        return clicked;
    }

    private (string Text, Color32 Color) GetErrorsSummary()
    {
        if (_edited.HasCurrentErrors)
        {
            var count = _edited.Errors.Count;
            return (count == 1 ? "1 error" : $"{count} errors", WindowColors.Error);
        }

        // A pending check hides the old errors, so the panel doesn't claim the text is fine
        return (_edited.IsChecked ? "No errors found" : "Checking...", WindowColors.Muted);
    }

    private IReadOnlyList<string> GetStartupWarnings()
    {
        var warnings = new List<string>();
        if (ShowsUpdateNeeded)
            warnings.Add($"{StartupCheck.UpdateNeededMessage}. Scripts are still checked, but Compile places nothing.");

        var disabledBlocks = ShownDisabledBlocks;
        if (disabledBlocks.Count > 0)
            warnings.Add($"Turned off for this game version: {string.Join(", ", disabledBlocks)}");

        return warnings;
    }

    /// <summary>
    /// Adds the warnings' region to the center's layout, leaving <paramref name="minRestHeight"/> under it.
    /// </summary>
    private static void DrawWarnings(ImGui gui, IReadOnlyList<string> warnings, float minRestHeight, string id)
    {
        if (warnings.Count == 0)
            return;

        gui.AddSpacingIfLayoutFrameNotEmpty();
        var width = gui.GetLayoutWidth();
        var height = WarningRegion.GetHeight(gui, warnings, width, gui.Layout.GetAvailableHeight(), minRestHeight);
        WarningRegion.Draw(gui, gui.Layout.AddRect(width, height), warnings, id);
    }
}
