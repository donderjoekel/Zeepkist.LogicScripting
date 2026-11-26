using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockTypeTests
{
    private static readonly BlockType _styled = new(
        "styled",
        BlockCategory.Operators,
        [new BlockStyle("normal", 1, 1), new BlockStyle("compact", 2, 1)],
        [new BlockConnector("a", 0), new BlockConnector("b", 1)],
        [new BlockConnector("out", 2)],
        [BlockSetting.Option("mode", "m", "one", "two")]);

    // Completion and Call Help show each argument's label and detail as they're listed here
    [Fact]
    public void Arguments_AreInputs_ThenSettings_ThenStyle_ThenHide_EachLabelledAndDescribed()
    {
        Assert.Equal(
            [
                ("a", BlockArgumentKind.Input, "a", "input", null),
                ("b", BlockArgumentKind.Input, "b", "input", null),
                ("mode", BlockArgumentKind.Setting, "mode:", "one, two", _styled.Settings[0]),
                ("style", BlockArgumentKind.Style, "style:", "normal, compact", null),
                ("hide", BlockArgumentKind.Setting, "hide:", BlockReference.DescribeValues(BlockSetting.Hide), BlockSetting.Hide)
            ],
            _styled.Arguments.Select(a => (a.Name, a.Kind, a.Label, a.Detail, a.Setting)));
    }

    [Fact]
    public void FindClosestName_PrefersStyle_WhenAsCloseToHide()
    {
        Assert.Equal("style", _styled.FindClosestName("hyle"));
    }

    [Fact]
    public void Arguments_LeaveOutStyle_WhenTheTypeHasOneStyle()
    {
        Assert.DoesNotContain(BlockManifest.Logic.Arguments, a => a.Kind == BlockArgumentKind.Style);
    }

    [Fact]
    public void Arguments_OfAPlacedOnlyType_LeaveOutHide()
    {
        Assert.DoesNotContain(BlockManifest.Lamp.Arguments, a => a.Setting == BlockSetting.Hide);
    }
}
