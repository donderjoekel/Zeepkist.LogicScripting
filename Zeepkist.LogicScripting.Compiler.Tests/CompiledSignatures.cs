using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Tells compiled blocks apart by what they are and what's wired into them, loops included, so two Compiles can be
/// compared without the import's own compare.
/// </summary>
internal static class CompiledSignatures
{
    // The same for both, as they can differ in how many Generators they have
    private const int Rounds = 50;

    /// <summary>
    /// The compiled blocks as the level would hold them.
    /// </summary>
    public static (List<ImportedBlock> Blocks, List<ImportedWire> Wires) Read(CompileResult compiled)
    {
        var blocks = compiled.Blocks.Select((block, i) => new ImportedBlock(
            GetUid(i),
            block.Style.BlockId,
            ints: block.Settings.Where(s => s.Kind == SettingKind.Int).ToDictionary(s => s.Key, s => s.IntValue),
            texts: block.Settings.Where(s => s.Kind == SettingKind.Text).ToDictionary(s => s.Key, s => s.TextValue)));
        var wires = compiled.Wires.Concat(compiled.PlacedWires)
            .Select(w => new ImportedWire(GetUid(w.From), w.FromConnector, GetUid(w.To), w.ToConnector));
        return (blocks.ToList(), wires.ToList());
    }

    /// <summary>
    /// One line per block and per Wire into a placed block, sorted. Generators without a Wire into them that are the
    /// same count as one, and a Setting at its default as left out.
    /// </summary>
    public static List<string> Describe(CompileResult compiled)
    {
        var (blocks, wires) = Read(compiled);
        return Describe(blocks, wires);
    }

    public static List<string> Describe(IReadOnlyList<ImportedBlock> blocks, IReadOnlyList<ImportedWire> wires)
    {
        var signatures = blocks.ToDictionary(b => b.Uid, b => $"{b.BlockId}[{string.Join(" ", GetSettings(b))}]");
        for (var round = 0; round < Rounds; round++)
            signatures = blocks.ToDictionary(b => b.Uid, b => Shorten($"{signatures[b.Uid]}({GetInputs(b.Uid, wires, signatures)})"));

        return
        [
            .. blocks.Where(b => b.BlockId != BlockManifest.Generator.DefaultStyle.BlockId).Select(b => signatures[b.Uid])
                .Concat(blocks.Where(b => b.BlockId == BlockManifest.Generator.DefaultStyle.BlockId).Select(b => signatures[b.Uid]).Distinct())
                .Concat(wires.Where(w => !signatures.ContainsKey(w.ToUid)).Select(w => $"{w.ToUid}.{w.ToConnector}<-{w.FromConnector}.{signatures[w.FromUid]}"))
                .OrderBy(s => s, StringComparer.Ordinal)
        ];
    }

    private static string GetInputs(string uid, IReadOnlyList<ImportedWire> wires, Dictionary<string, string> signatures)
    {
        return string.Join(" ", wires
            .Where(w => w.ToUid == uid)
            .OrderBy(w => w.ToConnector)
            .Select(w => $"{w.ToConnector}<-{w.FromConnector}.{signatures.GetValueOrDefault(w.FromUid, w.FromUid)}"));
    }

    private static IEnumerable<string> GetSettings(ImportedBlock block)
    {
        return block.Settings
            .Select(s => (s.Key, Value: s.Kind == SettingKind.Int ? s.IntValue.ToString(CultureInfo.InvariantCulture) : s.TextValue))
            .Where(s => s.Value != "0")
            .Select(s => $"{s.Key}={s.Value}")
            .OrderBy(s => s, StringComparer.Ordinal);
    }

    private static string Shorten(string signature)
    {
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(signature)));
    }

    private static string GetUid(WireEnd end)
    {
        return end.IsPlaced ? end.Uid : GetUid(end.Block);
    }

    private static string GetUid(int index)
    {
        return $"uid{index}";
    }
}
