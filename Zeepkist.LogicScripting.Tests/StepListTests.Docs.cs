using System;
using System.Linq;
using System.Text.RegularExpressions;
using TNRD.Zeepkist.LogicScripting.InGameTests;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public partial class StepListTests
{
    [Fact]
    public void TheStepList_HoldsExactlyTheStepsTheDocsList()
    {
        var documented = DocsTable("### Steps").Select(step => step.Split(' ')[0]);

        Assert.Equal(documented.OrderBy(n => n), StepList.Names.OrderBy(n => n));
    }

    [Fact]
    public void TheDocs_ListExactlyTheRunScriptsOptions()
    {
        var script = RepoFiles.Read("tools/ingame-test.sh");
        var options = Regex.Matches(script, @"^\s*((?:-[-\w]+)(?: \| -[-\w]+)*)\)", RegexOptions.Multiline)
            .SelectMany(match => match.Groups[1].Value.Split(" | "));
        var documented = DocsTable("### Options").SelectMany(option => option.Split(", ")).Select(option => option.Split(' ')[0]);

        Assert.Equal(documented.OrderBy(o => o), options.Where(o => o != "-*").OrderBy(o => o));
    }

    [Fact]
    public void TheRunScript_RemovesTheLevelsTheStepsSave()
    {
        Assert.Contains($"\ntest_level_prefix={TestLevels.Prefix}\n", RepoFiles.Read("tools/ingame-test.sh").Replace("\r\n", "\n"));
    }

    [Theory]
    [InlineData("smoke.zlstest")]
    [InlineData("usages.zlstest")]
    [InlineData("test-mode.zlstest")]
    [InlineData("keys.zlstest")]
    [InlineData("libraries.zlstest")]
    [InlineData("list-footer.zlstest")]
    [InlineData("startup-warning.zlstest")]
    [InlineData("select-layout-each-frame.zlstest")]
    [InlineData("set-block-setting.zlstest")]
    [InlineData("kept-state.zlstest")]
    [InlineData("group-block-turned-off.zlstest")]
    [InlineData("turned-off-back-on.zlstest")]
    [InlineData("window-size.zlstest")]
    [InlineData("placed-after-test-mode.zlstest")]
    [InlineData("readme-screenshots.zlstest")]
    [InlineData("wire-select-blocks.zlstest")]
    [InlineData("signals-and-positions.zlstest")]
    [InlineData("import-selected-edges.zlstest")]
    [InlineData("set-stored-setting.zlstest")]
    [InlineData("orphaned-groups.zlstest")]
    [InlineData("import-orphan.zlstest")]
    [InlineData("import-orphan-draft.zlstest")]
    [InlineData("import-orphan-kept.zlstest")]
    [InlineData("import-delay-feedback.zlstest")]
    [InlineData("import-contraption.zlstest")]
    [InlineData("draft-older-copy.zlstest")]
    public void TheShippedScenarios_CanBeRead(string file)
    {
        var text = RepoFiles.Read($"Zeepkist.LogicScripting.InGameTests/Scenarios/{file}");

        var scenario = ScenarioParser.Parse(file, text, out var error);

        Assert.Null(error);
        Assert.NotEmpty(scenario.Steps);
    }

    /// <summary>
    /// The first column of the table under <paramref name="heading"/> in docs/ingame-tests.md, without its backticks.
    /// </summary>
    private static string[] DocsTable(string heading)
    {
        var docs = RepoFiles.Read("docs/ingame-tests.md").Replace("\r\n", "\n");
        var section = docs.Substring(docs.IndexOf(heading + "\n", StringComparison.Ordinal));
        return [.. section.Split('\n')
            .SkipWhile(line => !line.StartsWith('|'))
            .TakeWhile(line => line.StartsWith('|'))
            .Skip(2)
            .Select(line => line.Split('|')[1].Trim().Replace("`", string.Empty))];
    }
}
