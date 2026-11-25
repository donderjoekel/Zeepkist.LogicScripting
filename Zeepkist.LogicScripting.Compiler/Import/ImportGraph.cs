using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// The blocks to import, the outside blocks their Wires and targets reach, and the Wires with an end on a block to
/// import.
/// </summary>
internal sealed class ImportGraph
{
    private readonly IReadOnlyList<ImportedBlock> _blocks;
    private readonly Func<string, PlacedBlock> _findPlacedBlock;
    private readonly IReadOnlyCollection<int> _disabledBlockIds;
    private readonly Dictionary<string, int> _indices;
    private readonly Dictionary<string, PlacedBlock> _outside = [];
    private readonly Dictionary<(string Uid, int Connector), ImportedWire> _inputs = [];
    private readonly List<KeptBlock> _kept = [];

    /// <param name="blocks">The blocks to import.</param>
    /// <param name="findPlacedBlock">An outside block by its UID, or null when there's none.</param>
    /// <param name="disabledBlockIds">Blocks the startup check found don't match the game.</param>
    public ImportGraph(
        IReadOnlyList<ImportedBlock> blocks,
        Func<string, PlacedBlock> findPlacedBlock = null,
        IReadOnlyCollection<int> disabledBlockIds = null)
    {
        _blocks = blocks;
        _findPlacedBlock = findPlacedBlock ?? (_ => null);
        _disabledBlockIds = disabledBlockIds ?? [];
        _indices = Enumerable.Range(0, blocks.Count).ToDictionary(i => blocks[i].Uid);
    }

    /// <summary>
    /// One for each wired input.
    /// </summary>
    public IReadOnlyCollection<ImportedWire> Wires => _inputs.Values;

    /// <summary>
    /// The outside blocks the Script refers to whether or not a Wire or target reaches them.
    /// </summary>
    public IReadOnlyList<KeptBlock> Kept => _kept;

    public bool IsImported(string uid)
    {
        return _indices.ContainsKey(uid);
    }

    /// <summary>
    /// The Wire into an input, or null.
    /// </summary>
    public ImportedWire FindWire(string uid, BlockConnector input)
    {
        return _inputs.TryGetValue((uid, input.Index), out var wire) ? wire : null;
    }

    public BlockType GetType(string uid)
    {
        return BlockManifest.GetPlacedType(GetBlockId(uid));
    }

    /// <summary>
    /// A block's outputs, as the mode it's in names them.
    /// </summary>
    public IReadOnlyList<BlockConnector> GetOutputs(string uid)
    {
        return GetType(uid).GetOutputs(IsImported(uid) ? _blocks[_indices[uid]].Settings : _outside[uid].Settings);
    }

    /// <summary>
    /// What a refusal calls a block to import or one that was added as an outside block.
    /// </summary>
    public string Describe(string uid)
    {
        return BlockImport.Describe(uid, GetBlockId(uid));
    }

    /// <summary>
    /// Returns why the Script can't refer to the block, or null when it's in the graph.
    /// </summary>
    public string AddKept(KeptBlock block)
    {
        _kept.Add(block);
        return AddOutside(block.Uid, "the selection has", false);
    }

    /// <summary>
    /// Returns why the Wire can't be imported, or null when it's in the graph. A Wire added before, read from its other
    /// end, stays one. A Wire between two outside blocks isn't part of the import.
    /// </summary>
    public string AddWire(ImportedWire wire)
    {
        if (!IsImported(wire.FromUid) && !IsImported(wire.ToUid))
            return null;

        if (!IsImported(wire.ToUid) && AddOutside(wire.ToUid, $"{Describe(wire.FromUid)} has a Wire to", true) is { } toProblem)
            return toProblem;

        if (!IsImported(wire.FromUid) && AddOutside(wire.FromUid, $"{Describe(wire.ToUid)} has a Wire from", true) is { } fromProblem)
            return fromProblem;

        if (!HasConnector(GetOutputs(wire.FromUid), wire.FromConnector) || !HasConnector(GetType(wire.ToUid).Inputs, wire.ToConnector))
            return $"the Wire from {Describe(wire.FromUid)} to {Describe(wire.ToUid)} doesn't go from an output to an input";

        if (!_inputs.TryGetValue((wire.ToUid, wire.ToConnector), out var other))
        {
            _inputs.Add((wire.ToUid, wire.ToConnector), wire);
            return null;
        }

        return other.FromUid == wire.FromUid && other.FromConnector == wire.FromConnector
            ? null
            : $"{Describe(wire.ToUid)} has several Wires into one input";
    }

    /// <summary>
    /// Returns why a block's target can't be imported, or null when the Script can refer to it.
    /// </summary>
    public string AddTarget(ImportedBlock block, string target)
    {
        return AddOutside(target, $"{Describe(block.Uid)} targets", false);
    }

