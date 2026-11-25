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
    /// The blocks, each after those wired into it and otherwise in the order they were given. Null when Wires make a
    /// loop, with a block in it or fed by it as <paramref name="stuck"/>.
    /// </summary>
    public IReadOnlyList<ImportedBlock> GetOrder(out ImportedBlock stuck)
    {
        var missing = new int[_blocks.Count];
        var fed = _blocks.Select(_ => new List<int>()).ToList();
        foreach (var wire in Wires)
        {
            missing[_indices[wire.ToUid]]++;
            fed[_indices[wire.FromUid]].Add(_indices[wire.ToUid]);
        }

        SortedSet<int> ready = [.. Enumerable.Range(0, _blocks.Count).Where(i => missing[i] == 0)];
        List<ImportedBlock> ordered = [];
        while (ready.Count > 0)
        {
            var index = ready.Min;
            ready.Remove(index);
            ordered.Add(_blocks[index]);
            foreach (var next in fed[index])
            {
                missing[next]--;
                if (missing[next] == 0)
                    ready.Add(next);
            }
        }

        stuck = _blocks.Where((_, i) => missing[i] > 0).FirstOrDefault();
        return stuck == null ? ordered : null;
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
