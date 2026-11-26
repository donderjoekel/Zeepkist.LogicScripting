using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class BlockImportKeptTests
{
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
}
