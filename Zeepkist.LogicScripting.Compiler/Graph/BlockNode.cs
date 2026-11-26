using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed class BlockNode
{
    private readonly List<Setting> _settings;
    private readonly int _index;

    public BlockNode(int index, Position position, BlockType type, BlockStyle style, IEnumerable<Setting> settings)
    {
        _index = index;
        Position = position;
        Type = type;
        Style = style;
        _settings = [.. settings];
        Outputs = type.GetOutputs(_settings);
    }

    public BlockNode(string uid, BlockType type, BlockStyle style, PlacedBlock placed)
        : this(0, default, type, style, placed.Settings)
    {
        Uid = uid;
        Placed = placed;
    }

    /// <summary>
    /// Blocks are added in Script line order.
    /// </summary>
    public int Index => IsPlaced
        ? throw new InvalidOperationException($"Block {Uid} is already in the level, so it has no index")
        : _index;

    /// <summary>
    /// The call, operator or literal that places the block; only a block the Compile places has one.
    /// </summary>
    public Position Position { get; }

    /// <summary>
    /// Null for a block the Compile places.
    /// </summary>
    public string Uid { get; }

    /// <summary>
    /// What the level says about the placed block; null for a block the Compile places.
    /// </summary>
    public PlacedBlock Placed { get; }

    public bool IsPlaced => Placed != null;

    public BlockType Type { get; }

    /// <summary>
    /// Null for a placed block the Block Manifest doesn't describe.
    /// </summary>
    public BlockStyle Style { get; }
    public IReadOnlyList<Setting> Settings => _settings;

    /// <summary>
    /// The outputs in this block's mode.
    /// </summary>
    public IReadOnlyList<BlockConnector> Outputs { get; }

    public bool HasSetting(string key)
    {
        return _settings.Exists(s => s.Key == key);
    }

    public void AddSetting(Setting setting)
    {
        _settings.Add(setting);
    }
}
