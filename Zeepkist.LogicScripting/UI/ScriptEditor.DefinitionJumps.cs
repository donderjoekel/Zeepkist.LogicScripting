using Imui.Controls;
using Imui.Core;
using Imui.IO.Events;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptEditor
{
    private const float JumpThickness = 1;

    private (string Text, int? Index) _jumpFound;
    private DefinitionJump _jump;

    // Until the next single click, so the rest of the click that jumped can't move the caret
    private bool _jumped;

    /// <summary>
    /// Whether the in-game tests hold the jump modifier, as a player holds Ctrl, for the clicks they send: Unity's
    /// <c>Input</c>, which a player's comes from, never reports their keys. Nothing in the mod itself sets it.
    /// </summary>
    public bool HoldsSentJumpModifier { get; set; }

    /// <summary>
    /// The Unity frame of the last click that jumped to a definition, or -1 before one did.
    /// </summary>
    public int JumpedFrame { get; private set; } = -1;

    /// <summary>
    /// The jump of the name under the mouse while Ctrl (Cmd on macOS) is held; null otherwise. Read again only when
    /// the text or the character change.
    /// </summary>
    private DefinitionJump FindJump(ImGui gui, ImRect textRect, ImRect textBounds, in ImTextEditBuffer buffer, bool hovered)
    {
        if (!hovered || !IsJumpModifierHeld())
            return null;

        var index = FindCharacterAt(gui, gui.Input.MousePosition, textRect, textBounds, in buffer);
        if (ReferenceEquals(_jumpFound.Text, Text) && _jumpFound.Index == index)
            return _jump;

        _jumpFound = (Text, index);
        _jump = index is { } at ? DefinitionJump.At(Text, at, _assist.Lookups) : null;
        return _jump;
    }

    private void DrawJumpUnderline(ImGui gui, DefinitionJump jump, ImRect textRect, SyntaxPalette palette)
    {
        var (from, to) = GetUnderlineEnds(gui, jump.Name, textRect);
        var y = GetRaisedUnderlineY(from);
        gui.Canvas.Line(new Vector2(from.x, y), new Vector2(to.x, y), palette.Get(null), JumpThickness);
    }

    /// <summary>
    /// Ctrl+click on a name with a jump goes to its definition instead of placing the caret, focusing the field if it
    /// wasn't; returns whether it took the mouse event. The drag and the double-click of that click are taken too, as
    /// they'd move the caret back to whatever text the jump scrolled under the mouse.
    /// </summary>
    private bool HandleJumpClick(ImGui gui, DefinitionJump jump)
    {
        ref readonly var evt = ref gui.Input.MouseEvent;
        if (evt.Type == ImMouseEventType.Down && evt.LeftButton && evt.Count == 1)
            _jumped = false;

        var isJumpClickRest = evt.Type is ImMouseEventType.BeginDrag or ImMouseEventType.Drag or ImMouseEventType.Down;
        if (_jumped && isJumpClickRest && evt.LeftButton)
        {
            gui.Input.UseMouseEvent();
            return true;
        }

        if (jump == null || evt.Type != ImMouseEventType.Down || !evt.LeftButton)
            return false;

        JumpTo(jump.Target.Start);
        _jumped = true;
        JumpedFrame = Time.frameCount;
        gui.Input.UseMouseEvent();
        return true;
    }

    /// <summary>
    /// Read from the keyboard rather than the mouse event, which only carries Ctrl, never Cmd. Not with Alt, like
    /// <see cref="IsCommand"/>, because AltGr arrives as Ctrl+Alt. Or held by the in-game tests, see
    /// <see cref="HoldsSentJumpModifier"/>.
    /// </summary>
    private bool IsJumpModifierHeld()
    {
        if (HoldsSentJumpModifier)
            return true;

        var control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        var command = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        return (control || command) && !alt;
    }
}
