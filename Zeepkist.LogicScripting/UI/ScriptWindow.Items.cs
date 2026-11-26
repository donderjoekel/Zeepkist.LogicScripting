using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using UnityEngine;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptWindow
{
    /// <summary>
    /// Writes the Draft of every Script that changed since its last one. Also call when the mod unloads.
    /// </summary>
    public void SaveDrafts()
    {
        _draftSaver.Save();
    }

    /// <summary>
    /// Writes the Libraries if they changed since they were last written. Also call when the mod unloads.
    /// </summary>
    public void SaveLibraries()
    {
        _librarySaver.Save();
    }

    /// <summary>
    /// Drops every Script, so the next level's come from that level, after saving their Drafts. Scripts the level
    /// doesn't hold are only kept in their Drafts.
    /// </summary>
    public void ForgetScripts()
    {
        _draftSaver.SaveAndForget();
        _scripts.Clear();
        _editors.Clear();
        _renamePrompt.Close();
        _scriptListPanel.Clear();
        _orphanedGroupsPanel.ClearMessage();
        _readLevelChanges = null;

        if (_edited?.BelongsToLevel ?? true)
            Select(null);
    }

    /// <summary>
    /// Reads the level's Scripts again after the level changed, but not while a level is still loading, when the old
    /// level's blocks are still there.
    /// </summary>
    private void RefreshScripts()
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null || central.saveload.isLoading || _readLevelChanges == LevelChanges.Count)
            return;

        var (stored, blockCounts) = LevelScripts.Find(central);
        var dropped = _scripts.Update(stored, blockCounts, _drafts.Find);
        _readLevelChanges = LevelChanges.Count;

        // Under another name it differs from what was taken as saved on import, and would be written over its Draft
        foreach (var entry in _scripts.RedoneImports)
            _draftSaver.TakeAsSaved(entry);

        foreach (var entry in dropped)
        {
            _scriptListPanel.ClosePromptOn(entry);
            ForgetScript(entry);
        }

        // It says the group was imported
        if (dropped.Count > 0)
            _orphanedGroupsPanel.ClearMessage();

        // A Compile or undo changes the text a Draft is based on
        _draftSaver.Request();
        if (_edited == null && _scripts.Entries.Count > 0)
            Select(_scripts.Entries[0]);
    }

    /// <summary>
    /// Orphaned Groups are listed under the Scripts, in the same scroll region.
    /// </summary>
    private void DrawScriptsTab(ImGui gui, ImRect rect)
    {
        _scriptListPanel.BeginList(gui, rect);

        // A scope for the Scripts, so one coming or going doesn't change the ids of the Orphaned Groups' buttons
        gui.PushId("scripts list");
        var selected = _scriptListPanel.DrawEntries(gui, CurrentScript);
        gui.PopId();

        // Imported or restored after drawing, which goes through the list that changes
        gui.PushId("orphaned groups");
        var collapsed = _state.OrphanedGroupsCollapsed;
        var (imported, restored) = _orphanedGroupsPanel.Draw(gui, _scripts.OrphanedGroups, ref collapsed);
        _state.OrphanedGroupsCollapsed = collapsed;
        gui.PopId();
        if (imported != null)
            ImportOrphan(imported);

        if (restored != null)
            RestoreOrphan(restored);

        selected = _scriptListPanel.EndList(gui) ?? selected;
        if (selected != null)
            Select(selected);
    }

    /// <summary>
    /// Selects the Script made from the group's blocks, or makes none. The group list's result line says which, or why
    /// not.
    /// </summary>
    private void ImportOrphan(OrphanedGroup orphan)
    {
        var name = _scripts.GetFreeName(orphan.Draft?.Name ?? ScriptImport.DefaultName);
        var stored = ScriptImport.RunOrphan(orphan.Id, name, _startupCheck, _logger, out var message);
        if (stored != null)
        {
            var entry = _scripts.AddImportedOrphan(stored);
            _draftSaver.TakeAsSaved(entry);
            Select(entry);
        }

        // Selecting clears it
        _orphanedGroupsPanel.ShowMessage(message);
    }

    private void RestoreOrphan(OrphanedGroup orphan)
    {
        var restored = _scripts.Restore(orphan);
        Select(restored);
        _orphanedGroupsPanel.ShowRestored(restored);
        _draftSaver.Request();
    }

    private void DrawLibrariesTab(ImGui gui, ImRect rect)
    {
        var readFailed = ShowsLibraryReadFailed;
        _libraryReadFailedLayout = new WarningLayout(Time.frameCount, readFailed);
        var library = _libraryListPanel.Draw(gui, rect, CurrentLibrary, readFailed);
        if (library != null)
            SelectLibrary(library);
    }

    private void OnLibraryDeleted(Library library)
    {
        _libraryEditors.Remove(library.Id);
        if (library == CurrentLibrary)
            Select(_scripts.Entries.Count > 0 ? _scripts.Entries[0] : null);
    }

    /// <summary>
    /// For a Script that's gone from the list.
    /// </summary>
    private void ForgetScript(ScriptEntry entry)
    {
        _editors.Remove(entry.Id);
        if (entry == CurrentScript)
            Select(_scripts.Entries.Count > 0 ? _scripts.Entries[0] : null);
    }

    private void Select(ScriptEntry entry)
    {
        Show(entry == null ? null : new EditedScript(entry, GetEditor(entry), _draftSaver, _startupCheck, Libraries, _logger));
    }

    private void SelectLibrary(Library library)
    {
        Show(new EditedLibrary(library, GetEditor(library), Libraries));
    }

    /// <summary>
    /// The result line belongs to the Script or Library it was made for, like the errors and checks, and the Orphaned
    /// Groups' to what the player did last.
    /// </summary>
    private void Show(EditedItem edited)
    {
        _edited = edited;
        _fieldFocused = false;
        _renamePrompt.Close();
        _result = string.Empty;
        _placedBlocksPanel.ClearMessage();
        _orphanedGroupsPanel.ClearMessage();
    }

    private ScriptEditor GetEditor(ScriptEntry entry)
    {
        if (!_editors.TryGetValue(entry.Id, out var editor))
        {
            editor = new ScriptEditor(entry.Text, _editorAssist);
            _editors.Add(entry.Id, editor);
        }

        return editor;
    }

    private ScriptEditor GetEditor(Library library)
    {
        if (!_libraryEditors.TryGetValue(library.Id, out var editor))
        {
            editor = new ScriptEditor(library.Text, _libraryEditorAssist);
            _libraryEditors.Add(library.Id, editor);
        }

        return editor;
    }

    /// <summary>
    /// Returns whether Ctrl+Enter was pressed in the field.
    /// </summary>
    private bool DrawField(ImGui gui, ImRect rect)
    {
        var editor = _edited.Editor;
        if (editor.Draw(gui, rect, out var compileRequested))
            OnTyped();

        _fieldFocused = FieldFocus.Update(gui, editor.Id, editor.HoldsEscape);

        return compileRequested;
    }

    /// <summary>
    /// The rename prompt picked its name in the text as it was.
    /// </summary>
    private void OnTyped()
    {
        _renamePrompt.Close();
        _placedBlocksPanel.ClearMessage();
        OnEdited();
    }

    /// <summary>
    /// Null leaves the result line as it is: a check just found errors, which the error list shows.
    /// </summary>
    private void ShowResult(string result)
    {
        if (result != null)
            _result = result;
    }

    /// <summary>
    /// The result line belongs to the text it was made for.
    /// </summary>
    private void OnEdited()
    {
        _edited.OnEdited();
        _result = string.Empty;
    }
}
