using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using TNRD.Zeepkist.LogicScripting.UI;
using UnityEngine;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// One per scenario, reset before each step.
/// </summary>
internal sealed class StepContext
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

    public PlacedBlockNames PlacedBlocks { get; } = new();

    public ImRect? RememberedWindowRect { get; set; }

    public Dictionary<string, BlockPose> RememberedPoses { get; } = [];

    public Dictionary<string, FileInfo> SavedLevels { get; } = [];

    /// <summary>
    /// The window's rect the first time a Scenario of the run drew it, which the run puts back before each Scenario after
    /// the first, or null while none has.
    /// </summary>
    public ImRect? StartWindowRect => _startWindowRect();

    public bool Failed { get; private set; }

    /// <summary>
    /// Shown whether the step passed or not.
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

    public void FailNoEntry(string name)
    {
        var list = Window.ShownList;
        Fail($"no {list.Noun} named \"{name}\" on the shown tab", name, string.Join(", ", list.Names));
    }

    public string DescribeOrphans()
    {
        var orphans = Window.Orphans.Select(o => $"\"{OrphanedGroupsPanel.GetName(o)}\" with {o.BlockCount} blocks and {(o.Draft == null ? "no Draft" : "a Draft")}");
        return Window.Orphans.Count == 0 ? "no Orphaned Groups" : string.Join(", ", orphans);
    }

    public OrphanedGroup RequireOrphan(string name)
    {
        var orphan = Window.FindOrphan(name);
        if (orphan == null)
            Fail($"no Orphaned Group is called \"{name}\"", name, DescribeOrphans());

        return orphan;
    }

    public void FailNameTaken(string name)
    {
        Fail($"a block was already placed or named as \"{name}\"", "a new name", PlacedBlocks.Listed);
    }

    public void FailUnknownUid(string name)
    {
        Fail($"no block placed or named as \"{name}\": add place-block <function> as {name} or name-group-block first", name, PlacedBlocks.Listed);
    }

    public BlockType RequireBlockType(string function)
    {
        var type = BlockManifest.All.FirstOrDefault(t => t.Function == function);
        if (type == null)
            Fail($"no Logic Block type is called '{function}'");

        return type;
    }

    public NamedBlock RequireNamedBlock(LEV_LevelEditorCentral central, string name)
    {
        var uid = RequireUid(name);
        if (uid == null)
            return null;

        var block = NamedBlock.Find(central, name, uid, out var error);
        if (block == null)
            Fail(error);

        return block;
    }

    public RunningBlock RequireRunningBlock(string name)
    {
        if (!TestModeControls.IsActive)
        {
            Fail("not in test mode: add test-mode first");
            return null;
        }

        var uid = RequireUid(name);
        if (uid == null)
            return null;

        var block = RunningBlock.Find(name, uid, out var error);
        if (block == null)
            Fail(error);

        return block;
    }

    /// <summary>
    /// Whether the level has the Wire or not: null only when one of the blocks or Connectors doesn't exist.
    /// </summary>
    public BlockWire RequireWire(LEV_LevelEditorCentral central, WireArgument wire, out NamedBlock from, out NamedBlock to)
    {
        to = null;
        from = RequireNamedBlock(central, wire.From);
        if (from == null)
            return null;

        to = RequireNamedBlock(central, wire.To);
        if (to == null)
            return null;

        var output = NamedBlock.FindConnector(wire.From, "output", from.Outputs, wire.Output, out var error);
        if (output == null)
        {
            Fail(error);
            return null;
        }

        var input = NamedBlock.FindConnector(wire.To, "input", to.Type.Inputs, wire.Input, out error);
        if (input == null)
        {
            Fail(error);
            return null;
        }

        return new BlockWire(from.Block.UID, output.Index, to.Block.UID, input.Index);
    }

    /// <summary>
    /// Saved as <c>&lt;scenario&gt;-&lt;name&gt;.png</c> in the run's results.
    /// </summary>
    public IEnumerator Screenshot(string name, Color? cover = null)
    {
        return _screenshots.Take($"{Scenario}-{name}", cover);
    }

    public string LastBlankColor => _screenshots.LastBlankColor;

    /// <summary>
    /// Doesn't fail the step when time runs out, so the step checks again afterwards.
    /// </summary>
    public IEnumerator WaitUntil(Func<bool> condition, float seconds)
    {
        var giveUpAt = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < giveUpAt)
        {
            yield return null;
        }
    }

    public IEnumerator PressEditorKey(EditorKey key)
    {
        EditorKeys.Press(key);
        yield return WaitUntil(() => !EditorKeys.IsPressed, KeyPressSeconds);
        if (EditorKeys.IsPressed)
        {
            EditorKeys.Release();
            Fail($"the level editor didn't read the key within {KeyPressSeconds} seconds");
        }
    }

    /// <summary>
    /// When the list isn't drawn in time, such as with the Scripts side collapsed, drops the input too, so it can't land
    /// in a later step.
    /// </summary>
    public IEnumerator WaitForShownList(string failure)
    {
        var list = Window.ShownList;
        yield return WaitUntil(() => !list.IsInputPending, ListDrawSeconds);
        if (list.IsInputPending)
        {
            list.CancelInput();
            Fail(failure);
        }
    }

    /// <summary>
    /// Waits for the window to be drawn, unless it was this frame, so its rect is the one Imui keeps now and not the one
    /// it had before it closed.
    /// </summary>
    public IEnumerator WaitForWindowDrawn()
    {
        yield return WaitUntil(() => Window.WindowLayout.Frame == Time.frameCount, WindowDrawSeconds);
        if (Window.WindowLayout.Frame != Time.frameCount)
            Fail("the window wasn't drawn", $"Unity frame {Time.frameCount}", $"Unity frame {Window.WindowLayout.Frame}");
    }

    public IEnumerator WaitForReferenceTabDrawn()
    {
        yield return WaitForWindowDrawn();
        if (Failed)
            yield break;

        var drawn = Window.ReferenceDrawnFrame;
        if (drawn != Time.frameCount)
        {
            Fail(
                "the Reference tab wasn't drawn: add show-blocks-tab reference first, with the Blocks side shown",
                $"Unity frame {Time.frameCount}",
                $"Unity frame {drawn}");
        }
    }

    public IEnumerator ExpectWindowRect(ImRect expected, string passDetail, string failDetail)
    {
        yield return WaitForWindowDrawn();
        if (Failed)
            yield break;

        var rect = Window.WindowRect;
        if (RectCheck.Matches(expected, rect))
            Pass(passDetail, RectCheck.Describe(expected), RectCheck.Describe(rect));
        else
            Fail(failDetail, RectCheck.Describe(expected), RectCheck.Describe(rect));
    }

    public IEnumerator WaitForWindowRect()
    {
        yield return WaitUntil(() => !Window.IsRectPending, WindowDrawSeconds);
        if (Window.IsRectPending)
        {
            Fail("the window wasn't drawn to take the size or position");
            yield break;
        }

        // A frame to lay the window out in its new rect, and one for the footer, which sizes itself from the last
        yield return null;
        yield return null;
    }

    public LEV_LevelEditorCentral RequireEditor()
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
            Fail("not in the level editor");

        return central;
    }

    public LEV_LevelEditorCentral RequireSelectedBlocks()
    {
        var central = RequireEditor();
        if (central == null)
            return null;

        if (central.gizmos.isGrabbing)
            Fail("blocks are grabbed: add drop-blocks first");
        else if (central.selection.list.Count == 0)
            Fail("no blocks are selected: add select-blocks first");

        return Failed ? null : central;
    }

    /// <summary>
    /// A player can't use the window's buttons while it's closed.
    /// </summary>
    public bool RequireOpenWindow()
    {
        if (!Window.IsOpen)
            Fail("the window isn't open: add open-window first");

        return Window.IsOpen;
    }

    /// <summary>
    /// Keys typed while no field in the window has focus go to the level editor.
    /// </summary>
    public bool RequireFocusedField()
    {
        if (!RequireOpenWindow())
            return false;

        if (!Window.IsFieldFocused)
            Fail("no field is focused: add caret-at-start or caret-at-end first");

        return Window.IsFieldFocused;
    }

    public bool RequireScript()
    {
        if (!RequireOpenWindow())
            return false;

        if (!Window.HasScript)
            Fail("no Script selected: add new-script or select-script first");

        return Window.HasScript;
    }

    public bool RequireText()
    {
        if (!RequireOpenWindow())
            return false;

        if (!Window.HasText)
            Fail("no Script or Library selected: add new-script, select-script, new-library or select-library first");

        return Window.HasText;
    }

    private string RequireUid(string name)
    {
        if (PlacedBlocks.TryGetUid(name, out var uid))
            return uid;

        Fail($"no block was placed or named as \"{name}\": add place-block ... as {name} or name-group-block first", "a placed block's name", PlacedBlocks.Listed);
        return null;
    }
}
