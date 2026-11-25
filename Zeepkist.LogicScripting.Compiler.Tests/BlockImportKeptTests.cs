using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Imports of selected blocks that store something a Script can't write, which stay as placed blocks.
/// </summary>
public class BlockImportKeptTests
{
    private const int GeneratorId = 2414;
    private const int DisplayId = 2415;
    private const int MathId = 2416;
    private const int TogglerId = 2417;
    private const int LogicId = 2420;
    private const int RandomId = 2425;
    private const int DelayId = 2428;
    private const int CounterId = 2434;
    private const int TelemetryId = 2440;
    private const int TimerId = 2444;
    private const int ForceGiverId = 2446;
    private const int LampId = 2448;
    private const int MultiplierId = 2449;
    private const int Math3PinsId = 2453;

    public static TheoryData<string, int, int, string, string> KeptOnBothSides => new()
    {
        { "math", MathId, 3, "oi2", "has a Setting a Script can't write, \"oi2\"" },
        { "math_3_pins", Math3PinsId, 4, "oi0", "has a Setting a Script can't write, \"oi0\"" },
        { "timer", TimerId, 2, "oi2", "has a Setting a Script can't write, \"oi2\"" },
        { "delay", DelayId, 1, "oi9", "has no ticks, which a Script has to give" },
        { "counter", CounterId, 3, "ld2", "has a value for min_reset that a Script can't write" },
        { "random", RandomId, 2, "xv1", "has a min above its max, which a Script can't write" },
        { "logic", LogicId, 3, "idle", "has a Setting a Script can't write, \"idle\"" }
    };

    private static ImportedBlock Generator(string uid = "g") => new(uid, GeneratorId);
    private static ImportedBlock Display(string uid = "p") => new(uid, DisplayId);
    private static ImportedBlock Logic(string uid = "l") => new(uid, LogicId);
    private static ImportedBlock Delay(string uid = "d") => Block(uid, DelayId, ("ldl", "1"));
    private static ImportedBlock KeptMath(string uid = "m") => Block(uid, MathId, ("oi2", "7"));

    [Theory]
    [MemberData(nameof(KeptOnBothSides))]
    public void AKeptBlockWiredOnBothSides_IsReadAndWired(string function, int blockId, int output, string key, string reason)
    {
        var level = new ImportLevel(
            [Generator(), Block("k", blockId, (key, "5")), Display()],
            new ImportedWire("g", 0, "k", 0),
            new ImportedWire("k", output, "p", 0));

        var import = AssertImports(
            level,
            $"""
            # {function} "k" {reason}
            {function}_1 = placed("k")

            {function}_1(0)
            power_display({function}_1)
            """,
            "g",
            "k",
            "p");

        Assert.Equal(["g", "p"], import.ImportedUids.OrderBy(u => u));
        var kept = Assert.Single(import.Kept);
        Assert.Equal("k", kept.Uid);
        Assert.Equal($"{function} \"k\" {reason}", kept.Reason);
    }

    [Theory]
    [InlineData("oi2")]
    [InlineData("oi4")]
    public void AForceGiverWithAnOverride_IsKept(string key)
    {
        var level = new ImportLevel([Generator(), Block("f", ForceGiverId, (key, "3"))], new ImportedWire("g", 0, "f", 0));

        AssertImports(
            level,
            $"""
            # force_giver "f" has a Setting a Script can't write, "{key}"
            force_giver_1 = placed("f")

            force_giver_1(0)
            """,
            "g",
            "f");
    }

    [Fact]
    public void APropertyMultiplierWithAMultiplier_IsKept()
    {
        var level = new ImportLevel(
            [Generator(), new ImportedBlock("m", MultiplierId, floats: new Dictionary<string, float> { ["x3"] = 0.5f })],
            new ImportedWire("g", 0, "m", 0));

        AssertImports(
            level,
            """
            # property_multiplier "m" has a Setting a Script can't write, "x3"
            property_multiplier_1 = placed("m")

            property_multiplier_1(0)
            """,
            "g",
            "m");
    }

    [Theory]
    [InlineData(0, 1, "x47")]
    [InlineData(1, 2, "x48")]
    public void APropertyMultiplierWithOtherThanFreshValues_IsKept(int x47, int x48, string key)
    {
        var multiplier = new ImportedBlock("m", MultiplierId, ints: new Dictionary<string, int> { ["x47"] = x47, ["x48"] = x48 });

        Assert.Equal(
            $"property_multiplier \"m\" has a Setting a Script can't write, \"{key}\"",
            Assert.Single(new ImportLevel([Generator(), multiplier]).Import("g", "m").Kept).Reason);
    }

