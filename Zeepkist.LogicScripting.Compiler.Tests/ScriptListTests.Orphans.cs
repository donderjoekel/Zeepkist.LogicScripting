using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class ScriptListTests
{
    [Fact]
    public void AGroupWithoutTextTheWindowDoesntHave_IsOrphaned()
    {
        var list = new ScriptList();

        list.Update([], new Dictionary<ScriptId, int> { [_orphanId] = 3 }, NoDraft);

        var orphan = Assert.Single(list.OrphanedGroups);
        Assert.Equal(_orphanId, orphan.Id);
        Assert.Equal(3, orphan.BlockCount);
        Assert.Null(orphan.Draft);
    }

    [Fact]
    public void AGroupWhoseScriptTheWindowHas_IsNotOrphaned()
    {
        var list = new ScriptList();
        list.Update([new StoredScript(_id, "doors", "x = 1", 1)], Groups(_id), NoDraft);

        list.Update([], Groups(_id), NoDraft);

        Assert.Empty(list.OrphanedGroups);
        Assert.Equal(ScriptMark.CarrierMissing, list.Entries[0].Mark);
    }

    [Fact]
    public void AnOrphan_ComesWithItsDraft()
    {
        var list = new ScriptList();
        var draft = new Draft(_orphanId, "doors", "x = 1", null);

        list.Update([], Groups(_orphanId), id => id == _orphanId ? draft : null);

        Assert.Same(draft, Assert.Single(list.OrphanedGroups).Draft);
    }

    [Fact]
    public void Restore_AddsTheDraftsScriptWithoutItsText()
    {
        var list = new ScriptList();
        list.Update([], Groups(_orphanId), _ => new Draft(_orphanId, "doors", "x = 1", Draft.HashOf("x = 0")));

        var entry = list.Restore(list.OrphanedGroups[0]);

        Assert.Equal(_orphanId, entry.Id);
        Assert.Equal("doors", entry.Name);
        Assert.Equal("x = 1", entry.Text);
        Assert.Equal(ScriptMark.CarrierMissing, entry.Mark);
        Assert.Empty(list.OrphanedGroups);
    }

    [Fact]
    public void Restore_TakesAFreeName()
    {
        var list = new ScriptList();
        list.Add("doors");
        list.Update([], Groups(_orphanId), _ => new Draft(_orphanId, "doors", "x = 1", null));

        var entry = list.Restore(list.OrphanedGroups[0]);

        Assert.Equal("doors 2", entry.Name);
    }

    [Fact]
    public void ARestoredScript_StaysOutOfTheOrphans()
    {
        var list = new ScriptList();
        list.Update([], Groups(_orphanId), _ => new Draft(_orphanId, "doors", "x = 1", null));
        list.Restore(list.OrphanedGroups[0]);

        list.Update([], Groups(_orphanId), NoDraft);

        Assert.Empty(list.OrphanedGroups);
        Assert.Single(list.Entries);
    }

    [Fact]
    public void AnImportedOrphan_KeepsItsIdAndIsNoOrphanAnyMore()
    {
        var list = new ScriptList();
        var stored = ImportOrphan(list);

        var entry = Assert.Single(list.Entries);
        Assert.Equal(_orphanId, entry.Id);
        Assert.Equal(ScriptMark.None, entry.Mark);
        Assert.Empty(list.OrphanedGroups);

        Assert.Empty(list.Update([stored], Groups(_orphanId), NoDraft));
        Assert.Same(entry, Assert.Single(list.Entries));
        Assert.Empty(list.OrphanedGroups);
    }

    [Fact]
    public void UndoingAnOrphansImport_ListsItAgain_AndRedoAndUndoDoToo()
    {
        var list = new ScriptList();
        var stored = ImportOrphan(list);
        var entry = list.Entries[0];

        Assert.Same(entry, Assert.Single(list.Update([], Groups(_orphanId), NoDraft)));
        Assert.Empty(list.Entries);
        Assert.Equal(_orphanId, Assert.Single(list.OrphanedGroups).Id);

        Assert.Empty(list.Update([stored], Groups(_orphanId), NoDraft));
        Assert.Equal("x = 1", Assert.Single(list.Entries).Text);
        Assert.Empty(list.OrphanedGroups);

        Assert.Single(list.Update([], Groups(_orphanId), NoDraft));
        Assert.Empty(list.Entries);
        Assert.Single(list.OrphanedGroups);
    }

    [Fact]
    public void RedoingAnOrphansImport_DoesntTakeItsDraft_SoUndoListsItAgain()
    {
        var list = new ScriptList();
        var stored = ImportOrphan(list);
        var draft = new Draft(_orphanId, "doors", "x = 2", Draft.HashOf("x = 1"));
        list.Update([], Groups(_orphanId), _ => draft);

        list.Update([stored], Groups(_orphanId), _ => draft);

        Assert.Equal("x = 1", Assert.Single(list.Entries).Text);
        list.Update([], Groups(_orphanId), _ => draft);
        Assert.Same(draft, Assert.Single(list.OrphanedGroups).Draft);
    }

    [Fact]
    public void RedoingAnOrphansImport_UnderAnotherName_StillUndoes()
    {
        var list = new ScriptList();
        var stored = ImportOrphan(list);
        list.Update([], Groups(_orphanId), NoDraft);
        list.Add("Imported");
        list.Update([stored], Groups(_orphanId), NoDraft);
        Assert.Equal("Imported 2", list.Entries[1].Name);

        list.Update([], Groups(_orphanId), NoDraft);

        Assert.Equal("Imported", Assert.Single(list.Entries).Name);
        Assert.Single(list.OrphanedGroups);
    }

    [Fact]
    public void RedoneImports_HoldsWhatTheLastUpdateBroughtBack()
    {
        var list = new ScriptList();
        var stored = ImportOrphan(list);
        Assert.Empty(list.RedoneImports);
        list.Update([], Groups(_orphanId), NoDraft);

        list.Update([stored], Groups(_orphanId), NoDraft);
        Assert.Same(Assert.Single(list.Entries), Assert.Single(list.RedoneImports));

        list.Update([stored], Groups(_orphanId), NoDraft);
        Assert.Empty(list.RedoneImports);

        list.Update([], Groups(_orphanId), NoDraft);
        list.Update([stored], Groups(_orphanId), NoDraft);
        list.Clear();
        Assert.Empty(list.RedoneImports);
    }

    [Theory]
    [InlineData("Imported", "x = 2")]
    [InlineData("doors", "x = 1")]
    public void UndoingAnEditedOrphansImport_KeepsTheScript(string name, string text)
    {
        var list = new ScriptList();
        ImportOrphan(list);
        list.Entries[0].Name = name;
        list.Entries[0].Text = text;

        Assert.Empty(list.Update([], Groups(_orphanId), NoDraft));

        Assert.Equal(ScriptMark.CarrierMissing, Assert.Single(list.Entries).Mark);
        Assert.Empty(list.OrphanedGroups);
    }

    [Fact]
    public void DeletingAnImportedOrphansBlocks_KeepsTheScript()
    {
        var list = new ScriptList();
        ImportOrphan(list);

        Assert.Empty(list.Update([], Groups(), NoDraft));

        Assert.Equal(ScriptMark.NeverCompiled, Assert.Single(list.Entries).Mark);
    }

    [Fact]
    public void RestoringAnOrphanWhoseImportWasUndone_KeepsIt_EvenWithTheImportsText()
    {
        var list = new ScriptList();
        ImportOrphan(list);
        var draft = new Draft(_orphanId, "Imported", "x = 1", null);
        list.Update([], Groups(_orphanId), _ => draft);
        list.Restore(list.OrphanedGroups[0]);

        Assert.Empty(list.Update([], Groups(_orphanId), _ => draft));

        Assert.Single(list.Entries);
        Assert.Empty(list.OrphanedGroups);
    }

    [Fact]
    public void Clear_ForgetsTheOrphansImports()
    {
        var list = new ScriptList();
        var stored = ImportOrphan(list);
        list.Clear();
        list.Update([stored], Groups(_orphanId), NoDraft);

        Assert.Empty(list.Update([], Groups(_orphanId), NoDraft));

        Assert.Equal(ScriptMark.CarrierMissing, Assert.Single(list.Entries).Mark);
    }

    private static StoredScript ImportOrphan(ScriptList list)
    {
        list.Update([], Groups(_orphanId), NoDraft);
        var stored = new StoredScript(_orphanId, list.GetFreeName("Imported"), "x = 1", 1);
        list.AddImportedOrphan(stored);
        return stored;
    }
}
