using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// Writes blocks already in the level as the text of a Script: the text compiles to the same blocks, Settings and Wires,
/// except that Generators of the same power may become one, or the import is refused. A block with Settings a Script
/// can't write stays in the level, and the text refers to it.
/// </summary>
public static class BlockImport
{
    /// <param name="blocks">
    /// The selected blocks, or an Orphaned Group's. Those a Script can't create stay outside blocks, like the rest of the
    /// level's.
    /// </param>
    /// <param name="wires">Every Wire with an end on one of them, each once or once from either end.</param>
    /// <param name="togglerTargets">The UIDs of the blocks the level's Togglers target, or null for none.</param>
    /// <param name="disabledBlockIds">Blocks the startup check found don't match the game, or null for none.</param>
    /// <param name="findPlacedBlock">
    /// The block in the level with a UID, or null when there's none. Its Wires leave out the blocks whose UIDs come
    /// second: those the import takes in, which the next Compile replaces. Without it, reaching an outside block
    /// refuses the import.
    /// </param>
    public static ImportResult Import(
        IReadOnlyList<ImportedBlock> blocks,
        IReadOnlyList<ImportedWire> wires,
        IReadOnlyCollection<string> togglerTargets = null,
        IReadOnlyCollection<int> disabledBlockIds = null,
        Func<string, IReadOnlyCollection<string>, PlacedBlock> findPlacedBlock = null)
    {
        var creatable = blocks.Where(IsCreatable).ToList();
        if (creatable.Count == 0)
            return ImportResult.Refused("a Script can't create any of the selected blocks");

        var refusal = creatable.Select(b => FindProblem(b, disabledBlockIds ?? [])).FirstOrDefault(p => p != null);
        if (refusal != null)
            return ImportResult.Refused(refusal);

        List<ImportedBlock> imported = [];
        List<KeptBlock> kept = [];
        foreach (var block in creatable)
        {
            if (FindUnwritable(block, wires) is { } reason)
                kept.Add(new KeptBlock(block.Uid, reason));
            else
                imported.Add(block);
        }

        if (imported.Count == 0)
            return ImportResult.Refused(DescribeNothingWritable(kept));

        // A kept block keeps its UID, so its Togglers still find it
        HashSet<string> targets = [.. togglerTargets ?? [], .. creatable.SelectMany(GetTargets)];
        if (imported.FirstOrDefault(b => targets.Contains(b.Uid)) is { } target)
            return ImportResult.Refused($"a Toggler targets {Describe(target)}, and would lose it at the next Compile");

        HashSet<string> uids = [.. imported.Select(b => b.Uid)];
        PlacedBlock FindOutsideBlock(string uid) => findPlacedBlock?.Invoke(uid, uids);

        var graph = new ImportGraph(imported, FindOutsideBlock, disabledBlockIds);
        refusal = kept.Select(graph.AddKept)
            .Concat(wires.Select(graph.AddWire))
            .Concat(imported.SelectMany(b => GetTargets(b).Select(t => graph.AddTarget(b, t))))
            .FirstOrDefault(p => p != null);
        if (refusal != null)
            return ImportResult.Refused(refusal);

        var ordered = graph.GetOrder(out var loop);
        if (ordered == null)
            return ImportResult.Refused($"a loop of Wires has no Delay in it: {string.Join(", ", loop.Select(Describe))}");

        var text = ImportText.Write(graph, ordered);
        var compiled = ScriptCompiler.Compile(text.Text, 0, disabledBlockIds, FindOutsideBlock);
        refusal = ImportCompare.FindDifference(graph, ordered, text, compiled);
        return refusal == null ? ImportResult.Written(text.Text, uids, kept) : ImportResult.Refused(refusal);
    }

    /// <summary>
    /// What a refusal calls a block.
    /// </summary>
    internal static string Describe(ImportedBlock block)
    {
        return Describe(block.Uid, block.BlockId);
    }

    internal static string Describe(string uid, int blockId)
    {
        return $"{BlockManifest.GetPlacedType(blockId).Function} \"{uid}\"";
    }

