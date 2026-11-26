using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptEditor
{
    private const float UsageThickness = 1;

    private (string Text, int Caret) _usagesFound;
    private IReadOnlyList<TextSpan> _usages = [];

    /// <summary>
    /// Where else the name at the caret is written, as the editor drew it this frame; empty when it drew none, such as
    /// while it isn't focused.
    /// </summary>
    public IReadOnlyList<TextSpan> DrawnUsages { get; private set; } = [];

    /// <summary>
    /// Underlines every other place the name at the caret is written, while the field has a typing cursor. Read again
    /// only when the text or the caret change.
    /// </summary>
    private void DrawUsages(ImGui gui, ImRect textRect, SyntaxPalette palette, bool typing)
    {
        DrawnUsages = [];
        if (!typing)
            return;

        if (!ReferenceEquals(_usagesFound.Text, Text) || _usagesFound.Caret != _state.Caret)
        {
            _usagesFound = (Text, _state.Caret);
            _usages = NameOccurrences.At(Text, _state.Caret);
        }

        foreach (var span in _usages)
        {
            if (span.End > Text.Length)
                continue;

            var line = ImTextEdit.FindLineAtCaretPosition(span.Start, in _layout, out var offset);
            var from = ImTextEdit.LineOffsetToViewPosition(line, offset, Text, textRect, gui.TextDrawer, in _layout);
            var to = ImTextEdit.LineOffsetToViewPosition(line, offset + span.Length, Text, textRect, gui.TextDrawer, in _layout);
            // Where the Ctrl+click underline goes, above an error underline
            var y = from.y - _layout.LineHeight + (ErrorThickness * 2) + JumpThickness;
            gui.Canvas.Line(new Vector2(from.x, y), new Vector2(to.x, y), palette.Usage, UsageThickness);
        }

        DrawnUsages = _usages;
    }
}