    [Theory]
    [InlineData("0", "has no ticks, which a Script has to give")]
    [InlineData("-1", "has a value for ticks that a Script can't write")]
    [InlineData("2147483648", "has a value for ticks that a Script can't write")]
    public void ADelayWithTicksAScriptCantWrite_IsKept(string ticks, string reason)
    {
        var level = new ImportLevel(
            [Generator(), Block("d", DelayId, ("ldl", ticks)), Display()],
            new ImportedWire("g", 0, "d", 0),
            new ImportedWire("d", 1, "p", 0));

        AssertImports(
            level,
            $"""
            # delay "d" {reason}
            delay_1 = placed("d")

            delay_1(0)
            power_display(delay_1)
            """,
            "g",
            "d",
            "p");
    }

    [Fact]
    public void ATogglerWithoutTargets_IsKept()
    {
        var level = new ImportLevel([Generator(), new ImportedBlock("t", TogglerId)], new ImportedWire("g", 0, "t", 0));

        AssertImports(
            level,
            """
            # toggler "t" has no targets, which a Script has to give
            toggler_1 = placed("t")

            toggler_1(0)
            """,
            "g",
            "t");
    }

    [Fact]
    public void ATogglerWithAGapInItsTargets_IsKept_AndStillRefusesAnImportedTarget()
    {
        ImportedBlock[] blocks = [Generator(), Logic(), Block("t", TogglerId, ("xuid2", "l"))];

        AssertImports(
            new ImportLevel(blocks),
            """
            # toggler "t" has a value for targets that a Script can't write
            toggler_1 = placed("t")

            generator()
            """,
            "g",
            "t");
        AssertRefused(new ImportLevel(blocks), "a Toggler targets logic \"l\", and would lose it at the next Compile", "g", "l", "t");
        Assert.Equal(
            "a Toggler targets logic \"l\", and would lose it at the next Compile",
            BlockImport.Import(blocks, []).Refusal);
    }

    [Fact]
    public void ATargetOfSelectedTogglers_IsNoReason_WhenItIsKept()
    {
        AssertImports(
            new ImportLevel([KeptMath(), Block("t", TogglerId, ("xuid1", "m"))]),
            """
            # math "m" has a Setting a Script can't write, "oi2"
            math_1 = placed("m")

            toggler(targets: [math_1])
            """,
            "m",
            "t");
    }

    [Fact]
    public void AnOptionOrAWordOutOfRange_IsKept()
    {
        var logic = new ImportedBlock("l", LogicId, ints: new Dictionary<string, int> { ["xmo"] = 6 });
        var hidden = new ImportedBlock("p", DisplayId, ints: new Dictionary<string, int> { ["lbhd"] = 2 });

        AssertImports(
            new ImportLevel([Generator(), logic, hidden], new ImportedWire("l", 3, "p", 0)),
            """
            # logic "l" has a value for op that a Script can't write
            logic_1 = placed("l")
            # power_display "p" has a value for hide that a Script can't write
            power_display_1 = placed("p")

            generator()
            """,
            "g",
            "l",
            "p");
    }

    [Theory]
    [InlineData("id")]
    [InlineData("idle")]
    [InlineData("id0-")]
    [InlineData("id0-x")]
    [InlineData("p1")]
    [InlineData("xuid1")]
    public void ATextUnderAKeyThatOnlyLooksReserved_IsKept(string key)
    {
        AssertKept(Block("l", LogicId, (key, "5")), key);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("ide1")]
    [InlineData("id0-0")]
    [InlineData("p")]
    [InlineData("pt1")]
    public void ANumberUnderAKeyThatOnlyLooksReserved_IsKept(string key)
    {
        AssertKept(new ImportedBlock("l", LogicId, ints: new Dictionary<string, int> { [key] = 5 }), key);
    }

    [Theory]
    [InlineData("id0")]
    [InlineData("p0")]
    public void AFloatUnderAReservedKey_IsKept(string key)
    {
        AssertKept(new ImportedBlock("l", LogicId, floats: new Dictionary<string, float> { [key] = 0.5f }), key);
    }

