using System.IO;
using TNRD.Zeepkist.LogicScripting.InGameTests;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class StartScreenStateTests
{
    [Fact]
    public void AStuckStartScreen_NamesItsTextAndBothWaits()
    {
        var state = new StartScreenState
        {
            IntroPanelActive = false,
            PlayerManagerExists = true,
            Texts = ["Cleaning Up Old Backups... (0 KB)"],
        };

        Assert.Equal(
            "the game stayed on its start screen, showing \"Cleaning Up Old Backups... (0 KB)\"; intro panel not active, player manager there",
            state.Describe());
    }

    [Fact]
    public void Texts_AreOneLine_WithoutRichTextTags_EmptyOrRepeatedOnesLeftOut()
    {
        var state = new StartScreenState { Texts = ["<b>Loading</b>\n  now", " ", null, "Loading now", "v1"] };

        Assert.StartsWith("the game stayed on its start screen, showing \"Loading now\", \"v1\";", state.Describe());
    }

    [Fact]
    public void AScreenWithoutText_SaysSo()
    {
        Assert.StartsWith("the game stayed on its start screen, showing no text;", new StartScreenState().Describe());
    }

    [Fact]
    public void ManyTexts_AreCut_AndCounted()
    {
        var state = new StartScreenState { Texts = ["1", "2", "3", "4", "5", "6", "7"] };

        Assert.Contains("\"5\" and 2 more;", state.Describe());
    }

    [Fact]
    public void ALongText_IsCut()
    {
        var state = new StartScreenState { Texts = [new string('a', 100)] };

        Assert.Contains($"\"{new string('a', 80)}…\"", state.Describe());
    }

    [Fact]
    public void BeforeTheStartScreen_TheWaitsArentNamed()
    {
        var state = new StartScreenState { ScreenFound = false, Texts = ["Loading"] };

        Assert.Equal("the game showed no start screen; on screen: \"Loading\"", state.Describe());
    }

    [Fact]
    public void AStartScreenTheGameLeft_SaysSo_NotThatThereWasNone()
    {
        var state = new StartScreenState { ScreenLeft = true, PlayerManagerExists = false, Texts = ["Loading"] };

        Assert.Equal("the game left its start screen, player manager missing; on screen: \"Loading\"", state.Describe());
    }

    [Fact]
    public void Shown_ListsTheTextsAsTheDetailDoes()
    {
        Assert.Equal("\"Play\", \"Level Editor\"", StartScreenState.Shown(["<b>Play</b>", "Level Editor", "Play"]));
        Assert.Equal("no text", StartScreenState.Shown([" "]));
    }

    [Fact]
    public void ARunRequest_ReadsStuckStartAndLeaveStartScreen()
    {
        var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(folder, "run.json"),
                "{\"runId\":\"r1\",\"stuckStart\":true,\"leaveStartScreen\":true,\"scenarios\":[]}");

            Assert.True(RunRequest.TryRead(folder, ["game.exe", RunRequest.Flag, "r1"], out var request));
            Assert.True(request.StuckStart);
            Assert.True(request.LeaveStartScreen);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
