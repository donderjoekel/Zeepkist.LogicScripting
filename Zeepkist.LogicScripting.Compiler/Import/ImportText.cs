using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// Writes one line per block, <c>&lt;function&gt;_&lt;n&gt; = &lt;function&gt;(...)</c>, or only the call for a block
/// without outputs, which can't have a name. When every block is hidden, <c>default hide: true</c> comes first instead
/// of <c>hide:</c> in each call.
/// </summary>
internal static class ImportText
{
    /// <summary>
    /// Leaves out what a Script can't write, which <see cref="ImportCompare"/> then finds missing.
    /// </summary>
    public static string Write(ImportGraph graph, IReadOnlyList<ImportedBlock> ordered)
    {
        // All of them first, as a Delay can refer to a block below it
        var names = NameBlocks(ordered);
        List<string> lines = [];
        var hidesAll = ordered.All(b => b.FindValues(BlockSetting.Hide).Count > 0);
        if (hidesAll)
            lines.Add($"default {BlockSetting.Hide.Name}: true");

        foreach (var block in ordered)
        {
            BlockManifest.TryGetBlock(block.BlockId, out var type, out var style);
            List<string> arguments = [.. WriteInputs(graph, block, type, names), .. WriteSettings(block, type.Settings)];
            if (!hidesAll)
                arguments.AddRange(WriteSettings(block, [BlockSetting.Hide]));

            if (style != type.DefaultStyle)
                arguments.Add($"{BlockType.StyleName}: {style.Name}");

            var call = $"{type.Function}({string.Join(", ", arguments)})";
            lines.Add(names.TryGetValue(block.Uid, out var name) ? $"{name} = {call}" : call);
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// The name of each block that has outputs, by UID.
    /// </summary>
    private static Dictionary<string, string> NameBlocks(IReadOnlyList<ImportedBlock> ordered)
    {
        Dictionary<string, string> names = [];
        Dictionary<string, int> counts = [];
        foreach (var block in ordered)
        {
            var type = BlockManifest.GetPlacedType(block.BlockId);
            if (type.Outputs.Count == 0)
                continue;

            counts.TryGetValue(type.Function, out var count);
            counts[type.Function] = count + 1;
            names.Add(block.Uid, $"{type.Function}_{count + 1}");
        }

        return names;
    }

    /// <summary>
    /// By position up to the first input without a Wire, and by name after it.
    /// </summary>
    private static IEnumerable<string> WriteInputs(
        ImportGraph graph,
        ImportedBlock block,
        BlockType type,
        IReadOnlyDictionary<string, string> names)
    {
        var skipped = false;
        foreach (var input in type.Inputs)
        {
            var wire = graph.FindWire(block, input);
            if (wire == null)
            {
                skipped = true;
                continue;
            }

            var output = WriteOutput(graph, wire, names);
            yield return skipped ? $"{input.Name}: {output}" : output;
        }
    }

    /// <summary>
    /// The block's name, followed by its output's when it has several.
    /// </summary>
    private static string WriteOutput(ImportGraph graph, ImportedWire wire, IReadOnlyDictionary<string, string> names)
    {
        var name = names[wire.FromUid];
        var outputs = graph.GetOutputs(wire.FromUid);
        return outputs.Count == 1 ? name : $"{name}.{outputs.First(o => o.Index == wire.FromConnector).Name}";
    }

    private static IEnumerable<string> WriteSettings(ImportedBlock block, IReadOnlyList<BlockSetting> settings)
    {
        foreach (var setting in settings)
        {
            if (block.FindValues(setting).FirstOrDefault() is { } stored && setting.ToText(stored) is { } text)
                yield return $"{setting.Name}: {text}";
        }
    }
}
