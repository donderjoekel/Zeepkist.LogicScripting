using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting;

internal abstract class CompilePlacement
{
    public abstract IReadOnlyList<BlockProperties> Group { get; }

    public abstract int QuarterTurns { get; }

    /// <summary>
    /// Goes through all the level's blocks, so call it on a Compile, not per frame.
    /// </summary>
    public static CompilePlacement For(LEV_LevelEditorCentral central, ScriptId scriptId)
    {
        var group = BlockGroups.Find(central, scriptId);
        return group.Count > 0 ? new RecompilePlacement(group) : new FirstCompilePlacement(central);
    }

    public abstract bool ChangesNothing(CompileResult result);

    public string GetNothingToPlaceLine()
    {
        return $"Nothing to place. {NotStoredNote}";
    }

    /// <summary>
    /// World position of the new group's center cell.
    /// </summary>
    public abstract Vector3 GetOrigin(CompileResult result);

    public string GetResultLine(CompileResult result)
    {
        var line = GetPlacedLine(result.Blocks.Count);
        var wiredBlocks = result.WiredPlacedBlockCount;
        if (wiredBlocks > 0)
            line += $", wired to {wiredBlocks} placed blocks";

        return result.Blocks.Count == 0 ? $"{line}. {NotStoredNote}" : line;
    }

    /// <summary>
    /// Why the level holds no text for the Script after placing no blocks: the text is stored on one of them. It
    /// starts a sentence, since it always follows <c>". "</c>.
    /// </summary>
    protected abstract string NotStoredNote { get; }

    protected abstract string GetPlacedLine(int blockCount);
}
