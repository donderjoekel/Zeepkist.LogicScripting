using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// Writes one line per block, <c>&lt;function&gt;_&lt;n&gt; = &lt;function&gt;(...)</c>, or only the call for a block
/// without outputs, which can't have a name. A Placed Block Reference to each outside block comes before them, and the
/// call that wires into one comes right after the last block that feeds it. When every block is hidden,
/// <c>default hide: true</c> comes first instead of <c>hide:</c> in each call.
/// </summary>
internal sealed class ImportText
{
    private readonly ImportGraph _graph;
    private readonly Dictionary<string, string> _names = [];
    private readonly Dictionary<string, int> _counts = [];
    private readonly List<ImportLine> _references = [];

    private ImportText(ImportGraph graph)
    {
        _graph = graph;
    }

    /// <summary>
    /// Leaves out what a Script can't write, which <see cref="ImportCompare"/> then finds missing.
    /// </summary>
    public static IReadOnlyList<ImportLine> Write(ImportGraph graph, IReadOnlyList<ImportedBlock> ordered)
    {
        return new ImportText(graph).Write(ordered);
    }

    private IReadOnlyList<ImportLine> Write(IReadOnlyList<ImportedBlock> ordered)
    {
        // All of them first, as a Delay can refer to a block below it
        foreach (var block in ordered)
        {
            var type = BlockManifest.GetPlacedType(block.BlockId);
            if (type.Outputs.Count > 0)
                _names.Add(block.Uid, NewName(type));
        }

        var hidesAll = ordered.All(b => b.FindValues(BlockSetting.Hide).Count > 0);
        var calls = FindOutsideCalls(ordered);
        List<ImportLine> lines = [];
        for (var i = 0; i < ordered.Count; i++)
        {
            var block = ordered[i];
            var call = WriteCall(block, hidesAll);
            lines.Add(new ImportLine(block.Uid, _names.TryGetValue(block.Uid, out var name) ? $"{name} = {call}" : call));
            foreach (var uid in calls[i])
                lines.Add(new ImportLine(uid, $"{GetName(uid)}({string.Join(", ", WriteInputs(uid))})"));
        }

        lines.InsertRange(0, _references);
        if (hidesAll)
            lines.Insert(0, new ImportLine(null, $"default {BlockSetting.Hide.Name}: true"));

        return lines;
    }

    /// <summary>
    /// The outside blocks to wire into after each block: those it's the last to feed.
    /// </summary>
    private ILookup<int, string> FindOutsideCalls(IReadOnlyList<ImportedBlock> ordered)
    {
        var indices = Enumerable.Range(0, ordered.Count).ToDictionary(i => ordered[i].Uid);
        return _graph.Wires
            .Where(w => !_graph.IsImported(w.ToUid))
            .GroupBy(w => w.ToUid)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToLookup(g => g.Max(w => indices[w.FromUid]), g => g.Key);
    }

    private string WriteCall(ImportedBlock block, bool hidesAll)
    {
        BlockManifest.TryGetBlock(block.BlockId, out var type, out var style);
        List<string> arguments = [.. WriteInputs(block.Uid), .. WriteSettings(block, type.Settings)];
        if (!hidesAll)
            arguments.AddRange(WriteSettings(block, [BlockSetting.Hide]));

        if (style != type.DefaultStyle)
            arguments.Add($"{BlockType.StyleName}: {style.Name}");

        return $"{type.Function}({string.Join(", ", arguments)})";
    }

    /// <summary>
    /// By position up to the first input without a Wire, and by name after it.
    /// </summary>
    private IEnumerable<string> WriteInputs(string uid)
    {
        var skipped = false;
        foreach (var input in _graph.GetType(uid).Inputs)
        {
            var wire = _graph.FindWire(uid, input);
            if (wire == null)
            {
                skipped = true;
                continue;
            }

            var output = WriteOutput(wire);
            yield return skipped ? $"{input.Name}: {output}" : output;
        }
    }

    /// <summary>
    /// The block's name, followed by its output's when it has several.
    /// </summary>
    private string WriteOutput(ImportedWire wire)
    {
        var name = GetName(wire.FromUid);
        var outputs = _graph.GetOutputs(wire.FromUid);
        return outputs.Count == 1 ? name : $"{name}.{outputs.First(o => o.Index == wire.FromConnector).Name}";
    }

    private IEnumerable<string> WriteSettings(ImportedBlock block, IReadOnlyList<BlockSetting> settings)
    {
        foreach (var setting in settings)
        {
            if (setting.Type == SettingType.Targets)
                yield return $"{setting.Name}: [{string.Join(", ", block.FindTargets(setting).Select(GetName))}]";
            else if (block.FindValues(setting).FirstOrDefault() is { } stored && setting.ToText(stored) is { } text)
                yield return $"{setting.Name}: {text}";
        }
    }

    /// <summary>
    /// An outside block gets its name, and its Placed Block Reference, where it's first used.
    /// </summary>
    private string GetName(string uid)
    {
        if (_names.TryGetValue(uid, out var name))
            return name;

        name = NewName(_graph.GetType(uid));
        _names.Add(uid, name);
        _references.Add(new ImportLine(uid, $"{name} = placed(\"{uid}\")"));
        return name;
    }

    private string NewName(BlockType type)
    {
        _counts.TryGetValue(type.Function, out var count);
        _counts[type.Function] = count + 1;
        return $"{type.Function}_{count + 1}";
    }
}
