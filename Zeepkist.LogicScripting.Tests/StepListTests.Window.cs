using System.Linq;
using TNRD.Zeepkist.LogicScripting.InGameTests;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using TNRD.Zeepkist.LogicScripting.UI;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public partial class StepListTests
{
    [Theory]
    [InlineData("show-tab scripts", false)]
    [InlineData("show-tab libraries", true)]
    public void ShowTab_ReadsTheTab(string line, bool libraries)
    {
        var step = Assert.IsType<ShowTabStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(libraries, step.Libraries);
    }

    [Theory]
    [InlineData("expect-tab scripts", false)]
    [InlineData("expect-tab libraries", true)]
    public void ExpectTab_ReadsTheTab(string line, bool libraries)
    {
        var step = Assert.IsType<ExpectTabStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(libraries, step.Libraries);
    }

    [Theory]
    [InlineData("library-read-failed on", true)]
    [InlineData("library-read-failed off", false)]
    public void LibraryReadFailed_ReadsOnOrOff(string line, bool show)
    {
        var step = Assert.IsType<LibraryReadFailedStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(show, step.Show);
    }

    [Theory]
    [InlineData("startup-warning on", true)]
    [InlineData("startup-warning off", false)]
    public void StartupWarning_ReadsOnOrOff(string line, bool show)
    {
        var step = Assert.IsType<StartupWarningStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(show, step.Show);
    }

    [Theory]
    [InlineData("expect-library-read-failed on", true)]
    [InlineData("expect-library-read-failed off", false)]
    public void ExpectLibraryReadFailed_ReadsOnOrOff(string line, bool shown)
    {
        var step = Assert.IsType<ExpectLibraryReadFailedStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(shown, step.Shown);
    }

    [Theory]
    [InlineData("expect-startup-warning on", true)]
    [InlineData("expect-startup-warning off", false)]
    public void ExpectStartupWarning_ReadsOnOrOff(string line, bool shown)
    {
        var step = Assert.IsType<ExpectStartupWarningStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(shown, step.Shown);
    }

    [Theory]
    [InlineData("show-blocks-tab placed", false)]
    [InlineData("show-blocks-tab reference", true)]
    public void ShowBlocksTab_ReadsTheTab(string line, bool reference)
    {
        var step = Assert.IsType<ShowBlocksTabStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(reference, step.Reference);
    }

    [Theory]
    [InlineData("expect-blocks-tab placed", false)]
    [InlineData("expect-blocks-tab reference", true)]
    public void ExpectBlocksTab_ReadsTheTab(string line, bool reference)
    {
        var step = Assert.IsType<ExpectBlocksTabStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(reference, step.Reference);
    }

    [Theory]
    [InlineData("set-reference-filter delay", "delay")]
    [InlineData("set-reference-filter timing.edge x", "timing.edge x")]
    [InlineData("set-reference-filter", "")]
    public void SetReferenceFilter_TakesTheRestOfTheLine_OrNothingToClearIt(string line, string text)
    {
        var step = Assert.IsType<SetReferenceFilterStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(text, step.Text);
    }

    [Theory]
    [InlineData("expect-reference-headings Libraries", "Libraries")]
    [InlineData("expect-reference-headings Inputs:closed  Libraries", "Inputs:closed Libraries")]
    [InlineData("expect-reference-headings none", "")]
    public void ExpectReferenceHeadings_ReadsEachHeadingAndWhetherItsOpen(string line, string headings)
    {
        var step = Assert.IsType<ExpectReferenceHeadingsStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(headings, ReferenceHeading.Describe(step.Headings));
    }

    [Theory]
    [InlineData("expect-reference-headings", "expect-reference-headings takes the headings, such as Inputs Libraries:closed, or none")]
    [InlineData("expect-reference-headings Inputs:open", "expect-reference-headings takes the headings, such as Inputs Libraries:closed, or none, not 'Inputs:open'")]
    [InlineData("expect-reference-headings none Inputs", "expect-reference-headings takes the headings, such as Inputs Libraries:closed, or none, not 'none'")]
    [InlineData("expect-reference-headings :closed", "expect-reference-headings takes the headings, such as Inputs Libraries:closed, or none, not ':closed'")]
    [InlineData("expect-reference-headings none:closed", "expect-reference-headings takes the headings, such as Inputs Libraries:closed, or none, not 'none:closed'")]
    public void ExpectReferenceHeadings_WithAWrongArgument_Fails(string line, string error)
    {
        ScenarioParser.Parse("steps", line, out var actual);

        Assert.Equal($"line 1: {error}", actual);
    }

    [Theory]
    [InlineData("expect-usages 3:12  1:1", "3:12 1:1")]
    [InlineData("expect-usages none", "")]
    public void ExpectUsages_ReadsEachLineAndColumn(string line, string positions)
    {
        var step = Assert.IsType<ExpectUsagesStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(positions, string.Join(" ", step.Positions));
    }

    [Theory]
    [InlineData("expect-usages", "expect-usages takes the one-based line and column of each underline, such as 1:1 3:12, or none")]
    [InlineData("expect-usages none 1:1", "expect-usages takes the one-based line and column of each underline, such as 1:1 3:12, or none, not 'none'")]
    [InlineData("expect-usages 0:1", "expect-usages takes the one-based line and column of each underline, such as 1:1 3:12, or none, not '0:1'")]
    [InlineData("expect-usages 1", "expect-usages takes the one-based line and column of each underline, such as 1:1 3:12, or none, not '1'")]
    [InlineData("expect-usages 1:x", "expect-usages takes the one-based line and column of each underline, such as 1:1 3:12, or none, not '1:x'")]
    public void ExpectUsages_WithAWrongArgument_Fails(string line, string error)
    {
        ScenarioParser.Parse("steps", line, out var actual);

        Assert.Equal($"line 1: {error}", actual);
    }

    [Theory]
    [InlineData("press-prompt ok", (int)PromptButton.Ok)]
    [InlineData("press-prompt cancel", (int)PromptButton.Cancel)]
    [InlineData("press-prompt delete", (int)PromptButton.Delete)]
    [InlineData("press-prompt keep-blocks", (int)PromptButton.KeepBlocks)]
    public void PressPrompt_ReadsTheButton(string line, int button)
    {
        var step = Assert.IsType<PressPromptStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal((PromptButton)button, step.Button);
    }

    [Theory]
    [InlineData("expect-prompt none", (int)ListPrompt.None)]
    [InlineData("expect-prompt new", (int)ListPrompt.New)]
    [InlineData("expect-prompt rename", (int)ListPrompt.Rename)]
    [InlineData("expect-prompt delete", (int)ListPrompt.Delete)]
    public void ExpectPrompt_ReadsThePrompt(string line, int prompt)
    {
        var step = Assert.IsType<ExpectPromptStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal((ListPrompt)prompt, step.Prompt);
    }

    [Theory]
    [InlineData("show-tab", "show-tab takes scripts or libraries")]
    [InlineData("show-tab blocks", "show-tab takes scripts or libraries, not 'blocks'")]
    [InlineData("expect-tab", "expect-tab takes scripts or libraries")]
    [InlineData("expect-tab blocks", "expect-tab takes scripts or libraries, not 'blocks'")]
    [InlineData("show-blocks-tab", "show-blocks-tab takes placed or reference")]
    [InlineData("show-blocks-tab libraries", "show-blocks-tab takes placed or reference, not 'libraries'")]
    [InlineData("expect-blocks-tab", "expect-blocks-tab takes placed or reference")]
    [InlineData("expect-blocks-tab libraries", "expect-blocks-tab takes placed or reference, not 'libraries'")]
    [InlineData("press-prompt yes", "press-prompt takes ok, cancel, delete or keep-blocks, not 'yes'")]
    [InlineData("expect-prompt open", "expect-prompt takes none, new, rename or delete, not 'open'")]
    [InlineData("set-window-height 0", "set-window-height takes a whole number of pixels above 0, not '0'")]
    [InlineData("set-window-height tall", "set-window-height takes a whole number of pixels above 0, not 'tall'")]
    [InlineData("set-scripts-width 0", "set-scripts-width takes a whole number of pixels above 0, not '0'")]
    [InlineData("expect-window-height", "expect-window-height takes a whole number of pixels above 0")]
    [InlineData("expect-window-height 12.5", "expect-window-height takes a whole number of pixels above 0, not '12.5'")]
    [InlineData("delete-carrier", "delete-carrier takes a Script's name")]
    [InlineData("delete-drafts now", "delete-drafts takes nothing after it")]
    [InlineData("expect-orphans some", "expect-orphans takes a whole number of 0 or more, not 'some'")]
    [InlineData("import-orphan", "import-orphan takes an Orphaned Group's name")]
    [InlineData("restore-orphan", "restore-orphan takes an Orphaned Group's name")]
    [InlineData("expect-orphan-result", "expect-orphan-result takes the text to look for, or none")]
    [InlineData("ctrl-click", "ctrl-click takes the text to Ctrl+click")]
    [InlineData("set-window-size", "set-window-size takes a width and a height, each a whole number of pixels above 0")]
    [InlineData("set-window-size 1600", "set-window-size takes a width and a height, each a whole number of pixels above 0, not '1600'")]
    [InlineData("set-window-size 1600 0", "set-window-size takes a width and a height, each a whole number of pixels above 0, not '1600 0'")]
    [InlineData("set-window-size wide 900", "set-window-size takes a width and a height, each a whole number of pixels above 0, not 'wide 900'")]
    [InlineData("set-window-size 1600 900 1", "set-window-size takes a width and a height, each a whole number of pixels above 0, not '1600 900 1'")]
    [InlineData("expect-window-size 1600x900", "expect-window-size takes a width and a height, each a whole number of pixels above 0, not '1600x900'")]
    [InlineData("center-window now", "center-window takes nothing after it")]
    [InlineData("expect-window-width", "expect-window-width takes a whole number of pixels above 0, or screen")]
    [InlineData("expect-window-width 0", "expect-window-width takes a whole number of pixels above 0, or screen, not '0'")]
    [InlineData("expect-window-width wide", "expect-window-width takes a whole number of pixels above 0, or screen, not 'wide'")]
    [InlineData("expect-window-centered now", "expect-window-centered takes nothing after it")]
    [InlineData("open-new now", "open-new takes nothing after it")]
    [InlineData("expect-footer-in-view now", "expect-footer-in-view takes nothing after it")]
    [InlineData("library-read-failed", "library-read-failed takes on or off")]
    [InlineData("library-read-failed yes", "library-read-failed takes on or off, not 'yes'")]
    [InlineData("startup-warning", "startup-warning takes on or off")]
    [InlineData("startup-warning yes", "startup-warning takes on or off, not 'yes'")]
    [InlineData("expect-library-read-failed", "expect-library-read-failed takes on or off")]
    [InlineData("expect-library-read-failed yes", "expect-library-read-failed takes on or off, not 'yes'")]
    [InlineData("expect-startup-warning", "expect-startup-warning takes on or off")]
    [InlineData("expect-startup-warning yes", "expect-startup-warning takes on or off, not 'yes'")]
    [InlineData("expect-editor-in-view now", "expect-editor-in-view takes nothing after it")]
    [InlineData("expect-editor-in-view-each-frame", "expect-editor-in-view-each-frame takes a whole number of 0 or more")]
    [InlineData("expect-editor-in-view-each-frame many", "expect-editor-in-view-each-frame takes a whole number of 0 or more, not 'many'")]
    [InlineData("drag-window-edge", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first")]
    [InlineData("drag-window-edge 0", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '0'")]
    [InlineData("drag-window-edge up", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not 'up'")]
    [InlineData("drag-window-edge -400 over 0", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 over 0'")]
    [InlineData("drag-window-edge -400 in 8", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 in 8'")]
    [InlineData("drag-window-edge -400 over", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 over'")]
    [InlineData("drag-window-edge-sideways", "drag-window-edge-sideways takes the pixels to drag the window's edge right, below 0 for left, and optionally 'over <frames>', with 'then' before each move after the first")]
    [InlineData("drag-window-edge-sideways 0", "drag-window-edge-sideways takes the pixels to drag the window's edge right, below 0 for left, and optionally 'over <frames>', with 'then' before each move after the first, not '0'")]
    [InlineData("drag-window-edge-sideways left", "drag-window-edge-sideways takes the pixels to drag the window's edge right, below 0 for left, and optionally 'over <frames>', with 'then' before each move after the first, not 'left'")]
    [InlineData("drag-window-edge -400 then", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 then'")]
    [InlineData("drag-window-edge -400 then 0", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 then 0'")]
    [InlineData("drag-window-edge then 300", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not 'then 300'")]
    [InlineData("drag-window-edge -400 300", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 300'")]
    [InlineData("drag-window-edge -400 over 8 then 300 over", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 over 8 then 300 over'")]
    [InlineData("drag-window-edge-sideways -400 over 0", "drag-window-edge-sideways takes the pixels to drag the window's edge right, below 0 for left, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 over 0'")]
    [InlineData("remember-window-rect now", "remember-window-rect takes nothing after it")]
    [InlineData("expect-window-rect now", "expect-window-rect takes nothing after it")]
    [InlineData("expect-window-start-rect now", "expect-window-start-rect takes nothing after it")]
    public void TheLayoutSteps_WithAWrongArgument_Fail(string line, string error)
    {
        ScenarioParser.Parse("steps", line, out var actual);

        Assert.Equal($"line 1: {error}", actual);
    }

    [Fact]
    public void SetWindowHeight_ReadsThePixels()
    {
        var step = Assert.IsType<SetWindowHeightStep>(Assert.Single(ScenarioParser.Parse("steps", "set-window-height 300", out _).Steps));

        Assert.Equal(300, step.Height);
    }

    [Fact]
    public void SetWindowSize_ReadsTheWidthAndHeight()
    {
        var step = Assert.IsType<SetWindowSizeStep>(Assert.Single(ScenarioParser.Parse("steps", "set-window-size 1600 900", out _).Steps));

        Assert.Equal(1600, step.Width);
        Assert.Equal(900, step.Height);
    }

    [Fact]
    public void ExpectWindowSize_ReadsTheWidthAndHeight()
    {
        var step = Assert.IsType<ExpectWindowSizeStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-window-size 1600 900", out _).Steps));

        Assert.Equal(1600, step.Width);
        Assert.Equal(900, step.Height);
    }

    [Theory]
    [InlineData("expect-window-width 64", 64)]
    [InlineData("expect-window-width screen", null)]
    public void ExpectWindowWidth_ReadsThePixels_OrTheScreen(string line, int? width)
    {
        var step = Assert.IsType<ExpectWindowWidthStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(width, step.Width);
    }

    [Fact]
    public void ExpectWindowHeight_ReadsThePixels()
    {
        var step = Assert.IsType<ExpectWindowHeightStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-window-height 640", out _).Steps));

        Assert.Equal(640, step.Height);
    }

    [Theory]
    [InlineData("drag-window-edge 300", 300, 1, false)]
    [InlineData("drag-window-edge -1000 over 10", -1000, 10, false)]
    [InlineData("drag-window-edge +50  over  2", 50, 2, false)]
    [InlineData("drag-window-edge-sideways 300", 300, 1, true)]
    [InlineData("drag-window-edge-sideways -1000 over 10", -1000, 10, true)]
    public void DragWindowEdge_ReadsThePixels_TheFrames_AndTheDirection(string line, int pixels, int frames, bool sideways)
    {
        var step = Assert.IsType<DragWindowEdgeStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        var move = Assert.Single(step.Moves);
        Assert.Equal((pixels, frames, sideways), (move.Pixels, move.Frames, step.Sideways));
    }

    [Theory]
    [InlineData("drag-window-edge -2000 over 10 then 300 over 3", "-2000/10 300/3")]
    [InlineData("drag-window-edge 300 then -50", "300/1 -50/1")]
    [InlineData("drag-window-edge-sideways -100 then 40 over 2 then -10", "-100/1 40/2 -10/1")]
    public void DragWindowEdge_ReadsEachMoveAfterThen_InOnePress(string line, string moves)
    {
        var step = Assert.IsType<DragWindowEdgeStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(moves, string.Join(" ", step.Moves.Select(move => $"{move.Pixels}/{move.Frames}")));
    }

    [Fact]
    public void ExpectEditorInViewEachFrame_ReadsTheFrames()
    {
        var step = Assert.IsType<ExpectEditorInViewEachFrameStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-editor-in-view-each-frame 5", out _).Steps));

        Assert.Equal(5, step.Frames);
    }
}
