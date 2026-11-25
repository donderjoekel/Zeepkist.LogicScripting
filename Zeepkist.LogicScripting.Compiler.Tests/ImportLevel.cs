using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// A level to import selected blocks from, which finds placed blocks in it the way the mod does.
/// </summary>
internal sealed class ImportLevel
{
    private readonly IReadOnlyList<ImportedBlock> _blocks;
    private readonly IReadOnlyList<ImportedWire> _wires;

    public ImportLevel(IReadOnlyList<ImportedBlock> blocks, params ImportedWire[] wires)
    {
        _blocks = blocks;
        _wires = wires;
    }

    public ImportResult Import(params string[] selected)
    {
        return Import(selected, []);
    }

    public ImportResult Import(IReadOnlyList<string> selected, IReadOnlyCollection<int> disabledBlockIds)
    {
        return BlockImport.Import(
            [.. selected.Select(uid => _blocks.First(b => b.Uid == uid))],
            [.. _wires.Where(w => selected.Contains(w.FromUid) || selected.Contains(w.ToUid))],
            [.. _blocks.SelectMany(GetTargets)],
            disabledBlockIds,
            Find);
    }

    /// <summary>
    /// What the text compiles to while <paramref name="import"/>'s blocks are still in the level.
    /// </summary>
    public CompileResult Compile(ImportResult import)
    {
        return ScriptCompiler.Compile(import.Text, 0, null, uid => Find(uid, import.ImportedUids));
    }

    private PlacedBlock Find(string uid, IReadOnlyCollection<string> replaced)
    {
        var block = _blocks.FirstOrDefault(b => b.Uid == uid);
        if (block == null)
            return null;

        return new PlacedBlock(
            block.BlockId,
            block.Settings,
            [.. _wires.Where(w => w.ToUid == uid && !replaced.Contains(w.FromUid)).Select(w => w.ToConnector)],
            GetDownstream(uid, replaced),
            block.IsInBlockGroup);
    }

    private HashSet<string> GetDownstream(string uid, IReadOnlyCollection<string> replaced)
    {
        HashSet<string> reached = [];
        Stack<string> open = new([uid]);
        while (open.Count > 0)
        {
            var from = open.Pop();
            foreach (var to in _wires.Where(w => w.FromUid == from).Select(w => w.ToUid))
            {
                if (replaced.Contains(to) || !reached.Add(to))
                    continue;

                if (BlockManifest.GetPlacedType(_blocks.First(b => b.Uid == to).BlockId) != BlockManifest.Delay)
                    open.Push(to);
            }
        }

        return reached;
    }

    private static IEnumerable<string> GetTargets(ImportedBlock block)
    {
        return BlockManifest.GetPlacedType(block.BlockId) == BlockManifest.Toggler
            ? block.Settings.Where(s => s.Key.StartsWith("xuid", System.StringComparison.Ordinal)).Select(s => s.TextValue)
            : [];
    }
}
