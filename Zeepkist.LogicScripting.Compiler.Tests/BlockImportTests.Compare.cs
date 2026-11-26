using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class BlockImportTests
{
    [Theory]
    [InlineData("a = generator()\nb = power_display(a)", null)]
    [InlineData("a = generator()", "its text compiles to 1 blocks, not 2")]
    [InlineData("a = generator()\nb = generator()", "its text doesn't give power_display \"d\" its own type and style")]
    [InlineData("a = generator()\nb = power_display(a, style: compact)", "its text doesn't give power_display \"d\" its own type and style")]
    [InlineData("a = generator(power: 2)\nb = power_display(a)", "generator \"g\" has a value for power that a Script can't write")]
    [InlineData("a = generator()\nb = power_display()", "its text misses a Wire into power_display \"d\"")]
    [InlineData("a = generator()\nb = power_display(b)", "its text doesn't compile: 'b' can't refer to itself outside delay(...)")]
    [InlineData("a = generator()\nelsewhere = power_display(a)", "its text compiles to a power_display that isn't one of the blocks")]
    public void TheCompare_CatchesTextThatCompilesToSomethingElse(string text, string difference)
    {
        ImportedBlock[] blocks = [new ImportedBlock("g", GeneratorId), new ImportedBlock("d", DisplayId)];
        var graph = new ImportGraph(blocks);
        Assert.Null(graph.AddWire(new ImportedWire("g", 0, "d", 0)));

        Assert.Equal(difference, Compare(graph, blocks, text, [(1, "g"), (2, "d")], []));
    }

    [Fact]
    public void TheCompare_CatchesAWireTheBlocksDoNotHave()
    {
        ImportedBlock[] blocks = [new ImportedBlock("g", GeneratorId), new ImportedBlock("d", DisplayId)];

        Assert.Equal(
            "its text gives power_display \"d\" a Wire it doesn't have",
            Compare(new ImportGraph(blocks), blocks, "a = generator()\nb = power_display(a)", [(1, "g"), (2, "d")], []));
    }

    [Fact]
    public void TheCompare_CatchesTwoBlocksForOne()
    {
        ImportedBlock[] blocks = [new ImportedBlock("g", GeneratorId), new ImportedBlock("h", GeneratorId)];

        Assert.Equal(
            "its text compiles to several blocks for one",
            Compare(new ImportGraph(blocks), blocks, "a = generator()\nb = generator()", [(1, "g"), (2, "g")], []));
    }

    [Fact]
    public void TheCompare_CatchesTwinsWithEachOthersWires()
    {
        // Each twin alone looks the same, so only the Wires tell them apart
        ImportedBlock[] blocks = [Delay("d"), Delay("e"), new ImportedBlock("p", DisplayId)];
        var graph = new ImportGraph(blocks);
        Assert.Null(graph.AddWire(new ImportedWire("d", 1, "e", 0)));
        Assert.Null(graph.AddWire(new ImportedWire("e", 1, "d", 0)));
        Assert.Null(graph.AddWire(new ImportedWire("d", 1, "p", 0)));
        (int, string)[] places = [(1, "d"), (2, "e"), (3, "p")];

        Assert.Null(Compare(graph, blocks, "a = delay(b, ticks: 5)\nb = delay(a, ticks: 5)\nc = power_display(a)", places, []));
        Assert.Equal(
            "its text gives delay \"d\" a Wire it doesn't have",
            Compare(graph, blocks, "a = delay(a, ticks: 5)\nb = delay(b, ticks: 5)\nc = power_display(a)", places, []));
        Assert.Equal(
            "its text gives power_display \"p\" a Wire it doesn't have",
            Compare(graph, blocks, "a = delay(b, ticks: 5)\nb = delay(a, ticks: 5)\nc = power_display(b)", places, []));
    }

    [Theory]
    [InlineData("5", "5", "a = generator(power: 5)\nb = logic(a, a)", null)]
    [InlineData("5", "6", "a = generator(power: 5)\nb = logic(a, a)", "generator \"h\" has a value for power that a Script can't write")]
    [InlineData("5", "5", "a = generator(power: 5)\nb = logic(a)", "its text misses a Wire into logic \"l\"")]
    [InlineData("5", "5", "a = generator(power: 5)\nb = logic(a, a, a)", "its text gives logic \"l\" a Wire it doesn't have")]
    public void TheCompare_AllowsSharedGeneratorsOfTheSamePower(string power, string otherPower, string text, string difference)
    {
        ImportedBlock[] blocks =
        [
            new ImportedBlock("g", GeneratorId, texts: Texts(("lbp", power))),
            new ImportedBlock("h", GeneratorId, texts: Texts(("lbp", otherPower))),
            new ImportedBlock("l", LogicId)
        ];
        var graph = new ImportGraph(blocks);
        Assert.Null(graph.AddWire(new ImportedWire("g", 0, "l", 0)));
        Assert.Null(graph.AddWire(new ImportedWire("h", 0, "l", 1)));

        Assert.Equal(difference, Compare(graph, blocks, text, [(1, "g"), (2, "l")], new Dictionary<string, string> { ["h"] = "g" }));
    }

    [Fact]
    public void TheCompare_AllowsNoOtherBlockToBeShared()
    {
        ImportedBlock[] blocks = [new ImportedBlock("d", DisplayId), new ImportedBlock("e", DisplayId)];

        Assert.Equal(
            "its text shares power_display \"e\", and only Generators can be shared",
            Compare(new ImportGraph(blocks), blocks, "a = power_display()", [(1, "d")], new Dictionary<string, string> { ["e"] = "d" }));
    }

    /// <summary>
    /// Compares the text as if an import wrote it, each place being where a line's one-letter name is given a block.
    /// </summary>
    private static string Compare(
        ImportGraph graph,
        IReadOnlyList<ImportedBlock> blocks,
        string text,
        IReadOnlyList<(int Line, string Uid)> places,
        Dictionary<string, string> shared)
    {
        var imported = new ImportedText(
            [.. text.Split('\n').Select(l => new ImportLine(null, l))],
            places.ToDictionary(p => (new Position(p.Line, 5), graph.GetType(p.Uid)), p => p.Uid),
            shared);
        return ImportCompare.FindDifference(graph, blocks, imported, ScriptCompiler.Compile(text, 0));
    }
}
