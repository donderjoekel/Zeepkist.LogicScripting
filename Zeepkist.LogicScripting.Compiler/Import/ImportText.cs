using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// Writes the text the way a player would: <c>default hide: true</c> when every block is hidden, then the Placed Block
/// References to the outside blocks, a kept one's below a comment that says why it stays, then the blocks circuit by
/// circuit, with a blank line between them. A block used once is written where it's used while the line stays short,
/// and one that nothing uses has no name. The call that wires into an outside block comes right after the last block
/// that feeds it.
/// </summary>
internal sealed class ImportText
{
    /// <summary>
    /// How long a line may get by nesting blocks into it: about what the window shows at its first size with both sides
    /// collapsed.
    /// </summary>
    public const int MaxNestedLength = 80;

    private readonly ImportGraph _graph;
    private readonly IReadOnlyList<ImportedBlock> _ordered;
    private readonly ImportShapes _shapes;
    private readonly HashSet<string> _loopDelays;
    private readonly HashSet<string> _nested = [];
    private readonly Dictionary<string, string> _names = [];
    private readonly HashSet<string> _takenNames = [];
    private readonly Dictionary<string, int> _counts = [];
    private readonly List<ImportLine> _lines = [];
    private readonly Dictionary<(Position Position, BlockType Type), string> _places = [];

    private ImportText(ImportGraph graph, IReadOnlyList<ImportedBlock> ordered)
    {
        _graph = graph;
        _ordered = ordered;
        _shapes = new ImportShapes(graph, ordered);
        _loopDelays = FindLoopDelays(graph, ordered);
    }

    public static ImportedText Write(ImportGraph graph, IReadOnlyList<ImportedBlock> ordered)
    {
        return new ImportText(graph, ordered).Write();
    }

    private ImportedText Write()
    {
        Nest();
        var circuits = FindCircuits();
        var outside = FindOutsideUses(circuits.SelectMany(c => c));
        foreach (var uid in outside)
            _names.Add(uid, NewName(_graph.GetType(uid).Function));

        // All of them first, as a Delay can refer to a block below it
        foreach (var uid in circuits.SelectMany(c => c).Where(IsNamed))
            _names.Add(uid, NewName(GetNameStart(uid)));

        if (_shapes.HidesAll)
            _lines.Add(new ImportLine(null, $"default {BlockSetting.Hide.Name}: true"));

        if (outside.Count > 0)
            StartSection();

        foreach (var uid in outside)
        {
            if (_graph.Kept.FirstOrDefault(k => k.Uid == uid) is { } kept)
                _lines.Add(new ImportLine(uid, $"# {kept.Reason}"));

            _lines.Add(new ImportLine(uid, $"{_names[uid]} = {GraphBuilder.PlacedFunction}(\"{uid}\")"));
        }

        foreach (var circuit in circuits)
        {
            StartSection();
            foreach (var uid in circuit)
                AddLine(uid);
        }

        return new ImportedText(_lines, _places, _shapes.Shared);
    }

    /// <summary>
    /// The Delays that refer to a block below them, or to themselves. They keep a name, as every loop of Wires has one.
    /// </summary>
    private static HashSet<string> FindLoopDelays(ImportGraph graph, IReadOnlyList<ImportedBlock> ordered)
    {
        var indices = GetIndices(ordered);
        return
        [
            .. graph.Wires
                .Where(w => indices.ContainsKey(w.FromUid) && indices.TryGetValue(w.ToUid, out var to) && indices[w.FromUid] >= to)
                .Select(w => w.ToUid)
        ];
    }

    private static Dictionary<string, int> GetIndices(IReadOnlyList<ImportedBlock> ordered)
    {
        return Enumerable.Range(0, ordered.Count).ToDictionary(i => ordered[i].Uid);
    }

    /// <summary>
    /// Each block after those nested into it, so a Delay that refers ahead comes after the blocks below it.
    /// </summary>
    private void Nest()
    {
        var digits = new string('9', _graph.BlockCount.ToString(CultureInfo.InvariantCulture).Length);
        var uids = _ordered.Select(b => b.Uid).ToList();
        foreach (var uid in uids.Where(u => !_loopDelays.Contains(u)).Concat(uids.Where(_loopDelays.Contains)).Concat(FindFedOutsideBlocks()))
        {
            foreach (var input in _graph.GetType(uid).Inputs)
            {
                if (_graph.FindWire(uid, input)?.FromUid is not { } feeder || !IsNestable(feeder))
                    continue;

                _nested.Add(feeder);
                var writer = new ImportLineWriter(_graph, _shapes, _nested, GetLongestName);
                WriteLine(writer, uid, IsNamed(uid));
                if (writer.Text.Length > MaxNestedLength || writer.Depth > Parser.MaxDepth)
                    _nested.Remove(feeder);
            }
        }

        // Numbers come after nesting, so this leaves room for any of them
        string GetLongestName(string uid)
        {
            return $"{GetNameStart(uid)}_{digits}";
        }
    }

