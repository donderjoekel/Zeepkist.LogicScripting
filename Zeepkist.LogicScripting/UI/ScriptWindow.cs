using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using TNRD.Zeepkist.LogicScripting.Storage;
using UnityEngine;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;
using ZeepSDK.UI;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Editor mouse input is blocked while the cursor is over the window, editor keyboard input while one of its fields
/// has focus.
/// </summary>
internal sealed partial class ScriptWindow : IZeepGUIDrawer
{
    private const string LogFile = "BepInEx/LogOutput.log";

    private readonly ManualLogSource _logger;
    private readonly StartupCheck _startupCheck;
    private readonly ConfigEntry<KeyCode> _toggleKey;
    private readonly EditorAssist _editorAssist;
    private readonly EditorAssist _libraryEditorAssist;
    private readonly PlacedBlockHighlight _highlight = new();
    private readonly ScriptList _scripts = new();
    private readonly DraftStore _drafts;
    private readonly DraftSaver _draftSaver;
    private readonly LibraryStore _libraryStore;
    private readonly LibrarySaver _librarySaver;
    private readonly WindowStateStore _stateStore;
    private readonly WindowState _state;

    // One each, so every Script keeps its own caret and undo
    private readonly Dictionary<ScriptId, ScriptEditor> _editors = [];
    private readonly Dictionary<LibraryId, ScriptEditor> _libraryEditors = [];
    private readonly ScriptListPanel _scriptListPanel;
    private readonly LibraryListPanel _libraryListPanel;
    private readonly OrphanedGroupsPanel _orphanedGroupsPanel;
    private readonly PlacedBlocksPanel _placedBlocksPanel = new();
    private readonly ReferencePanel _referencePanel;

    private EditedItem _edited;
    private int? _readLevelChanges;
    private string _result = string.Empty;
    private bool _mouseOver;
    private bool _fieldFocused;
    private bool _blockingMouse;
    private bool _blockingKeyboard;
    private bool _failed;

    // Set by the in-game tests
    private readonly RectRequest _requestedRect = new();

    // Set by the in-game tests to show the libraries.json warning without a file that failed to read
    private bool _showLibraryReadFailed;

    // Set by the in-game tests to show the startup check's warnings on a game version it passes on
    private bool _showStartupWarning;

    // Kept for the in-game tests
    private EditorLayout _editorLayout;
    private WindowLayout _windowLayout;
    private WarningLayout _startupWarningLayout;
    private WarningLayout _libraryReadFailedLayout;

    private bool AnyFieldFocused => _fieldFocused || _scriptListPanel.NameFocused || _libraryListPanel.NameFocused || _referencePanel.FilterFocused
        || _renamePrompt.Focused;
    private LibraryList Libraries => _libraryStore.Libraries;
    private bool ShowsLibraryReadFailed => _libraryStore.ReadFailed || _showLibraryReadFailed;
    private bool ShowsUpdateNeeded => !_startupCheck.CanPlace || _showStartupWarning;

    /// <summary>
    /// The in-game tests, on a game version the startup check passes on, get the first few Logic Blocks, so the line
    /// wraps the way a real game update's would.
    /// </summary>
    private IReadOnlyList<string> ShownDisabledBlocks => _startupCheck.DisabledBlocks.Count == 0 && _showStartupWarning
        ? [.. BlockManifest.All.Take(3).Select(t => t.Describe(t.Styles[0]))]
        : _startupCheck.DisabledBlocks;
    private ScriptEntry CurrentScript => (_edited as EditedScript)?.Entry;
    private Library CurrentLibrary => (_edited as EditedLibrary)?.Library;

    public ScriptWindow(
        ManualLogSource logger,
        StartupCheck startupCheck,
        DraftStore drafts,
        LibraryStore libraries,
        WindowStateStore stateStore,
        ConfigEntry<KeyCode> toggleKey,
        EditorAssist editorAssist)
    {
        _logger = logger;
        _startupCheck = startupCheck;
        _drafts = drafts;
        _stateStore = stateStore;
        _state = stateStore.Load();
        _savedState = _state.Copy();
        _toggleKey = toggleKey;
        _editorAssist = editorAssist;
        _libraryEditorAssist = editorAssist.ForLibrary();
        _draftSaver = new DraftSaver(drafts, _scripts);
        _libraryStore = libraries;
        _librarySaver = new LibrarySaver(libraries);
        _scriptListPanel = new ScriptListPanel(logger, startupCheck, _scripts, _draftSaver, ForgetScript);
        _libraryListPanel = new LibraryListPanel(libraries.Libraries, OnLibraryDeleted);
        _referencePanel = new ReferencePanel(libraries.Libraries);
        _orphanedGroupsPanel = new OrphanedGroupsPanel(logger);
    }

    public bool IsOpen => _state.Open;

    /// <summary>
    /// ZeepSDK draws every drawer in one loop without catching, so an exception here would stop the drawers after this
    /// one, and come back every frame with the editor's input still blocked. Taking the checkpoint is guarded too: it
    /// reads Imui's private fields, which a game update can rename.
    /// </summary>
    public void OnZeepGUI(ImGui gui)
    {
        GuiCheckpoint? checkpoint = null;
        try
        {
            checkpoint = GuiCheckpoint.Take(gui);
            DrawFrame(gui);
        }
        catch (Exception e)
        {
            OnFailed(gui, e);
            checkpoint?.Restore(gui);
        }
    }

    private void DrawFrame(ImGui gui)
    {
        // Outside the editor nothing changes the Scripts any more, so waiting longer could only lose the last edits
        if (LevelEditorApi.IsInLevelEditor)
        {
            _draftSaver.Update();
            _librarySaver.Update();
        }
        else
        {
            _draftSaver.SavePending();
            _librarySaver.Save();
        }

        SaveStateWhenSettled();

        // Hidden, not closed: the window comes back where it was after test mode
        if (!LevelEditorApi.IsInLevelEditor || LevelEditorApi.IsTestingLevel)
        {
            ReleaseInput();
            return;
        }

        var toggleKey = _toggleKey.Value;
        if (toggleKey != KeyCode.None && !AnyFieldFocused && Input.GetKeyDown(toggleKey))
            SetOpen(!_state.Open);

        if (!_state.Open)
        {
            ReleaseInput();
            return;
        }

        // Imui never sees clicks that land in the level, so a field would keep focus and keep taking keys
        if (!_mouseOver && AnyFieldFocused && IsAnyMouseButtonDown())
            gui.ResetActiveControl();

        var hovered = DrawWindow(gui);
        SetKeyboardBlocked(AnyFieldFocused);
        _highlight.Set(ComponentCache.Get<LEV_LevelEditorCentral>(), _state.Open ? hovered : null);
    }

    /// <summary>
    /// Logs only the first failure until the window is opened again, as most come back every frame. The highlight is
    /// left for the next frame, which removes it with the window closed: it's game code, and may be what failed.
    /// </summary>
    private void OnFailed(ImGui gui, Exception e)
    {
        var wasOpen = _state.Open;
        if (!_failed)
        {
            _failed = true;
            _logger.LogError($"The Script window failed: {e}");
            MessengerApi.LogError(wasOpen
                ? $"The Logic Script window ran into an error and closed. Details are in {LogFile}"
                : $"Logic Scripting ran into an error. Details are in {LogFile}");
        }

        // A field that was active when drawing stopped would otherwise take focus, and the keyboard, on reopening
        if (wasOpen)
            gui.ResetActiveControl();

        _state.Open = false;
        UnblockEditor();
    }
}
