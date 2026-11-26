using System.Linq;
using TNRD.Zeepkist.LogicScripting.InGameTests;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public partial class StepListTests
{
    [Theory]
    [InlineData("expect-completion a", "a", null)]
    [InlineData("expect-completion a logic", "a", "logic")]
    [InlineData("expect-completion a placed block", "a", "placed block")]
    public void ExpectCompletion_ALabel_ThenTheDetail(string line, string label, string detail)
    {
        var step = Assert.IsType<ExpectCompletionStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(label, step.Label);
        Assert.Equal(detail, step.Detail);
    }

    [Fact]
    public void ExpectCompletionNoDetail_ALabel_AndAnEmptyDetail()
    {
        var step = Assert.IsType<ExpectCompletionStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-completion-no-detail f", out _).Steps));

        Assert.Equal("f", step.Label);
        Assert.Equal(string.Empty, step.Detail);
    }

    [Theory]
    [InlineData("expect-completion-no-detail", "takes a row's label")]
    [InlineData("expect-completion-no-detail f (a, a)", "with no spaces")]
    [InlineData("expect-completion-missing", "takes a row's label")]
    [InlineData("expect-completion-missing a b", "with no spaces")]
    public void TheLabelOnlyCompletionSteps_TakeOneLabel(string line, string message)
    {
        var scenario = ScenarioParser.Parse("steps", line, out var error);

        Assert.Null(scenario);
        Assert.Contains(message, error);
    }

    [Fact]
    public void ExpectCompletionMissing_ReadsTheLabel()
    {
        var step = Assert.IsType<ExpectCompletionMissingStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-completion-missing a:", out _).Steps));

        Assert.Equal("a:", step.Label);
    }

    [Theory]
    [InlineData("expect-call-help-active op:", "op:")]
    [InlineData("expect-call-help-no-active", null)]
    public void ExpectCallHelpActive_ReadsTheLabel(string line, string label)
    {
        var step = Assert.IsType<ExpectCallHelpActiveStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(label, step.Label);
    }

    [Theory]
    [InlineData("press enter", "Return", '\n', (int)KeyModifiers.None)]
    [InlineData("press ctrl+space", "Space", ' ', (int)KeyModifiers.Control)]
    [InlineData("press ctrl+/", "Slash", '/', (int)KeyModifiers.Control)]
    [InlineData("press Shift+A", "A", 'A', (int)KeyModifiers.Shift)]
    [InlineData("press ctrl+shift+k", "K", 'K', (int)(KeyModifiers.Control | KeyModifiers.Shift))]
    [InlineData("press escape", "Escape", '\0', (int)KeyModifiers.None)]
    [InlineData("press 7", "Alpha7", '7', (int)KeyModifiers.None)]
    public void Press_ReadsTheKeyAndItsModifiers(string line, string key, char character, int modifiers)
    {
        var step = Assert.IsType<PressStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        var press = Assert.Single(step.Keys);
        Assert.Equal(key, press.Key);
        Assert.Equal(character, press.Character);
        Assert.Equal((KeyModifiers)modifiers, press.Modifiers);
    }

    [Fact]
    public void Press_SeveralKeys_KeepsTheirOrder()
    {
        var step = Assert.IsType<PressStep>(Assert.Single(ScenarioParser.Parse("steps", "press  escape enter", out _).Steps));

        Assert.Equal(["Escape", "Return"], step.Keys.Select(k => k.Key));
    }

    [Theory]
    [InlineData("press", "press takes 1 to 8 keys, such as enter or ctrl+space")]
    [InlineData("press a b c d e f g h i", "press takes 1 to 8 keys, such as enter or ctrl+space, not 9")]
    [InlineData("press ctrl+", "press doesn't know the key 'ctrl+'")]
    [InlineData("press hyper+a", "press doesn't know the key 'hyper+a'")]
    [InlineData("press (", "press doesn't know the key '('")]
    public void Press_WithoutKnownKeys_Fails(string line, string error)
    {
        ScenarioParser.Parse("steps", line, out var actual);

        Assert.Equal($"line 1: {error}", actual);
    }

    [Theory]
    [InlineData('a', "A", (int)KeyModifiers.None)]
    [InlineData('A', "A", (int)KeyModifiers.Shift)]
    [InlineData(' ', "Space", (int)KeyModifiers.None)]
    [InlineData('=', "Equals", (int)KeyModifiers.None)]
    [InlineData('(', "None", (int)KeyModifiers.None)]
    public void Type_SendsTheKeyForEachCharacter(char character, string key, int modifiers)
    {
        var press = KeyNames.ForCharacter(character);

        Assert.Equal(key, press.Key);
        Assert.Equal(character, press.Character);
        Assert.Equal((KeyModifiers)modifiers, press.Modifiers);
    }

    [Fact]
    public void ExpectError_APosition_ThenTheText()
    {
        var step = Assert.IsType<ExpectErrorStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-error 12:3 'nope' isn't defined", out _).Steps));

        Assert.Equal((12, 3), (step.Position.Line, step.Position.Column));
        Assert.Equal("'nope' isn't defined", step.Text);
    }

    [Theory]
    [InlineData("expect-error 1:5")]
    [InlineData("expect-error nope")]
    [InlineData("expect-error 1 text")]
    [InlineData("expect-error 0:1 text")]
    [InlineData("expect-error 1:x text")]
    [InlineData("expect-error 1:2:3 text")]
    public void ExpectError_NeedsAOneBasedPosition_AndText(string line)
    {
        var scenario = ScenarioParser.Parse("steps", line, out var error);

        Assert.Null(scenario);
        Assert.Contains("takes a line and column", error);
    }
}
