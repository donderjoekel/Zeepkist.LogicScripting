using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class BlockImportTests
{
    [Fact]
    public void ADelayThatFeedsItself_IsWrittenWithItsOwnName()
    {
        AssertText(
            "delay_1 = delay(delay_1, ticks: 5)",
            [Delay("d")],
            new ImportedWire("d", 1, "d", 0));
    }

    [Fact]
    public void TwoDelaysFeedingEachOther_OnlyTheFirstHasAName()
    {
        AssertText(
            """
            delay_1 = delay(delay(delay_1, ticks: 6), ticks: 5)
            """,
            [Delay("d", 5), Delay("e", 6)],
            new ImportedWire("d", 1, "e", 0),
            new ImportedWire("e", 1, "d", 0));
    }

    [Fact]
    public void ALatchThroughSeveralBlocks_RefersAheadOnlyInItsDelay()
    {
        AssertText(
            """
            delay_1 = delay(logic_2, ticks: 5)
            logic_1 = 0 | delay_1
            logic_2 = logic_1 & logic_1
            power_display(logic_2)
            """,
            [
                new ImportedBlock("p", DisplayId),
                new ImportedBlock("m", LogicId),
                new ImportedBlock("l", LogicId, ints: Ints(("xmo", 1))),
                Delay("d"),
                new ImportedBlock("g", GeneratorId)
            ],
            new ImportedWire("g", 0, "l", 0),
            new ImportedWire("d", 1, "l", 1),
            new ImportedWire("l", 3, "m", 0),
            new ImportedWire("l", 3, "m", 1),
            new ImportedWire("m", 3, "d", 0),
            new ImportedWire("m", 3, "p", 0));
    }

    [Fact]
    public void OneDelayFeedingSeveralLoops_RefersAheadOnce()
    {
        AssertText(
            """
            delay_1 = delay(logic(delay_1) & logic(delay_1, op: or), ticks: 5)
            """,
            [
                new ImportedBlock("a", LogicId),
                new ImportedBlock("b", LogicId, ints: Ints(("xmo", 1))),
                new ImportedBlock("c", LogicId),
                Delay("d")
            ],
            new ImportedWire("d", 1, "a", 0),
            new ImportedWire("d", 1, "b", 0),
            new ImportedWire("a", 3, "c", 0),
            new ImportedWire("b", 3, "c", 1),
            new ImportedWire("c", 3, "d", 0));
    }

    [Fact]
    public void ADelayFedByALoop_ComesAfterIt()
    {
        AssertText(
            """
            delay_1 = delay(logic_1, ticks: 5)
            logic_1 = logic(delay_1)
            delay(delay(logic_1, ticks: 6), ticks: 7)
            """,
            [Delay("f", 7), Delay("e", 6), Delay("d"), new ImportedBlock("l", LogicId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("l", 3, "e", 0),
            new ImportedWire("e", 1, "f", 0));
    }

    [Fact]
    public void ADelayThatAnotherOneTakesOutOfItsLoops_NeedsNoName()
    {
        AssertText(
            """
            delay_1 = delay(delay(logic(delay_1), ticks: 6), ticks: 5)
            """,
            [Delay("d"), Delay("e", 6), new ImportedBlock("l", LogicId)],
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("l", 3, "e", 0),
            new ImportedWire("e", 1, "d", 0));
    }

    [Fact]
    public void AnOutputOfSeveral_CanBeReferredAheadTo()
    {
        Assert.True(BlockManifest.TryGetFunction("demultiplexer", out var type));
        var output = type.Outputs[1];
        AssertText(
            $"""
            delay_1 = delay({output.Name}_1, ticks: 5)
            {output.Name}_1 = demultiplexer(delay_1).{output.Name}
            """,
            [new ImportedBlock("m", type.DefaultStyle.BlockId), Delay("d")],
            new ImportedWire("d", 1, "m", type.Inputs[0].Index),
            new ImportedWire("m", output.Index, "d", 0));
    }

    [Fact]
    public void LoopsWithWiresInAnyOrder_GiveTheSameText()
    {
        ImportedBlock[] blocks = [Delay("d"), Delay("e", 6), new ImportedBlock("l", LogicId), new ImportedBlock("m", LogicId)];
        ImportedWire[] wires =
        [
            new("d", 1, "l", 0), new("e", 1, "l", 1), new("l", 3, "m", 0), new("m", 3, "d", 0), new("l", 3, "e", 0)
        ];

        var import = BlockImport.Import(blocks, wires);

        Assert.Null(import.Refusal);
        for (var i = 1; i < wires.Length; i++)
            Assert.Equal(import.Text, BlockImport.Import(blocks, [.. wires.Skip(i), .. wires.Take(i)]).Text);

        Assert.Equal(import.Text, BlockImport.Import(blocks, [.. Enumerable.Reverse(wires)]).Text);
    }

    [Fact]
    public void ADependencyLoop_IsRefused_NamingItsBlocksInTheDirectionOfTheWires()
    {
        AssertRefused(
            "a loop of Wires has no Delay in it: logic \"l\", power_display \"d\", power_display \"e\"",
            [new ImportedBlock("e", DisplayId), new ImportedBlock("l", LogicId), new ImportedBlock("d", DisplayId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "e", 0),
            new ImportedWire("e", 1, "l", 0));
    }

    [Fact]
    public void ABlockThatFeedsItself_IsADependencyLoop()
    {
        AssertRefused(
            "a loop of Wires has no Delay in it: logic \"l\"",
            [new ImportedBlock("l", LogicId)],
            new ImportedWire("l", 3, "l", 0));
    }

    [Fact]
    public void ADependencyLoopsRefusal_LeavesOutWhatIsOnlyFedByIt()
    {
        AssertRefused(
            "a loop of Wires has no Delay in it: power_display \"p\", logic \"l\"",
            [Delay("d"), new ImportedBlock("q", DisplayId), new ImportedBlock("l", LogicId), new ImportedBlock("p", DisplayId)],
            new ImportedWire("l", 3, "p", 0),
            new ImportedWire("p", 1, "l", 0),
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "q", 0));
    }

    [Fact]
    public void ADependencyLoopNextToALoopThroughADelay_IsRefusedWithoutTheDelay()
    {
        AssertRefused(
            "a loop of Wires has no Delay in it: logic \"m\", logic \"l\"",
            [Delay("d"), new ImportedBlock("l", LogicId), new ImportedBlock("m", LogicId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("l", 3, "m", 0),
            new ImportedWire("m", 3, "l", 1));
    }
}
