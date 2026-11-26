using Imui.Controls;
using Imui.Core;
using Imui.IO.Events;
using Imui.Rendering;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The new name for a name in the Script, in one row between the name row and the editor, opened with F2.
/// </summary>
/// <remarks>
/// Enter is taken from Imui's key event, not Unity's <c>Input</c>, which comes a frame earlier: the editor, focused
/// again by then, would get the Enter as a line break.
/// </remarks>
internal sealed class RenamePrompt
{
    private const string OkLabel = "Rename";
    private const string CancelLabel = "Cancel";

    private string _input = string.Empty;
    private string _error = string.Empty;
    private bool _focusRequested;

    public bool IsOpen { get; private set; }

    public bool Focused { get; private set; }

    /// <summary>
    /// The name being renamed.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Where the caret was in the Script when it opened, which picks the name.
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// The error the last confirmed name got, or empty.
    /// </summary>
    public string Error => _error;

    /// <summary>
    /// The row, and the error's while there is one.
    /// </summary>
    public float GetHeight(ImGui gui)
    {
        if (!IsOpen)
            return 0;

        var rows = _error.Length > 0 ? 2 : 1;
        return gui.GetRowsHeightWithSpacing(rows) + gui.Style.Layout.Spacing;
    }

    public void Open(string name, int index)
    {
        Name = name;
        Index = index;
        _input = name;
        _error = string.Empty;
        IsOpen = true;
        _focusRequested = true;
    }

    public void Close()
    {
        IsOpen = false;
        Focused = false;
        _error = string.Empty;
    }

    /// <summary>
    /// Replaces the text in the field, as typing it would.
    /// </summary>
    public void SetName(string name)
    {
        _input = name;
        _error = string.Empty;
    }

    /// <summary>
    /// Keeps the prompt open with why the name was refused.
    /// </summary>
    public void ShowError(string error)
    {
        _error = error ?? string.Empty;
        _focusRequested = true;
    }

    /// <summary>
    /// Returns the name once the player confirms it, or null. <paramref name="pressed"/> is a button pressed for the
    /// player, handled as clicked. Canceling closes it.
    /// </summary>
    public string Draw(ImGui gui, PromptButton? pressed)
    {
        if (!IsOpen)
        {
            Focused = false;
            return null;
        }

        gui.AddSpacingIfLayoutFrameNotEmpty();
        var row = gui.AddSingleRowRect(default);
        var spacing = gui.Style.Layout.Spacing;
        var cancelRect = row.TakeRight(GetButtonWidth(gui, CancelLabel), spacing, out row);
        var okRect = row.TakeRight(GetButtonWidth(gui, OkLabel), spacing, out row);
        var label = $"Rename \"{Name}\" to";
        var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f, overflow: ImTextOverflow.Ellipsis);
        var labelRect = row.TakeLeft(Mathf.Min(gui.MeasureTextSize(label, settings).x, row.W / 2), spacing, out var fieldRect);
        gui.Text(label, settings, labelRect);

        var id = gui.GetNextControlId();
        ref var state = ref gui.Storage.Get<ImTextEditState>(id);
        if (_focusRequested)
        {
            // All of it selected, so typing replaces the old name
            gui.SetActiveControl(id);
            state.Caret = _input.Length;
            state.Selection = -_input.Length;
            _focusRequested = false;
        }

        var key = Focused ? TakeKey(gui) : null;
        if (gui.TextEdit(id, ref _input, ref state, fieldRect, false))
            _error = string.Empty;

        Focused = gui.IsControlActive(id);
        var confirmed = key is KeyCode.Return || gui.Button(OkLabel, okRect) || pressed == PromptButton.Ok;
        // Unity's Escape too, so the level editor never sees one while the prompt has focus; its key event a frame
        // later finds the editor without a held Escape, which ignores it
        var canceled = key is KeyCode.Escape
            || (Focused && Input.GetKeyDown(KeyCode.Escape))
            || gui.Button(CancelLabel, cancelRect)
            || pressed == PromptButton.Cancel;
        if (_error.Length > 0)
            gui.Text(_error, wrap: true);

        if (canceled)
        {
            Close();
            return null;
        }

        return confirmed ? _input.Trim() : null;
    }

    /// <summary>
    /// Takes this frame's Enter or Escape from the field before it sees them; null when neither was pressed. Enter
    /// also comes as a separate line break character, which is taken too: the editor, focused again this same frame
    /// once the prompt closes, would otherwise type it.
    /// </summary>
    private static KeyCode? TakeKey(ImGui gui)
    {
        KeyCode? taken = null;
        for (var i = 0; i < gui.Input.KeyboardEventsCount; i++)
        {
            ref readonly var evt = ref gui.Input.GetKeyboardEvent(i);
            if (evt.Type != ImKeyboardEventType.Down)
                continue;

            var key = evt.Key switch
            {
                KeyCode.Return or KeyCode.KeypadEnter => KeyCode.Return,
                KeyCode.Escape => KeyCode.Escape,
                KeyCode.None when evt.Char is '\n' or '\r' => KeyCode.Return,
                KeyCode.None when evt.Char is '\u001b' => KeyCode.Escape,
                _ => (KeyCode?)null
            };

            if (key == null)
                continue;

            gui.Input.UseKeyboardEvent(i);
            taken ??= key;
        }

        return taken;
    }

    private static float GetButtonWidth(ImGui gui, string label)
    {
        return gui.MeasureTextSize(label).x + (gui.Style.Layout.Indent * 2);
    }
}