    private bool IsNestable(string uid)
    {
        return _graph.IsImported(uid)
            && !_shapes.TryGetLiteral(uid, out _)
            && !_loopDelays.Contains(uid)
            && _shapes.IsUsedOnce(uid);
    }

    private IEnumerable<string> FindFedOutsideBlocks()
    {
        return _graph.Wires
            .Select(w => w.ToUid)
            .Where(u => !_graph.IsImported(u))
            .Distinct()
            .OrderBy(u => u, StringComparer.Ordinal);
    }

    /// <summary>
    /// The blocks to import and the outside blocks to wire into that have a line each, in the lines' order, split into
    /// those that are connected. A literal connects nothing, as its Generator is shared.
    /// </summary>
    private List<List<string>> FindCircuits()
    {
        var calls = FindOutsideCalls();
        List<string> uids = [];
        for (var i = 0; i < _ordered.Count; i++)
        {
            var uid = _ordered[i].Uid;
            if (!_nested.Contains(uid) && !_shapes.TryGetLiteral(uid, out _))
                uids.Add(uid);

            uids.AddRange(calls[i]);
        }

        Dictionary<string, string> parents = [];
        foreach (var wire in _graph.Wires.Where(w => !_shapes.TryGetLiteral(w.FromUid, out _)))
            parents[FindRoot(wire.FromUid)] = FindRoot(wire.ToUid);

        return uids.GroupBy(FindRoot).Select(g => g.ToList()).ToList();

        string FindRoot(string uid)
        {
            while (parents.TryGetValue(uid, out var parent) && parent != uid)
                uid = parent;

            return uid;
        }
    }

    /// <summary>
    /// The outside blocks to wire into after each block: those it's the last to feed.
    /// </summary>
    private ILookup<int, string> FindOutsideCalls()
    {
        var indices = GetIndices(_ordered);
        return FindFedOutsideBlocks().ToLookup(uid =>
        {
            var feeders = _graph.Wires.Where(w => w.ToUid == uid).Select(w => w.FromUid).ToList();
            var written = feeders.Where(f => !_shapes.TryGetLiteral(f, out _)).ToList();
            return (written.Count > 0 ? written : feeders).Max(f => indices[f]);
        });
    }

    /// <summary>
    /// The outside blocks in the order the lines use them, those a call reads before the one it wires into, and then the
    /// kept blocks no line uses.
    /// </summary>
    private List<string> FindOutsideUses(IEnumerable<string> uids)
    {
        List<string> outside = [];
        foreach (var uid in uids)
        {
            List<string> used = [];
            WriteLine(new ImportLineWriter(_graph, _shapes, _nested, AddUse), uid, false);
            outside.AddRange(used.Where(u => u != uid && !_graph.IsImported(u)));
            if (!_graph.IsImported(uid))
                outside.Add(uid);

            string AddUse(string name)
            {
                used.Add(name);
                return name;
            }
        }

        return outside.Concat(_graph.Kept.Select(k => k.Uid)).Distinct().ToList();
    }

    private void AddLine(string uid)
    {
        var writer = new ImportLineWriter(_graph, _shapes, _nested, u => _names[u], _lines.Count + 1);
        WriteLine(writer, uid, IsNamed(uid));
        foreach (var place in writer.Places)
            _places.Add(place.Key, place.Value);

        _lines.Add(new ImportLine(uid, writer.Text));
    }

    private void WriteLine(ImportLineWriter writer, string uid, bool isNamed)
    {
        if (_graph.IsImported(uid))
            writer.WriteStatement(uid, isNamed);
        else
            writer.WriteOutsideCall(uid);
    }

    private void StartSection()
    {
        if (_lines.Count > 0)
            _lines.Add(new ImportLine(null, ""));
    }

    /// <summary>
    /// An operator needs a name even when nothing uses it, as a value on its own line is an error.
    /// </summary>
    private bool IsNamed(string uid)
    {
        return _graph.IsImported(uid)
            && (_shapes.IsUsed(uid) || _shapes.TryGetUnary(uid, out _) || _shapes.TryGetBinary(uid, out _));
    }

    private string GetNameStart(string uid)
    {
        return _shapes.TryGetPickedOutput(uid, out var output) ? output.Name : _graph.GetType(uid).Function;
    }

    private string NewName(string start)
    {
        string name;
        do
        {
            _counts.TryGetValue(start, out var count);
            _counts[start] = count + 1;
            name = $"{start}_{count + 1}";
        }
        while (GraphBuilder.IsReserved(name) || !_takenNames.Add(name));

        return name;
    }
}
