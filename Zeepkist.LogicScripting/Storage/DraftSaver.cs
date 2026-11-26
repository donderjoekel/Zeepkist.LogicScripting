using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// Saves the Drafts of the window's Scripts shortly after the last change, so they aren't written on every key while
/// typing.
/// </summary>
internal sealed class DraftSaver
{
    private const float Delay = 1;

    private readonly DraftStore _store;
    private readonly ScriptList _scripts;
    private readonly DraftChanges _changes = new();
    private float? _saveTime;

    public DraftSaver(DraftStore store, ScriptList scripts)
    {
        _store = store;
        _scripts = scripts;
    }

    public void Request()
    {
        _saveTime = Time.unscaledTime + Delay;
    }

    /// <inheritdoc cref="DraftChanges.TakeAsSaved"/>
    public void TakeAsSaved(ScriptEntry entry)
    {
        _changes.TakeAsSaved(entry);
    }

    /// <summary>
    /// Call every frame.
    /// </summary>
    public void Update()
    {
        if (_saveTime != null && Time.unscaledTime >= _saveTime)
            Save();
    }

    /// <summary>
    /// Saves a requested change without waiting for the delay.
    /// </summary>
    public void SavePending()
    {
        if (_saveTime != null)
            Save();
    }

    /// <summary>
    /// Writes the Draft of every Script that changed since its last one.
    /// </summary>
    public void Save()
    {
        _saveTime = null;
        foreach (var draft in _changes.TakeChanged(_scripts.Entries, _store.Find))
        {
            _store.Save(draft);
        }
    }

    /// <summary>
    /// Saves the Drafts, then forgets every Script seen, so a level loaded next treats its Scripts as new: an older
    /// copy of a level doesn't write over a newer Draft.
    /// </summary>
    public void SaveAndForget()
    {
        Save();
        _changes.Clear();
    }
}
