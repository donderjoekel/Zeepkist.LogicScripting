using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// Writes blocks already in the level as the text of a Script, all or nothing: the text compiles to the same blocks,
/// Settings and Wires, or the import is refused.
/// </summary>
public static class BlockImport
{
    private static readonly IReadOnlyList<BlockType> _types = [BlockManifest.Generator, BlockManifest.Logic, BlockManifest.PowerDisplay];

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
        var refusal = blocks.Select(b => FindProblem(b, togglerTargets ?? [])).FirstOrDefault(p => p != null);
        if (refusal != null)
            return ImportResult.Refused(refusal);

        var graph = new ImportGraph(blocks);
        refusal = wires.Select(graph.AddWire).FirstOrDefault(p => p != null);
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

    private static string FindProblem(ImportedBlock block, IReadOnlyCollection<string> togglerTargets)
    {
        if (block.IsInBlockGroup)
            return $"{Describe(block)} is {MessageFragments.InBlockGroup}";

        if (!_types.Contains(BlockManifest.GetPlacedType(block.BlockId)))
            return $"{Describe(block)} isn't a type Import takes: {string.Join(", ", _types.Select(t => t.Function))}";

        return togglerTargets.Contains(block.Uid)
            ? $"a Toggler targets {Describe(block)}, and would lose it at the next Compile"
            : null;
    }
}
