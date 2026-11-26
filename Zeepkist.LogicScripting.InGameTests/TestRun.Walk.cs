using System;
using System.Collections;
using TNRD.Zeepkist.LogicScripting.Blocks;
using UnityEngine;
using ZeepSDK.Utilities;
using Object = UnityEngine.Object;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

internal sealed partial class TestRun
{
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

    /// <summary>
    /// Takes the game to a new level in the editor the way a player starts one: out of test mode if the last scenario
    /// stopped there, back to the main menu without saving, and into the editor again.
    /// </summary>
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

    /// <summary>
    /// Clicks the main menu's Level Editor button, or calls what it calls when no button is wired to it.
    /// </summary>
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
}
