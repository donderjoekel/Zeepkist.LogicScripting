using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// The blocks to import and the Wires between them.
/// </summary>
internal sealed class ImportGraph
{
    private readonly IReadOnlyList<ImportedBlock> _blocks;
    private readonly Dictionary<string, int> _indices;
    private readonly Dictionary<(string Uid, int Connector), ImportedWire> _inputs = [];

    public ImportGraph(IReadOnlyList<ImportedBlock> blocks)
    {
        _blocks = blocks;
        _indices = Enumerable.Range(0, blocks.Count).ToDictionary(i => blocks[i].Uid);
    }

    /// <summary>
    /// One for each wired input.
    /// </summary>
    public IReadOnlyCollection<ImportedWire> Wires => _inputs.Values;

    /// <summary>
    /// The Wire into an input, or null.
    /// </summary>
    public ImportedWire FindWire(ImportedBlock block, BlockConnector input)
    {
        return _inputs.TryGetValue((block.Uid, input.Index), out var wire) ? wire : null;
    }

    /// <summary>
    /// A block's outputs, as the mode it's in names them.
    /// </summary>
    public IReadOnlyList<BlockConnector> GetOutputs(string uid)
    {
        var block = Find(uid);
        return TypeOf(block).GetOutputs(block.Settings);
    }

    /// <summary>
    /// Returns why the Wire can't be imported, or null when it's in the graph. A Wire added before, read from its other
    /// end, stays one.
    /// </summary>
    public string AddWire(ImportedWire wire)
    {
        var from = Find(wire.FromUid);
        var to = Find(wire.ToUid);
        if (to == null)
            return $"{BlockImport.Describe(from)} has a Wire to block \"{wire.ToUid}\", which isn't selected";

        if (from == null)
            return $"{BlockImport.Describe(to)} has a Wire from block \"{wire.FromUid}\", which isn't selected";

        if (!HasConnector(GetOutputs(wire.FromUid), wire.FromConnector) || !HasConnector(TypeOf(to).Inputs, wire.ToConnector))
            return $"the Wire from {BlockImport.Describe(from)} to {BlockImport.Describe(to)} doesn't go from an output to an input";

        if (!_inputs.TryGetValue((wire.ToUid, wire.ToConnector), out var other))
        {
            _inputs.Add((wire.ToUid, wire.ToConnector), wire);
            return null;
        }

        return other.FromUid == wire.FromUid && other.FromConnector == wire.FromConnector
            ? null
            : $"{BlockImport.Describe(to)} has several Wires into one input";
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
        foreach (var wire in Wires)
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
        foreach (var delay in Enumerable.Range(0, _blocks.Count).Where(i => TypeOf(_blocks[i]) == BlockManifest.Delay))
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

    private ImportedBlock Find(string uid)
    {
        return _indices.TryGetValue(uid, out var index) ? _blocks[index] : null;
    }

    private static BlockType TypeOf(ImportedBlock block)
    {
        return BlockManifest.GetPlacedType(block.BlockId);
    }

    private static bool HasConnector(IReadOnlyList<BlockConnector> connectors, int index)
    {
        return connectors.Any(c => c.Index == index);
    }
}
