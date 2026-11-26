using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class BlockImportTextTests
{
    [Fact]
    public void ABlockUsedOnce_MovesIntoTheCallThatUsesIt()
    {
        AssertText(
            """
            a = ticker()
            b = pulser(a, mode: fall)
            c = counter(b, reset: 3)
            power_display(c, style: compact)
            """,
            "power_display(counter(pulser(ticker(), mode: fall), reset: 3), style: compact)");
    }

    [Fact]
    public void ABlockUsedTwice_OrWithSeveralOutputs_KeepsItsLine()
    {
        AssertText(
            """
            a = ticker()
            x = a + a
            p = player_input(player: 2)
            power_display(p.left | p.right | x)
            power_display(p.horn)
            """,
            """
            ticker_1 = ticker()
            player_input_1 = player_input(player: 2)
            power_display(player_input_1.left | player_input_1.right | ticker_1 + ticker_1)
            power_display(player_input_1.horn)
            """);
    }

    [Fact]
    public void ALineStaysReadable()
    {
        var text = "power_display(" + string.Join(" + ", Enumerable.Repeat("pulser(ticker())", 12)) + ")";

        var lines = Import(text).Text.Split('\n');

        Assert.True(lines.Length > 1);
        Assert.All(lines, l => Assert.InRange(l.Length, 1, ImportText.MaxNestedLength));
        Assert.Contains(lines, l => l.Length > ImportText.MaxNestedLength / 2);
    }

    [Fact]
    public void ALongChainOfShortOperators_NestsNoDeeperThanAScriptCan()
    {
        var lines = Enumerable.Range(1, 200).Select(i => $"x{i} = !x{i - 1}");

        var import = Import($"x0 = ticker()\n{string.Join("\n", lines)}\npower_display(x200)");

        Assert.Contains(new string('!', 20), import.Text);
        Assert.True(ScriptCompiler.Compile(import.Text, 0).Succeeded);
        Assert.Equal(202, ScriptCompiler.Compile(import.Text, 0).Blocks.Count);
        Assert.True(Parser.MaxDepth < 200);
    }

    [Fact]
    public void AnOperatorNothingUses_HasAName_AndACallHasNone()
    {
        AssertText("x = 1 + 2\ny = delay(3, ticks: 1)", "math_1 = 1 + 2\n\ndelay(3, ticks: 1)");
    }

    [Fact]
    public void TheOnlyOutputUsed_GivesTheName()
    {
        AssertText(
            """
            p = player_input(player: 2)
            q = player_input(player: 3)
            t = simple_telemetry(mode: global_velocity)
            power_display(p.horn & p.horn)
            power_display(q.horn)
            power_display(t.heading)
            """,
            """
            horn_1 = player_input(player: 2).horn
            power_display(horn_1 & horn_1)

            horn_2 = player_input(player: 3).horn
            power_display(horn_2)

            heading_1 = simple_telemetry(mode: global_velocity).heading
            power_display(heading_1)
            """);
    }

    [Fact]
    public void AnOutsideBlocksOutput_StaysOnItsName()
    {
        AssertText(
            "s = placed(\"sensor\")\npower_display(s.y)",
            "distance_sensor_1 = placed(\"sensor\")\n\npower_display(distance_sensor_1.y)");
    }

    [Fact]
    public void SeparateCircuits_HaveABlankLineBetween_AndALiteralConnectsNone()
    {
        AssertText(
            """
            a = ticker() > 1
            b = resettable_ticker() > 1
            power_display(a)
            power_display(b)
            power_display(b)
            power_display(a)
            """,
            """
            math_1 = ticker() > 1
            power_display(math_1)
            power_display(math_1)

            math_2 = resettable_ticker() > 1
            power_display(math_2)
            power_display(math_2)
            """);
    }

    [Fact]
    public void AnOutsideBlock_ConnectsACircuit()
    {
        AssertText(
            """
            t = placed("trigger")
            l = placed("lamp")
            power_display(t)
            l(red: !t)
            l(blue: ticker())
            power_display(1)
            """,
            """
            trigger_1 = placed("trigger")
            lamp_1 = placed("lamp")

            power_display(trigger_1)
            lamp_1(red: !trigger_1, blue: ticker())

            power_display(1)
            """);
    }

    [Fact]
    public void NumbersFollowTheLines_OutsideBlocksFirst()
    {
        var level = new ImportLevel(
            [new("out", LogicId), new("a", LogicId), new("b", LogicId), new("c", DisplayId), new("d", DisplayId), new("e", DisplayId), new("f", DisplayId)],
            new ImportedWire("out", 3, "a", 0),
            new ImportedWire("a", 3, "b", 0),
            new ImportedWire("b", 3, "c", 0),
            new ImportedWire("b", 3, "d", 0));

        Assert.Equal(
            """
            logic_1 = placed("out")

            logic_2 = logic(logic(logic_1))
            power_display(logic_2)
            power_display(logic_2)

            power_display()

            power_display()
            """.Replace("\r\n", "\n"),
            level.Import("a", "b", "c", "d", "e", "f").Text);
    }

    [Fact]
    public void ADelayLoop_NestsIntoTheDelayThatRefersAhead()
    {
        AssertText(
            """
            set = ticker() > 100
            on = delay(hold, ticks: 1)
            hold = on | set
            """,
            "delay_1 = delay(delay_1 | ticker() > 100, ticks: 1)");
    }

    [Fact]
    public void ANestedBlock_CanReferAheadInsideItsDelay()
    {
        AssertText(
            """
            a = delay(x & y, ticks: 1)
            x = !a
            y = digitizer(x)
            power_display(x)
            power_display(y)
            """,
            """
            delay_1 = delay(digitizer_1 & digitizer_2, ticks: 1)
            digitizer_1 = !delay_1
            digitizer_2 = digitizer(digitizer_1)
            power_display(digitizer_1)
            power_display(digitizer_2)
            """);
    }

    [Fact]
    public void TheSameBlocks_GiveTheSameText_WhateverOrderTheWiresComeIn()
    {
        var (blocks, wires) = Read(ScriptCompiler.Compile(
            """
            p = player_input(player: 1)
            on = delay(on ^ pulser(p.horn), ticks: 1)
            power_display(on & p.left > 3)
            power_display(on + 3)
            a = delay(b, ticks: 2)
            b = delay(a, ticks: 3)
            power_display(3)
            """,
            0));
        var level = new ImportLevel(blocks, [.. wires]);
        var text = level.Import([.. blocks.Select(b => b.Uid)]).Text;

        Assert.NotNull(text);
        for (var i = 0; i < wires.Count; i++)
        {
            ImportedWire[] turned = [.. wires.Skip(i), .. wires.Take(i)];
            Assert.Equal(text, BlockImport.Import(blocks, turned).Text);
            Assert.Equal(text, BlockImport.Import(blocks, [.. Enumerable.Reverse(turned)]).Text);
        }
    }

    [Fact]
    public void ImportingTheImportedText_GivesTheSameText()
    {
        var text = Import(
            """
            x = (1 + 2) * ticker()
            on = delay(on ^ x, ticks: 4)
            power_display(on)
            power_display(x)
            power_display(1)
            """).Text;

        Assert.Equal(text, Import(text).Text);
    }
}
