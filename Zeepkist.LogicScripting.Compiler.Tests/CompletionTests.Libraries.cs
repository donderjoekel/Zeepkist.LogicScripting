using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class CompletionTests
{
    [Theory]
    [InlineData("x = timing.edge(|", new[] { "a:", "b:" })]
    [InlineData("x = timing.edge(1, |", new[] { "b:" })]
    [InlineData("x = timing.edge(b: 1, |", new[] { "a:" })]
    [InlineData("x = timing.edge(1, b: 2, |", new string[0])]
    [InlineData("x = logic(timing.edge(1, |", new[] { "b:" })]
    [InlineData("function edge(x)\nend\nx = timing.edge(1, |", new[] { "b:" })]
    [InlineData("function g()\n  x = timing.edge(1, |\nend", new[] { "b:" })]
    [InlineData("x = timing.edge(1, |\ny = 2", new[] { "b:" })]
    public void Items_InACallToALibrarysFunction_ItsInputsItDoesntHaveYet(string text, string[] arguments)
    {
        var items = Arguments(text, Library(TwoInputLibrary));

        Assert.Equal(arguments, items.Select(i => i.Label));
        Assert.All(items, i => Assert.Equal("input", i.Detail));
    }

    [Fact]
    public void Items_NamingAnArgumentOfALibrarysFunction_ItsInputsMatchingThePrefix()
    {
        Assert.Equal(["b:"], Arguments("x = timing.edge(1, b|: 2)", Library(TwoInputLibrary)).Select(i => i.Label));
    }

    [Theory]
    [InlineData("x = timing.edge(|", TwoInputLibrary + "\nx = 1")]
    [InlineData("x = timing.edge(|", TwoInputLibrary + "\nfunction other(a) -> b\n  b = (a\nend")]
    [InlineData("x = timing.other(|", TwoInputLibrary)]
    [InlineData("x = nothing.edge(|", TwoInputLibrary)]
    [InlineData("x = a.timing.edge(|", TwoInputLibrary)]
    [InlineData("x = timing .edge(|", TwoInputLibrary)]
    [InlineData("x = timing. edge(|", TwoInputLibrary)]
    public void Items_InACallToALibrarysFunction_NoneFromALibraryWithAnErrorOrWhenItsNotALibrarysFunction(string text, string libraryText)
    {
        Assert.Empty(Arguments(text, Library(libraryText)));
    }

    [Fact]
    public void Items_InACallToALibrarysFunction_NoneWithoutTheLibraries()
    {
        Assert.Empty(Arguments("x = timing.edge(|"));
    }

    [Fact]
    public void Accept_ALibrarysFunctionsInput_AddsItsColon()
    {
        var context = At("x = timing.edge(1, |");
        var item = Completion.GetItems(context, lookups: Library(TwoInputLibrary)).First(i => i.Label == "b:");

        Assert.Equal("x = timing.edge(1, b: |", Format(Completion.Accept(context, item)));
    }

    [Theory]
    [InlineData("x = timing.|")]
    [InlineData("x = logic(timing.|")]
    [InlineData("function g()\n  x = timing.|\nend")]
    [InlineData("function timing(a)\nend\nx = timing.|")]
    [InlineData("timing = 1\nx = timing.|")]
    [InlineData("x = timing.|\ntiming = simple_telemetry()")]
    public void Items_AfterALibrarysNameAndADot_ItsFunctions_WithTheirInputs(string text)
    {
        var items = Names(text, Library(TwoFunctionLibrary));

        Assert.Equal(["edge", "pulse"], items.Select(i => i.Label));
        Assert.All(items, i => Assert.Equal(CompletionKind.Function, i.Kind));
        Assert.Equal(["(a)", "(a, ticks)"], items.Select(i => i.Detail));
    }

    [Fact]
    public void Items_AfterALibrarysNameAndADot_TheFunctionsMatchingThePrefix()
    {
        Assert.Equal(["pulse"], NameLabels("x = timing.pu|", Library(TwoFunctionLibrary)));
    }

    [Theory]
    [InlineData("timing = simple_telemetry()\nx = timing.|", new[] { "speed", "yaw", "up", "state", "edge", "pulse" })]
    [InlineData("timing = simple_telemetry()\nx = timing.|(1)", new[] { "edge", "pulse" })]
    [InlineData("timing = simple_telemetry()\nx = timing.| (1)", new[] { "edge", "pulse" })]
    public void Items_AfterADot_ANameTheScriptDefinesAndALibrarysName_WhatTheCompilerResolvesEachTo(string text, string[] expected)
    {
        Assert.Equal(expected, NameLabels(text, Library(TwoFunctionLibrary)));
    }

    [Theory]
    [InlineData("x = timing.|", EdgeLibrary + "\nx = 1")]
    [InlineData("x = timing.|", "")]
    [InlineData("x = nothing.|", EdgeLibrary)]
    [InlineData("x = a.timing.|", EdgeLibrary)]
    [InlineData("x = timing .|", EdgeLibrary)]
    public void Items_AfterALibrarysNameAndADot_NothingFromALibraryWithAnErrorOrNoFunctionsOrWhenItsNotALibrarysName(string text, string libraryText)
    {
        Assert.Empty(NameLabels(text, Library(libraryText)));
    }

    [Fact]
    public void Items_AfterALibrarysNameAndADot_NothingWithoutTheLibraries()
    {
        Assert.Empty(NameLabels("x = timing.|"));
    }

    [Theory]
    [MemberData(nameof(RejectedFirstLines.All), MemberType = typeof(RejectedFirstLines))]
    public void Items_AfterALibrarysNameAndADot_NothingFromALibraryWithAFunctionWhoseFirstLineTheCompilerRejects(string firstLine)
    {
        Assert.Empty(NameLabels("x = timing.|", Library($"{firstLine}\nend\nfunction g()\nend")));
    }

    [Theory]
    [InlineData("x = timing.|", "x = timing.edge(|")]
    [InlineData("x = timing.ed|", "x = timing.edge(|")]
    [InlineData("x = timing.e|(1)", "x = timing.edge(|1)")]
    [InlineData("x = timing.e| (1)", "x = timing.edge (|1)")]
    public void Accept_ALibrarysFunction_OpensItsQualifiedCall(string text, string expected)
    {
        var context = At(text);
        var item = Completion.GetItems(context, lookups: Library(TwoFunctionLibrary)).First(i => i.Label == "edge");

        Assert.Equal(expected, Format(Completion.Accept(context, item)));
    }

    [Theory]
    [InlineData("x = timing.edge(1).|", "function edge(a) -> hi\n  hi = a\nend")]
    [InlineData("x = timing.other(1).|", EdgeLibrary)]
    [InlineData("x = nothing.edge(1).|", EdgeLibrary)]
    [InlineData("x = timing.logic(1).|", EdgeLibrary)]
    public void Items_AfterALibraryFunctionCallsDot_NothingWhenItsNotToALibraryFunctionWithSeveralOutputs(string text, string libraryText)
    {
        Assert.Empty(NameLabels(text, Library(libraryText)));
    }

    [Theory]
    [InlineData(EdgeLibrary + "\nx = 1")]
    [InlineData(EdgeLibrary + "\nfunction other(a) -> b\n  b = (a\nend")]
    [InlineData(EdgeLibrary + "\nfunction edge(a) -> x, y\n  x = a\n  y = a\nend")]
    public void Items_AfterALibraryFunctionCallsDot_NothingFromALibraryWithAnError(string libraryText)
    {
        Assert.Empty(NameLabels("x = timing.edge(1).|", Library(libraryText)));
    }

    [Fact]
    public void Items_AfterALibraryFunctionCallsDot_NothingWithoutTheLibraries()
    {
        Assert.Empty(NameLabels("x = timing.edge(1).|"));
    }

    [Theory]
    [MemberData(nameof(RejectedFirstLines.All), MemberType = typeof(RejectedFirstLines))]
    public void Items_AfterALibraryFunctionCallsDot_NothingFromAFunctionWhoseFirstLineTheCompilerRejects(string firstLine)
    {
        Assert.Empty(NameLabels("x = timing.f(1).|", Library($"{firstLine}\nend\nfunction g()\nend")));
    }
}
