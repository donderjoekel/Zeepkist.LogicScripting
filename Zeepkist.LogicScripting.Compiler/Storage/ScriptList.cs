using System;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// The Scripts the window shows: those the level holds, plus those made in the window and not stored in the level.
/// </summary>
public sealed class ScriptList
{
    private readonly List<ScriptEntry> _entries = [];
    private readonly List<OrphanedGroup> _orphanedGroups = [];

    // The name and text each Script imported from an Orphaned Group came in with
    private readonly Dictionary<ScriptId, (string Name, string Text)> _orphanImports = [];

    // Those that are Orphaned Groups again
    private readonly HashSet<ScriptId> _undoneImports = [];

    private readonly List<ScriptEntry> _redoneImports = [];

    public IReadOnlyList<ScriptEntry> Entries => _entries;

    /// <summary>
    /// The level's Block Groups whose Script isn't in <see cref="Entries"/>, as of the last <see cref="Update"/>.
    /// </summary>
    public IReadOnlyList<OrphanedGroup> OrphanedGroups => _orphanedGroups;

    /// <summary>
    /// The Scripts imported from an Orphaned Group that the last <see cref="Update"/> brought back, such as by redo.
    /// Take each as saved: their Draft, newer than the blocks' text, stays until the Script is edited.
    /// </summary>
    public IReadOnlyList<ScriptEntry> RedoneImports => _redoneImports;

    /// <summary>
    /// Returns why <paramref name="name"/> can't name a new Script, or the Script with <paramref name="renamedId"/>,
    /// or null when it can. Trim it first. The level's names count too: a Script renamed in the window keeps its old
    /// name in the level until it's compiled.
    /// </summary>
    public string GetNameError(string name, ScriptId renamedId = null)
    {
        var others = _entries.Where(e => e.Id != renamedId).ToList();
        var names = others.Select(e => e.Name).Concat(others.Where(e => e.Stored != null).Select(e => e.Stored.Name));
        return ScriptName.GetError(name, names);
    }

    /// <summary>
    /// Adds an empty Script with a new id. Check the name with <see cref="GetNameError"/> first.
    /// </summary>
    public ScriptEntry Add(string name)
    {
        var entry = new ScriptEntry(ScriptId.New(), name, string.Empty);
        _entries.Add(entry);
        return entry;
    }

    /// <summary>
    /// Adds a Script the level already holds, with its name and text. Take the name from <see cref="GetFreeName"/>.
    /// </summary>
    public ScriptEntry Add(StoredScript stored)
    {
        var entry = new ScriptEntry(stored.Id, stored.Name, stored.Text) { Stored = stored, HasGroup = true };
        _entries.Add(entry);
        return entry;
    }

    /// <summary>
    /// Adds the Script an Orphaned Group was imported as, like <see cref="Add(StoredScript)"/>.
    /// </summary>
    public ScriptEntry AddImportedOrphan(StoredScript stored)
    {
        _orphanedGroups.RemoveAll(o => o.Id == stored.Id);
        _undoneImports.Remove(stored.Id);
        _orphanImports[stored.Id] = (stored.Name, stored.Text);
        return Add(stored);
    }

    public ScriptEntry Find(ScriptId id)
    {
        return _entries.FirstOrDefault(e => e.Id == id);
    }

