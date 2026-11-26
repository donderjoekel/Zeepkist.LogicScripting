using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptWindow
{
    internal sealed partial class Driver
    {
        /// <summary>
        /// Like New Script on the Scripts tab and naming it in the prompt, which selects it. Returns why the name isn't
        /// taken, or null.
        /// </summary>
        public string NewScript(string name)
        {
            ShowTab(libraries: false);
            var entry = _window._scriptListPanel.New(name, out var error);
            if (entry != null)
                _window.Select(entry);

            return error;
        }

        /// <summary>
        /// Like clicking the Script in the list on the Scripts tab, which selects it the next time the list is drawn:
        /// see <see cref="IListPanel.IsInputPending"/>. Returns whether there's one by that name.
        /// </summary>
        public bool SelectScript(string name)
        {
            if (!_window._scriptListPanel.Click(name))
                return false;

            ShowTab(libraries: false);
            return true;
        }

        /// <summary>
        /// Like Select group in the Script's context menu on the Scripts tab. Returns whether there's one by that name.
        /// </summary>
        public bool SelectGroup(string name)
        {
            var entry = FindScript(name);
            if (entry == null)
                return false;

            ShowTab(libraries: false);
            _window._scriptListPanel.SelectGroup(entry);
            return true;
        }

        /// <summary>
        /// Turns a style off as the startup check does when it doesn't match this game version, so Compile and the checks
        /// report a Script that places one or refers to one. Returns false when it's already off.
        /// </summary>
        public bool TurnOffStyle(BlockType type, BlockStyle style)
        {
            return _window._startupCheck.TurnOff(type, style);
        }

        /// <summary>
        /// The Script's ID, or null when there's none by that name.
        /// </summary>
        public ScriptId FindScriptId(string name)
        {
            return FindScript(name)?.Id;
        }

        /// <summary>
        /// Like New Library on the Libraries tab and naming it in the prompt, which selects it. Returns why the name
        /// isn't taken, or null.
        /// </summary>
        public string NewLibrary(string name)
        {
            ShowTab(libraries: true);
            var library = _window._libraryListPanel.New(name, out var error);
            if (library != null)
                _window.SelectLibrary(library);

            return error;
        }

        /// <summary>
        /// Like clicking the Library in the list on the Libraries tab, which selects it the next time the list is drawn:
        /// see <see cref="IListPanel.IsInputPending"/>. Returns whether there's one by that name.
        /// </summary>
        public bool SelectLibrary(string name)
        {
            if (!_window._libraryListPanel.Click(name))
                return false;

            ShowTab(libraries: true);
            return true;
        }

        /// <summary>
        /// Like Delete on every Library on the Libraries tab, without the prompts. A prompt left open is closed too, as
        /// it could be for a Library that's gone.
        /// </summary>
        public void DeleteLibraries()
        {
            _window._libraryListPanel.Clear();
            foreach (var library in _window.Libraries.Entries.ToList())
            {
                _window.Libraries.Remove(library.Id);
                _window.OnLibraryDeleted(library);
            }
        }

        /// <summary>
        /// Null when there's none by that name.
        /// </summary>
        private ScriptEntry FindScript(string name)
        {
            return _window._scripts.Entries.FirstOrDefault(e => e.Name == name);
        }

        /// <summary>
        /// Replaces the selected Script's or Library's text as one edit, as typing it would leave it.
        /// </summary>
        public void SetText(string text)
        {
            _window._edited.Editor.Replace(text, text.Length, Time.unscaledTimeAsDouble);
            _window.OnTyped();
        }

        /// <summary>
        /// Like clicking at a character index of the selected Script's or Library's text, which focuses the field.
        /// </summary>
        public void PlaceCaret(int index)
        {
            _window._edited.Editor.JumpTo(index);
        }

        /// <inheritdoc cref="ScriptEditor.RequestCompletion"/>
        public void RequestCompletion()
        {
            _window._edited.Editor.RequestCompletion();
        }

        /// <inheritdoc cref="ScriptEditor.IsCompletionRequestPending"/>
        public bool IsCompletionRequestPending => _window._edited?.Editor.IsCompletionRequestPending ?? false;

        /// <inheritdoc cref="ScriptEditor.HoldSentEscape"/>
        public void HoldSentEscape()
        {
            _window._edited?.Editor.HoldSentEscape();
        }

        /// <summary>
        /// Holds the jump modifier for the selected Script's or Library's editor, or lets go of it, as
        /// <see cref="ScriptEditor.HoldsSentJumpModifier"/> says.
        /// </summary>
        public void HoldSentJumpModifier(bool held)
        {
            if (_window._edited != null)
                _window._edited.Editor.HoldsSentJumpModifier = held;
        }

        /// <inheritdoc cref="ScriptEditor.JumpedFrame"/>
        public int JumpedFrame => _window._edited?.Editor.JumpedFrame ?? -1;

        /// <inheritdoc cref="ScriptEditor.Caret"/>
        public int Caret => _window._edited?.Editor.Caret ?? -1;

        /// <summary>
        /// Like Insert selected. Returns whether it changed the text.
        /// </summary>
        public bool InsertSelected(LEV_LevelEditorCentral central)
        {
            var edited = _window._placedBlocksPanel.InsertSelected(central, _window._edited.Editor, Time.unscaledTimeAsDouble);
            if (edited)
                _window.OnEdited();

            return edited;
        }

        /// <summary>
        /// Like Import selected on the Scripts tab, which selects the Script it makes. Returns why it made none, or null.
        /// </summary>
        public string ImportSelected()
        {
            ShowTab(libraries: false);
            var entry = _window._scriptListPanel.ImportSelected();
            if (entry == null)
                return _window._scriptListPanel.Message;

            _window.Select(entry);
            return null;
        }

        /// <summary>
        /// Like Ctrl+Enter in the editor. Returns the result line, which says why when Compile can't run, or null when
        /// the check it runs first on unchecked text found errors, which the error list shows.
        /// </summary>
        public string Compile()
        {
            var result = _window._edited.CompileFromKeyboard();
            _window.ShowResult(result);
            return result;
        }
    }
}