    /// <summary>
    /// The blocks, each after those wired into it and otherwise in the order they were given. Only a Delay can come
    /// before the block wired into it, or be it, when that Wire closes a loop. Null when a Dependency Loop leaves no
    /// such order, with its blocks as <paramref name="loop"/>.
    /// </summary>
    public IReadOnlyList<ImportedBlock> GetOrder(out IReadOnlyList<ImportedBlock> loop)
    {
        // The blocks wired into each one, until they're in the order
        var feeders = _blocks.Select(_ => new SortedSet<int>()).ToList();
        var fed = _blocks.Select(_ => new List<int>()).ToList();
        foreach (var wire in Wires.Where(w => IsImported(w.FromUid) && IsImported(w.ToUid)))
        {
            feeders[_indices[wire.ToUid]].Add(_indices[wire.FromUid]);
            fed[_indices[wire.FromUid]].Add(_indices[wire.ToUid]);
        }

        var isOrdered = new bool[_blocks.Count];
        List<ImportedBlock> ordered = [];
        SortedSet<int> ready = [.. Enumerable.Range(0, _blocks.Count).Where(i => feeders[i].Count == 0)];
        OrderReady();

        // Ordering a block opens the loops it's in and never makes one, so a Delay that isn't in a loop by its turn
        // won't be in one later
        foreach (var delay in Enumerable.Range(0, _blocks.Count).Where(i => GetType(_blocks[i].Uid) == BlockManifest.Delay))
        {
            if (isOrdered[delay] || !IsInLoop(delay, feeders))
                continue;

            ready.Add(delay);
            OrderReady();
        }

        loop = ordered.Count == _blocks.Count ? null : FindLoop(Array.IndexOf(isOrdered, false), feeders);
        return loop == null ? ordered : null;

        void OrderReady()
        {
            while (ready.Count > 0)
            {
                var index = ready.Min;
                ready.Remove(index);
                isOrdered[index] = true;
                ordered.Add(_blocks[index]);
                foreach (var next in fed[index])
                {
                    feeders[next].Remove(index);
                    if (feeders[next].Count == 0 && !isOrdered[next])
                        ready.Add(next);
                }
            }
        }
    }

    private static bool IsInLoop(int block, IReadOnlyList<SortedSet<int>> feeders)
    {
        HashSet<int> seen = [];
        Stack<int> pending = new([block]);
        while (pending.Count > 0)
        {
            foreach (var feeder in feeders[pending.Pop()])
            {
                if (feeder == block)
                    return true;

                if (seen.Add(feeder))
                    pending.Push(feeder);
            }
        }

        return false;
    }

    /// <summary>
    /// A loop the block is in or fed by, in the direction of its Wires.
    /// </summary>
    private List<ImportedBlock> FindLoop(int block, IReadOnlyList<SortedSet<int>> feeders)
    {
        List<int> path = [];
        HashSet<int> seen = [];
        while (seen.Add(block))
        {
            path.Add(block);
            block = feeders[block].Min;
        }

        return [.. path.Skip(path.IndexOf(block)).Reverse().Select(i => _blocks[i])];
    }

    /// <param name="uid">The outside block's UID.</param>
    /// <param name="reach">How the import reaches it, which a refusal starts with.</param>
    /// <param name="isWired">Whether it needs Connectors, which a target doesn't.</param>
    private string AddOutside(string uid, string reach, bool isWired)
    {
        if (!_outside.TryGetValue(uid, out var placed))
        {
            placed = _findPlacedBlock(uid);
            if (placed == null)
                return $"{reach} block \"{uid}\", which isn't in the level";

            _outside.Add(uid, placed);
        }

        if (placed.IsInBlockGroup)
            return $"{reach} {Describe(uid)}, but that one is {MessageFragments.InBlockGroup}";

        if (!BlockManifest.TryGetBlock(placed.BlockId, out var type, out var style))
            return isWired ? $"{reach} {Describe(uid)}, which a Script can't wire" : null;

        if (_disabledBlockIds.Contains(style.BlockId))
            return $"{reach} {Describe(uid)}, but '{type.Describe(style)}' {MessageFragments.TurnedOff}";

        return type.Modes != null && placed.Settings.Any(s => s.Key == type.Modes.Setting.Key && type.Modes.Setting.ToText(s) == null)
            ? $"{reach} {Describe(uid)}, which has a value for {type.Modes.Setting.Name} that a Script can't read"
            : null;
    }

    private int GetBlockId(string uid)
    {
        return IsImported(uid) ? _blocks[_indices[uid]].BlockId : _outside[uid].BlockId;
    }

    private static bool HasConnector(IReadOnlyList<BlockConnector> connectors, int index)
    {
        return connectors.Any(c => c.Index == index);
    }
}