    /// <summary>
    /// Takes in what the level holds now. A stored Script the window doesn't have yet is added with the level's name
    /// and text, or its Draft's when the Draft was typed over that text; a Script the window has keeps its own name and
    /// text, whatever the level holds. The exception is a Script imported from an Orphaned Group and not edited since,
    /// which is dropped when the level loses its text, such as by undo: its group is an Orphaned Group again.
    /// </summary>
    /// <param name="stored">Every Script whose text the level holds.</param>
    /// <param name="blockCounts">The number of blocks of every Script that has blocks in the level.</param>
    /// <param name="findDraft">The Script's Draft, or null when there is none.</param>
    /// <returns>The dropped Scripts.</returns>
    public IReadOnlyList<ScriptEntry> Update(
        IReadOnlyList<StoredScript> stored,
        IReadOnlyDictionary<ScriptId, int> blockCounts,
        Func<ScriptId, Draft> findDraft)
    {
        _redoneImports.Clear();
        foreach (var script in stored)
        {
            if (Find(script.Id) != null)
                continue;

            // Redo brings back the import, whose text no Draft was typed over yet
            if (_undoneImports.Remove(script.Id))
            {
                var name = GetFreeName(script.Name);
                _orphanImports[script.Id] = (name, script.Text);
                var redone = new ScriptEntry(script.Id, name, script.Text);
                _entries.Add(redone);
                _redoneImports.Add(redone);
                continue;
            }

            var draft = findDraft(script.Id);
            _entries.Add(draft != null && draft.IsBasedOn(script)
                ? new ScriptEntry(script.Id, GetFreeName(draft.Name), draft.Text)
                : new ScriptEntry(script.Id, GetFreeName(script.Name), script.Text));
        }

        foreach (var entry in _entries)
        {
            entry.Stored = stored.FirstOrDefault(s => s.Id == entry.Id);
            entry.HasGroup = blockCounts.ContainsKey(entry.Id);
        }

        var dropped = _entries.Where(IsUndoneImport).ToList();
        foreach (var entry in dropped)
        {
            _entries.Remove(entry);
            _undoneImports.Add(entry.Id);
        }

        _orphanedGroups.Clear();
        foreach (var group in blockCounts)
        {
            if (Find(group.Key) == null)
                _orphanedGroups.Add(new OrphanedGroup(group.Key, group.Value, findDraft(group.Key)));
        }

        return dropped;
    }

    private bool IsUndoneImport(ScriptEntry entry)
    {
        return entry.Mark == ScriptMark.CarrierMissing
            && _orphanImports.TryGetValue(entry.Id, out var imported)
            && imported == (entry.Name, entry.Text);
    }

    /// <summary>
    /// Brings an Orphaned Group's Script back from its Draft, under a free name. Only the window has it until it's
    /// compiled.
    /// </summary>
    public ScriptEntry Restore(OrphanedGroup orphan)
    {
        if (orphan.Draft == null)
            throw new ArgumentException("An Orphaned Group without a Draft can't be restored", nameof(orphan));

        var entry = new ScriptEntry(orphan.Id, GetFreeName(orphan.Draft.Name), orphan.Draft.Text)
        {
            HasGroup = true
        };

        // Restored, it stays in the window, even when its Draft is what an import undone before wrote
        _orphanImports.Remove(orphan.Id);
        _undoneImports.Remove(orphan.Id);
        _entries.Add(entry);
        _orphanedGroups.Remove(orphan);
        return entry;
    }

    /// <summary>
    /// Drops the Script from the window. The level's copy and blocks are the caller's to remove.
    /// </summary>
    public void Remove(ScriptId id)
    {
        _entries.RemoveAll(e => e.Id == id);
    }

    /// <summary>
    /// <paramref name="name"/> made into one a new Script can have, with a number after it when it's taken: for a Script
    /// the player hasn't named, or a level from another mod version or edited by hand.
    /// </summary>
    public string GetFreeName(string name)
    {
        var baseName = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (baseName.Length == 0)
            baseName = "Script";

        var candidate = baseName;
        for (var number = 2; GetNameError(candidate) != null && number < 1000; number++)
        {
            var suffix = $" {number}";
            candidate = baseName.Substring(0, Math.Min(baseName.Length, ScriptName.MaxLength - suffix.Length)) + suffix;
        }

        return candidate;
    }

    public void Clear()
    {
        _entries.Clear();
        _orphanedGroups.Clear();
        _orphanImports.Clear();
        _undoneImports.Clear();
        _redoneImports.Clear();
    }
}
