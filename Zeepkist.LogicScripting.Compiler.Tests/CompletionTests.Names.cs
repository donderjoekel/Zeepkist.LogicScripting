using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class CompletionTests
{
    [Fact]
    public void Items_BlockFunctionsAliasesPlacedAndBooleans()
    {
        var labels = Labels("x = |");

        Assert.Contains("logic", labels);
        Assert.Contains("power_display", labels);
        Assert.Contains("mux", labels);
        Assert.Contains("placed", labels);
        Assert.Contains("true", labels);
        Assert.Contains("false", labels);
        Assert.DoesNotContain("lamp", labels);
        Assert.DoesNotContain("block", labels);
    }

    [Fact]
    public void Items_PrefixMatchesFirst_ThenSubstringMatches()
    {
        var labels = Labels("x = mul|");

        Assert.Equal(["multiplexer", "demultiplexer", "property_multiplier"], labels);
    }

    [Fact]
    public void Items_IgnoreCase()
    {
        Assert.Equal("logic", Labels("x = LO|")[0]);
    }

    [Fact]
    public void Items_FunctionsDescribeTheirInputs_ThenTheirSettings()
    {
        var logic = Completion.GetItems(At("lo|")).First(i => i.Label == "logic");

        Assert.Equal("(a, b, op_in, op:)", logic.Detail);
    }

    [Fact]
    public void Items_FunctionsWithoutSettings_DescribeOnlyTheirInputs()
    {
        var multiplexer = Completion.GetItems(At("multi|")).First(i => i.Label == "multiplexer");

        Assert.Equal("(in1, in2, in3, in4, select)", multiplexer.Detail);
    }

    [Fact]
    public void Items_FunctionsWithStyles_DescribeTheirStyle()
    {
        var powerDisplay = Completion.GetItems(At("pow|")).First(i => i.Label == "power_display");

        Assert.Equal("(x, style:)", powerDisplay.Detail);
    }

    [Fact]
    public void Items_AliasesDescribeTheirFunction()
    {
        var mux = Completion.GetItems(At("mu|")).First(i => i.Label == "mux");
        var lerp = Completion.GetItems(At("ler|")).First(i => i.Label == "lerp");

        Assert.Equal("multiplexer(in1, in2, in3, in4, select)", mux.Detail);
        Assert.Equal("math_3_pins(start, end, t, op_in, op:)", lerp.Detail);
    }

    [Fact]
    public void Items_NoneWhereNothingCompletes()
    {
        Assert.Empty(Completion.GetItems(At("# lo|")));
    }

    [Fact]
    public void Items_NamesDefinedAbove_WithTheirBlockType()
    {
        var a = Names("a = generator(power: 1)\nb = 1\n\nx = |").First(i => i.Label == "a");

        Assert.Equal(CompletionKind.Name, a.Kind);
        Assert.Equal("generator", a.Detail);
        Assert.Contains("b", NameLabels("a = generator(power: 1)\nb = 1\n\nx = |"));
    }

    [Fact]
    public void Items_AreInAlphabeticalOrder_WithTheNames()
    {
        Assert.Equal(["lo", "logic", "lower"], NameLabels("lower = 1\nlo = 1\nx = lo|").Take(3));
    }

    [Theory]
    [InlineData("x = |\na = 1")]
    [InlineData("a = |")]
    [InlineData("a = logic(1, |")]
    [InlineData("a = logic(\n    1,\n    |\n)")]
    [InlineData("x = logic(1, delay(2), |\na = 1")]
    [InlineData("a = logic(\n    1,\n    |")]
    [InlineData("a = logic(\n    |\nb = 1")]
    [InlineData("x = timing.delay(|\na = 1")]
    public void Items_NoNamesDefinedAtOrBelowTheCaret(string text)
    {
        Assert.DoesNotContain("a", NameLabels(text));
    }

    [Theory]
    [InlineData("x = delay(|\na = 1")]
    [InlineData("a = delay(|")]
    [InlineData("a = delay(1 & |)")]
    [InlineData("a = delay(logic(1, |))")]
    [InlineData("a = delay(\n    logic(1, b: |)\n)")]
    [InlineData("x = delay(\n    |\na = 1")]
    public void Items_AnyName_InsideDelay(string text)
    {
        Assert.Contains("a", NameLabels(text));
    }

    [Fact]
    public void Items_NamesAboveADefinition_AfterACallLeftOpen()
    {
        Assert.Contains("a", NameLabels("a = logic(\nb = |"));
    }

    [Fact]
    public void Items_ANameDefinedTwice_Once()
    {
        Assert.Single(NameLabels("a = 1\na = 2\nx = |"), l => l == "a");
    }

    [Fact]
    public void Items_NoNamesWhereNothingCompletes()
    {
        Assert.Empty(Names("a = 1\n# |"));
    }

    [Theory]
    [InlineData("function edge(x, n) -> y\nend\nz = |")]
    [InlineData("z = |\nfunction edge(x, n) -> y\nend")]
    [InlineData("function edge(x, n) -> y\n    a = |\nend")]
    public void Items_TheScriptsFunctions_WithTheirInputs(string text)
    {
        var edge = Completion.GetItems(At(text)).First(i => i.Label == "edge");

        Assert.Equal(CompletionKind.Function, edge.Kind);
        Assert.Equal("(x, n)", edge.Detail);
    }

    [Theory]
    [MemberData(nameof(RejectedFirstLines.All), MemberType = typeof(RejectedFirstLines))]
    public void Items_AFunctionWhoseFirstLineTheCompilerRejects_WithoutItsInputs(string firstLine)
    {
        var f = Completion.GetItems(At($"{firstLine}\nend\nfunction g()\nend\nx = |")).First(i => i.Label == "f");

        Assert.Equal(CompletionKind.Function, f.Kind);
        Assert.Equal(string.Empty, f.Detail);
    }

    [Fact]
    public void Items_AFunctionDefinedTwice_WithoutInputs_WhenTheCompilerRejectsTheFirstDefinitionsFirstLine()
    {
        var f = Completion.GetItems(At("function f(a, a)\nend\nfunction f(b)\nend\nx = |")).First(i => i.Label == "f");

        Assert.Equal(string.Empty, f.Detail);
    }

    [Fact]
    public void Items_AFunctionDefinedTwice_Once()
    {
        Assert.Single(Labels("function f()\nend\nfunction f(a)\nend\nx = |"), l => l == "f");
    }

    [Theory]
    [InlineData("|", "function")]
    [InlineData("x = 1\nfu|", "function")]
    [InlineData("function f()\nend\n|", "function")]
    [InlineData("function f()\n    |\nend", "end")]
    [InlineData("function f()\n    x = 1\n    en|", "end")]
    [InlineData("function f()\n    x = 1\nend|", "end")]
    public void Items_FunctionAndEnd_AtTheStartOfAStatement(string text, string keyword)
    {
        var item = Completion.GetItems(At(text)).First(i => i.Label == keyword);

        Assert.Equal(CompletionKind.Keyword, item.Kind);
    }

    [Theory]
    [InlineData("x = |")]
    [InlineData("x = 1 & |")]
    [InlineData("logic(|")]
    [InlineData("function f()\n    x = |\nend")]
    public void Items_NoFunctionOrEnd_InsideAStatement(string text)
    {
        var labels = Labels(text);

        Assert.DoesNotContain("function", labels);
        Assert.DoesNotContain("end", labels);
    }

    [Fact]
    public void Items_NoEnd_InACallLeftOpen()
    {
        Assert.DoesNotContain("end", Labels("function f(x)\n    a = logic(x,\n    |"));
    }

    [Fact]
    public void Items_ANameAFunctionHas_OnlyAsTheFunction()
    {
        var items = Names("f = 1\nfunction f(x)\nend\nb = |").Where(i => i.Label == "f").ToList();

        Assert.Equal(CompletionKind.Function, Assert.Single(items).Kind);
    }

    [Fact]
    public void Items_InsideAFunctionWithoutAName_OnlyItsOwnNames()
    {
        var labels = NameLabels("a = 1\nfunction (x)\n    q = 1\n    r = |\nend");

        Assert.Contains("q", labels);
        Assert.DoesNotContain("a", labels);
    }

    [Fact]
    public void Items_NoEnd_OutsideAFunction_NorFunction_InsideOne()
    {
        Assert.DoesNotContain("end", Labels("x = 1\n|"));
        Assert.DoesNotContain("function", Labels("function f()\n    |\nend"));
    }

    [Theory]
    [InlineData("function f(|")]
    [InlineData("function f(a, |")]
    [InlineData("function f(a) -> |")]
    [InlineData("function f(a) -> b, |\nend")]
    [InlineData("function fo|")]
    public void None_OnAFunctionsFirstLine(string text)
    {
        Assert.Equal(CaretContextKind.None, At(text).Kind);
    }

    [Fact]
    public void Items_InsideAFunction_ItsInputsAndDefinitionsAbove_NotTheTopLevels()
    {
        var labels = NameLabels("a = 1\nfunction f(x)\n    b = 1\n    c = |\n    d = 1\nend");

        Assert.Contains("x", labels);
        Assert.Contains("b", labels);
        Assert.DoesNotContain("a", labels);
        Assert.DoesNotContain("c", labels);
        Assert.DoesNotContain("d", labels);
    }

    [Fact]
    public void Items_AFunctionsInputs_DescribedAsInputs()
    {
        Assert.Equal("input", Names("function f(x)\n    c = |\nend").First(i => i.Label == "x").Detail);
    }

    [Fact]
    public void Items_InsideDelayInAFunction_OnlyItsOwnNames()
    {
        var labels = NameLabels("a = 1\nfunction f(x)\n    c = delay(|)\n    d = 1\nend\nb = 1");

        Assert.Contains("x", labels);
        Assert.Contains("d", labels);
        Assert.DoesNotContain("a", labels);
        Assert.DoesNotContain("b", labels);
    }

    [Theory]
    [InlineData("function f(x)\n    b = 1\nend\nc = |")]
    [InlineData("c = delay(|)\nfunction f(x)\n    b = 1\nend")]
    public void Items_OutsideAFunction_NoneOfItsNames(string text)
    {
        var labels = NameLabels(text);

        Assert.DoesNotContain("x", labels);
        Assert.DoesNotContain("b", labels);
    }
}
