using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.InGameTests;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public partial class StepListTests
{
    [Theory]
    [InlineData("turn-off-style lamp", "lamp", null)]
    [InlineData("turn-off-style logic  wide", "logic", "wide")]
    public void TurnOffStyle_AFunction_ThenOptionallyAStyle(string line, string function, string style)
    {
        var step = Assert.IsType<TurnOffStyleStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal((function, style), (step.Function, step.Style));
    }

    [Theory]
    [InlineData("turn-off-style")]
    [InlineData("turn-off-style logic wide now")]
    public void TurnOffStyle_TakesAFunction_AndOptionallyAStyle(string line)
    {
        ScenarioParser.Parse("steps", line, out var error);

        Assert.Contains("turn-off-style takes a Block Function's name, and optionally one of its styles", error);
    }

    [Theory]
    [InlineData("basic", 0)]
    [InlineData("global_velocity", 2)]
    public void SetBlockSetting_AnOption_IsStoredAsItsIndex(string value, int index)
    {
        var setting = SetBlockSettingStep.ToSetting(BlockManifest.SimpleTelemetryMode, value, out var error);

        Assert.Null(error);
        Assert.Equal(("xbs", SettingKind.Int, index), (setting.Key, setting.Kind, setting.IntValue));
    }

    [Theory]
    [InlineData("true", 1)]
    [InlineData("false", 0)]
    public void SetBlockSetting_ABool_IsStoredAsOneOrZero(string value, int stored)
    {
        var setting = SetBlockSettingStep.ToSetting(BlockManifest.DigitizerInvert, value, out var error);

        Assert.Null(error);
        Assert.Equal(("xt1", SettingKind.Int, stored), (setting.Key, setting.Kind, setting.IntValue));
    }

    [Fact]
    public void SetBlockSetting_APlayer_IsStoredAsItsIndex()
    {
        var setting = SetBlockSettingStep.ToSetting(BlockManifest.PlayerOrEveryone, "2", out var error);

        Assert.Null(error);
        Assert.Equal(("xpr", SettingKind.Int, 2), (setting.Key, setting.Kind, setting.IntValue));
    }

    [Theory]
    [InlineData("5", "5")]
    [InlineData("-12", "-12")]
    public void SetBlockSetting_ANumber_IsStoredAsText(string value, string stored)
    {
        var setting = SetBlockSettingStep.ToSetting(BlockManifest.RandomMinimum, value, out var error);

        Assert.Null(error);
        Assert.Equal(("xv1", SettingKind.Text, stored), (setting.Key, setting.Kind, setting.TextValue));
    }

    [Theory]
    [InlineData("mode", "fast", "'fast' isn't an option of mode. Use basic, local_velocity, global_velocity")]
    [InlineData("invert", "yes", "invert takes true or false, not 'yes'")]
    [InlineData("player", "5", "player takes all, 1, 2, 3 or 4, not '5'")]
    [InlineData("min", "1.5", "min takes a whole number, not '1.5'")]
    [InlineData("ticks", "0", "ticks must be at least 1, not '0'")]
    [InlineData("ticks", "99999999999", "ticks must be at most 2147483647, not '99999999999'")]
    public void SetBlockSetting_AValueTheSettingDoesNotTake_SaysWhatItTakes(string name, string value, string error)
    {
        var setting = new[]
        {
            BlockManifest.SimpleTelemetryMode,
            BlockManifest.DigitizerInvert,
            BlockManifest.PlayerOrEveryone,
            BlockManifest.RandomMinimum,
            BlockManifest.Delay.FindSetting("ticks"),
        }.Single(s => s.Name == name);

        Assert.Null(SetBlockSettingStep.ToSetting(setting, value, out var actual));
        Assert.Equal(error, actual);
    }

    [Fact]
    public void SetBlockSetting_ATogglersTargets_CantBeSet()
    {
        var targets = BlockManifest.All.SelectMany(t => t.Settings).First(s => s.Type == SettingType.Targets);

        Assert.Null(SetBlockSettingStep.ToSetting(targets, "a", out var error));
        Assert.Equal($"set-block-setting can't set {targets.Name}, a list of blocks", error);
    }
}
