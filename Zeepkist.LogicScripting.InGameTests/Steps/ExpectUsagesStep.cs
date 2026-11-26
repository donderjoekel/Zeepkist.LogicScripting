using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the editor underlines exactly these places as the other usages of the name at the caret, each by the
/// one-based line and column it starts at, or none.
/// </summary>
internal sealed class ExpectUsagesStep : ScenarioStep
{
    /// <summary>
    /// What <c>expect-usages</c> takes, and its result says, for no underlines.
    /// </summary>
    public const string NoUsages = "none";

    private const float WaitSeconds = 1;

    public ExpectUsagesStep(IReadOnlyList<Position> positions)
    {
        Positions = positions;
    }

    /// <summary>
    /// Empty for none.
    /// </summary>
    public IReadOnlyList<Position> Positions { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireText())
            yield break;

        // A key the step before pressed moves the caret in the frame that draws the underlines of where it was
        yield return null;
        yield return context.WaitForWindowDrawn();
        if (context.Failed)
            yield break;

        var expected = Describe(Positions);
        yield return context.WaitUntil(() => Describe(context) == expected, WaitSeconds);
        var actual = Describe(context);
        if (actual == expected)
            context.Pass($"the editor underlines {actual}", expected, actual);
        else
            context.Fail($"the editor underlines {actual} instead", expected, actual);
    }

    private static string Describe(StepContext context)
    {
        var text = context.Window.Text ?? string.Empty;
        return Describe(context.Window.DrawnUsages.Select(s => ToPosition(text, s.Start)).ToList());
    }

    private static string Describe(IReadOnlyList<Position> positions)
    {
        return positions.Count == 0 ? NoUsages : string.Join(" ", positions.OrderBy(p => p).Select(p => p.ToString()));
    }

    private static Position ToPosition(string text, int index)
    {
        var line = 1;
        var lineStart = 0;
        for (var i = 0; i < index && i < text.Length; i++)
        {
            if (text[i] != '\n')
                continue;

            line++;
            lineStart = i + 1;
        }

        return new Position(line, index - lineStart + 1);
    }
}
