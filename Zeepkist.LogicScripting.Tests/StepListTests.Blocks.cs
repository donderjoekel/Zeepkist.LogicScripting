using System;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.InGameTests;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public partial class StepListTests
{
    [Theory]
    [InlineData("place-block lamp", "lamp", null)]
    [InlineData("place-block lamp as lamp_1", "lamp", "lamp_1")]
    [InlineData("place-block simple_telemetry as speed-gauge", "simple_telemetry", "speed-gauge")]
    public void PlaceBlock_AFunction_ThenOptionallyAName(string line, string function, string name)
    {
        var step = Assert.IsType<PlaceBlockStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(function, step.Function);
        Assert.Equal(name, step.Name);
    }

    [Theory]
    [InlineData("place-block lamp as")]
    [InlineData("place-block lamp as a}b")]
    [InlineData("place-block lamp as two words")]
    public void PlaceBlock_TakesANameOfLettersDigitsDashesAndUnderscores(string line)
    {
        var scenario = ScenarioParser.Parse("steps", line, out var error);

        Assert.Null(scenario);
        Assert.Contains("takes a name of letters", error);
    }

    [Fact]
    public void SetBlockSetting_AName_ASetting_ThenAValue()
    {
        var step = Assert.IsType<SetBlockSettingStep>(Assert.Single(ScenarioParser.Parse("steps", "set-block-setting speed-gauge  mode global_velocity", out _).Steps));

        Assert.Equal("speed-gauge", step.Name);
        Assert.Equal("mode", step.Setting);
        Assert.Equal("global_velocity", step.Value);
    }

    [Theory]
    [InlineData("set-block-setting")]
    [InlineData("set-block-setting t")]
    [InlineData("set-block-setting t mode")]
    [InlineData("set-block-setting t mode basic now")]
    public void SetBlockSetting_TakesThreeWords(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("set-block-setting takes a placed block's name, a Setting's name and its value", error);
    }

    [Fact]
    public void SetStoredSetting_AName_AKind_AKey_ThenAValue()
    {
        var step = Assert.IsType<SetStoredSettingStep>(Assert.Single(ScenarioParser.Parse("steps", "set-stored-setting m float x3  0.5", out _).Steps));

        Assert.Equal("m", step.Name);
        Assert.Equal(StoredKind.Float, step.Kind);
        Assert.Equal("x3", step.Key);
        Assert.Equal("0.5", step.Value);
    }

    [Theory]
    [InlineData("set-stored-setting")]
    [InlineData("set-stored-setting m text oi2")]
    [InlineData("set-stored-setting m text oi2 5 6")]
    [InlineData("set-stored-setting m word oi2 5")]
    [InlineData("set-stored-setting m 2 oi2 5")]
    [InlineData("set-stored-setting m Text oi2 5")]
    public void SetStoredSetting_TakesFourWords(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("set-stored-setting takes a placed block's name, int, float or text", error);
    }

    [Theory]
    [InlineData("set-stored-setting m int x47 five", "int, not 'five'")]
    [InlineData("set-stored-setting m int x47 1.5", "int, not '1.5'")]
    [InlineData("set-stored-setting m float x3 half", "float, not 'half'")]
    public void SetStoredSetting_ANumberHasToBeOne(string line, string error)
    {
        ScenarioParser.Parse("steps", line, out var actual);

        Assert.Contains($"set-stored-setting takes a number for {error}", actual);
    }

    [Fact]
    public void SetStoredSetting_TakesANameOfLettersDigitsDashesAndUnderscores()
    {
        ScenarioParser.Parse("steps", "set-stored-setting a}b text oi2 5", out var error);

        Assert.Contains("takes a name of letters", error);
    }

    [Fact]
    public void SetBlockSetting_TakesANameOfLettersDigitsDashesAndUnderscores()
    {
        ScenarioParser.Parse("steps", "set-block-setting a}b mode basic", out var error);

        Assert.Contains("takes a name of letters", error);
    }

    [Theory]
    [InlineData("name-group-block Lights logic as l", "Lights", "logic", "l")]
    [InlineData("name-group-block My Lights  power_display as display-1", "My Lights", "power_display", "display-1")]
    public void NameGroupBlock_AScript_AFunction_ThenAName(string line, string script, string function, string name)
    {
        var step = Assert.IsType<NameGroupBlockStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal((script, function, name), (step.Script, step.Function, step.Name));
    }

    [Theory]
    [InlineData("name-group-block")]
    [InlineData("name-group-block Lights logic")]
    [InlineData("name-group-block logic as l")]
    [InlineData("name-group-block Lights logic is l")]
    public void NameGroupBlock_TakesAScript_AFunction_AndAsAName(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("name-group-block takes a Script's name, a Block Function's name and 'as <name>'", error);
    }

    [Fact]
    public void NameGroupBlock_TakesANameOfLettersDigitsDashesAndUnderscores()
    {
        ScenarioParser.Parse("steps", "name-group-block Lights logic as a}b", out var error);

        Assert.Contains("takes a name of letters", error);
    }

    [Theory]
    [InlineData("wire-blocks speed-gauge  heading to l op_in", "speed-gauge heading to l op_in")]
    [InlineData("wire-blocks to out to to a", "to out to to a")]
    public void WireBlocks_AnOutput_ThenAnInput(string line, string wire)
    {
        var step = Assert.IsType<WireBlocksStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(wire, step.Wire.ToString());
    }

    [Fact]
    public void WireDependencyLoop_AnOutput_ThenAnInput()
    {
        var step = Assert.IsType<WireDependencyLoopStep>(Assert.Single(ScenarioParser.Parse("steps", "wire-dependency-loop o out to n a", out _).Steps));

        Assert.Equal("o out to n a", step.Wire.ToString());
    }

    [Fact]
    public void ExpectWire_AnOutput_ThenAnInput()
    {
        var step = Assert.IsType<ExpectWireStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-wire g out to l a", out _).Steps));

        Assert.Equal(("g", "out", "l", "a"), (step.Wire.From, step.Wire.Output, step.Wire.To, step.Wire.Input));
    }

    [Theory]
    [InlineData("wire-blocks")]
    [InlineData("wire-blocks g to l a")]
    [InlineData("wire-blocks g out l a")]
    [InlineData("wire-blocks g out into l a")]
    [InlineData("wire-blocks g out to l a b")]
    [InlineData("wire-dependency-loop")]
    [InlineData("wire-dependency-loop l out g a")]
    [InlineData("expect-wire")]
    [InlineData("expect-wire g out to l")]
    public void TheWireSteps_TakeAnOutput_To_AndAnInput(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("takes a placed block's name and output, 'to', and a placed block's name and input", error);
    }

    [Theory]
    [InlineData("wire-blocks a}b out to l a")]
    [InlineData("wire-blocks g out to a}b a")]
    [InlineData("expect-wire a}b out to l a")]
    [InlineData("select-blocks g a}b")]
    [InlineData("expect-wires a}b 1")]
    public void TheNamedBlockSteps_TakeNamesOfLettersDigitsDashesAndUnderscores(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("takes a name of letters", error);
    }

    [Theory]
    [InlineData("select-blocks g", "g")]
    [InlineData("select-blocks l  g lamp-1", "l g lamp-1")]
    public void SelectBlocks_ReadsTheNames_InOrder(string line, string names)
    {
        var step = Assert.IsType<SelectBlocksStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(names, string.Join(" ", step.Names));
    }

    [Theory]
    [InlineData("select-blocks")]
    [InlineData("select-blocks g l g")]
    public void SelectBlocks_TakesNames_EachOnce(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("select-blocks takes placed blocks' names, each once", error);
    }

    [Fact]
    public void ExpectWires_AName_ThenACount()
    {
        var step = Assert.IsType<ExpectWiresStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-wires l 0", out _).Steps));

        Assert.Equal(("l", 0), (step.Name, step.Count));
    }

    [Theory]
    [InlineData("expect-wires", "takes a placed block's name and how many Wires it has")]
    [InlineData("expect-wires l", "takes a placed block's name and how many Wires it has")]
    [InlineData("expect-wires l 1 2", "takes a placed block's name and how many Wires it has")]
    [InlineData("expect-wires l -1", "takes a whole number of 0 or more")]
    public void ExpectWires_WithAWrongArgument_Fails(string line, string message)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains(message, error);
    }

    [Fact]
    public void RememberBlocksAndExpectBlocksUnmoved_ReadTheNames_InOrder()
    {
        var remember = Assert.IsType<RememberBlocksStep>(Assert.Single(ScenarioParser.Parse("steps", "remember-blocks l  g", out _).Steps));
        var expect = Assert.IsType<ExpectBlocksUnmovedStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-blocks-unmoved l g", out _).Steps));

        Assert.Equal(["l", "g"], remember.Names);
        Assert.Equal(["l", "g"], expect.Names);
    }

    [Fact]
    public void PasteBlocks_ReadsTheCopiesNames_InOrder()
    {
        var step = Assert.IsType<PasteBlocksStep>(Assert.Single(ScenarioParser.Parse("steps", "paste-blocks as l2 g2", out _).Steps));

        Assert.Equal(["l2", "g2"], step.Names);
    }

    [Theory]
    [InlineData("save-level saved", null)]
    [InlineData("load-level saved", null)]
    [InlineData("load-level saved while import-selected", typeof(ImportSelectedStep))]
    [InlineData("load-level saved while expect-step-fails import-selected because a while b", typeof(ExpectStepFailsStep))]
    public void TheLevelFileSteps_ReadTheName_AndAStepForWhileItLoads(string line, Type meanwhile)
    {
        var step = Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps);

        Assert.Equal("saved", step is LoadLevelStep load ? load.Name : Assert.IsType<SaveLevelStep>(step).Name);
        Assert.Equal(meanwhile, (step as LoadLevelStep)?.Meanwhile?.GetType());
    }

    [Theory]
    [InlineData("grab-blocks g", "grab-blocks takes nothing after it")]
    [InlineData("drop-blocks g", "drop-blocks takes nothing after it")]
    [InlineData("paste-blocks", "paste-blocks takes 'as' and a name for each selected block's copy")]
    [InlineData("paste-blocks g2", "paste-blocks takes 'as' and a name for each selected block's copy")]
    [InlineData("paste-blocks as g2 g2", "paste-blocks takes placed blocks' names, each once")]
    [InlineData("paste-blocks as a}b", "paste-blocks takes a name of letters")]
    [InlineData("save-level", "save-level takes a name of letters")]
    [InlineData("save-level ../saved", "save-level takes a name of letters")]
    [InlineData("load-level", "load-level takes a saved level's name")]
    [InlineData("load-level two words", "load-level takes a name of letters")]
    [InlineData("load-level saved while set-text", "load-level takes a saved level's name")]
    [InlineData("load-level saved while load-level saved", "load-level takes a saved level's name")]
    [InlineData("load-level saved while nope", "unknown step 'nope'")]
    public void TheGrabPasteAndLevelFileSteps_WithAWrongArgument_Fail(string line, string message)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains(message, error);
    }

    [Fact]
    public void FindConnector_FindsItByName()
    {
        var input = NamedBlock.FindConnector("l", "input", BlockManifest.Logic.Inputs, "op_in", out var error);

        Assert.Null(error);
        Assert.Equal(2, input.Index);
    }

    [Fact]
    public void FindConnector_AnOutputOfAMode_IsThatModesOutput()
    {
        var output = NamedBlock.FindConnector("t", "output", BlockManifest.SimpleTelemetry.GetOutputs("global_velocity"), "heading", out _);

        Assert.Equal(3, output.Index);
    }

    [Fact]
    public void FindConnector_AnUnknownName_SaysWhichThereAre()
    {
        Assert.Null(NamedBlock.FindConnector("l", "input", BlockManifest.Logic.Inputs, "c", out var unknown));
        Assert.Null(NamedBlock.FindConnector("lamp", "output", BlockManifest.Lamp.Outputs, "out", out var none));

        Assert.Equal("\"l\" has no input called 'c'. Use a, b, op_in", unknown);
        Assert.Equal("\"lamp\" has no output called 'out'. It has no outputs", none);
    }
}
