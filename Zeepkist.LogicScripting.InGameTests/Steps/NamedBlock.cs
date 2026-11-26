using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// A block <c>place-block ... as &lt;name&gt;</c> placed or <c>name-group-block</c> named, as it's in the level now.
/// </summary>
internal sealed class NamedBlock
{
    private NamedBlock(string name, BlockProperties block, BlockType type, IReadOnlyList<BlockConnector> outputs)
    {
        Name = name;
        Block = block;
        Type = type;
        Outputs = outputs;
    }

    public string Name { get; }
    public BlockProperties Block { get; }
    public BlockType Type { get; }

    /// <summary>
    /// Named by the mode the block is set to in the level.
    /// </summary>
    public IReadOnlyList<BlockConnector> Outputs { get; }

    public static NamedBlock Find(LEV_LevelEditorCentral central, string name, string uid, out string error)
    {
        var block = LevelBlocks.Find(central, uid);
        if (block == null)
        {
            error = $"the level has no block with \"{name}\"'s UID \"{uid}\" any more";
            return null;
        }

        if (!BlockManifest.TryGetBlock(block.blockID, out var type, out _))
        {
            error = $"\"{name}\", block {block.blockID}, isn't a Logic Block type the mod knows";
            return null;
        }

        error = null;
        return new NamedBlock(name, block, type, type.GetOutputs(PlacedBlocks.FindWithoutWires(central, uid).Settings));
    }

    /// <summary>
    /// <paramref name="kind"/> is <c>output</c> or <c>input</c>, for the error.
    /// </summary>
    public static BlockConnector FindConnector(string block, string kind, IReadOnlyList<BlockConnector> connectors, string name, out string error)
    {
        var connector = connectors.FirstOrDefault(c => c.Name == name);
        var use = connectors.Count == 0 ? $"It has no {kind}s" : $"Use {string.Join(", ", connectors.Select(c => c.Name))}";
        error = connector == null ? $"\"{block}\" has no {kind} called '{name}'. {use}" : null;
        return connector;
    }

    /// <summary>
    /// Every Wire the block itself stores, a Wire from it to itself once.
    /// </summary>
    public List<BlockWire> ReadWires()
    {
        List<BlockWire> wires = [];
        foreach (var output in Outputs)
        {
            wires.AddRange(PlacedBlocks.GetWires(Block, output.Index)
                .Select(w => new BlockWire(Block.UID, output.Index, w.targetUID, w.connectorID)));
        }

        foreach (var input in Type.Inputs)
        {
            wires.AddRange(PlacedBlocks.GetWires(Block, input.Index)
                .Where(w => w.targetUID != Block.UID)
                .Select(w => new BlockWire(w.targetUID, w.connectorID, Block.UID, input.Index)));
        }

        return wires;
    }
}
