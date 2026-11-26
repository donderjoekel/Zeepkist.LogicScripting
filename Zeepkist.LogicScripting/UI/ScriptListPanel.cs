using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using TNRD.Zeepkist.LogicScripting.Storage;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The window's Scripts with their marks, New, Import selected, and per Script Rename, Select group and Delete.
/// </summary>
internal sealed class ScriptListPanel : ItemListPanel<ScriptEntry>
{
    public const string Help = "Right-click a Script to rename it, select its blocks or delete it";

    private const string ImportHelp = "Make a Script from the selected Logic Blocks, which stay as they are until you compile it";

    private readonly ManualLogSource _logger;
    private readonly StartupCheck _startupCheck;
    private readonly ScriptList _scripts;
    private readonly DraftSaver _draftSaver;
    private readonly Action<ScriptEntry> _deleted;

    /// <summary>
    /// <paramref name="deleted"/> is called with a Script after Delete dropped it from <paramref name="scripts"/>.
    /// </summary>
    public ScriptListPanel(
        ManualLogSource logger,
        StartupCheck startupCheck,
        ScriptList scripts,
        DraftSaver draftSaver,
        Action<ScriptEntry> deleted)
        : base("Script")
    {
        _logger = logger;
        _startupCheck = startupCheck;
        _scripts = scripts;
        _draftSaver = draftSaver;
        _deleted = deleted;
    }

    protected override IReadOnlyList<ScriptEntry> Entries => _scripts.Entries;
    protected override string EmptyText => "None yet in this level. New Script makes one, and right-clicking a Script renames it, selects its blocks or deletes it.";

    protected override bool HasImport => true;

    /// <summary>
    /// Like <see cref="ItemListPanel{T}.GetMinHeight"/>, with Import selected's row.
    /// </summary>
    public static float GetMinHeightWithImport(ImGui gui)
    {
        return GetMinHeight(gui) + WindowRegion.GetRowHeight(gui);
    }

    protected override ScriptEntry DrawImport(ImGui gui, ImRect rect)
    {
        var central = ScriptImport.GetCentral(out var blocker);
        gui.BeginReadOnly(blocker != null);
        var clicked = gui.Button(central == null ? "Import selected" : $"Import selected ({central.selection.list.Count})", rect);
        gui.EndReadOnly();
        gui.TooltipAtLastControl(blocker ?? ImportHelp);
        return clicked && blocker == null ? ImportSelected() : null;
    }

    /// <summary>
    /// What Import selected does. Returns the new Script, or null with why not as the result line.
    /// </summary>
    public ScriptEntry ImportSelected()
    {
        ClosePrompts();
        var stored = ScriptImport.RunSelected(_scripts.GetFreeName(ScriptImport.DefaultName), _startupCheck, _logger, out var message);
        Message = message;
        if (stored == null)
            return null;

        var entry = _scripts.Add(stored);
        _draftSaver.Request();
        return entry;
    }

    protected override string GetName(ScriptEntry entry)
    {
        return entry.Name;
    }

    protected override string GetLabel(ScriptEntry entry)
    {
        var (glyph, _, _) = ScriptMarkText.Get(entry.Mark);
        return glyph == null ? entry.Name : $"{glyph} {entry.Name}";
    }

    protected override string GetTooltip(ScriptEntry entry)
    {
        var (_, _, tooltip) = ScriptMarkText.Get(entry.Mark);
        return tooltip == null ? Help : $"{tooltip}. {Help}";
    }

    protected override void DrawMenuItems(ImGui gui, ScriptEntry entry)
    {
        if (gui.Menu("Select group"))
            SelectGroup(entry);
    }

    protected override string GetNameError(string name, ScriptEntry renamed)
    {
        return _scripts.GetNameError(name, renamed?.Id);
    }

    protected override ScriptEntry Add(string name)
    {
        var entry = _scripts.Add(name);
        _draftSaver.Request();
        return entry;
    }

    protected override void Rename(ScriptEntry entry, string name)
    {
        entry.Name = name;
        _draftSaver.Request();
    }

    protected override bool AsksAboutBlocks(ScriptEntry entry)
    {
        return entry.HasGroup;
    }

    protected override string GetDeleteQuestion(ScriptEntry entry)
    {
        return entry.HasGroup
            ? $"Delete \"{entry.Name}\"? Its blocks can go too, or stay as plain Logic Blocks."
            : $"Delete \"{entry.Name}\"? It has no blocks in the level.";
    }

    /// <summary>
    /// Removes the Script's blocks or untags them as one undo step, then drops the Script from the window. Its Draft
    /// stays, so undo brings the Script back with the text typed since its last Compile.
    /// </summary>
    protected override void Delete(ScriptEntry entry, DeleteChoice choice)
    {
        var central = GetCentral();
        if (central == null)
            return;

        var edited = LevelEdit.TryEditGroup(
            _logger,
            $"Deleting Script \"{entry.Name}\"",
            () => choice == DeleteChoice.KeepBlocks
                ? BlockGroupEdits.Untag(central, entry.Id)
                : BlockGroupEdits.Delete(central, entry.Id));

        if (edited is not { } count)
        {
            Message = "Delete failed, level unchanged";
            return;
        }

        _scripts.Remove(entry.Id);
        _deleted(entry);

        if (count == 0)
            Message = $"Deleted \"{entry.Name}\"";
        else if (choice == DeleteChoice.KeepBlocks)
            Message = $"Deleted \"{entry.Name}\" and kept its {count} blocks as plain Logic Blocks";
        else
            Message = $"Deleted \"{entry.Name}\" and its {count} blocks";
    }

    /// <summary>
    /// The level editor, or null with why not as the message.
    /// </summary>
    private LEV_LevelEditorCentral GetCentral()
    {
        var central = LevelEdit.GetCentral(out var blocker);
        if (central == null)
            Message = blocker;

        return central;
    }

    /// <summary>
    /// What Select group in the Script's context menu does.
    /// </summary>
    public void SelectGroup(ScriptEntry entry)
    {
        var central = GetCentral();
        if (central == null)
            return;

        var group = BlockGroups.Find(central, entry.Id);
        if (group.Count == 0)
        {
            Message = $"\"{entry.Name}\" has no blocks in the level";
            return;
        }

        BlockGroups.Select(central, group);
        Message = $"Selected {group.Count} blocks of \"{entry.Name}\"";
    }
}
