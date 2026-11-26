using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// A Wire is stored on both its blocks.
/// </summary>
internal static class LevelWires
{
    public static bool IsStoredAtOutput(LEV_LevelEditorCentral central, BlockWire wire)
    {
        return Stores(central, wire.FromUid, wire.FromConnector, wire.ToUid, wire.ToConnector);
    }

    public static bool IsStoredAtInput(LEV_LevelEditorCentral central, BlockWire wire)
    {
        return Stores(central, wire.ToUid, wire.ToConnector, wire.FromUid, wire.FromConnector);
    }

    /// <summary>
    /// Each as <c>expect-wire</c> takes it, such as <c>g out to l a</c>, with the UID of a block that has no name.
    /// </summary>
    public static string Describe(LEV_LevelEditorCentral central, PlacedBlockNames names, IEnumerable<BlockWire> wires)
    {
        var described = wires.Select(w => Describe(central, names, w)).Distinct().ToList();
        return described.Count == 0 ? "(no Wires)" : string.Join(", ", described);
    }

    /// <summary>
    /// Null when the level has no loop. A block that has no name is written by its UID.
    /// </summary>
    public static string DescribeDependencyLoops(LEV_LevelEditorCentral central, PlacedBlockNames names)
    {
        // No Script has this id, so no Block Group is left out of the Wires it follows
        var scriptId = ScriptId.New();
        List<string> looped = [];
        foreach (var block in central.undoRedo.allBlocksDictionary.Values)
        {
            // Downstream stops at a Delay, so a block finds itself in it only through a loop without one
            if (block != null
                && BlockManifest.TryGetBlock(block.blockID, out var type, out _)
                && type != BlockManifest.Delay
                && PlacedBlocks.Find(central, block.UID, scriptId).Downstream.Contains(block.UID))
                looped.Add($"{type.Function} \"{names.FindName(block.UID) ?? block.UID}\"");
        }

        return looped.Count == 0 ? null : string.Join(", ", looped);
    }

    private static string Describe(LEV_LevelEditorCentral central, PlacedBlockNames names, BlockWire wire)
    {
        var from = DescribeEnd(central, names, wire.FromUid, wire.FromConnector, isOutput: true);
        var to = DescribeEnd(central, names, wire.ToUid, wire.ToConnector, isOutput: false);
        return $"{from} to {to}";
    }

    private static string DescribeEnd(LEV_LevelEditorCentral central, PlacedBlockNames names, string uid, int connector, bool isOutput)
    {
        IReadOnlyList<BlockConnector> connectors = [];
        var block = LevelBlocks.Find(central, uid);
        if (block != null && BlockManifest.TryGetBlock(block.blockID, out var type, out _))
            connectors = isOutput ? type.GetOutputs(PlacedBlocks.FindWithoutWires(central, uid).Settings) : type.Inputs;

        var connectorName = connectors.FirstOrDefault(c => c.Index == connector)?.Name ?? connector.ToString(CultureInfo.InvariantCulture);
        return $"{names.FindName(uid) ?? uid} {connectorName}";
    }

    private static bool Stores(LEV_LevelEditorCentral central, string uid, int connector, string otherUid, int otherConnector)
    {
        var block = LevelBlocks.Find(central, uid);
        return block != null && PlacedBlocks.GetWires(block, connector).Any(w => w.targetUID == otherUid && w.connectorID == otherConnector);
    }
}
