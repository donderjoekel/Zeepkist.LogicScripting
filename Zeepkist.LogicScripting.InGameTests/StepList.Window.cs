using System;
using System.Collections.Generic;
using System.Globalization;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

internal static partial class StepList
{
    /// <summary>
    /// <c>&lt;pixels&gt;</c>, below 0 to drag up, or left when <paramref name="sideways"/>, and optionally <c>over &lt;frames&gt;</c>, 1 when left out,
    /// with <c>then</c> before each move after the first, all in one press.
    /// </summary>
    private static ScenarioStep DragWindowEdge(string step, string argument, bool sideways)
    {
        var direction = sideways ? "right, below 0 for left" : "down, below 0 for up";
        var what = $"the pixels to drag the window's edge {direction}, and optionally 'over <frames>', with 'then' before each move after the first";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        var moves = new List<DragMove>();
        var start = 0;
        // Each move ends at the next 'then', or at the end of the words
        for (var end = 0; end <= words.Length; end++)
        {
            if (end < words.Length && words[end] != "then")
                continue;

            if (!TryReadDragMove(words, start, end - start, out var move))
                throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

            moves.Add(move);
            start = end + 1;
        }

        return new DragWindowEdgeStep(moves, sideways);
    }

    /// <summary>
    /// Reads <c>&lt;pixels&gt; [over &lt;frames&gt;]</c> from the <paramref name="count"/> words at <paramref name="start"/>.
    /// </summary>
    private static bool TryReadDragMove(string[] words, int start, int count, out DragMove move)
    {
        move = default;
        var frames = 1;
        if (count == 0 || !int.TryParse(words[start], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var pixels) || pixels == 0
            || !(count == 1 || (count == 3 && words[start + 1] == "over" && StepArgument.TryPositive(words[start + 2], out frames))))
            return false;

        move = new DragMove(pixels, frames);
        return true;
    }
}
