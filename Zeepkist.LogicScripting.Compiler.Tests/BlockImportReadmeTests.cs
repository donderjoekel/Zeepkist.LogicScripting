using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Imports the compiled blocks of every Script example in the README.
/// </summary>
public class BlockImportReadmeTests
{
    private const string Fence = "```";
    private const string ScriptFence = Fence + "logic";
    private const string LibraryStart = "# In a Library called timing\n";
    private const int WallId = 1;

    public static IEnumerable<object[]> Examples()
    {
        var examples = ReadExamples().Where(e => !e.StartsWith(LibraryStart, StringComparison.Ordinal)).ToList();
        for (var i = 0; i < examples.Count; i++)
        {
            // A line shown on its own goes on from the example before it
            var text = Compile(examples[i]).Succeeded || i == 0 ? examples[i] : $"{examples[i - 1]}\n{examples[i]}";
            yield return [text];
        }
    }

    [Fact]
    public void TheReadme_HasItsExamples()
    {
        Assert.InRange(Examples().Count(), 15, 100);
        Assert.Single(ReadExamples(), e => e.StartsWith(LibraryStart, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public void EachExample_ImportsIntoTextThatCompilesToTheSame(string example)
    {
        var compiled = Compile(example);
        Assert.Empty(compiled.Errors);
        var (blocks, wires) = CompiledSignatures.Read(compiled);
        var outside = compiled.PlacedBlockReferences.Select(r => r.Uid).Distinct().Select(u => new ImportedBlock(u, GetPlacedId(u)));
        var level = new ImportLevel([.. blocks, .. outside], [.. wires]);

        if (blocks.Count == 0)
            return;

        var import = level.Import([.. blocks.Select(b => b.Uid)]);

        Assert.Null(import.Refusal);
        var again = level.Compile(import);
        Assert.Empty(again.Errors);
        Assert.Equal(CompiledSignatures.Describe(compiled), CompiledSignatures.Describe(again));
        Assert.Equal(import.Text, level.Import([.. blocks.Select(b => b.Uid)]).Text);
    }

    [Fact]
    public void ADoorThatOpensWhileSomeoneStandsInATrigger()
    {
        AssertExample(
            "toggler(trigger_1 > 0, targets: [wall_1])",
            """
            default hide: true

            trigger_1 = placed("trigger-uid")
            block_1 = placed("wall-uid")

            toggler(trigger_1 > 0, targets: [block_1])
            """);
    }

    [Fact]
    public void OpenAWallAfterThreeLaps()
    {
        AssertExample(
            "laps = counter(finish_1 > 0)",
            """
            default hide: true

            trigger_1 = placed("trigger-finish-uid")
            block_1 = placed("wall-uid")

            counter_1 = counter(trigger_1 > 0)
            toggler(counter_1 >= 3, targets: [block_1])
            power_display(counter_1)
            """);
    }

    [Fact]
    public void TwoTriggersInOrder()
    {
        AssertExample(
            "armed = simple_memory",
            """
            default hide: true

            trigger_1 = placed("trigger-1-uid")
            trigger_2 = placed("trigger-2-uid")
            block_1 = placed("wall-uid")

            logic_1 = simple_memory(1, trigger_1 > 0) & trigger_2 > 0
            toggler(simple_memory(1, logic_1), targets: [block_1])
            """);
    }

    private static void AssertExample(string line, string expected)
    {
        var compiled = Compile(ReadExamples().Single(e => e.Contains(line, StringComparison.Ordinal)));
        var (blocks, wires) = CompiledSignatures.Read(compiled);
        var outside = compiled.PlacedBlockReferences.Select(r => new ImportedBlock(r.Uid, GetPlacedId(r.Uid)));

        var import = new ImportLevel([.. blocks, .. outside], [.. wires]).Import([.. blocks.Select(b => b.Uid)]);

        Assert.Equal(expected.Replace("\r\n", "\n"), import.Text);
    }

    private static CompileResult Compile(string text)
    {
        return ScriptCompiler.Compile(
            text,
            0,
            null,
            uid => new PlacedBlock(GetPlacedId(uid)),
            name => name == "timing" ? ReadExamples().Single(e => e.StartsWith(LibraryStart, StringComparison.Ordinal)) : null);
    }

    /// <summary>
    /// The README's UIDs start with what the block is.
    /// </summary>
    private static int GetPlacedId(string uid)
    {
        return BlockManifest.TryGetPlacedOnly(uid.Split('-')[0], out var type) ? type.DefaultStyle.BlockId : WallId;
    }

    private static List<string> ReadExamples([CallerFilePath] string file = null)
    {
        List<string> examples = [];
        List<string> open = null;
        var indent = 0;
        foreach (var line in File.ReadAllLines(Path.Combine(Path.GetDirectoryName(file), "..", "README.md")))
        {
            if (open == null && line.TrimStart() == ScriptFence)
            {
                indent = line.IndexOf(Fence, StringComparison.Ordinal);
                open = [];
            }
            else if (open != null && line.TrimStart() == Fence)
            {
                examples.Add(string.Join("\n", open));
                open = null;
            }
            else
            {
                open?.Add(line.Length < indent ? line : line.Substring(indent));
            }
        }

        return examples;
    }
}
