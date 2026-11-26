using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.UI;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// What a step works with, and where it leaves its outcome. One per scenario, reset before each step.
/// </summary>
internal sealed partial class StepContext
{
    private const float KeyPressSeconds = 5;
    private const float ListDrawSeconds = 2;
    private const float WindowDrawSeconds = 2;

    private readonly Screenshots _screenshots;
    private readonly Func<ImRect?> _startWindowRect;

    public StepContext(
        string scenario,
        ScriptWindow.Driver window,
        GameEvents game,
        ExceptionLog exceptions,
        ManualLogSource logger,
        Screenshots screenshots,
        Func<ImRect?> startWindowRect)
    {
        Scenario = scenario;
        Window = window;
        Game = game;
        Exceptions = exceptions;
        Logger = logger;
        _screenshots = screenshots;
        _startWindowRect = startWindowRect;
    }

    public string Scenario { get; }
    public ScriptWindow.Driver Window { get; }
    public GameEvents Game { get; }
    public ExceptionLog Exceptions { get; }
    public ManualLogSource Logger { get; }

    /// <summary>
    /// They last the scenario.
    /// </summary>
    public PlacedBlockNames PlacedBlocks { get; } = new();

    /// <summary>
    /// What remember-window-rect remembered, for the rest of the scenario, or null.
    /// </summary>
    public ImRect? RememberedWindowRect { get; set; }

    /// <summary>
    /// What remember-blocks remembered, by the block's name, for the rest of the scenario.
    /// </summary>
    public Dictionary<string, BlockPose> RememberedPoses { get; } = [];

    /// <summary>
    /// The files save-level saved, by the name it was given, for the rest of the scenario.
    /// </summary>
    public Dictionary<string, FileInfo> SavedLevels { get; } = [];

    /// <summary>
    /// The window's rect the first time a Scenario of the run drew it, which the run puts back before each Scenario after
    /// the first, or null while none has.
    /// </summary>
    public ImRect? StartWindowRect => _startWindowRect();

    public bool Failed { get; private set; }

    /// <summary>
    /// What the step found, such as a result line, shown whether it passed or not.
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    public string Expected { get; private set; }
    public string Actual { get; private set; }

    public void Reset()
    {
        Failed = false;
        Detail = string.Empty;
        Expected = null;
        Actual = null;
    }

    /// <summary>
    /// For a check that passed, so the results show what it compared.
    /// </summary>
    public void Pass(string detail, string expected, string actual)
    {
        Detail = detail;
        Expected = expected;
        Actual = actual;
    }

    public void Fail(string detail, string expected = null, string actual = null)
    {
        Failed = true;
        Detail = detail;
        Expected = expected;
        Actual = actual;
    }

    public void FailNoScript(string name)
    {
        Fail($"no Script named \"{name}\"", name, string.Join(", ", Window.ScriptNames));
    }

    /// <summary>
    /// For a step on an entry of the shown tab's list that has none by the name.
    /// </summary>
    public void FailNoEntry(string name)
    {
        var list = Window.ShownList;
        Fail($"no {list.Noun} named \"{name}\" on the shown tab", name, string.Join(", ", list.Names));
    }

    /// <summary>
    /// Saved as <c>&lt;scenario&gt;-&lt;name&gt;.png</c> in the run's results.
    /// </summary>
    public IEnumerator Screenshot(string name, Color? cover = null)
    {
        return _screenshots.Take($"{Scenario}-{name}", cover);
    }

    public string LastBlankColor => _screenshots.LastBlankColor;
}
