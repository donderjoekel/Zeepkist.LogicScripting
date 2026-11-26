using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// F2 in a Script's editor renames the name at the caret, at its definition and every use in that Script.
/// </summary>
internal sealed partial class ScriptWindow
{
    private readonly RenamePrompt _renamePrompt = new();

    /// <summary>
    /// Opens the prompt for the name at the caret, or says why there's none. A Library's Functions are called from
    /// other Scripts, which a rename can't reach, so a Library's editor has none.
    /// </summary>
    private void StartRename()
    {
        if (_edited is not EditedScript)
            return;

        var editor = _edited.Editor;
        var name = NameRename.Find(editor.Text, editor.Caret, editor.Lookups, out var error);
        if (name == null)
        {
            _result = error;
            return;
        }

        _result = string.Empty;
        _renamePrompt.Open(name, editor.Caret, editor.Text);
    }

    /// <summary>
    /// The prompt, while it's open. The rename is one undo step, and the blocks only change at the next Compile. Any
    /// change to the text closes it, as its index may then be on another name.
    /// </summary>
    private void DrawRenamePrompt(ImGui gui)
    {
        var editor = _edited.Editor;
        if (_renamePrompt.IsOpen && _renamePrompt.Text != editor.Text)
            _renamePrompt.Close();

        var wasOpen = _renamePrompt.IsOpen;
        var newName = _renamePrompt.Draw(gui, null);
        if (newName == null)
        {
            if (wasOpen && !_renamePrompt.IsOpen)
                editor.JumpTo(editor.Caret);

            return;
        }

        var oldName = _renamePrompt.Name;
        var result = NameRename.Rename(editor.Text, _renamePrompt.Index, newName, editor.Lookups);
        if (!result.Succeeded)
        {
            _renamePrompt.ShowError(result.Error);
            return;
        }

        _renamePrompt.Close();
        if (result.Count > 0)
        {
            editor.Replace(result.Text, result.Caret, gui.Input.Time);
            OnEdited();
            _result = result.Count == 1
                ? $"Renamed \"{oldName}\" to \"{newName}\""
                : $"Renamed \"{oldName}\" to \"{newName}\" in {result.Count} places";
        }

        editor.JumpTo(result.Caret);
    }
}
