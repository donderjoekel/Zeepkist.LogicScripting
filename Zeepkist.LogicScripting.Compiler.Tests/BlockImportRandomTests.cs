using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Imports circuits made at random, the same ones each time.
/// </summary>
public class BlockImportRandomTests
{
    private static readonly BlockType[] _types =
    [
        BlockManifest.Generator, BlockManifest.Generator, BlockManifest.Logic, BlockManifest.Logic, BlockManifest.Math, BlockManifest.Math,
        BlockManifest.Digitizer, BlockManifest.Delay, BlockManifest.Delay, BlockManifest.Multiplexer, BlockManifest.Demultiplexer,
        BlockManifest.PlayerInput, BlockManifest.PowerDisplay, BlockManifest.Death
    ];

    private static readonly BlockType[] _outsideTypes = [BlockManifest.Trigger, BlockManifest.Lamp, BlockManifest.DistanceSensor];

    public static IEnumerable<object[]> Seeds()
    {
        return Enumerable.Range(0, 20).Select(i => new object[] { i });
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void ACircuit_ImportsIntoTextThatCompilesToTheSame(int seed)
    {
        var random = new Random(seed);
        for (var i = 0; i < 50; i++)
            AssertImports(random);
    }

    private static void AssertImports(Random random)
    {
        var hidden = random.Next(4);
        List<ImportedBlock> blocks = [.. Enumerable.Range(0, random.Next(1, 30)).Select(i => Create($"b{i}", _types[random.Next(_types.Length)], random, hidden))];
        List<ImportedBlock> outside = [.. Enumerable.Range(0, 4).Select(i => new ImportedBlock($"o{i}", _outsideTypes[i % 3].DefaultStyle.BlockId))];

        List<ImportedWire> wires = [];
        for (var i = 0; i < blocks.Count; i++)
        {
            var type = GetType(blocks[i]);

            // Only a Delay can be fed by a block after it, so every loop has one
            var feeders = type == BlockManifest.Delay ? blocks : blocks.Take(i).ToList();
            AddWires(blocks[i], type, [.. feeders, .. outside], 0.7);
        }

        foreach (var block in outside)
            AddWires(block, GetType(block), blocks, 0.3);

        ImportedBlock[] shuffled = [.. blocks.OrderBy(_ => random.Next())];
        var level = new ImportLevel([.. blocks, .. outside], [.. wires.OrderBy(_ => random.Next())]);
        var import = level.Import([.. shuffled.Select(b => b.Uid)]);

        Assert.Null(import.Refusal);
        var compiled = level.Compile(import);
        Assert.Empty(compiled.Errors);
        var (compiledBlocks, compiledWires) = CompiledSignatures.Read(compiled);
        var wired = wires.Where(w => blocks.Any(b => b.Uid == w.FromUid || b.Uid == w.ToUid)).ToList();
        Assert.Equal(CompiledSignatures.Describe(blocks, wired), CompiledSignatures.Describe(compiledBlocks, compiledWires));

        void AddWires(ImportedBlock to, BlockType type, IReadOnlyList<ImportedBlock> feeders, double chance)
        {
            foreach (var input in type.Inputs)
            {
                var sources = feeders.Where(f => GetType(f).Outputs.Count > 0).ToList();
                if (sources.Count == 0 || random.NextDouble() > chance)
                    continue;

                var from = sources[random.Next(sources.Count)];
                var outputs = GetType(from).Outputs;
                wires.Add(new ImportedWire(from.Uid, outputs[random.Next(outputs.Count)].Index, to.Uid, input.Index));
            }
        }
    }

    /// <summary>
    /// <paramref name="hidden"/> is 0 for all of them, 1 for some and anything else for none.
    /// </summary>
    private static ImportedBlock Create(string uid, BlockType type, Random random, int hidden)
    {
        Dictionary<string, int> ints = [];
        Dictionary<string, string> texts = [];
        if (hidden == 0 || (hidden == 1 && random.Next(3) == 0))
            ints[BlockSetting.Hide.Key] = 1;

        foreach (var setting in type.Settings.Where(s => s.IsRequired || random.Next(3) > 0))
        {
            if (setting.Type == SettingType.Number)
                texts[setting.Key] = Math.Max(setting.Minimum, random.Next(-2, 3)).ToString(CultureInfo.InvariantCulture);
            else
                ints[setting.Key] = random.Next(Math.Min(setting.Values.Count, 12));
        }

        return new ImportedBlock(uid, type.DefaultStyle.BlockId, ints: ints, texts: texts);
    }

    private static BlockType GetType(ImportedBlock block)
    {
        return BlockManifest.GetPlacedType(block.BlockId);
    }
}
