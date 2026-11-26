using UnityEngine;
using ZeepSDK.LevelEditor;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptWindow
{
    /// <summary>
    /// Also call when the mod unloads, or the level editor's input stays blocked.
    /// </summary>
    public void ReleaseInput()
    {
        UnblockEditor();
        _highlight.Set(ComponentCache.Get<LEV_LevelEditorCentral>(), null);
    }

    private void UnblockEditor()
    {
        _mouseOver = false;
        _fieldFocused = false;
        ReleasePanelFocus();
        // Not in ReleasePanelFocus, which runs every frame: the prompt reads its keys by last frame's focus
        _renamePrompt.ReleaseFocus();
        SetMouseBlocked(false);
        SetKeyboardBlocked(false);
    }

    private void ReleasePanelFocus()
    {
        _scriptListPanel.ReleaseFocus();
        _libraryListPanel.ReleaseFocus();
        _referencePanel.ReleaseFocus();
    }

    private static bool IsAnyMouseButtonDown()
    {
        return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
    }

    private void SetMouseBlocked(bool blocked)
    {
        if (blocked == _blockingMouse)
            return;

        if (blocked)
            LevelEditorApi.BlockMouseInput(this);
        else
            LevelEditorApi.UnblockMouseInput(this);

        _blockingMouse = blocked;
    }

    private void SetKeyboardBlocked(bool blocked)
    {
        if (blocked == _blockingKeyboard)
            return;

        if (blocked)
            LevelEditorApi.BlockKeyboardInput(this);
        else
            LevelEditorApi.UnblockKeyboardInput(this);

        _blockingKeyboard = blocked;
    }
}
