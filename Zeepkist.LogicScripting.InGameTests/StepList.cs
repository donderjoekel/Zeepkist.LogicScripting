using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Every step a scenario can use, by the name it's written with. A new step is a class in Steps/ and a line here.
/// </summary>
internal static partial class StepList
{
    public const string SetText = "set-text";
    public const string End = "end";

    /// <summary>
    /// What <c>expect-reference-headings</c> takes for a Reference tab that draws no headings.
    /// </summary>
    public const string NoHeadings = "none";

    /// <summary>
    /// Whether the tab is the Libraries tab.
    /// </summary>
    private static readonly Dictionary<string, bool> _tabs = new()
    {
        ["scripts"] = false,
        ["libraries"] = true,
    };

    /// <summary>
    /// Whether the Blocks side's tab is the Reference tab.
    /// </summary>
    private static readonly Dictionary<string, bool> _blocksTabs = new()
    {
        ["placed"] = false,
        ["reference"] = true,
    };

    private static readonly Dictionary<string, bool> _onOff = new()
    {
        ["on"] = true,
        ["off"] = false,
    };

    private static readonly Dictionary<string, Func<string, string, ScenarioStep>> _steps = new()
    {
        ["open-window"] = (step, argument) => None(step, argument, new OpenWindowStep()),
        ["close-window"] = (step, argument) => None(step, argument, new CloseWindowStep()),
        ["new-script"] = (step, argument) => new NewScriptStep(StepArgument.Text(step, argument, "a name")),
        ["select-script"] = (step, argument) => new SelectScriptStep(StepArgument.Text(step, argument, "a name")),
        ["new-library"] = (step, argument) => new NewLibraryStep(StepArgument.Text(step, argument, "a name")),
        ["select-library"] = (step, argument) => new SelectLibraryStep(StepArgument.Text(step, argument, "a name")),
        ["open-delete"] = (step, argument) => new OpenDeleteStep(StepArgument.Text(step, argument, "a name")),
        ["show-tab"] = (step, argument) => new ShowTabStep(StepArgument.Choice(step, argument, _tabs)),
        ["expect-tab"] = (step, argument) => new ExpectTabStep(StepArgument.Choice(step, argument, _tabs)),
        ["show-blocks-tab"] = (step, argument) => new ShowBlocksTabStep(StepArgument.Choice(step, argument, _blocksTabs)),
        ["expect-blocks-tab"] = (step, argument) => new ExpectBlocksTabStep(StepArgument.Choice(step, argument, _blocksTabs)),
        ["set-reference-filter"] = (_, argument) => new SetReferenceFilterStep(argument),
        ["expect-reference-headings"] = ExpectReferenceHeadings,
        ["open-new"] = (step, argument) => None(step, argument, new OpenNewStep()),
        ["open-rename"] = (step, argument) => new OpenRenameStep(StepArgument.Text(step, argument, "a name")),
        ["type-name"] = (step, argument) => new TypeNameStep(StepArgument.Text(step, argument, "the name to type")),
        ["press-prompt"] = (step, argument) => new PressPromptStep(StepArgument.Choice(step, argument, PromptNames.Buttons)),
        ["select-group"] = (step, argument) => new SelectGroupStep(StepArgument.Text(step, argument, "a name")),
        ["expect-list-result"] = (step, argument) => new ExpectListResultStep(StepArgument.Text(step, argument, "the text to look for")),
        ["expect-prompt"] = (step, argument) => new ExpectPromptStep(StepArgument.Choice(step, argument, PromptNames.Prompts)),
        ["expect-footer-in-view"] = (step, argument) => None(step, argument, new ExpectFooterInViewStep()),
        ["set-window-height"] = (step, argument) => new SetWindowHeightStep(StepArgument.Pixels(step, argument)),
        ["expect-window-height"] = (step, argument) => new ExpectWindowHeightStep(StepArgument.Pixels(step, argument)),
        ["set-window-size"] = (step, argument) => new SetWindowSizeStep(StepArgument.Size(step, argument)),
        ["center-window"] = (step, argument) => None(step, argument, new CenterWindowStep()),
        ["set-scripts-width"] = (step, argument) => new SetScriptsWidthStep(StepArgument.Pixels(step, argument)),
        ["expect-window-size"] = (step, argument) => new ExpectWindowSizeStep(StepArgument.Size(step, argument)),
        ["expect-window-width"] = (step, argument) => new ExpectWindowWidthStep(StepArgument.PixelsOrScreen(step, argument)),
        ["expect-window-centered"] = (step, argument) => None(step, argument, new ExpectWindowCenteredStep()),
        ["drag-window-edge"] = (step, argument) => DragWindowEdge(step, argument, false),
        ["drag-window-edge-sideways"] = (step, argument) => DragWindowEdge(step, argument, true),
        ["remember-window-rect"] = (step, argument) => None(step, argument, new RememberWindowRectStep()),
        ["expect-window-rect"] = (step, argument) => None(step, argument, new ExpectWindowRectStep()),
        ["expect-window-start-rect"] = (step, argument) => None(step, argument, new ExpectWindowStartRectStep()),
        ["library-read-failed"] = (step, argument) => new LibraryReadFailedStep(StepArgument.Choice(step, argument, _onOff)),
        ["startup-warning"] = (step, argument) => new StartupWarningStep(StepArgument.Choice(step, argument, _onOff)),
        ["expect-library-read-failed"] = (step, argument) => new ExpectLibraryReadFailedStep(StepArgument.Choice(step, argument, _onOff)),
        ["expect-startup-warning"] = (step, argument) => new ExpectStartupWarningStep(StepArgument.Choice(step, argument, _onOff)),
        ["expect-editor-in-view"] = (step, argument) => None(step, argument, new ExpectEditorInViewStep()),
        ["expect-editor-in-view-each-frame"] = (step, argument) => new ExpectEditorInViewEachFrameStep(StepArgument.Count(step, argument)),
        ["compile"] = (step, argument) => None(step, argument, new CompileStep()),
        ["expect-text"] = (step, argument) => new ExpectTextStep(StepArgument.Text(step, argument, "the text to look for")),
        ["expect-result"] = (step, argument) => new ExpectResultStep(StepArgument.Text(step, argument, "the text to look for")),
        ["expect-no-errors"] = (step, argument) => None(step, argument, new ExpectNoErrorsStep()),
        ["expect-errors"] = (step, argument) => new ExpectErrorsStep(StepArgument.Count(step, argument)),
        ["expect-error"] = ExpectError,
        ["expect-blocks"] = (step, argument) => new ExpectBlocksStep(StepArgument.Count(step, argument)),
        ["caret-at-start"] = (step, argument) => None(step, argument, new PlaceCaretStep(atEnd: false)),
        ["caret-at-end"] = (step, argument) => None(step, argument, new PlaceCaretStep(atEnd: true)),
        ["complete"] = (step, argument) => None(step, argument, new CompleteStep()),
        ["expect-completion"] = (step, argument) => ExpectCompletion(StepArgument.Text(step, argument, "a row's label, and optionally its detail")),
        ["expect-completion-no-detail"] = (step, argument) => new ExpectCompletionStep(ReadLabel(step, argument), string.Empty),
        ["expect-completion-missing"] = (step, argument) => new ExpectCompletionMissingStep(ReadLabel(step, argument)),
        ["expect-call-help"] = (step, argument) => new ExpectCallHelpStep(StepArgument.Text(step, argument, "the text to look for")),
        ["expect-call-help-active"] = (step, argument) => new ExpectCallHelpActiveStep(StepArgument.Text(step, argument, "the highlighted input's or Setting's label")),
        ["expect-call-help-no-active"] = (step, argument) => None(step, argument, new ExpectCallHelpActiveStep(null)),
        ["expect-no-completion"] = (step, argument) => None(step, argument, new ExpectNoCompletionStep()),
        ["expect-no-call-help"] = (step, argument) => None(step, argument, new ExpectNoCallHelpStep()),
        ["hover"] = (step, argument) => new HoverStep(StepArgument.Text(step, argument, "the text to move the mouse onto")),
        ["expect-hover"] = (step, argument) => new ExpectHoverStep(StepArgument.Text(step, argument, "the text to look for")),
        ["expect-no-hover"] = (step, argument) => None(step, argument, new ExpectNoHoverStep()),
        ["expect-usages"] = ExpectUsages,
        ["ctrl-click"] = (step, argument) => new CtrlClickStep(StepArgument.Text(step, argument, "the text to Ctrl+click")),
        ["press"] = (step, argument) => new PressStep(StepArgument.Keys(step, argument, KeysStep.MaxKeysPerFrame)),
        ["type"] = (step, argument) => new TypeStep(StepArgument.Text(step, argument, "the text to type")),
        ["place-block"] = PlaceBlock,
        ["set-block-setting"] = SetBlockSetting,
        ["set-stored-setting"] = SetStoredSetting,
        ["name-group-block"] = NameGroupBlock,
        ["wire-blocks"] = (step, argument) => new WireBlocksStep(ReadWire(step, argument)),
        ["wire-dependency-loop"] = (step, argument) => new WireDependencyLoopStep(ReadWire(step, argument)),
        ["select-blocks"] = (step, argument) => new SelectBlocksStep(ReadBlockNames(step, argument)),
        ["expect-wire"] = (step, argument) => new ExpectWireStep(ReadWire(step, argument)),
        ["expect-wires"] = ExpectWires,
        ["remember-blocks"] = (step, argument) => new RememberBlocksStep(ReadBlockNames(step, argument)),
        ["expect-blocks-unmoved"] = (step, argument) => new ExpectBlocksUnmovedStep(ReadBlockNames(step, argument)),
        ["move-block"] = MoveBlock,
        ["expect-signal"] = ExpectSignal,
        ["expect-display"] = ExpectDisplay,
        ["turn-off-style"] = TurnOffStyle,
        ["insert-selected"] = (step, argument) => None(step, argument, new InsertSelectedStep()),
        ["import-selected"] = (step, argument) => None(step, argument, new ImportSelectedStep()),
        ["delete-carrier"] = (step, argument) => new DeleteCarrierStep(StepArgument.Text(step, argument, "a Script's name")),
        ["delete-drafts"] = (step, argument) => None(step, argument, new DeleteDraftsStep()),
        ["expect-orphans"] = (step, argument) => new ExpectOrphansStep(StepArgument.Count(step, argument)),
        ["import-orphan"] = (step, argument) => new ImportOrphanStep(StepArgument.Text(step, argument, "an Orphaned Group's name")),
        ["restore-orphan"] = (step, argument) => new RestoreOrphanStep(StepArgument.Text(step, argument, "an Orphaned Group's name")),
        ["expect-orphan-result"] = (step, argument) => new ExpectOrphanResultStep(StepArgument.Text(step, argument, $"the text to look for, or {ExpectOrphanResultStep.NoResult}")),
        ["grab-blocks"] = (step, argument) => None(step, argument, new GrabBlocksStep()),
        ["drop-blocks"] = (step, argument) => None(step, argument, new DropBlocksStep()),
        ["paste-blocks"] = PasteBlocks,
        ["save-level"] = (step, argument) => new SaveLevelStep(StepArgument.FileName(step, argument)),
        ["load-level"] = LoadLevel,
        ["undo"] = (step, argument) => None(step, argument, new UndoStep()),
        ["redo"] = (step, argument) => None(step, argument, new RedoStep()),
        ["place-start"] = (step, argument) => None(step, argument, new PlaceStartStep()),
        ["test-mode"] = (step, argument) => None(step, argument, new TestModeStep()),
        ["restart-round"] = (step, argument) => None(step, argument, new RestartRoundStep()),
        ["back-to-editor"] = (step, argument) => None(step, argument, new BackToEditorStep()),
        ["wait"] = (step, argument) => new WaitStep(StepArgument.Seconds(step, argument)),
        ["screenshot"] = (step, argument) => new ScreenshotStep(StepArgument.FileName(step, argument)),
        ["expect-blank-screenshot"] = (step, argument) => new ExpectBlankScreenshotStep(StepArgument.FileName(step, argument)),
        ["expect-no-exceptions"] = (step, argument) => None(step, argument, new ExpectNoExceptionsStep()),
        ["expect-step-fails"] = ExpectStepFails,
        ["log"] = (step, argument) => new LogStep(StepArgument.Text(step, argument, "the text to log")),
    };

    /// <summary>
    /// Includes <see cref="SetText"/>, which <see cref="ScenarioParser"/> reads itself as it spans several lines.
    /// </summary>
    public static IEnumerable<string> Names => [.. _steps.Keys, SetText];

    /// <summary>
    /// Returns null for an unknown step.
    /// </summary>
    public static ScenarioStep Create(string step, string argument)
    {
        return _steps.TryGetValue(step, out var create) ? create(step, argument) : null;
    }

    private static ScenarioStep None(string step, string argument, ScenarioStep created)
    {
        StepArgument.None(step, argument);
        return created;
    }
}
