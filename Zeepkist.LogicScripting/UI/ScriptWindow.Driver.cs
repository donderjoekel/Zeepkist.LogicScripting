using System.Collections.Generic;
using System.Linq;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptWindow
{
    /// <summary>
    /// Does what the window's buttons and typing do, through the same code paths, for the in-game tests. Nothing in the
    /// mod itself uses it.
    /// </summary>
    internal sealed partial class Driver
    {
        private readonly ScriptWindow _window;

        public Driver(ScriptWindow window)
        {
            _window = window;
        }

        public bool IsOpen => _window.IsOpen;
        public bool HasScript => _window.CurrentScript != null;

        /// <summary>
        /// Whether a Script or a Library is selected, so there's text in the editor.
        /// </summary>
        public bool HasText => _window._edited != null;

        public IReadOnlyList<string> ScriptNames => [.. _window._scripts.Entries.Select(e => e.Name)];
        public IReadOnlyList<string> LibraryNames => [.. _window.Libraries.Entries.Select(l => l.Name)];

        /// <summary>
        /// Empty until a Compile, and again after an edit or selecting another Script.
        /// </summary>
        public string Result => _window._result;

        /// <summary>
        /// The selected Script's or Library's text in the editor, or null with neither selected.
        /// </summary>
        public string Text => _window._edited?.Editor.Text;

        /// <summary>
        /// Whether the window has checked the current text. It only checks while it's open, a moment after the last edit.
        /// </summary>
        public bool IsChecked => _window._edited?.IsChecked ?? false;

        /// <summary>
        /// The errors the last check found, or null when it couldn't run.
        /// </summary>
        public IReadOnlyList<Diagnostic> Errors => _window._edited?.Errors;

        /// <inheritdoc cref="ScriptEditor.CompletionItems"/>
        public IReadOnlyList<CompletionItem> CompletionItems => _window._edited?.Editor.CompletionItems ?? [];

        /// <inheritdoc cref="ScriptEditor.CallHelp"/>
        public CallHelp CallHelp => _window._edited?.Editor.CallHelp;

        /// <inheritdoc cref="ScriptEditor.HoverRestingFrame"/>
        public int HoverRestingFrame => _window._edited?.Editor.HoverRestingFrame ?? -1;

        /// <inheritdoc cref="ScriptEditor.HoverLines"/>
        public IReadOnlyList<string> HoverLines => _window._edited?.Editor.HoverLines;

        /// <inheritdoc cref="ScriptEditor.DrawnUsages"/>
        public IReadOnlyList<TextSpan> DrawnUsages => _window._edited?.Editor.DrawnUsages ?? [];

        /// <inheritdoc cref="ScriptEditor.FindDrawnCharacter"/>
        public (ImRect Character, ImRect TextBounds, int Frame)? FindDrawnCharacter(int index)
        {
            return _window._edited?.Editor.FindDrawnCharacter(index);
        }

        public bool IsFieldFocused => _window.AnyFieldFocused;

        /// <summary>
        /// The shown tab's list and footer.
        /// </summary>
        public IListPanel ShownList => _window._state.ShowLibraries ? _window._libraryListPanel : _window._scriptListPanel;

        /// <summary>
        /// The window's rect as Imui last drew it.
        /// </summary>
        public ImRect WindowRect => new(_window._state.X, _window._state.Y, _window._state.Width, _window._state.Height);

        /// <inheritdoc cref="UI.EditorLayout"/>
        public EditorLayout EditorLayout => _window._editorLayout;

        /// <inheritdoc cref="UI.WindowLayout"/>
        public WindowLayout WindowLayout => _window._windowLayout;

        /// <summary>
        /// Whether the center drew the startup check's warnings.
        /// </summary>
        public WarningLayout StartupWarningLayout => _window._startupWarningLayout;

        /// <summary>
        /// Whether the Libraries tab drew its libraries.json warning, the last frame it was shown.
        /// </summary>
        public WarningLayout LibraryReadFailedLayout => _window._libraryReadFailedLayout;

        /// <summary>
        /// Kept while the Scripts side is collapsed, for when it's shown again.
        /// </summary>
        public bool ShowsLibraries => _window._state.ShowLibraries;

        /// <summary>
        /// Whether the Blocks side shows its Reference tab rather than its Placed blocks tab. Kept while the Blocks side
        /// is collapsed, for when it's shown again.
        /// </summary>
        public bool ShowsReference => _window._showReference;

        /// <summary>
        /// The Unity frame the Reference tab was last drawn in, which it isn't while the Placed blocks tab shows or the
        /// Blocks side is collapsed.
        /// </summary>
        public int ReferenceDrawnFrame => _window._referenceDrawnFrame;

        /// <inheritdoc cref="ReferencePanel.DrawnHeadings"/>
        public IReadOnlyList<ReferenceHeading> ReferenceHeadings => _window._referencePanel.DrawnHeadings;

        /// <summary>
        /// Whether a size or position asked for waits for the window to be drawn.
        /// </summary>
        public bool IsRectPending => _window._requestedRect.IsPending;

        public void SetOpen(bool open)
        {
            _window.SetOpen(open);
        }
    }
}
