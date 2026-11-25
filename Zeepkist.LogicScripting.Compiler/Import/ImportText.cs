using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// Writes one line per block, <c>&lt;function&gt;_&lt;n&gt; = &lt;function&gt;(...)</c>.
/// </summary>
internal static class ImportText
{
    /// <summary>
    /// Leaves out what a Script can't write, which <see cref="ImportCompare"/> then finds missing.
    /// </summary>
    public static string Write(ImportGraph graph, IReadOnlyList<ImportedBlock> ordered)
    {
        Dictionary<string, string> names = [];
        Dictionary<string, int> counts = [];
        List<string> lines = [];
        foreach (var block in ordered)
        {
            BlockManifest.TryGetBlock(block.BlockId, out var type, out var style);
            counts.TryGetValue(type.Function, out var count);
            counts[type.Function] = count + 1;
            var name = $"{type.Function}_{count + 1}";
            names.Add(block.Uid, name);

            List<string> arguments = [.. WriteInputs(graph, block, type, names), .. WriteSettings(block, type)];
            if (style != type.DefaultStyle)
                arguments.Add($"{BlockType.StyleName}: {style.Name}");

            lines.Add($"{name} = {type.Function}({string.Join(", ", arguments)})");
        }

        return string.Join("\n", lines);
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
                skipped = true;
            else
                yield return skipped ? $"{input.Name}: {names[wire.FromUid]}" : names[wire.FromUid];
        }
    }

    private static IEnumerable<string> WriteSettings(ImportedBlock block, BlockType type)
    {
        foreach (var setting in type.Settings.Append(BlockSetting.Hide))
        {
            var stored = block.Settings.FirstOrDefault(s => s.Key == setting.Key && ImportCompare.GetValue(s) != null);
            if (stored != null && setting.ToText(stored) is { } text)
                yield return $"{setting.Name}: {text}";
        }
    }
}
