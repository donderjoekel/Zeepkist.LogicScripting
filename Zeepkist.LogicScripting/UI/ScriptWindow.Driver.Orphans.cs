using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptWindow
{
    internal sealed partial class Driver
    {
        /// <summary>
        /// The Orphaned Groups the Scripts tab lists.
        /// </summary>
        public IReadOnlyList<OrphanedGroup> Orphans => _window._scripts.OrphanedGroups;

        /// <inheritdoc cref="OrphanedGroupsPanel.DrawnMessage"/>
        public (int Frame, string Text) OrphanResult => _window._orphanedGroupsPanel.DrawnMessage;

        /// <summary>
        /// The Orphaned Group whose row has the name, the first of several, or null when there's none.
        /// </summary>
        public OrphanedGroup FindOrphan(string name)
        {
            return Orphans.FirstOrDefault(o => OrphanedGroupsPanel.GetName(o) == name);
        }

        /// <summary>
        /// Like Restore from Draft on the group's row on the Scripts tab, which selects the Script. The group needs a
        /// Draft, or the button is grayed out.
        /// </summary>
        public void RestoreOrphan(OrphanedGroup orphan)
        {
            ShowTab(libraries: false);
            _window.RestoreOrphan(orphan);
        }

        /// <summary>
        /// Presses Import on the group's row on the Scripts tab, the next time the row is drawn.
        /// </summary>
        public void PressImportOrphan(OrphanedGroup orphan)
        {
            ShowTab(libraries: false);
            _window._orphanedGroupsPanel.PressImport(orphan);
        }

        /// <summary>
        /// Whether a press handed to the Orphaned Groups' list waits for it to be drawn.
        /// </summary>
        public bool IsOrphanInputPending => _window._orphanedGroupsPanel.IsInputPending;

        /// <inheritdoc cref="OrphanedGroupsPanel.CancelInput"/>
        public void CancelOrphanInput()
        {
            _window._orphanedGroupsPanel.CancelInput();
        }

        /// <summary>
        /// The Orphaned Groups' result line, drawn yet or not.
        /// </summary>
        public string OrphanMessage => _window._orphanedGroupsPanel.Message;

        /// <summary>
        /// Leaves the Orphaned Groups as on a machine that never had their Scripts, from when the window is next drawn.
        /// </summary>
        public void DeleteOrphanDrafts()
        {
            foreach (var orphan in Orphans)
            {
                _window._drafts.Delete(orphan.Id);
            }

            _window._readLevelChanges = null;
        }
    }
}
