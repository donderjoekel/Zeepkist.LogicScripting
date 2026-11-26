using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// The text an import writes, from the blocks a Script compiles to.
/// </summary>
public partial class BlockImportTextTests
{
    private const int TriggerId = 2413;
    private const int GeneratorId = 2414;
    private const int DisplayId = 2415;
    private const int LogicId = 2420;
    private const int LampId = 2448;
    private const int SensorId = 2442;

    [Fact]
    public void TheReadmesLatch()
    {
        AssertText(
            """
            p = player_input(player: 1)
            press = pulser(p.horn)
            on = delay(on ^ press, ticks: 1)
            power_display(on)
            """,
            """
            horn_1 = player_input().horn
            delay_1 = delay(delay_1 ^ pulser(horn_1), ticks: 1)
            power_display(delay_1)
            """);
    }

    [Fact]
    public void ALampFromATrigger()
    {
        AssertText(
            """
            t = placed("trigger")
            l = placed("lamp")
            l(red: 255, green: (t > 0) * 255)
            """,
            """
            trigger_1 = placed("trigger")
            lamp_1 = placed("lamp")

            lamp_1(red: 255, green: (trigger_1 > 0) * 255)
            """);
    }

    [Theory]
    [InlineData("(a | b) & c")]
    [InlineData("a | b & c")]
    [InlineData("a ^ b | c & a")]
    [InlineData("a - (b - c)")]
    [InlineData("a - b - c")]
    [InlineData("a * (b + c) / a")]
    [InlineData("a / (b * c)")]
    [InlineData("a + b * c - a % b")]
    [InlineData("a == b == c")]
    [InlineData("a == (b == c)")]
    [InlineData("a < b == c >= a")]
    [InlineData("a <= b != c")]
    [InlineData("(a & b) + c")]
    [InlineData("!a & !(b | c)")]
    [InlineData("!!a")]
    [InlineData("-a * b")]
    [InlineData("-(a * b)")]
    [InlineData("--a")]
    [InlineData("a - -b")]
    [InlineData("-!a")]
    [InlineData("!-a")]
    [InlineData("a - -5")]
    [InlineData("-5 - a")]
    [InlineData("!-5")]
    [InlineData("0 - 5")]
    [InlineData("0 - -5")]
    [InlineData("a - 0")]
    [InlineData("0 + a")]
    [InlineData("-a - 0")]
    [InlineData("math(a, b, op: min) + logic(c, op: nand)")]
    [InlineData("multiplexer(a | b, select: -c)")]
    [InlineData("delay(a & b, ticks: 3) | c")]
    public void OperatorsHaveParenthesesOnlyWherePrecedenceNeedsThem(string expression)
    {
        // Only names used several times stay names, so each is displayed too
        var text = $"a = ticker()\nb = timer()\nc = pulser()\nx = {expression}\n"
            + "power_display(a)\npower_display(a)\npower_display(b)\npower_display(b)\npower_display(c)\npower_display(c)\n"
            + "power_display(x)\npower_display(x)";

        var lines = Import(text).Text.Split('\n');

        var names = new Dictionary<string, string> { ["a"] = "ticker_1", ["b"] = "timer_1", ["c"] = "pulser_1" };
        var expected = string.Concat(System.Text.RegularExpressions.Regex.Split(expression, @"\b([abc])\b").Select(p => names.GetValueOrDefault(p, p)));
        Assert.Contains(lines, l => l.EndsWith($" = {expected}", System.StringComparison.Ordinal));
    }

    [Fact]
    public void Parentheses_ThatTheTextDidNotNeed_AreLeftOut()
    {
        AssertText("power_display((1 + (2 * 3)) - ((4)))", "power_display(1 + 2 * 3 - 4)");
        AssertText("power_display((!(1)) | (-(2 > 1)))", "power_display(!1 | -(2 > 1))");
    }