    internal static string DescribeUnwritable(ImportedBlock block, BlockSetting setting)
    {
        return $"{Describe(block)} has a value for {setting.Name} that a Script can't write";
    }

    internal static string DescribeUnknown(ImportedBlock block, string key)
    {
        // A comment in the text holds it, and a line break would end the comment
        var shown = string.Concat(key.Where(c => !char.IsControl(c)));
        return $"{Describe(block)} has a Setting a Script can't write, \"{shown}\"";
    }

    private static string DescribeNothingWritable(IReadOnlyList<KeptBlock> kept)
    {
        return kept.Count == 1
            ? kept[0].Reason
            : $"a Script can't write any of the blocks: {string.Join(", ", kept.Select(k => k.Reason))}";
    }

    private static bool IsCreatable(ImportedBlock block)
    {
        return !BlockManifest.GetPlacedType(block.BlockId).IsPlacedOnly;
    }

    private static IEnumerable<string> GetTargets(ImportedBlock block)
    {
        return BlockManifest.GetPlacedType(block.BlockId).Settings
            .Where(s => s.Type == SettingType.Targets)
            .SelectMany(block.FindTargets);
    }

    /// <summary>
    /// What keeps the Script from even referring to the block.
    /// </summary>
    private static string FindProblem(ImportedBlock block, IReadOnlyCollection<int> disabledBlockIds)
    {
        if (block.IsInBlockGroup)
            return $"{Describe(block)} is {MessageFragments.InBlockGroup}";

        BlockManifest.TryGetBlock(block.BlockId, out var type, out var style);
        if (disabledBlockIds.Contains(style.BlockId))
            return $"{Describe(block)} can't be imported: '{type.Describe(style)}' {MessageFragments.TurnedOff}";

        // The block has no outputs in a mode that isn't one of the options
        return type.Modes != null && !IsWritable(block, type.Modes.Setting) ? DescribeUnwritable(block, type.Modes.Setting) : null;
    }

    /// <summary>
    /// What the block stores that a fresh Compile wouldn't, so that the block has to stay, or null.
    /// </summary>
    private static string FindUnwritable(ImportedBlock block, IReadOnlyList<ImportedWire> wires)
    {
        var type = BlockManifest.GetPlacedType(block.BlockId);
        if (type.Settings.Append(BlockSetting.Hide).FirstOrDefault(s => !IsWritable(block, s)) is { } setting)
        {
            return setting.IsRequired && !block.HasValue(setting)
                ? $"{Describe(block)} has no {setting.Name}, which a Script has to give"
                : DescribeUnwritable(block, setting);
        }

        return block.FindUnknownKey(type) is { } key ? DescribeUnknown(block, key) : FindRangeProblem(block, wires);
    }

    private static bool IsWritable(ImportedBlock block, BlockSetting setting)
    {
        if (setting.Type == SettingType.Targets)
            return block.HasWritableTargets(setting);

        var stored = block.FindValues(setting);
        return stored.Count == 0 ? !setting.IsRequired : stored.All(s => setting.ToText(s) != null);
    }

    /// <summary>
    /// The same rule a Compile has for a <see cref="BlockType.Range"/>, whose error wouldn't say which block it is.
    /// </summary>
    private static string FindRangeProblem(ImportedBlock block, IReadOnlyList<ImportedWire> wires)
    {
        var type = BlockManifest.GetPlacedType(block.BlockId);
        var range = type.Range;
        if (range == null)
            return null;

        var input = type.Inputs[type.IndexOfInput(range.MaximumInput)];
        if (wires.Any(w => w.ToUid == block.Uid && w.ToConnector == input.Index))
            return null;

        return GetNumber(block, range.Minimum) > GetNumber(block, range.Maximum)
            ? $"{Describe(block)} has a {range.Minimum.Name} above its {range.Maximum.Name}, which a Script can't write"
            : null;
    }

    private static long GetNumber(ImportedBlock block, BlockSetting setting)
    {
        return block.FindValues(setting).FirstOrDefault() is { } stored
            ? long.Parse(setting.ToText(stored), CultureInfo.InvariantCulture)
            : 0;
    }
}
