using TNRD.Zeepkist.LogicScripting.InGameTests;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public partial class StepListTests
{
    [Theory]
    [InlineData("remember-blocks", "remember-blocks takes placed blocks' names, each once")]
    [InlineData("remember-blocks g g", "remember-blocks takes placed blocks' names, each once")]
    [InlineData("expect-blocks-unmoved", "expect-blocks-unmoved takes placed blocks' names, each once")]
    [InlineData("move-block l 1 2", "move-block takes a placed block's name and how far to move it")]
    [InlineData("move-block l 1 2 x", "move-block takes a placed block's name and how far to move it")]
    [InlineData("move-block a}b 1 2 3", "takes a name of letters")]
    [InlineData("expect-signal l a", "expect-signal takes a placed block's name, its input and a whole number")]
    [InlineData("expect-signal l a 1.5", "expect-signal takes a placed block's name, its input and a whole number")]
    [InlineData("expect-display p", "expect-display takes a placed Power Display's name and what it shows")]
    public void ThePositionAndSignalSteps_WithAWrongArgument_Fail(string line, string message)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains(message, error);
    }

    [Fact]
    public void MoveBlock_AName_ThenAnOffset()
    {
        var step = Assert.IsType<MoveBlockStep>(Assert.Single(ScenarioParser.Parse("steps", "move-block l 16 0 -0.5", out _).Steps));

        Assert.Equal(("l", 16f, 0f, -0.5f), (step.Name, step.X, step.Y, step.Z));
    }

    [Fact]
    public void ExpectSignal_AName_ThenAnInput_ThenASignal()
    {
        var step = Assert.IsType<ExpectSignalStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-signal l op_in -9000000000", out _).Steps));

        Assert.Equal(("l", "op_in", -9000000000L), (step.Name, step.Input, step.Signal));
    }

    [Fact]
    public void ExpectDisplay_AName_ThenTheRestOfTheLine()
    {
        var step = Assert.IsType<ExpectDisplayStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-display p 1 2", out _).Steps));

        Assert.Equal(("p", "1 2"), (step.Name, step.Text));
    }

    [Fact]
    public void BlockPose_FindsWhatChanged()
    {
        var pose = new BlockPose((16, 0, -8), (0, 90, 0), (1, 1, 1));

        Assert.Empty(pose.FindChanges(new BlockPose((16.0005f, 0, -8), (360, 90, -0.0005f), (1, 1, 1))));
        Assert.Equal(["position"], pose.FindChanges(new BlockPose((16, 0.01f, -8), (0, 90, 0), (1, 1, 1))));
        Assert.Equal(["rotation"], pose.FindChanges(new BlockPose((16, 0, -8), (0, 91, 0), (1, 1, 1))));
        Assert.Equal(["scale"], pose.FindChanges(new BlockPose((16, 0, -8), (0, 90, 0), (1, 1, 1.5f))));
        Assert.Equal(["position", "rotation", "scale"], pose.FindChanges(new BlockPose((0, 0, 0), (0, 0, 0), (2, 2, 2))));
    }
}
