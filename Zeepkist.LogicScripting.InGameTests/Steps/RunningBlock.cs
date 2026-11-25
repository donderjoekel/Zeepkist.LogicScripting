using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// A named block as it runs in test mode, where the level editor and its block dictionary aren't there to look it up in.
/// </summary>
internal sealed class RunningBlock
{
    private RunningBlock(string name, BlockType type, LogicScript_Base script)
    {
        Name = name;
        Type = type;
        Script = script;
    }

    public string Name { get; }
    public BlockType Type { get; }
    public LogicScript_Base Script { get; }

    /// <summary>
    /// Null, with why in <paramref name="error"/>, when the level that's running has no Logic Block with the UID.
    /// </summary>
    public static RunningBlock Find(string name, string uid, out string error)
    {
        if (!StaticConnectorTracker.allBlocksByUID.TryGetValue(uid, out var block) || block == null)
        {
            error = $"the running level has no block with \"{name}\"'s UID \"{uid}\"";
            return null;
        }

        if (!BlockManifest.TryGetBlock(block.blockID, out var type, out _))
        {
            error = $"\"{name}\", block {block.blockID}, isn't a Logic Block type the mod knows";
            return null;
        }

        var script = block.GetAllBlockEditV18sFromThisBlock()
            .OfType<BlockEdit_v18_C_Logic_Base>()
            .Select(e => e.logicScript)
            .FirstOrDefault(s => s != null);
        error = script == null ? $"\"{name}\" has no logic running" : null;
        return script == null ? null : new RunningBlock(name, type, script);
    }
}
