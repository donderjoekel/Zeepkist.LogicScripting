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
    /// The first difference, or null. Where the text says it writes a block pairs it with the compiled one, and any
    /// pairing under which all the blocks and Wires are the same proves they are.
    /// </summary>
    public static string FindDifference(
        ImportGraph graph,
        IReadOnlyList<ImportedBlock> blocks,
        ImportedText text,
        CompileResult compiled)
    {
        if (!compiled.Succeeded)
        {
            var error = compiled.Errors[0];
            var place = text.Lines[error.Position.Line - 1].Uid is { } uid ? $" at {graph.Describe(uid)}" : "";
            return $"its text doesn't compile{place}: {error.Message}";
        }

        var expected = blocks.Count - text.Shared.Count;
        if (compiled.Blocks.Count != expected)
            return $"its text compiles to {compiled.Blocks.Count} blocks, not {expected}";

        var uids = compiled.Blocks.Select(b => FindUid(text, b)).ToList();
        for (var i = 0; i < uids.Count; i++)
        {
            if (uids[i] == null)
                return $"its text compiles to a {compiled.Blocks[i].Type.Function} that isn't one of the blocks";
        }

        if (uids.Distinct().Count() != uids.Count)
            return "its text compiles to several blocks for one";

        var byUid = Enumerable.Range(0, uids.Count).ToDictionary(i => uids[i], i => compiled.Blocks[i]);
        foreach (var block in blocks)
        {
            if (FindDifference(block, text, byUid) is { } difference)
                return difference;
        }

        HashSet<(string From, int FromConnector, string To, int ToConnector)> missing =
            [.. graph.Wires.Select(w => (GetSharedUid(text, w.FromUid), w.FromConnector, w.ToUid, w.ToConnector))];
        foreach (var wire in compiled.Wires.Concat(compiled.PlacedWires))
        {
            var to = GetUid(wire.To, uids);
            if (!missing.Remove((GetUid(wire.From, uids), wire.FromConnector, to, wire.ToConnector)))
                return $"its text gives {graph.Describe(to)} a Wire it doesn't have";
        }

        return missing.Count > 0 ? $"its text misses a Wire into {graph.Describe(missing.First().To)}" : null;
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

    /// <summary>
    /// The block the text says it writes there, whatever its type, so a type that differs is found and named.
    /// </summary>
    private static string FindUid(ImportedText text, CompiledBlock compiled)
    {
        return text.Places.TryGetValue((compiled.Position, compiled.Type), out var uid)
            ? uid
            : text.Places.Where(p => p.Key.Position.CompareTo(compiled.Position) == 0).Select(p => p.Value).FirstOrDefault();
    }

    private static string GetUid(WireEnd end, IReadOnlyList<string> uids)
    {
        return end.IsPlaced ? end.Uid : uids[end.Block];
    }

    private static string GetSharedUid(ImportedText text, string uid)
    {
        return text.Shared.TryGetValue(uid, out var shared) ? shared : uid;
    }

    private static string FindDifference(ImportedBlock block, ImportedText text, IReadOnlyDictionary<string, CompiledBlock> byUid)
    {
        if (!byUid.TryGetValue(GetSharedUid(text, block.Uid), out var compiled))
            return $"its text leaves out {BlockImport.Describe(block)}";

        return text.Shared.ContainsKey(block.Uid) && compiled.Type != BlockManifest.Generator
            ? $"its text shares {BlockImport.Describe(block)}, and only Generators can be shared"
            : FindDifference(block, compiled);
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
    public static Dictionary<(SettingKind Kind, string Key), string> GetValues(IReadOnlyList<Setting> settings, BlockType type)
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
