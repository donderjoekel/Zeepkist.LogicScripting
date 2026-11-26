using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class BlockImportOutsideTests
{
    [Fact]
    public void ALoopThroughAnOutsideBlock_WithADelay_IsWritten()
    {
        var level = new ImportLevel(
            [Logic(), new ImportedBlock("p", PassthroughId), Delay()],
            new ImportedWire("l", 3, "p", 0),
            new ImportedWire("p", 1, "d", 0),
            new ImportedWire("d", 1, "l", 0));

        AssertImports(
            level,
            "passthrough_1 = placed(\"p\")\n\npassthrough_1(logic(delay(passthrough_1, ticks: 1)))",
            "l",
            "d");
    }

    [Fact]
    public void ALoopThroughAnOutsideDelay_IsWritten()
    {
        var level = new ImportLevel([Logic(), Delay()], new ImportedWire("l", 3, "d", 0), new ImportedWire("d", 1, "l", 0));

        AssertImports(level, "delay_1 = placed(\"d\")\n\ndelay_1(logic(delay_1))", "l");
    }

    [Fact]
    public void ADependencyLoopThroughAnOutsideBlock_IsRefused()
    {
        var level = new ImportLevel(
            [Logic(), new ImportedBlock("p", PassthroughId)],
            new ImportedWire("l", 3, "p", 0),
            new ImportedWire("p", 1, "l", 0));

        AssertRefused(
            level,
            "its text doesn't compile at passthrough \"p\": This Wire makes a Dependency Loop, which stops all logic in play. Put a delay(...) in the loop",
            "l");
    }

    [Fact]
    public void ADependencyLoopThroughWiresBetweenOutsideBlocks_IsRefused()
    {
        ImportedBlock[] blocks =
        [
            Logic("a"), new ImportedBlock("p", PassthroughId), new ImportedBlock("mid", PassthroughId),
            new ImportedBlock("q", PassthroughId), Logic("b")
        ];
        var level = new ImportLevel(
            blocks,
            new ImportedWire("a", 3, "p", 0),
            new ImportedWire("p", 1, "mid", 0),
            new ImportedWire("mid", 1, "q", 0),
            new ImportedWire("q", 1, "b", 0),
            new ImportedWire("b", 3, "a", 0));

        AssertRefused(
            level,
            "its text doesn't compile at passthrough \"p\": This Wire makes a Dependency Loop, which stops all logic in play. Put a delay(...) in the loop",
            "a",
            "b");
    }

    [Fact]
    public void ALatchBetweenOutsideBlocks_RefersAheadOnlyInItsDelay()
    {
        var level = new ImportLevel(
            [Trigger(), Logic(), Delay(), Lamp()],
            new ImportedWire("t", 0, "l", 0),
            new ImportedWire("d", 1, "l", 1),
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("l", 3, "lamp", 0),
            new ImportedWire("d", 1, "lamp", 1));

        AssertImports(
            level,
            """
            trigger_1 = placed("t")
            lamp_1 = placed("lamp")

            delay_1 = delay(logic_1, ticks: 1)
            logic_1 = trigger_1 & delay_1
            lamp_1(logic_1, delay_1)
            """.Replace("\r\n", "\n"),
            "l",
            "d");
    }

    [Fact]
    public void ALoopThroughAnOutsideBlock_AndOneInsideTheImport_AreWritten()
    {
        var level = new ImportLevel(
            [Logic(), Delay(), new ImportedBlock("p", PassthroughId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("d", 1, "p", 0),
            new ImportedWire("p", 1, "l", 1));

        AssertImports(
            level,
            """
            passthrough_1 = placed("p")

            delay_1 = delay(delay_1 & passthrough_1, ticks: 1)
            passthrough_1(delay_1)
            """.Replace("\r\n", "\n"),
            "l",
            "d");
    }

    [Fact]
    public void ADependencyLoopThroughAnOutsideBlock_NextToALoopThroughADelay_IsRefused()
    {
        var level = new ImportLevel(
            [Logic(), Delay(), new ImportedBlock("p", PassthroughId)],
            new ImportedWire("l", 3, "d", 0),
            new ImportedWire("d", 1, "l", 0),
            new ImportedWire("l", 3, "p", 0),
            new ImportedWire("p", 1, "l", 1));

        AssertRefused(
            level,
            "its text doesn't compile at passthrough \"p\": This Wire makes a Dependency Loop, which stops all logic in play. Put a delay(...) in the loop",
            "l",
            "d");
    }

    [Fact]
    public void ADependencyLoopInsideTheImport_IsRefusedWithoutTheOutsideBlocks()
    {
        var level = new ImportLevel(
            [Trigger(), Logic("a"), Logic("b"), Lamp()],
            new ImportedWire("t", 0, "a", 0),
            new ImportedWire("a", 3, "b", 0),
            new ImportedWire("b", 3, "a", 1),
            new ImportedWire("b", 3, "lamp", 0));

        AssertRefused(level, "a loop of Wires has no Delay in it: logic \"b\", logic \"a\"", "a", "b", "lamp");
    }
}
