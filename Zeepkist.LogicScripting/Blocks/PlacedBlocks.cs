using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

internal static class PlacedBlocks
{
    /// <summary>
    /// The Block Group of the Script being compiled is left out of the block's Wires: a recompile replaces it, Wires
    /// and all.
    /// </summary>
    public static PlacedBlock Find(LEV_LevelEditorCentral central, string uid, ScriptId scriptId)
    {
        return Find(central, uid, other => BlockGroups.IsInGroup(central, other, scriptId));
    }

    /// <summary>
    /// Leaves the blocks the next Compile replaces out of the block's Wires. A block in the Block Group of
    /// <paramref name="leftId"/>, when given, counts as in none, as it's about to leave it.
    /// </summary>
    public static PlacedBlock Find(LEV_LevelEditorCentral central, string uid, Func<string, bool> isReplaced, ScriptId leftId = null)
    {
        var block = LevelBlocks.Find(central, uid);
        if (block == null)
            return null;

        var isInBlockGroup = BlockGroups.IsInOtherGroup(block, leftId);
        if (!BlockManifest.TryGetBlock(block.blockID, out var type, out _))
            return new PlacedBlock(block.blockID, isInBlockGroup: isInBlockGroup);

        List<int> wiredInputs = [];
        foreach (var input in type.Inputs)
        {
            if (GetWires(block, input.Index).Any(w => !isReplaced(w.targetUID)))
                wiredInputs.Add(input.Index);
        }

        return new PlacedBlock(block.blockID, GetSettings(block, type), wiredInputs, GetDownstream(central, block, type, isReplaced), isInBlockGroup);
    }

    /// <summary>
    /// The block's ID, Settings and Block Group only, for the editor's help, which looks up every reference on each
    /// edit and has no use for the Wires <see cref="Find(LEV_LevelEditorCentral, string, ScriptId)"/> follows.
    /// </summary>
    public static PlacedBlock FindWithoutWires(LEV_LevelEditorCentral central, string uid)
    {
        var block = LevelBlocks.Find(central, uid);
        if (block == null)
            return null;

        var settings = BlockManifest.TryGetBlock(block.blockID, out var type, out _) ? GetSettings(block, type) : null;
        return new PlacedBlock(block.blockID, settings, isInBlockGroup: BlockGroups.IsInAnyGroup(block));
    }

    private static List<Setting> GetSettings(BlockProperties block, BlockType type)
    {
        List<Setting> settings = [];
        if (type.Modes != null)
        {
            var key = type.Modes.Setting.Key;
            settings.Add(Setting.Int(key, block.customBlockEditProperties.GetIntDictionaryValue(key)));
        }

        return settings;
    }

    /// <summary>
    /// Stops at Delays, which pass a Signal on only in the next Tick, and at the blocks the Compile replaces.
    /// </summary>
    private static HashSet<string> GetDownstream(
        LEV_LevelEditorCentral central,
        BlockProperties start,
        BlockType startType,
        Func<string, bool> isReplaced)
    {
        HashSet<string> reached = [];
        Stack<(BlockProperties Block, BlockType Type)> open = new([(start, startType)]);
        while (open.Count > 0)
        {
            var (block, type) = open.Pop();
            foreach (var output in type.Outputs)
            {
                foreach (var wire in GetWires(block, output.Index))
                {
                    if (isReplaced(wire.targetUID) || !reached.Add(wire.targetUID))
                        continue;

                    var next = LevelBlocks.Find(central, wire.targetUID);
                    if (next != null
                        && BlockManifest.TryGetBlock(next.blockID, out var nextType, out _)
                        && nextType != BlockManifest.Delay)
                        open.Push((next, nextType));
                }
            }
        }

        return reached;
    }

    /// <summary>
    /// Each holds the block and Connector at the Wire's other end.
    /// </summary>
    public static List<ConnectionStruct> GetWires(BlockProperties block, int connector)
    {
        return block.allConnectorHubsOnThisBlock.TryGetValue(connector, out var hub) && hub != null
            ? hub.GetThisHubsConnectionStructs() ?? []
            : [];
    }

    /// <summary>
    /// The Wires of all the block's Connectors.
    /// </summary>
    public static IEnumerable<ConnectionStruct> GetAllWires(BlockProperties block)
    {
        return block.allConnectorHubsOnThisBlock.Values.SelectMany(hub => hub?.GetThisHubsConnectionStructs() ?? []);
    }
}