    [Theory]
    [InlineData("power_display(logic(1, 2, 3))")]
    [InlineData("power_display(logic(1))")]
    [InlineData("power_display(logic(b: 2, op: or))")]
    [InlineData("power_display(logic(1, 2, op: nand))")]
    [InlineData("power_display(math(1, 2, op: sin))")]
    [InlineData("power_display(math(1, 2, hide: true))")]
    [InlineData("power_display(digitizer(1))")]
    [InlineData("power_display(digitizer(invert: true))")]
    [InlineData("power_display(digitizer(1, invert: true, hide: true))")]
    [InlineData("power_display(generator(power: 5, hide: true))")]
    [InlineData("generator(power: 5)")]
    [InlineData("power_display(math(generator(hide: true), 1, op: subtract, hide: true))")]
    public void WhatAnOperatorOrLiteralDoesNotGive_StaysACall(string text)
    {
        AssertText(text, text);
    }

    [Fact]
    public void AHiddenGenerator_IsNoLiteralForAMinusSign()
    {
        AssertText("power_display(generator(hide: true) - ticker())", "power_display(generator(hide: true) - ticker())");
    }

    [Fact]
    public void BlocksThatAreAllHidden_AreOperatorsAndLiterals()
    {
        AssertText("default hide: true\npower_display(!(1 + 2))", "default hide: true\n\npower_display(!(1 + 2))");
    }

    [Fact]
    public void GeneratorsOfTheSamePower_BecomeOne()
    {
        ImportedBlock[] blocks =
        [
            Generator("g", "5"), Generator("h", "05"), Generator("i", "0"), Generator("j"),
            new("k", GeneratorId, texts: new Dictionary<string, string> { ["lbp"] = "5" }, ints: new Dictionary<string, int> { ["lbhd"] = 1 }),
            new("l", LogicId), new("m", LogicId), new("d", DisplayId)
        ];
        ImportedWire[] wires =
        [
            new("g", 0, "l", 0), new("h", 0, "l", 1), new("i", 0, "m", 0), new("j", 0, "m", 1), new("k", 0, "d", 0)
        ];

        var import = BlockImport.Import(blocks, wires);

        Assert.Null(import.Refusal);
        Assert.Equal("logic_1 = 5 & 5\n\nlogic_2 = 0 & 0\n\npower_display(generator(power: 5, hide: true))", import.Text);
        Assert.Equal(6, ScriptCompiler.Compile(import.Text, 0).Blocks.Count);
        Assert.Equal(8, import.ImportedUids.Count);
    }

    [Theory]
    [InlineData("9223372036854775807")]
    [InlineData("-9223372036854775808")]
    [InlineData("-1")]
    [InlineData("0")]
    public void ALiteral_IsAnyNumberASignalHolds(string power)
    {
        AssertText($"power_display(generator(power: {power}))", $"power_display({power})");
    }

    private static void AssertText(string script, string expected)
    {
        Assert.Equal(expected.Replace("\r\n", "\n"), Import(script).Text);
    }

    /// <summary>
    /// Imports everything the Script compiles to, and checks that the text compiles to the same.
    /// </summary>
    private static ImportResult Import(string script)
    {
        var compiled = ScriptCompiler.Compile(script, 0, null, FindPlaced);
        Assert.Empty(compiled.Errors);
        var (blocks, wires) = Read(compiled);
        var uids = blocks.Select(b => b.Uid).ToList();
        var level = new ImportLevel([.. blocks, .. new[] { "trigger", "lamp", "sensor" }.Select(u => new ImportedBlock(u, FindPlaced(u).BlockId))], [.. wires]);

        var import = level.Import([.. uids]);

        Assert.Null(import.Refusal);
        var again = level.Compile(import);
        Assert.Empty(again.Errors);
        Assert.Equal(CompiledSignatures.Describe(compiled), CompiledSignatures.Describe(again));
        return import;
    }

    private static PlacedBlock FindPlaced(string uid)
    {
        return new PlacedBlock(uid switch
        {
            "trigger" => TriggerId,
            "lamp" => LampId,
            _ => SensorId
        });
    }

    private static ImportedBlock Generator(string uid, string power = null)
    {
        return new ImportedBlock(uid, GeneratorId, texts: power == null ? null : new Dictionary<string, string> { ["lbp"] = power });
    }

    private static (List<ImportedBlock> Blocks, List<ImportedWire> Wires) Read(CompileResult compiled)
    {
        return CompiledSignatures.Read(compiled);
    }
}