    [Theory]
    [InlineData("xuid1")]
    [InlineData("xuid9")]
    public void ATargetStoredInAWayAScriptDoesnt_IsKept(string key)
    {
        var toggler = key == "xuid9"
            ? Block("t", TogglerId, ("xuid1", "lamp"), (key, "lamp"))
            : new ImportedBlock(
                "t",
                TogglerId,
                ints: new Dictionary<string, int> { [key] = 1 },
                texts: new Dictionary<string, string> { [key] = "lamp" });
        var level = new ImportLevel([Generator(), toggler, new ImportedBlock("lamp", LampId)]);

        Assert.Equal(
            $"toggler \"t\" has a Setting a Script can't write, \"{key}\"",
            Assert.Single(level.Import("g", "t").Kept).Reason);
    }

    [Fact]
    public void AKeyWithALineBreak_StaysOnTheCommentsLine()
    {
        AssertImports(
            new ImportLevel([Generator(), Block("l", LogicId, ("a\r\ngenerator()", "5"))]),
            """
            # logic "l" has a Setting a Script can't write, "agenerator()"
            logic_1 = placed("l")

            generator()
            """,
            "g",
            "l");
    }

    [Fact]
    public void SeveralKeptBlocks_EachHaveTheirOwnComment_AndKeepTheWireBetweenThem()
    {
        var level = new ImportLevel(
            [Generator(), KeptMath("m"), KeptMath("n"), Display()],
            new ImportedWire("g", 0, "m", 0),
            new ImportedWire("m", 3, "n", 0),
            new ImportedWire("n", 3, "p", 0));

        var import = AssertImports(
            level,
            """
            # math "m" has a Setting a Script can't write, "oi2"
            math_1 = placed("m")
            # math "n" has a Setting a Script can't write, "oi2"
            math_2 = placed("n")

            math_1(0)

            power_display(math_2)
            """,
            "g",
            "m",
            "n",
            "p");

        Assert.Equal(2, import.Kept.Count);
    }

    [Fact]
    public void GeneratorsOfTheSamePower_ShareALiteral_IntoAKeptBlockToo()
    {
        var level = new ImportLevel(
            [Generator("g"), Generator("h"), KeptMath(), Display()],
            new ImportedWire("g", 0, "m", 0),
            new ImportedWire("h", 0, "p", 0));

        var import = level.Import("g", "h", "m", "p");

        Assert.Null(import.Refusal);
        Assert.Equal(
            """
            # math "m" has a Setting a Script can't write, "oi2"
            math_1 = placed("m")

            math_1(0)

            power_display(0)
            """.Replace("\r\n", "\n"),
            import.Text);
        Assert.Equal(["g", "h", "p"], import.ImportedUids.OrderBy(u => u));
        var compiled = level.Compile(import);
        Assert.Equal(2, compiled.Blocks.Count);
        Assert.Single(compiled.PlacedWires);
    }

    [Fact]
    public void AnotherOutsideBlock_HasNoComment()
    {
        AssertImports(
            new ImportLevel(
                [KeptMath(), Logic(), new ImportedBlock("lamp", LampId)],
                new ImportedWire("m", 3, "l", 0),
                new ImportedWire("l", 3, "lamp", 0)),
            """
            # math "m" has a Setting a Script can't write, "oi2"
            math_1 = placed("m")
            lamp_1 = placed("lamp")

            lamp_1(logic(math_1))
            """,
            "m",
            "l",
            "lamp");
    }

    [Fact]
    public void AnUnselectedBlockWithSuchASetting_IsAnOutsideBlockLikeAnother()
    {
        var import = AssertImports(
            new ImportLevel([KeptMath(), Display()], new ImportedWire("m", 3, "p", 0)),
            """
            math_1 = placed("m")

            power_display(math_1)
            """,
            "p");

        Assert.Empty(import.Kept);
    }

    [Fact]
    public void AllImportedBlocksHidden_TheDefaultIsTheFirstLine()
    {
        var hidden = new ImportedBlock("g", GeneratorId, ints: new Dictionary<string, int> { ["lbhd"] = 1 });

        AssertImports(
            new ImportLevel([hidden, KeptMath()]),
            """
            default hide: true

            # math "m" has a Setting a Script can't write, "oi2"
            math_1 = placed("m")

            generator()
            """,
            "g",
            "m");
    }

    [Fact]
    public void ALoopThroughAKeptBlock_WithADelay_IsWritten()
    {
        AssertImports(
            new ImportLevel(
                [KeptMath(), Logic(), Delay()],
                new ImportedWire("l", 3, "m", 0),
                new ImportedWire("m", 3, "d", 0),
                new ImportedWire("d", 1, "l", 0)),
            """
            # math "m" has a Setting a Script can't write, "oi2"
            math_1 = placed("m")

            math_1(logic(delay(math_1, ticks: 1)))
            """,
            "m",
            "l",
            "d");
    }

