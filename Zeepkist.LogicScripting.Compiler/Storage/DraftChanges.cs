using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// Remembers each Script's last saved Draft, so only the ones that changed are written again.
/// </summary>
public sealed class DraftChanges
{
    private readonly Dictionary<ScriptId, Draft> _saved = [];

    /// <summary>
    /// Returns the Drafts of <paramref name="entries"/> that differ from the last ones returned, and takes them as saved.
    /// A Script seen for the first time since the last <see cref="Clear"/> keeps a Draft typed over other text than the level's until it's edited: the
    /// level may be an older copy, and the Draft the only place that text is left. <paramref name="findSaved"/> returns
    /// a Script's saved Draft, or null.
    /// </summary>
    public List<Draft> TakeChanged(IEnumerable<ScriptEntry> entries, Func<ScriptId, Draft> findSaved)
    {
        List<Draft> changed = [];
        foreach (var entry in entries)
        {
            var draft = Draft.For(entry);
            if (!_saved.TryGetValue(entry.Id, out var saved))
                saved = KeepsSavedDraft(entry, draft, findSaved(entry.Id)) ? draft : null;

            _saved[entry.Id] = draft;
            if (!draft.HasSameContent(saved))
                changed.Add(draft);
        }

        return changed;
    }

    /// <summary>
    /// Takes the Script as saved though its Draft wasn't: the Draft there is, if any, stays until the Script is edited.
    /// For a Script imported from an Orphaned Group, whose Draft may hold text newer than the blocks.
    /// </summary>
    public void TakeAsSaved(ScriptEntry entry)
    {
        _saved[entry.Id] = Draft.For(entry);
    }

    /// <summary>
    /// Forgets every Script seen, so the next level's are taken as seen for the first time. Call after saving.
    /// </summary>
    public void Clear()
    {
        _saved.Clear();
    }

    private static bool KeepsSavedDraft(ScriptEntry entry, Draft draft, Draft saved)
    {
        if (saved == null)
            return false;

        return draft.HasSameContent(saved) || (entry.Mark == ScriptMark.None && saved.BaseHash != draft.BaseHash);
    }
}
