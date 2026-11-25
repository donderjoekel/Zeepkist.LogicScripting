using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// Writes blocks already in the level as the text of a Script, all or nothing: the text compiles to the same blocks,
/// Settings and Wires, or the import is refused.
/// </summary>
public static class BlockImport
{
    /// <param name="blocks">The blocks to import.</param>
    /// <param name="wires">Every Wire with an end on one of them, each once or once from either end.</param>
    /// <param name="togglerTargets">The UIDs of the blocks the level's Togglers target, or null for none.</param>
    /// <param name="disabledBlockIds">Blocks the startup check found don't match the game, or null for none.</param>
    public static ImportResult Import(
        IReadOnlyList<ImportedBlock> blocks,
        IReadOnlyList<ImportedWire> wires,
        IReadOnlyCollection<string> togglerTargets = null,
        IReadOnlyCollection<int> disabledBlockIds = null)
    {
        var refusal = blocks.Select(b => FindProblem(b, togglerTargets ?? [], disabledBlockIds ?? [])).FirstOrDefault(p => p != null);
        if (refusal != null)
            return ImportResult.Refused(refusal);

        var graph = new ImportGraph(blocks);
        refusal = wires.Select(graph.AddWire).FirstOrDefault(p => p != null);
        if (refusal != null)
            return ImportResult.Refused(refusal);

        refusal = blocks.Select(b => FindRangeProblem(graph, b)).FirstOrDefault(p => p != null);
        if (refusal != null)
            return ImportResult.Refused(refusal);

        var ordered = graph.GetOrder(out var stuck);
        if (ordered == null)
            return ImportResult.Refused($"{Describe(stuck)} is in a loop of Wires, or fed by one");

        var text = ImportText.Write(graph, ordered);
        var compiled = ScriptCompiler.Compile(text, 0, disabledBlockIds);
        refusal = ImportCompare.FindDifference(graph, ordered, compiled);
        return refusal == null ? ImportResult.Written(text) : ImportResult.Refused(refusal);
    }

    /// <summary>
    /// What a refusal calls a block.
    /// </summary>
    internal static string Describe(ImportedBlock block)
    {
        return $"{BlockManifest.GetPlacedType(block.BlockId).Function} \"{block.Uid}\"";
    }

    internal static string DescribeUnwritable(ImportedBlock block, BlockSetting setting)
    {
        return $"{Describe(block)} has a value for {setting.Name} that a Script can't write";
    }

    private static string FindProblem(
        ImportedBlock block,
        IReadOnlyCollection<string> togglerTargets,
        IReadOnlyCollection<int> disabledBlockIds)
    {
        if (block.IsInBlockGroup)
            return $"{Describe(block)} is {MessageFragments.InBlockGroup}";

        if (!BlockManifest.TryGetBlock(block.BlockId, out var type, out var style) || type.IsPlacedOnly)
            return $"a Script can't create {Describe(block)}";

        if (type.Settings.FirstOrDefault(s => s.Type == SettingType.Targets) is { } targets)
            return $"{Describe(block)} has {targets.Name}, which Import can't write";

        if (disabledBlockIds.Contains(style.BlockId))
            return $"{Describe(block)} can't be imported: '{type.Describe(style)}' {MessageFragments.TurnedOff}";

        if (togglerTargets.Contains(block.Uid))
            return $"a Toggler targets {Describe(block)}, and would lose it at the next Compile";

        return type.Settings.Append(BlockSetting.Hide).FirstOrDefault(s => !IsWritable(block, s)) is { } setting
            ? DescribeUnwritable(block, setting)
            : null;
    }

    /// <summary>
    /// Checked before anything asks for the block's outputs, which a mode that isn't one of the options doesn't have.
    /// </summary>
    private static bool IsWritable(ImportedBlock block, BlockSetting setting)
    {
        var stored = block.FindValues(setting);
        return stored.Count == 0 ? !setting.IsRequired : stored.All(s => setting.ToText(s) != null);
    }

    /// <summary>
    /// The same rule a Compile has for a <see cref="BlockType.Range"/>, whose error wouldn't say which block it is.
    /// </summary>
    private static string FindRangeProblem(ImportGraph graph, ImportedBlock block)
    {
        var type = BlockManifest.GetPlacedType(block.BlockId);
        var range = type.Range;
        if (range == null || graph.FindWire(block, type.Inputs[type.IndexOfInput(range.MaximumInput)]) != null)
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
