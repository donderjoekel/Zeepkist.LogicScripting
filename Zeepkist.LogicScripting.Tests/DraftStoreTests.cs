using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using TNRD.Zeepkist.LogicScripting.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class DraftStoreTests
{
    private static readonly ScriptId _id = new("id");

    private readonly MemoryStorage _storage = new();

    [Fact]
    public void ASavedDraft_LoadsAgain()
    {
        CreateStore().Save(new Draft(_id, "Lights", "a = 1", "hash"));

        var draft = CreateStore().Find(_id);

        Assert.Equal("Lights", draft.Name);
        Assert.Equal("a = 1", draft.Text);
        Assert.Equal("hash", draft.BaseHash);
    }

    [Fact]
    public void ADeletedDraft_IsGoneFromTheStoreAndItsFile()
    {
        var store = CreateStore();
        store.Save(new Draft(_id, "Lights", "a = 1", null));

        store.Delete(_id);

        Assert.Null(store.Find(_id));
        Assert.Null(CreateStore().Find(_id));
    }

    [Fact]
    public void DeletingADraftThatIsntThere_DoesNothing()
    {
        var store = CreateStore();

        store.Delete(_id);

        Assert.Null(store.Find(_id));
    }

    private DraftStore CreateStore()
    {
        return new DraftStore(_storage, new ManualLogSource("Tests"));
    }
}
