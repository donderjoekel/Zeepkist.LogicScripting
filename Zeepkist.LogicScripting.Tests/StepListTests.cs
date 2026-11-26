using System;
using TNRD.Zeepkist.LogicScripting.InGameTests;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public partial class StepListTests
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

    [Theory]
    [InlineData("place-start now")]
    [InlineData("test-mode 5")]
    [InlineData("restart-round now")]
    [InlineData("back-to-editor now")]
    public void TheTestModeSteps_TakeNothingAfterThem(string line)
    {
        var scenario = ScenarioParser.Parse("steps", line, out var error);

        Assert.Null(scenario);
        Assert.Contains("takes nothing after it", error);
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
}
