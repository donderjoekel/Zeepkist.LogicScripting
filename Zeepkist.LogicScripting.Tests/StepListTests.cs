using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.InGameTests;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using TNRD.Zeepkist.LogicScripting.UI;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class StepListTests
{
    [Theory]
    [InlineData("open-window", typeof(OpenWindowStep))]
    [InlineData("close-window", typeof(CloseWindowStep))]
    [InlineData("new-script Lights", typeof(NewScriptStep))]
    [InlineData("select-script Lights", typeof(SelectScriptStep))]
    [InlineData("new-library timing", typeof(NewLibraryStep))]
    [InlineData("select-library timing", typeof(SelectLibraryStep))]
    [InlineData("open-delete Lights", typeof(OpenDeleteStep))]
    [InlineData("show-tab libraries", typeof(ShowTabStep))]
    [InlineData("expect-tab scripts", typeof(ExpectTabStep))]
    [InlineData("show-blocks-tab reference", typeof(ShowBlocksTabStep))]
    [InlineData("expect-blocks-tab placed", typeof(ExpectBlocksTabStep))]
    [InlineData("set-reference-filter delay", typeof(SetReferenceFilterStep))]
    [InlineData("set-reference-filter", typeof(SetReferenceFilterStep))]
    [InlineData("expect-reference-headings Inputs Libraries", typeof(ExpectReferenceHeadingsStep))]
    [InlineData("open-new", typeof(OpenNewStep))]
    [InlineData("open-rename Lights", typeof(OpenRenameStep))]
    [InlineData("type-name Lights", typeof(TypeNameStep))]
    [InlineData("press-prompt keep-blocks", typeof(PressPromptStep))]
    [InlineData("select-group Lights", typeof(SelectGroupStep))]
    [InlineData("expect-list-result Deleted", typeof(ExpectListResultStep))]
    [InlineData("expect-prompt none", typeof(ExpectPromptStep))]
    [InlineData("expect-footer-in-view", typeof(ExpectFooterInViewStep))]
    [InlineData("set-window-height 300", typeof(SetWindowHeightStep))]
    [InlineData("expect-window-height 300", typeof(ExpectWindowHeightStep))]
    [InlineData("set-window-size 1600 900", typeof(SetWindowSizeStep))]
    [InlineData("center-window", typeof(CenterWindowStep))]
    [InlineData("set-scripts-width 200", typeof(SetScriptsWidthStep))]
    [InlineData("expect-window-size 1600 900", typeof(ExpectWindowSizeStep))]
    [InlineData("expect-window-width 64", typeof(ExpectWindowWidthStep))]
    [InlineData("expect-window-width screen", typeof(ExpectWindowWidthStep))]
    [InlineData("expect-window-centered", typeof(ExpectWindowCenteredStep))]
    [InlineData("drag-window-edge -400 over 8", typeof(DragWindowEdgeStep))]
    [InlineData("drag-window-edge-sideways -400 over 8", typeof(DragWindowEdgeStep))]
    [InlineData("remember-window-rect", typeof(RememberWindowRectStep))]
    [InlineData("expect-window-rect", typeof(ExpectWindowRectStep))]
    [InlineData("expect-window-start-rect", typeof(ExpectWindowStartRectStep))]
    [InlineData("library-read-failed on", typeof(LibraryReadFailedStep))]
    [InlineData("library-read-failed off", typeof(LibraryReadFailedStep))]
    [InlineData("startup-warning on", typeof(StartupWarningStep))]
    [InlineData("startup-warning off", typeof(StartupWarningStep))]
    [InlineData("expect-library-read-failed on", typeof(ExpectLibraryReadFailedStep))]
    [InlineData("expect-startup-warning off", typeof(ExpectStartupWarningStep))]
    [InlineData("expect-editor-in-view", typeof(ExpectEditorInViewStep))]
    [InlineData("set-text\na = 1\nend", typeof(SetTextStep))]
    [InlineData("compile", typeof(CompileStep))]
    [InlineData("expect-text a = 1", typeof(ExpectTextStep))]
    [InlineData("expect-result Placed", typeof(ExpectResultStep))]
    [InlineData("expect-no-errors", typeof(ExpectNoErrorsStep))]
    [InlineData("expect-errors 2", typeof(ExpectErrorsStep))]
    [InlineData("expect-error 1:5 isn't defined", typeof(ExpectErrorStep))]
    [InlineData("expect-blocks 3", typeof(ExpectBlocksStep))]
    [InlineData("caret-at-start", typeof(PlaceCaretStep))]
    [InlineData("caret-at-end", typeof(PlaceCaretStep))]
    [InlineData("complete", typeof(CompleteStep))]
    [InlineData("expect-completion t", typeof(ExpectCompletionStep))]
    [InlineData("expect-completion-no-detail function", typeof(ExpectCompletionStep))]
    [InlineData("expect-completion-missing op:", typeof(ExpectCompletionMissingStep))]
    [InlineData("expect-call-help logic(", typeof(ExpectCallHelpStep))]
    [InlineData("expect-call-help-active op:", typeof(ExpectCallHelpActiveStep))]
    [InlineData("expect-call-help-no-active", typeof(ExpectCallHelpActiveStep))]
    [InlineData("expect-no-completion", typeof(ExpectNoCompletionStep))]
    [InlineData("expect-no-call-help", typeof(ExpectNoCallHelpStep))]
    [InlineData("hover b:", typeof(HoverStep))]
    [InlineData("expect-hover b input", typeof(ExpectHoverStep))]
    [InlineData("expect-no-hover", typeof(ExpectNoHoverStep))]
    [InlineData("expect-usages 1:1 3:12", typeof(ExpectUsagesStep))]
    [InlineData("expect-usages none", typeof(ExpectUsagesStep))]
    [InlineData("press ctrl+space", typeof(PressStep))]
    [InlineData("type b = a", typeof(TypeStep))]
    [InlineData("place-block lamp", typeof(PlaceBlockStep))]
    [InlineData("place-block lamp as lamp", typeof(PlaceBlockStep))]
    [InlineData("set-block-setting t mode global_velocity", typeof(SetBlockSettingStep))]
    [InlineData("set-stored-setting m text oi2 5", typeof(SetStoredSettingStep))]
    [InlineData("name-group-block Lights logic as l", typeof(NameGroupBlockStep))]
    [InlineData("wire-blocks g out to l a", typeof(WireBlocksStep))]
    [InlineData("wire-dependency-loop l out to g a", typeof(WireDependencyLoopStep))]
    [InlineData("select-blocks g l", typeof(SelectBlocksStep))]
    [InlineData("expect-wire g out to l a", typeof(ExpectWireStep))]
    [InlineData("expect-wires l 2", typeof(ExpectWiresStep))]
    [InlineData("expect-step-fails expect-blocks 1 because 0 Logic Blocks", typeof(ExpectStepFailsStep))]
    [InlineData("remember-blocks g l", typeof(RememberBlocksStep))]
    [InlineData("expect-blocks-unmoved g l", typeof(ExpectBlocksUnmovedStep))]
    [InlineData("move-block l 16 0 -8", typeof(MoveBlockStep))]
    [InlineData("expect-signal l a 1", typeof(ExpectSignalStep))]
    [InlineData("expect-display p 1", typeof(ExpectDisplayStep))]
    [InlineData("turn-off-style lamp", typeof(TurnOffStyleStep))]
    [InlineData("insert-selected", typeof(InsertSelectedStep))]
    [InlineData("import-selected", typeof(ImportSelectedStep))]
    [InlineData("delete-carrier Lights", typeof(DeleteCarrierStep))]
    [InlineData("delete-drafts", typeof(DeleteDraftsStep))]
    [InlineData("expect-orphans 1", typeof(ExpectOrphansStep))]
    [InlineData("import-orphan Lights", typeof(ImportOrphanStep))]
    [InlineData("restore-orphan Lights", typeof(RestoreOrphanStep))]
    [InlineData("expect-orphan-result Restored", typeof(ExpectOrphanResultStep))]
    [InlineData("grab-blocks", typeof(GrabBlocksStep))]
    [InlineData("drop-blocks", typeof(DropBlocksStep))]
    [InlineData("paste-blocks as g2 l2", typeof(PasteBlocksStep))]
    [InlineData("save-level saved", typeof(SaveLevelStep))]
    [InlineData("load-level saved", typeof(LoadLevelStep))]
    [InlineData("undo", typeof(UndoStep))]
    [InlineData("redo", typeof(RedoStep))]
    [InlineData("place-start", typeof(PlaceStartStep))]
    [InlineData("test-mode", typeof(TestModeStep))]
    [InlineData("restart-round", typeof(RestartRoundStep))]
    [InlineData("back-to-editor", typeof(BackToEditorStep))]
    [InlineData("wait 1", typeof(WaitStep))]
    [InlineData("screenshot compiled", typeof(ScreenshotStep))]
    [InlineData("expect-blank-screenshot covered", typeof(ExpectBlankScreenshotStep))]
    [InlineData("log hello", typeof(LogStep))]
    [InlineData("expect-no-exceptions", typeof(ExpectNoExceptionsStep))]
    public void EachStep_ParsesToItsOwnClass(string line, Type type)
    {
        var scenario = ScenarioParser.Parse("steps", line, out var error);

        Assert.Null(error);
        Assert.IsType(type, Assert.Single(scenario.Steps));
    }

    // A row per step: each step's line in StepList wires the check up on its own
    [Theory]
    [InlineData("open-window now")]
    [InlineData("open-new now")]
    [InlineData("expect-footer-in-view now")]
    [InlineData("center-window now")]
    [InlineData("expect-window-centered now")]
    [InlineData("remember-window-rect now")]
    [InlineData("expect-window-rect now")]
    [InlineData("expect-window-start-rect now")]
    [InlineData("expect-editor-in-view now")]
    [InlineData("delete-drafts now")]
    [InlineData("grab-blocks g")]
    [InlineData("drop-blocks g")]
    [InlineData("place-start now")]
    [InlineData("test-mode 5")]
    [InlineData("restart-round now")]
    [InlineData("back-to-editor now")]
    public void TheStepsThatTakeNothing_FailWithSomethingAfterThem(string line)
    {
        var scenario = ScenarioParser.Parse("steps", line, out var error);

        Assert.Null(scenario);
        Assert.Equal($"line 1: {line.Split(' ')[0]} takes nothing after it", error);
    }

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

    // One row of each answer per word list, and the other steps that share the list once each
    [Theory]
    [InlineData("show-tab scripts", false)]
    [InlineData("show-tab libraries", true)]
    [InlineData("expect-tab libraries", true)]
    [InlineData("show-blocks-tab placed", false)]
    [InlineData("show-blocks-tab reference", true)]
    [InlineData("expect-blocks-tab reference", true)]
    [InlineData("library-read-failed on", true)]
    [InlineData("library-read-failed off", false)]
    [InlineData("startup-warning on", true)]
    [InlineData("expect-library-read-failed on", true)]
    [InlineData("expect-startup-warning on", true)]
    public void TheStepsTakingOneOfTwoWords_ReadWhichOne(string line, bool second)
    {
        var step = Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps);

        var read = step switch
        {
            ShowTabStep s => s.Libraries,
            ExpectTabStep s => s.Libraries,
            ShowBlocksTabStep s => s.Reference,
            ExpectBlocksTabStep s => s.Reference,
            LibraryReadFailedStep s => s.Show,
            StartupWarningStep s => s.Show,
            ExpectLibraryReadFailedStep s => s.Shown,
            ExpectStartupWarningStep s => s.Shown,
            _ => throw new InvalidOperationException(step.GetType().Name),
        };
        Assert.Equal(second, read);
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
    [InlineData("show-blocks-tab", "show-blocks-tab takes placed or reference")]
    [InlineData("show-blocks-tab libraries", "show-blocks-tab takes placed or reference, not 'libraries'")]
    [InlineData("press-prompt yes", "press-prompt takes ok, cancel, delete or keep-blocks, not 'yes'")]
    [InlineData("expect-prompt open", "expect-prompt takes none, new, rename or delete, not 'open'")]
    [InlineData("set-window-height tall", "set-window-height takes a whole number of pixels above 0, not 'tall'")]
    [InlineData("expect-window-height", "expect-window-height takes a whole number of pixels above 0")]
    [InlineData("delete-carrier", "delete-carrier takes a Script's name")]
    [InlineData("expect-orphans some", "expect-orphans takes a whole number of 0 or more, not 'some'")]
    [InlineData("expect-orphan-result", "expect-orphan-result takes the text to look for, or none")]
    [InlineData("set-window-size", "set-window-size takes a width and a height, each a whole number of pixels above 0")]
    [InlineData("set-window-size 1600 0", "set-window-size takes a width and a height, each a whole number of pixels above 0, not '1600 0'")]
    [InlineData("expect-window-size 1600x900", "expect-window-size takes a width and a height, each a whole number of pixels above 0, not '1600x900'")]
    [InlineData("expect-window-width", "expect-window-width takes a whole number of pixels above 0, or screen")]
    [InlineData("expect-window-width wide", "expect-window-width takes a whole number of pixels above 0, or screen, not 'wide'")]
    [InlineData("library-read-failed", "library-read-failed takes on or off")]
    [InlineData("library-read-failed yes", "library-read-failed takes on or off, not 'yes'")]
    [InlineData("expect-editor-in-view-each-frame", "expect-editor-in-view-each-frame takes a whole number of 0 or more")]
    [InlineData("expect-editor-in-view-each-frame many", "expect-editor-in-view-each-frame takes a whole number of 0 or more, not 'many'")]
    [InlineData("drag-window-edge", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first")]
    [InlineData("drag-window-edge 0", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '0'")]
    [InlineData("drag-window-edge up", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not 'up'")]
    [InlineData("drag-window-edge -400 over 0", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 over 0'")]
    [InlineData("drag-window-edge -400 in 8", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 in 8'")]
    [InlineData("drag-window-edge -400 over", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 over'")]
    [InlineData("drag-window-edge-sideways", "drag-window-edge-sideways takes the pixels to drag the window's edge right, below 0 for left, and optionally 'over <frames>', with 'then' before each move after the first")]
    [InlineData("drag-window-edge -400 then", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 then'")]
    [InlineData("drag-window-edge -400 then 0", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 then 0'")]
    [InlineData("drag-window-edge then 300", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not 'then 300'")]
    [InlineData("drag-window-edge -400 300", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 300'")]
    [InlineData("drag-window-edge -400 over 8 then 300 over", "drag-window-edge takes the pixels to drag the window's edge down, below 0 for up, and optionally 'over <frames>', with 'then' before each move after the first, not '-400 over 8 then 300 over'")]
    public void TheLayoutSteps_WithAWrongArgument_Fail(string line, string error)
    {
        ScenarioParser.Parse("steps", line, out var actual);

        Assert.Equal($"line 1: {error}", actual);
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

    [Theory]
    [InlineData("place-block lamp", "lamp", null)]
    [InlineData("place-block lamp as lamp_1", "lamp", "lamp_1")]
    [InlineData("place-block simple_telemetry as speed-gauge", "simple_telemetry", "speed-gauge")]
    public void PlaceBlock_AFunction_ThenOptionallyAName(string line, string function, string name)
    {
        var step = Assert.IsType<PlaceBlockStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(function, step.Function);
        Assert.Equal(name, step.Name);
    }

    [Fact]
    public void SetBlockSetting_AName_ASetting_ThenAValue()
    {
        var step = Assert.IsType<SetBlockSettingStep>(Assert.Single(ScenarioParser.Parse("steps", "set-block-setting speed-gauge  mode global_velocity", out _).Steps));

        Assert.Equal("speed-gauge", step.Name);
        Assert.Equal("mode", step.Setting);
        Assert.Equal("global_velocity", step.Value);
    }

    [Theory]
    [InlineData("set-block-setting")]
    [InlineData("set-block-setting t")]
    [InlineData("set-block-setting t mode")]
    [InlineData("set-block-setting t mode basic now")]
    public void SetBlockSetting_TakesThreeWords(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("set-block-setting takes a placed block's name, a Setting's name and its value", error);
    }

    [Fact]
    public void SetStoredSetting_AName_AKind_AKey_ThenAValue()
    {
        var step = Assert.IsType<SetStoredSettingStep>(Assert.Single(ScenarioParser.Parse("steps", "set-stored-setting m float x3  0.5", out _).Steps));

        Assert.Equal("m", step.Name);
        Assert.Equal(StoredKind.Float, step.Kind);
        Assert.Equal("x3", step.Key);
        Assert.Equal("0.5", step.Value);
    }

    [Theory]
    [InlineData("set-stored-setting")]
    [InlineData("set-stored-setting m text oi2")]
    [InlineData("set-stored-setting m text oi2 5 6")]
    [InlineData("set-stored-setting m word oi2 5")]
    [InlineData("set-stored-setting m 2 oi2 5")]
    [InlineData("set-stored-setting m Text oi2 5")]
    public void SetStoredSetting_TakesFourWords(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("set-stored-setting takes a placed block's name, int, float or text", error);
    }

    [Theory]
    [InlineData("set-stored-setting m int x47 five", "int, not 'five'")]
    [InlineData("set-stored-setting m int x47 1.5", "int, not '1.5'")]
    [InlineData("set-stored-setting m float x3 half", "float, not 'half'")]
    public void SetStoredSetting_ANumberHasToBeOne(string line, string error)
    {
        ScenarioParser.Parse("steps", line, out var actual);

        Assert.Contains($"set-stored-setting takes a number for {error}", actual);
    }

    [Theory]
    [InlineData("name-group-block Lights logic as l", "Lights", "logic", "l")]
    [InlineData("name-group-block My Lights  power_display as display-1", "My Lights", "power_display", "display-1")]
    public void NameGroupBlock_AScript_AFunction_ThenAName(string line, string script, string function, string name)
    {
        var step = Assert.IsType<NameGroupBlockStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal((script, function, name), (step.Script, step.Function, step.Name));
    }

    [Theory]
    [InlineData("name-group-block")]
    [InlineData("name-group-block Lights logic")]
    [InlineData("name-group-block logic as l")]
    [InlineData("name-group-block Lights logic is l")]
    public void NameGroupBlock_TakesAScript_AFunction_AndAsAName(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("name-group-block takes a Script's name, a Block Function's name and 'as <name>'", error);
    }

    [Theory]
    [InlineData("wire-blocks speed-gauge  heading to l op_in", "speed-gauge heading to l op_in")]
    [InlineData("wire-blocks to out to to a", "to out to to a")]
    [InlineData("wire-dependency-loop o out to n a", "o out to n a")]
    [InlineData("expect-wire g out to l a", "g out to l a")]
    public void TheWireSteps_AnOutput_ThenAnInput(string line, string wire)
    {
        var step = Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps);

        var read = step switch
        {
            WireBlocksStep s => s.Wire,
            WireDependencyLoopStep s => s.Wire,
            ExpectWireStep s => s.Wire,
            _ => throw new InvalidOperationException(step.GetType().Name),
        };
        Assert.Equal(wire, read.ToString());
    }

    [Theory]
    [InlineData("wire-blocks")]
    [InlineData("wire-blocks g to l a")]
    [InlineData("wire-blocks g out l a")]
    [InlineData("wire-blocks g out into l a")]
    [InlineData("wire-blocks g out to l a b")]
    [InlineData("wire-dependency-loop")]
    [InlineData("wire-dependency-loop l out g a")]
    [InlineData("expect-wire")]
    [InlineData("expect-wire g out to l")]
    public void TheWireSteps_TakeAnOutput_To_AndAnInput(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("takes a placed block's name and output, 'to', and a placed block's name and input", error);
    }

    [Theory]
    [InlineData("place-block lamp as")]
    [InlineData("place-block lamp as a}b")]
    [InlineData("place-block lamp as two words")]
    [InlineData("set-block-setting a}b mode basic")]
    [InlineData("set-stored-setting a}b text oi2 5")]
    [InlineData("name-group-block Lights logic as a}b")]
    [InlineData("wire-blocks a}b out to l a")]
    [InlineData("wire-blocks g out to a}b a")]
    [InlineData("expect-wire a}b out to l a")]
    [InlineData("select-blocks g a}b")]
    [InlineData("expect-wires a}b 1")]
    public void TheNamedBlockSteps_TakeNamesOfLettersDigitsDashesAndUnderscores(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("takes a name of letters", error);
    }

    [Theory]
    [InlineData("select-blocks g", "g")]
    [InlineData("select-blocks l  g lamp-1", "l g lamp-1")]
    public void SelectBlocks_ReadsTheNames_InOrder(string line, string names)
    {
        var step = Assert.IsType<SelectBlocksStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(names, string.Join(" ", step.Names));
    }

    [Theory]
    [InlineData("select-blocks")]
    [InlineData("select-blocks g l g")]
    public void SelectBlocks_TakesNames_EachOnce(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("select-blocks takes placed blocks' names, each once", error);
    }

    [Fact]
    public void ExpectWires_AName_ThenACount()
    {
        var step = Assert.IsType<ExpectWiresStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-wires l 0", out _).Steps));

        Assert.Equal(("l", 0), (step.Name, step.Count));
    }

    [Theory]
    [InlineData("expect-wires", "takes a placed block's name and how many Wires it has")]
    [InlineData("expect-wires l", "takes a placed block's name and how many Wires it has")]
    [InlineData("expect-wires l 1 2", "takes a placed block's name and how many Wires it has")]
    [InlineData("expect-wires l -1", "takes a whole number of 0 or more")]
    public void ExpectWires_WithAWrongArgument_Fails(string line, string message)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains(message, error);
    }

    [Fact]
    public void RememberBlocksAndExpectBlocksUnmoved_ReadTheNames_InOrder()
    {
        var remember = Assert.IsType<RememberBlocksStep>(Assert.Single(ScenarioParser.Parse("steps", "remember-blocks l  g", out _).Steps));
        var expect = Assert.IsType<ExpectBlocksUnmovedStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-blocks-unmoved l g", out _).Steps));

        Assert.Equal(["l", "g"], remember.Names);
        Assert.Equal(["l", "g"], expect.Names);
    }

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

    [Fact]
    public void ExpectStepFails_AStep_ThenTheReasonAfterTheLastBecause()
    {
        var step = Assert.IsType<ExpectStepFailsStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-step-fails expect-text a because b because the text doesn't contain it", out _).Steps));

        Assert.Equal("a because b", Assert.IsType<ExpectTextStep>(step.Step).Text);
        Assert.Equal("the text doesn't contain it", step.Reason);
    }

    [Theory]
    [InlineData("expect-step-fails", "expect-step-fails takes a step of one line")]
    [InlineData("expect-step-fails expect-blocks 1", "expect-step-fails takes a step of one line")]
    [InlineData("expect-step-fails expect-blocks 1 because", "expect-step-fails takes a step of one line")]
    [InlineData("expect-step-fails set-text because no Script", "expect-step-fails takes a step of one line")]
    [InlineData("expect-step-fails expect-step-fails undo because a because b", "expect-step-fails takes a step of one line")]
    [InlineData("expect-step-fails nope because it isn't one", "unknown step 'nope'")]
    [InlineData("expect-step-fails expect-blocks many because 0", "expect-blocks takes a whole number of 0 or more, not 'many'")]
    public void ExpectStepFails_TakesAStepOfOneLine_AndAReason(string line, string message)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains(message, error);
    }

    [Fact]
    public void PasteBlocks_ReadsTheCopiesNames_InOrder()
    {
        var step = Assert.IsType<PasteBlocksStep>(Assert.Single(ScenarioParser.Parse("steps", "paste-blocks as l2 g2", out _).Steps));

        Assert.Equal(["l2", "g2"], step.Names);
    }

    [Theory]
    [InlineData("save-level saved", null)]
    [InlineData("load-level saved", null)]
    [InlineData("load-level saved while import-selected", typeof(ImportSelectedStep))]
    [InlineData("load-level saved while expect-step-fails import-selected because a while b", typeof(ExpectStepFailsStep))]
    public void TheLevelFileSteps_ReadTheName_AndAStepForWhileItLoads(string line, Type meanwhile)
    {
        var step = Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps);

        Assert.Equal("saved", step is LoadLevelStep load ? load.Name : Assert.IsType<SaveLevelStep>(step).Name);
        Assert.Equal(meanwhile, (step as LoadLevelStep)?.Meanwhile?.GetType());
    }

    [Theory]
    [InlineData("paste-blocks", "paste-blocks takes 'as' and a name for each selected block's copy")]
    [InlineData("paste-blocks g2", "paste-blocks takes 'as' and a name for each selected block's copy")]
    [InlineData("paste-blocks as g2 g2", "paste-blocks takes placed blocks' names, each once")]
    [InlineData("paste-blocks as a}b", "paste-blocks takes a name of letters")]
    [InlineData("save-level", "save-level takes a name of letters")]
    [InlineData("save-level ../saved", "save-level takes a name of letters")]
    [InlineData("load-level", "load-level takes a saved level's name")]
    [InlineData("load-level two words", "load-level takes a name of letters")]
    [InlineData("load-level saved while set-text", "load-level takes a saved level's name")]
    [InlineData("load-level saved while load-level saved", "load-level takes a saved level's name")]
    [InlineData("load-level saved while nope", "unknown step 'nope'")]
    public void TheGrabPasteAndLevelFileSteps_WithAWrongArgument_Fail(string line, string message)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains(message, error);
    }

    [Fact]
    public void FindConnector_FindsItByName()
    {
        var input = NamedBlock.FindConnector("l", "input", BlockManifest.Logic.Inputs, "op_in", out var error);

        Assert.Null(error);
        Assert.Equal(2, input.Index);
    }

    [Fact]
    public void FindConnector_AnOutputOfAMode_IsThatModesOutput()
    {
        var output = NamedBlock.FindConnector("t", "output", BlockManifest.SimpleTelemetry.GetOutputs("global_velocity"), "heading", out _);

        Assert.Equal(3, output.Index);
    }

    [Fact]
    public void FindConnector_AnUnknownName_SaysWhichThereAre()
    {
        Assert.Null(NamedBlock.FindConnector("l", "input", BlockManifest.Logic.Inputs, "c", out var unknown));
        Assert.Null(NamedBlock.FindConnector("lamp", "output", BlockManifest.Lamp.Outputs, "out", out var none));

        Assert.Equal("\"l\" has no input called 'c'. Use a, b, op_in", unknown);
        Assert.Equal("\"lamp\" has no output called 'out'. It has no outputs", none);
    }

    [Theory]
    [InlineData("turn-off-style lamp", "lamp", null)]
    [InlineData("turn-off-style logic  wide", "logic", "wide")]
    public void TurnOffStyle_AFunction_ThenOptionallyAStyle(string line, string function, string style)
    {
        var step = Assert.IsType<TurnOffStyleStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal((function, style), (step.Function, step.Style));
    }

    [Theory]
    [InlineData("turn-off-style")]
    [InlineData("turn-off-style logic wide now")]
    public void TurnOffStyle_TakesAFunction_AndOptionallyAStyle(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("turn-off-style takes a Block Function's name, and optionally one of its styles", error);
    }

    // BlockSettingTests checks what each Setting takes; these check the step adds the value to all but an option's error
    [Theory]
    [InlineData("mode", "fast", "'fast' isn't an option of mode. Use basic, local_velocity, global_velocity")]
    [InlineData("min", "1.5", "min takes a whole number, not '1.5'")]
    public void SetBlockSetting_AValueTheSettingDoesNotTake_SaysWhatItTakes(string name, string value, string error)
    {
        var setting = new[] { BlockManifest.SimpleTelemetryMode, BlockManifest.RandomMinimum }.Single(s => s.Name == name);

        Assert.Null(SetBlockSettingStep.ToSetting(setting, value, out var actual));
        Assert.Equal(error, actual);
    }

    [Fact]
    public void SetBlockSetting_ATogglersTargets_CantBeSet()
    {
        var targets = BlockManifest.All.SelectMany(t => t.Settings).First(s => s.Type == SettingType.Targets);

        Assert.Null(SetBlockSettingStep.ToSetting(targets, "a", out var error));
        Assert.Equal($"set-block-setting can't set {targets.Name}, a list of blocks", error);
    }

    [Fact]
    public void TheStepList_HoldsExactlyTheStepsTheDocsList()
    {
        var documented = DocsTable("### Steps").Select(step => step.Split(' ')[0]);

        Assert.Equal(documented.OrderBy(n => n), StepList.Names.OrderBy(n => n));
    }

    [Fact]
    public void TheDocs_ListExactlyTheRunScriptsOptions()
    {
        var script = RepoFiles.Read("tools/ingame-test.sh");
        var options = Regex.Matches(script, @"^\s*((?:-[-\w]+)(?: \| -[-\w]+)*)\)", RegexOptions.Multiline)
            .SelectMany(match => match.Groups[1].Value.Split(" | "));
        var documented = DocsTable("### Options").SelectMany(option => option.Split(", ")).Select(option => option.Split(' ')[0]);

        Assert.Equal(documented.OrderBy(o => o), options.Where(o => o != "-*").OrderBy(o => o));
    }

    [Fact]
    public void TheRunScript_RemovesTheLevelsTheStepsSave()
    {
        Assert.Contains($"\ntest_level_prefix={TestLevels.Prefix}\n", RepoFiles.Read("tools/ingame-test.sh").Replace("\r\n", "\n"));
    }

    public static TheoryData<string> ShippedScenarios()
    {
        var files = Directory.GetFiles(Path.Combine(RepoFiles.Root, "Zeepkist.LogicScripting.InGameTests", "Scenarios"), "*.zlstest");
        Assert.NotEmpty(files);
        return [.. files.Select(Path.GetFileName).OrderBy(f => f, StringComparer.Ordinal)];
    }

    [Theory]
    [MemberData(nameof(ShippedScenarios))]
    public void TheShippedScenarios_CanBeRead(string file)
    {
        var text = RepoFiles.Read($"Zeepkist.LogicScripting.InGameTests/Scenarios/{file}");

        var scenario = ScenarioParser.Parse(file, text, out var error);

        Assert.Null(error);
        Assert.NotEmpty(scenario.Steps);
    }

    /// <summary>
    /// The first column of the table under <paramref name="heading"/> in docs/ingame-tests.md, without its backticks.
    /// </summary>
    private static string[] DocsTable(string heading)
    {
        var docs = RepoFiles.Read("docs/ingame-tests.md").Replace("\r\n", "\n");
        var section = docs.Substring(docs.IndexOf(heading + "\n", StringComparison.Ordinal));
        return [.. section.Split('\n')
            .SkipWhile(line => !line.StartsWith('|'))
            .TakeWhile(line => line.StartsWith('|'))
            .Skip(2)
            .Select(line => line.Split('|')[1].Trim().Replace("`", string.Empty))];
    }
}
