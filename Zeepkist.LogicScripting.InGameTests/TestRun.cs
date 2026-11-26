using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using TMPro;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using TNRD.Zeepkist.LogicScripting.UI;
using UnityEngine;
using ZeepSDK.Utilities;
using Object = UnityEngine.Object;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Walks from the start screen to a new level in the level editor the way a player does, takes a screenshot of it, runs
/// the scenarios, each on a new level of its own, and quits. Each wait gives up after <see cref="WaitSeconds"/>, so a
/// changed menu fails the run instead of hanging it.
/// </summary>
internal sealed class TestRun : IDisposable
{
    // Starts every line this plugin logs, so a run's lines can be picked out of the BepInEx log
    public const string Marker = "[ZLS-TEST]";
    private const float WaitSeconds = 120;

    // How long a run asked to stay on the start screen waits there, which is only to show the failure it reports
    private const float StuckStartSeconds = 10;

    private readonly ManualLogSource _logger;
    private readonly RunResults _results;
    private readonly ExceptionLog _exceptions;
    private readonly RunRequest _request;
    private readonly string _folder;
    private readonly GameEvents _game = new();
    private readonly Screenshots _screenshots;

    // Set once a step of the walk failed, which ends the run, unlike a failed step of a scenario
    private bool _walkFailed;

    // The scenario a walk to a new level is for, so the results list its steps under it
    private string _walkScenario = string.Empty;

    public TestRun(ManualLogSource logger, RunResults results, ExceptionLog exceptions, RunRequest request, string folder)
    {
        _logger = logger;
        _results = results;
        _exceptions = exceptions;
        _request = request;
        _folder = folder;
        _screenshots = new Screenshots(results, logger);
        Application.logMessageReceived += OnLogMessage;
    }

    public void Dispose()
    {
        Application.logMessageReceived -= OnLogMessage;
        _game.Dispose();
    }

    /// <summary>
    /// Reads every scenario first, so one that can't be read fails without the game starting it. An exception anywhere
    /// in the walk fails the run, and still quits the game, instead of leaving the run script to time out.
    /// </summary>
    public IEnumerator Run()
    {
        List<Scenario> scenarios = [];
        Exception error = null;
        try
        {
            scenarios = ReadScenarios();
        }
        catch (Exception exception)
        {
            error = exception;
        }

        if (error == null && scenarios.Count > 0)
            yield return GuardedRoutine.Run(Walk(), exception => error = exception);

        if (error != null)
        {
            _logger.LogError(error);
            Step(RunStep.Fail("no errors", $"{error.GetType().Name}: {error.Message}"));
            yield return _screenshots.Take("failure");
        }
        else if (scenarios.Count > 0 && !_walkFailed)
        {
            yield return RunScenarios(scenarios);
        }

        yield return Finish();
    }

    private List<Scenario> ReadScenarios()
    {
        List<Scenario> scenarios = [];
        foreach (var file in _request.Scenarios)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var scenario = ScenarioParser.Parse(name, File.ReadAllText(Path.Combine(_folder, "scenarios", file)), out var error);
            if (scenario == null)
                Step(new RunStep { Scenario = name, Name = "read scenario", Detail = error });
            else
                scenarios.Add(scenario);
        }