    [Fact]
    public void ALoopThroughAKeptDelay_IsWritten()
    {
        AssertImports(
            new ImportLevel(
                [Logic(), Block("d", DelayId, ("ldl", "0"))],
                new ImportedWire("l", 3, "d", 0),
                new ImportedWire("d", 1, "l", 0)),
            """
            # delay "d" has no ticks, which a Script has to give
            delay_1 = placed("d")

            delay_1(logic(delay_1))
            """,
            "l",
            "d");
    }

    [Fact]
    public void ADependencyLoopThroughAKeptBlock_IsRefused_NamingIt()
    {
        AssertRefused(
            new ImportLevel(
                [KeptMath(), Logic()],
                new ImportedWire("l", 3, "m", 0),
                new ImportedWire("m", 3, "l", 0)),
            "math \"m\" has a Setting a Script can't write, \"oi2\", and it can't stay as a placed block: "
            + "This Wire makes a Dependency Loop, which stops all logic in play. Put a delay(...) in the loop",
            "m",
            "l");
    }

    [Fact]
    public void AModeOutOfRange_IsRefused_AsItsOutputsHaveNoNames()
    {
        var telemetry = new ImportedBlock("s", TelemetryId, ints: new Dictionary<string, int> { ["xbs"] = 3 });

        AssertRefused(
            new ImportLevel([telemetry, Generator()]),
            "simple_telemetry \"s\" has a value for mode that a Script can't write",
            "s",
            "g");
    }

    [Fact]
    public void OnlyBlocksAScriptCantWrite_AreRefused_NamingEach()
    {
        AssertRefused(new ImportLevel([KeptMath()]), "math \"m\" has a Setting a Script can't write, \"oi2\"", "m");
        AssertRefused(
            new ImportLevel([KeptMath(), new ImportedBlock("t", TogglerId), new ImportedBlock("lamp", LampId)]),
            "a Script can't write any of the selected blocks: math \"m\" has a Setting a Script can't write, \"oi2\", "
            + "toggler \"t\" has no targets, which a Script has to give",
            "m",
            "t",
            "lamp");
    }

    [Fact]
    public void AKeptBlockInABlockGroup_IsRefused()
    {
        var grouped = new ImportedBlock("m", MathId, isInBlockGroup: true, texts: new Dictionary<string, string> { ["oi2"] = "7" });

        AssertRefused(new ImportLevel([Generator(), grouped]), "math \"m\" is in a Block Group, which a recompile replaces", "g", "m");
    }

    [Fact]
    public void AKeptBlockTheStartupCheckTurnedOff_IsRefused()
    {
        Assert.Equal(
            "math \"m\" can't be imported: 'math' doesn't match this game version and is turned off",
            new ImportLevel([Generator(), KeptMath()]).Import(["g", "m"], [MathId]).Refusal);
    }

    private static void AssertKept(ImportedBlock logic, string key)
    {
        Assert.Equal(
            $"logic \"l\" has a Setting a Script can't write, \"{key}\"",
            Assert.Single(new ImportLevel([Generator(), logic]).Import("g", "l").Kept).Reason);
    }

    /// <summary>
    /// Also that compiling the text over the imported blocks gives as many blocks.
    /// </summary>
    private static ImportResult AssertImports(ImportLevel level, string text, params string[] selected)
    {
        var import = level.Import(selected);

        Assert.Null(import.Refusal);
        Assert.Equal(text.Replace("\r\n", "\n"), import.Text);
        Assert.DoesNotContain(import.Kept, k => import.ImportedUids.Contains(k.Uid));
        var compiled = level.Compile(import);
        Assert.Empty(compiled.Errors);
        Assert.Equal(import.ImportedUids.Count, compiled.Blocks.Count);
        return import;
    }

    private static void AssertRefused(ImportLevel level, string refusal, params string[] selected)
    {
        var import = level.Import(selected);

        Assert.Equal(refusal, import.Refusal);
        Assert.Null(import.Text);
        Assert.Empty(import.ImportedUids);
        Assert.Empty(import.Kept);
    }

    private static ImportedBlock Block(string uid, int blockId, params (string Key, string Value)[] texts)
    {
        return new ImportedBlock(uid, blockId, texts: texts.ToDictionary(t => t.Key, t => t.Value));
    }
}
