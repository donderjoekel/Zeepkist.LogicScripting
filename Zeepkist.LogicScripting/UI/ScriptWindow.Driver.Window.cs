using Imui.Core;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptWindow
{
    internal sealed partial class Driver
    {
        /// <summary>
        /// The Scripts side's width as it was last drawn.
        /// </summary>
        public float DrawnScriptsWidth => _window._drawnScriptsWidth;

        /// <summary>
        /// Like dragging the Scripts side's edge until it's this wide, which it is from the next frame, as near as its
        /// minimum and the rest of the window let it.
        /// </summary>
        public void SetScriptsWidth(float width)
        {
            _window._state.Scripts.Size = width;
        }

        /// <summary>
        /// Like dragging the window's edge until it's this tall, which it is from the next frame.
        /// </summary>
        public void SetWindowHeight(float height)
        {
            _window._requestedRect.SetHeight(height);
        }

        /// <summary>
        /// Like dragging the window's resize handle until it's this big, which it is from the next frame.
        /// </summary>
        public void SetWindowSize(float width, float height)
        {
            _window._requestedRect.SetWidth(width);
            _window._requestedRect.SetHeight(height);
        }

        /// <summary>
        /// Moves the window to the middle of the screen, at the size it has then, from the next frame.
        /// </summary>
        public void CenterWindow()
        {
            _window._requestedRect.MoveToCenter();
        }

        /// <summary>
        /// Makes the Libraries tab show, or stop showing, the warning it shows when libraries.json couldn't be read.
        /// Libraries still save.
        /// </summary>
        public void ShowLibraryReadFailed(bool show)
        {
            _window._showLibraryReadFailed = show;
        }

        /// <summary>
        /// Makes the center show, or stop showing, the startup check's warnings, as on a game version the mod doesn't
        /// fully support. Compile and the checks work as before.
        /// </summary>
        public void ShowStartupWarning(bool show)
        {
            _window._showStartupWarning = show;
        }

        /// <summary>
        /// What a Scenario can change that the window keeps into the next one: its size and position, the Scripts side's
        /// width, the shown tabs, the Reference tab's filter and the warnings the tests turn on. A window that was never drawn has no rect, for
        /// the one it first opens at. The styles the tests turn off aren't in it, as none are at the start of a run.
        /// </summary>
        public KeptState RememberKeptState()
        {
            return new KeptState(
                DrawnWindowRect,
                _window._state.ShowLibraries,
                _window._showReference,
                _window._referencePanel.Filter,
                _window._showLibraryReadFailed,
                _window._showStartupWarning,
                _window._state.Scripts.Size);
        }

        /// <summary>
        /// The window's rect as Imui last drew it, or null when it was never drawn.
        /// </summary>
        public ImRect? DrawnWindowRect => _window._state.Width > 0 && _window._state.Height > 0 ? WindowRect : null;

        /// <summary>
        /// Puts back what <see cref="RememberKeptState"/> remembered, and turns back on the styles
        /// <see cref="TurnOffStyle"/> turned off. A closed window takes the size and position when it next opens. With no
        /// rect remembered the window was never drawn, so it still opens at its first size and position.
        /// </summary>
        public void PutBackKeptState(KeptState state)
        {
            _window._startupCheck.TurnTestStylesBackOn();
            if (state.WindowRect is { } rect)
            {
                SetWindowSize(rect.W, rect.H);
                _window._requestedRect.MoveTo(new Vector2(rect.X, rect.Top));
            }

            SetScriptsWidth(state.ScriptsSize);
            ShowTab(state.ShowLibraries);
            ShowBlocksTab(state.ShowReference);
            _window._referencePanel.SetFilter(state.ReferenceFilter);
            ShowLibraryReadFailed(state.ShowLibraryReadFailed);
            ShowStartupWarning(state.ShowStartupWarning);
        }

        /// <summary>
        /// Like clicking the Scripts or the Libraries tab, whose list the buttons for one are in.
        /// </summary>
        public void ShowTab(bool libraries)
        {
            _window._state.ShowLibraries = libraries;
        }

        /// <summary>
        /// Like clicking the Blocks side's Placed blocks or Reference tab.
        /// </summary>
        public void ShowBlocksTab(bool reference)
        {
            _window._showReference = reference;
        }

        /// <summary>
        /// Replaces the text in the Reference tab's filter, as typing it would, so only the headings with a match show,
        /// open.
        /// </summary>
        public void SetReferenceFilter(string filter)
        {
            _window._referencePanel.SetFilter(filter);
        }
    }
}
