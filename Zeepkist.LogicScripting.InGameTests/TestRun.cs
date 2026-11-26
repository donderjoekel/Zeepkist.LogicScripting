using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using TNRD.Zeepkist.LogicScripting.UI;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Walks from the start screen to a new level in the level editor the way a player does, takes a screenshot of it, runs
/// the scenarios, each on a new level of its own, and quits. Each wait gives up after <see cref="WaitSeconds"/>, so a
/// changed menu fails the run instead of hanging it.
/// </summary>
internal sealed partial class TestRun : IDisposable
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
