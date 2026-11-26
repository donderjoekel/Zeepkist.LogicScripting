using TNRD.Zeepkist.LogicScripting.Compiler;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

internal static class BlockGroupEdits
{
    private const string DeleteLabel = "Logic Scripting Delete group";
    private const string UntagLabel = "Logic Scripting Untag group";

    public static int Delete(LEV_LevelEditorCentral central, ScriptId scriptId)
    {
        var group = BlockGroups.Find(central, scriptId);
        if (group.Count > 0)
            BlockBatch.Apply(central, [], group, [], DeleteLabel);

        return group.Count;
    }

    /// <summary>
    /// Leaves the group's blocks in the level as plain Logic Blocks, without their tag or the Script's text.
    /// </summary>
    public static int Untag(LEV_LevelEditorCentral central, ScriptId scriptId)
    {
        var group = BlockGroups.Find(central, scriptId);
        if (group.Count > 0)
            BlockBatch.Change(central, group, block => LevelScripts.Untag(block.customBlockEditProperties), UntagLabel);

        return group.Count;
    }
}
