using System.Collections.Generic;
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

            var (from, to) = GetUnderlineEnds(gui, span, textRect);
            var y = GetRaisedUnderlineY(from);
            gui.Canvas.Line(new Vector2(from.x, y), new Vector2(to.x, y), palette.Usage, UsageThickness);
        }

        DrawnUsages = _usages;
    }
}
