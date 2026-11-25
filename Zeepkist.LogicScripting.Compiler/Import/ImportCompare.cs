using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// Compares what an import read with what its own text compiles to.
/// </summary>
internal static class ImportCompare
{
    /// <summary>
    /// The first difference, or null. The text has one call per block and no literals, which would share a Generator,
    /// so the compiled blocks come in the text's order.
    /// </summary>
    public static string FindDifference(ImportGraph graph, IReadOnlyList<ImportedBlock> ordered, CompileResult compiled)
    {
        if (!compiled.Succeeded)
            return $"its text doesn't compile: {compiled.Errors[0].Message}";

        if (compiled.Blocks.Count != ordered.Count)
            return $"its text compiles to {compiled.Blocks.Count} blocks, not {ordered.Count}";

        for (var i = 0; i < ordered.Count; i++)
        {
            if (FindDifference(ordered[i], compiled.Blocks[i]) is { } difference)
                return difference;
        }

        var indices = Enumerable.Range(0, ordered.Count).ToDictionary(i => ordered[i].Uid);
        HashSet<(int From, int FromConnector, int To, int ToConnector)> missing =
            [.. graph.Wires.Select(w => (indices[w.FromUid], w.FromConnector, indices[w.ToUid], w.ToConnector))];
        foreach (var wire in compiled.Wires)
        {
            if (!missing.Remove((wire.From.Block, wire.FromConnector, wire.To.Block, wire.ToConnector)))
                return $"its text gives {BlockImport.Describe(ordered[wire.To.Block])} a Wire it doesn't have";
        }

        return missing.Count > 0
            ? $"its text misses a Wire into {BlockImport.Describe(ordered[missing.First().To])}"
            : null;
    }

    /// <summary>
    /// The stored value, the same for texts the game reads as the same number, or null for the value a block placed by
    /// hand starts with, which is the same as storing none.
    /// </summary>
    public static string GetValue(Setting setting)
    {
        if (setting.Kind == SettingKind.Int)
            return setting.IntValue == 0 ? null : setting.IntValue.ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrEmpty(setting.TextValue))
            return null;

        if (!long.TryParse(setting.TextValue, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number))
            return setting.TextValue;

        return number == 0 ? null : number.ToString(CultureInfo.InvariantCulture);
    }

    private static string FindDifference(ImportedBlock block, CompiledBlock compiled)
    {
        if (block.BlockId != compiled.Style.BlockId)
            return $"its text doesn't give {BlockImport.Describe(block)} its own type and style";

        var read = GetValues(block.Settings, compiled.Type);
        var written = GetValues(compiled.Settings, compiled.Type);
        var key = read.Keys.Union(written.Keys)
            .Where(k => !read.TryGetValue(k, out var a) || !written.TryGetValue(k, out var b) || a != b)
            .Select(k => k.Key)
            .Concat(block.Floats.Where(f => f.Value != 0).Select(f => f.Key))
            .FirstOrDefault();
        if (key == null)
            return null;

        var setting = compiled.Type.Settings.Append(BlockSetting.Hide).FirstOrDefault(s => s.Key == key);
        return setting == null
            ? $"{BlockImport.Describe(block)} has a Setting a Script can't write, \"{key}\""
            : BlockImport.DescribeUnwritable(block, setting);
    }

    /// <summary>
    /// The values that differ from what a fresh block of the type stores, which a Compile's block stores too unless
    /// the Script gives another.
    /// </summary>
    private static Dictionary<(SettingKind Kind, string Key), string> GetValues(IReadOnlyList<Setting> settings, BlockType type)
    {
        Dictionary<(SettingKind Kind, string Key), string> values = [];
        foreach (var setting in settings)
        {
            var fresh = type.FreshSettings.FirstOrDefault(s => s.Kind == setting.Kind && s.Key == setting.Key);
            var value = GetValue(setting);
            if (value != (fresh == null ? null : GetValue(fresh)))
                values[(setting.Kind, setting.Key)] = value ?? "0";
        }

        return values;
    }
}