        return scenarios;
    }

    /// <summary>
    /// A failed step stops its scenario with a screenshot, and the run goes on with the next, on a new level and without
    /// the Libraries the last one made. It puts back what the window kept from the last one too, which a failed step
    /// stopped short of doing, and each scenario gives Imui the player's mouse again as it ends. A run starts with a
    /// window that was never drawn, so it remembers the window's rect after the first step that draws it, before a later
    /// step can change it: the one the window first opens at. The walk already brought the first one to a new level.
    /// </summary>
    private IEnumerator RunScenarios(List<Scenario> scenarios)
    {
        var window = ((LogicScripting.Plugin)Chainloader.PluginInfos[LogicScripting.MyPluginInfo.PLUGIN_GUID].Instance).ScriptWindow;
        var driver = new ScriptWindow.Driver(window);
        var startState = driver.RememberKeptState();
        for (var i = 0; i < scenarios.Count; i++)
        {
            var scenario = scenarios[i];
            _logger.LogInfo($"{Marker} scenario {scenario.Name}");
            _exceptions.StartScenario(scenario.Name);
            if (i > 0)
            {
                driver.DeleteLibraries();
                driver.PutBackKeptState(startState);
                yield return GuardedRoutine.Run(NewLevel(scenario.Name), WalkFailed);
                _walkScenario = string.Empty;
                if (_walkFailed)
                    yield break;
            }

            var context = new StepContext(scenario.Name, driver, _game, _exceptions, _logger, _screenshots, () => startState.WindowRect);
            foreach (var step in scenario.Steps)
            {
                context.Reset();
                var startedAt = Time.realtimeSinceStartup;
                yield return GuardedRoutine.Run(step.Run(context), exception =>
                {
                    _logger.LogError(exception);
                    context.Fail($"{exception.GetType().Name}: {exception.Message}");
                });

                if (startState.WindowRect == null && driver.DrawnWindowRect is { } rect)
                {
                    startState = startState.WithWindowRect(rect);
                    _logger.LogInfo($"{Marker} start window rect {RectCheck.Describe(rect)}");
                }

                Step(new RunStep
                {
                    Scenario = scenario.Name,
                    Line = step.Line,
                    Name = step.Source,
                    Passed = !context.Failed,
                    Detail = context.Detail,
                    Expected = context.Expected,
                    Actual = context.Actual,
                    Seconds = Math.Round(Time.realtimeSinceStartup - startedAt, 2),
                });

                if (context.Failed)
                {
                    yield return context.Screenshot("failure");
                    break;
                }
            }

            // hover leaves the mouse where it put it, for the steps after it
            ImuiMouse.GiveBack();
        }
    }

    private IEnumerator Walk()
    {
        // BepInEx flushes its log file every couple of seconds, and the copies it skipped are logged before plugins load
        yield return new WaitForSecondsRealtime(3);
        var buildCheck = BuildCheck.Run(_folder);
        Step(buildCheck);
        _walkFailed = !buildCheck.Passed;
        if (_walkFailed)
            yield break;

        PressAnyKeyToStartGame startScreen = null;
        yield return WaitFor(() => startScreen = Find<PressAnyKeyToStartGame>(), "the start screen", () => DescribeStartScreen(null));
        var stuck = _request.StuckStart;
        if (stuck)
            _logger.LogWarning($"{Marker} staying on the start screen, as the run asked");

        if (_request.LeaveStartScreen)
        {
            yield return LeaveStartScreen(startScreen);
            if (_walkFailed)
                yield break;
        }

        // The game can leave its start screen by itself, which destroys it, and then the main menu wait checks where it
        // went. Unity's == null is what sees a destroyed object.
        yield return WaitUntil(
            () => !stuck
                  && PlayerManager.Instance != null
                  && (startScreen == null || (startScreen.disableThisIntroPanel != null && startScreen.disableThisIntroPanel.activeInHierarchy)),
            "press any key",
            () => DescribeStartScreen(startScreen),
            stuck ? StuckStartSeconds : WaitSeconds);
        if (_walkFailed)
            yield break;

        // Skips the main menu's one-time warning panels for this session, without changing settings
        PlayerManager.Instance.modPanelHasAlreadyBeenDisplayed = true;
        PlayerManager.Instance.epilepsyPanelHasAlreadyBeenDisplayed = true;
        if (startScreen != null)
            startScreen.LoadNextLevel();
        else
            _logger.LogWarning($"{Marker} the game left its start screen by itself");

        // The game has put its saved resolution in place by now, which it starts without
        _results.Display = Display();
        _logger.LogInfo($"{Marker} display {_results.Display}");
        Step(RunStep.Pass("start screen"));

        yield return EnterEditor();
        if (_walkFailed)
            yield break;

        yield return _screenshots.Take("editor");
        Step(RunStep.Pass("editor reached"));
    }

    /// <summary>
    /// Takes the game off its start screen the way the game does when it moves on by itself: once the start screen is
    /// ready, without the walk's skipping of the main menu's warning panels, and waits until the start screen is gone.
    /// </summary>
    private IEnumerator LeaveStartScreen(PressAnyKeyToStartGame startScreen)
    {
        _logger.LogWarning($"{Marker} leaving the start screen before the walk checks it, as the run asked");
        yield return WaitUntil(
            () => startScreen.disableThisIntroPanel.activeInHierarchy && PlayerManager.Instance != null,
            "start screen ready to leave",
            () => DescribeStartScreen(startScreen));
        if (_walkFailed)
            yield break;

        startScreen.LoadNextLevel();
        yield return WaitUntil(() => startScreen == null, "start screen left", () => DescribeStartScreen(startScreen));
    }

    /// <summary>
    /// From the main menu, or on the way to it, to a new level in the editor.
    /// </summary>
    private IEnumerator EnterEditor()
    {
        LevelEditorUI menu = null;
        yield return WaitFor(
            () => menu = Find<LevelEditorUI>(),
            "the main menu",
            () => $"the game showed no main menu; on screen: {StartScreenState.Shown(VisibleTexts(null))}");
        if (_walkFailed)
            yield break;

        // Let the menu finish opening, as a player can't click it any sooner
        yield return new WaitForSecondsRealtime(2);
        var entriesBefore = _game.EditorEntries;
        var blocksBefore = LevelChanges.Count;
        PressLevelEditorButton(menu);
        Step(RunStep.Pass("main menu"));

        yield return WaitUntil(() => _game.EditorEntries > entriesBefore && LevelChanges.Count > blocksBefore, "the level editor");
        if (_walkFailed)
            yield break;

        // The editor's own UI fades in after the level is ready
        yield return new WaitForSecondsRealtime(2);
    }

    private IEnumerator NewLevel(string scenario)
    {
        _walkScenario = scenario;
        if (TestModeControls.IsActive)
        {
            var loadsBefore = _game.LevelLoads;
            yield return TestModeControls.Leave();
            yield return WaitUntil(() => TestModeControls.IsLevelBack(_game, loadsBefore), "the level editor, from test mode");
            if (_walkFailed)
                yield break;
        }

        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
        {
            WalkFailed("new level", "not in the level editor, where a new level starts from");
            yield return _screenshots.Take($"{scenario}-failure");
            yield break;
        }

        central.pause.QuitToMenu();
        if (central.unsavedContentPopup.IsOpen)
            central.unsavedContentPopup.Yes();

        yield return EnterEditor();
        if (!_walkFailed)
            Step(RunStep.Pass("new level"));
    }

    private static T Find<T>()
        where T : Object
    {
        return Object.FindObjectOfType<T>(true);
    }

    private void PressLevelEditorButton(LevelEditorUI menu)
    {
        foreach (var button in Object.FindObjectsOfType<GenericButton>(true))
        {
            for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) == menu
                    && button.onClick.GetPersistentMethodName(i) == nameof(LevelEditorUI.StartLevelEditor))
                {
                    button.onClick.Invoke();
                    return;
                }
            }
        }

        _logger.LogWarning($"{Marker} no Level Editor button found, starting the editor directly");
        menu.StartLevelEditor();
    }

    /// <summary>
    /// Looks for an object once now and once after each scene load, rather than searching the scene every frame.
    /// </summary>
    private IEnumerator WaitFor(Func<Object> find, string what, Func<string> describe = null)
    {
        var checkedAt = -1;
        return WaitUntil(
            () =>
            {
                if (checkedAt == _game.ScenesLoaded)
                    return false;

                checkedAt = _game.ScenesLoaded;
                return find() != null;
            },
            what,
            describe);
    }

    /// <summary>
    /// With <paramref name="describe"/>, a wait that gives up says what the game showed instead.
    /// </summary>
    private IEnumerator WaitUntil(Func<bool> condition, string what, Func<string> describe = null, float seconds = WaitSeconds)
    {
        if (_walkFailed)
            yield break;

        var giveUpAt = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > giveUpAt)
            {
                var detail = $"not reached after {seconds} seconds";
                if (describe != null)
                    detail += $": {describe()}";

                WalkFailed(what, detail);
                yield return _screenshots.Take(_walkScenario.Length > 0 ? $"{_walkScenario}-failure" : "failure");
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// What the screen shows, for a walk that gave up before the main menu, with <paramref name="screen"/> once the game
    /// has its start screen. The start screen's own fields go to the log only, as they're the game's.
    /// </summary>
    private string DescribeStartScreen(PressAnyKeyToStartGame screen)
    {
        var state = new StartScreenState
        {
            // A destroyed screen still has its C# object, which Unity's == null takes for none
            ScreenFound = !ReferenceEquals(screen, null),
            ScreenLeft = !ReferenceEquals(screen, null) && screen == null,
            IntroPanelActive = screen != null && screen.disableThisIntroPanel != null && screen.disableThisIntroPanel.activeInHierarchy,
            PlayerManagerExists = PlayerManager.Instance != null,
            Texts = VisibleTexts(screen),
        };

        // The detail keeps only the first few, so all of them go to the log
        _logger.LogInfo($"{Marker} texts on screen: {string.Join(" | ", state.Texts.ConvertAll(text => text.Replace('\n', ' ')))}");
        if (screen != null)
            _logger.LogInfo($"{Marker} start screen fields: {FieldValues(screen)}");

        return state.Describe();
    }

    /// <summary>
    /// The texts on screen: those of active text components, which is what a person sees on a menu. Those under
    /// <paramref name="screen"/> come first, then those under its root, so the start screen's own status isn't crowded out
    /// of the step's detail by the rest of the scene.
    /// </summary>
    private static List<string> VisibleTexts(Component screen)
    {
        HashSet<Component> seen = [];
        List<string> texts = [];

        void Add(IEnumerable<TMP_Text> tmpTexts, IEnumerable<UnityEngine.UI.Text> uiTexts)
        {
            foreach (var text in tmpTexts)
            {
                if (text.isActiveAndEnabled && seen.Add(text))
                    texts.Add(text.text);
            }

            foreach (var text in uiTexts)
            {
                if (text.isActiveAndEnabled && seen.Add(text))
                    texts.Add(text.text);
            }
        }

        if (screen != null)
        {
            Add(screen.GetComponentsInChildren<TMP_Text>(), screen.GetComponentsInChildren<UnityEngine.UI.Text>());
            var root = screen.transform.root;
            Add(root.GetComponentsInChildren<TMP_Text>(), root.GetComponentsInChildren<UnityEngine.UI.Text>());
        }

        Add(Object.FindObjectsOfType<TMP_Text>(), Object.FindObjectsOfType<UnityEngine.UI.Text>());
        return texts;
    }

    private static string FieldValues(Component component)
    {
        List<string> values = [];
        foreach (var field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            var value = field.GetValue(component);
            var type = field.FieldType;
            if (type.IsPrimitive || type.IsEnum || type == typeof(string))
            {
                values.Add($"{field.Name}={value}");
            }
            else if (typeof(Object).IsAssignableFrom(type))
            {
                var unityObject = value as Object;
                var shown = unityObject == null ? "none"
                    : unityObject is GameObject gameObject ? (gameObject.activeInHierarchy ? "active" : "inactive")
                    : unityObject is Behaviour behaviour ? (behaviour.isActiveAndEnabled ? "enabled" : "disabled")
                    : "set";
                values.Add($"{field.Name}={shown}");
            }
        }

        return string.Join(", ", values);
    }

    private void WalkFailed(Exception exception)
    {
        _logger.LogError(exception);
        WalkFailed("new level", $"{exception.GetType().Name}: {exception.Message}");
    }

    private void WalkFailed(string what, string detail)
    {
        _walkFailed = true;
        Step(RunStep.Fail(what, detail));
    }

    /// <summary>
    /// The screen is the monitor the game's window is on, or the desktop's size without one.
    /// </summary>
    private static string Display()
    {
        var screen = Screen.currentResolution;
        return $"{Screen.width}×{Screen.height} px, {Screen.fullScreenMode}, screen {screen.width}×{screen.height} px";
    }

    private void Step(RunStep step)
    {
        if (step.Scenario.Length == 0)
            step.Scenario = _walkScenario;

        _results.Add(step);
        var scenario = step.Scenario.Length > 0 ? $"{step.Scenario}:{step.Line} " : string.Empty;
        var expected = step.Expected == null ? string.Empty : $" (expected: {step.Expected}, actual: {step.Actual})";
        var line = $"{Marker} {scenario}{step.Name} {(step.Passed ? "PASS" : "FAIL")} {step.Detail}{expected}".TrimEnd();
        if (step.Passed)
            _logger.LogInfo(line);
        else
            _logger.LogError(line);
    }

    private void OnLogMessage(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Exception)
            _exceptions.Record(message, stackTrace);
    }

    private IEnumerator Finish()
    {
        // What's logged from here on is the game quitting, which the run script reads from the log once it has quit
        Application.logMessageReceived -= OnLogMessage;
        _results.Finish();
        _logger.LogInfo($"{Marker} run {_results.RunId} {(_results.Passed ? "passed" : "failed")}");
        FlushDiskLog();

        // Quitting this way skips the editor's unsaved-level prompt
        yield return null;
        Application.Quit();
    }

    /// <summary>
    /// BepInEx flushes its log file every couple of seconds, and the game can quit before the next time, so the run script
    /// would read a log that stops before the run's end.
    /// </summary>
    private static void FlushDiskLog()
    {
        foreach (var listener in BepInEx.Logging.Logger.Listeners)
        {
            if (listener is DiskLogListener disk)
                disk.LogWriter?.Flush();
        }
    }
}
