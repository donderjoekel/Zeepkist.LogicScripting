using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Logging;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Lists the level's Orphaned Groups, each with Delete, Untag, Select, Import and Restore Script from Draft. Delete and
/// Untag are one undo step each, Select is the editor's own selection step; importing and restoring are the caller's.
/// </summary>
internal sealed class OrphanedGroupsPanel
{
    private readonly ManualLogSource _logger;
    private string _message = string.Empty;

    // Set by the in-game tests
    private OrphanedGroup _pressedImport;

    public string Message => _message;

    public bool IsInputPending => _pressedImport != null;

    public OrphanedGroupsPanel(ManualLogSource logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// The result line as the list was last drawn, empty when it drew none, and the Unity frame that was in, for the
    /// in-game tests.
    /// </summary>
    public (int Frame, string Text) DrawnMessage { get; private set; } = (-1, string.Empty);

    /// <summary>
    /// What the row calls the group.
    /// </summary>
    public static string GetName(OrphanedGroup orphan)
    {
        return orphan.Draft?.Name ?? "Unnamed";
    }

    /// <summary>
    /// Presses Import on the group's row the next time it's drawn.
    /// </summary>
    public void PressImport(OrphanedGroup orphan)
    {
        _pressedImport = orphan;
    }

    public void CancelInput()
    {
        _pressedImport = null;
    }

    public void ClearMessage()
    {
        _message = string.Empty;
    }

    /// <summary>
    /// Returns the Orphaned Groups the player chose to import and to restore from its Draft, null for none. Collapsed,
    /// the section is its heading and the result line, and without Orphaned Groups left only the result line.
    /// </summary>
    public (OrphanedGroup Imported, OrphanedGroup Restored) Draw(ImGui gui, IReadOnlyList<OrphanedGroup> orphanedGroups, ref bool collapsed)
    {
        DrawnMessage = (Time.frameCount, string.Empty);
        if (orphanedGroups.Count > 0 && DrawHeading(gui, orphanedGroups.Count, collapsed))
            collapsed = !collapsed;

        var chosen = collapsed ? default : DrawGroups(gui, orphanedGroups);
        if (_message.Length > 0)
        {
            gui.Text(_message, wrap: true);
            DrawnMessage = (Time.frameCount, _message);
        }

        return chosen;
    }

    private (OrphanedGroup Imported, OrphanedGroup Restored) DrawGroups(ImGui gui, IReadOnlyList<OrphanedGroup> orphanedGroups)
    {
        OrphanedGroup imported = null;
        OrphanedGroup restored = null;
        foreach (var orphan in orphanedGroups)
        {
            var name = GetName(orphan);
            gui.Text(orphan.BlockCount == 1 ? $"{name}, 1 block" : $"{name}, {orphan.BlockCount} blocks", wrap: true);

            // Three short buttons share a row and the others get their own, so all fit the narrow Scripts side
            gui.AddSpacingIfLayoutFrameNotEmpty();
            var row = gui.AddSingleRowRect(default);
            var third = (row.W - (gui.Style.Layout.Spacing * 2)) / 3;
            var deleteRect = row.TakeLeft(third, gui.Style.Layout.Spacing, out var rest);
            var untagRect = rest.TakeLeft(third, gui.Style.Layout.Spacing, out var selectRect);
            if (gui.Button("Delete", deleteRect))
                Edit(orphan, "Deleted", BlockGroupEdits.Delete);

            if (gui.Button("Untag", untagRect))
                Edit(orphan, "Untagged", BlockGroupEdits.Untag);

            gui.TooltipAtLastControl("Keep the blocks as plain Logic Blocks");

            if (gui.Button("Select", selectRect))
                Select(orphan);

            if (gui.Button("Import") || orphan == _pressedImport)
            {
                imported = orphan;
                _pressedImport = null;
            }

            gui.TooltipAtLastControl("Make a Script from these blocks, which stay as they are until you compile it");

            gui.BeginReadOnly(orphan.Draft == null);
            if (gui.Button("Restore from Draft") && orphan.Draft != null)
                restored = orphan;

            gui.EndReadOnly();
            gui.TooltipAtLastControl(orphan.Draft == null
                ? "This machine has no Draft of this Script"
                : "Bring the Script back from this machine's Draft. Compile to store it in the level again");
        }

        return (imported, restored);
    }

    /// <summary>
    /// Drawn like a Reference category's header, in the warning color and with the count on the right. Returns whether
    /// it was clicked.
    /// </summary>
    private static bool DrawHeading(ImGui gui, int count, bool collapsed)
    {
        gui.AddSpacingIfLayoutFrameNotEmpty();
        var rect = gui.AddSingleRowRect(default);
        var clicked = gui.Button(gui.GetNextControlId(), rect, in gui.Style.Foldout.Button, out _);
        gui.TooltipAtLastControl("Blocks of Scripts whose text the level no longer holds. Import or restore one to compile it again");

        var spacing = gui.Style.Layout.InnerSpacing;
        var content = ImButton.CalculateContentRect(gui, rect);
        var arrowRect = content.TakeLeft(gui.Style.Layout.TextSize, spacing, out var labelRect).WithAspect(1.0f);
        if (collapsed)
            ImFoldout.DrawArrowRight(gui.Canvas, arrowRect, WindowColors.Warning, gui.Style.Foldout.ArrowScale);
        else
            ImFoldout.DrawArrowDown(gui.Canvas, arrowRect, WindowColors.Warning, gui.Style.Foldout.ArrowScale);

        var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f, overflow: ImTextOverflow.Ellipsis);
        var countText = count.ToString(CultureInfo.InvariantCulture);
        var countRect = labelRect.TakeRight(gui.MeasureTextSize(countText, settings).x, spacing, out labelRect);
        gui.Text("Orphaned Groups", settings, WindowColors.Warning, labelRect);
        gui.Text(countText, settings, WindowColors.Warning, countRect);
        return clicked;
    }

    public void ShowMessage(string message)
    {
        _message = message;
    }

    public void ShowRestored(ScriptEntry entry)
    {
        _message = $"Restored \"{entry.Name}\" from its Draft. Compile to store it in the level again";
    }

    private void Edit(OrphanedGroup orphan, string verb, Func<LEV_LevelEditorCentral, ScriptId, int> edit)
    {
        var central = GetCentral();
        if (central == null)
            return;

        var count = LevelEdit.TryEditGroup(_logger, $"{verb} orphaned group", () => edit(central, orphan.Id));
        if (count == null)
            _message = "That failed, level unchanged";
        else
            _message = count == 1 ? $"{verb} 1 block" : $"{verb} {count} blocks";
    }

    private void Select(OrphanedGroup orphan)
    {
        var central = GetCentral();
        if (central == null)
            return;

        var group = BlockGroups.Find(central, orphan.Id);
        BlockGroups.Select(central, group);
        _message = $"Selected {group.Count} blocks";
    }

    /// <summary>
    /// Null, with the reason as the message, when the level can't take a change now.
    /// </summary>
    private LEV_LevelEditorCentral GetCentral()
    {
        var central = LevelEdit.GetCentral(out var blocker);
        if (central == null)
            _message = blocker;

        return central;
    }
}
