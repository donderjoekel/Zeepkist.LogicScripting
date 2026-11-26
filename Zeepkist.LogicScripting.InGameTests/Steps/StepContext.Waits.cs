using System;
using System.Collections;
using Imui.Core;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed partial class StepContext
{
    /// <summary>
    /// Waits until <paramref name="condition"/> holds or <paramref name="seconds"/> pass, whichever is first, so the
    /// step checks again afterwards.
    /// </summary>
    public IEnumerator WaitUntil(Func<bool> condition, float seconds)
    {
        var giveUpAt = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < giveUpAt)
        {
            yield return null;
        }
    }

    /// <summary>
    /// Presses one of the level editor's keys once and waits until the editor has read it. Fails the step when it isn't
    /// read in time.
    /// </summary>
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
    /// Waits for the shown tab's list to take the button or click handed to it, the next time the window draws it. When
    /// it isn't drawn in time, such as with the Scripts side collapsed, fails the step and drops the input, so it can't
    /// land in a later step.
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
    /// it had before it closed. Fails the step when it isn't drawn in time.
    /// </summary>
    public IEnumerator WaitForWindowDrawn()
    {
        yield return WaitUntil(() => Window.WindowLayout.Frame == Time.frameCount, WindowDrawSeconds);
        if (Window.WindowLayout.Frame != Time.frameCount)
            Fail("the window wasn't drawn", $"Unity frame {Time.frameCount}", $"Unity frame {Window.WindowLayout.Frame}");
    }

    /// <summary>
    /// Waits for the window to be drawn, as <see cref="WaitForWindowDrawn"/> does, and fails the step when that frame
    /// didn't draw the Reference tab: with the Placed blocks tab shown or the Blocks side collapsed.
    /// </summary>
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

    /// <summary>
    /// For a step that checks the window is drawn at <paramref name="expected"/>, so its result has both rects.
    /// </summary>
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

    /// <summary>
    /// Waits for the window to take the size or position a step asked for, which it does as it's next drawn, and to lay
    /// itself out in it. Fails the step when it isn't drawn in time.
    /// </summary>
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
}
