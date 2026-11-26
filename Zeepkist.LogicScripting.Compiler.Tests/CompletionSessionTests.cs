using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class CompletionSessionTests
{
    [Fact]
    public void AcceptingAFunction_OpensWithItsNamedArguments()
    {
        var session = Open("x = logi");

        var accepted = session.Accept(0)!.Value;
        session.Update(accepted, typedName: false, whileTyping: true, focused: true);

        Assert.Equal("x = logic(", accepted.Text);
        Assert.True(session.IsOpen);
        Assert.Equal(CompletionKind.Argument, session.Items[0].Kind);
    }

    [Theory]
    [InlineData("x = placed", true)]
    [InlineData("x = logi", false)]
    public void AcceptingAFunction_StaysClosed_WithoutNamedArgumentsOrWhenNotCompletingWhileTyping(string text, bool whileTyping)
    {
        var session = Open(text);

        var accepted = session.Accept(0)!.Value;
        session.Update(accepted, typedName: false, whileTyping: whileTyping, focused: true);

        Assert.False(session.IsOpen);
    }

    [Theory]
    [InlineData("x = logic(a, b, op:")]
    [InlineData("x = logic(a, b, op: ")]
    [InlineData("default hide: ")]
    public void TypingASettingsColonOrASpaceAfterIt_OpensWithItsValues(string text)
    {
        var session = new CompletionSession();

        session.Update(new TextSnapshot(text, text.Length), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        Assert.True(session.IsOpen);
        Assert.Equal(CompletionKind.Value, session.Items[0].Kind);
    }

    [Fact]
    public void AcceptingASetting_OpensWithItsValues()
    {
        var session = Open("x = logic(a, o");

        var accepted = session.Accept(0)!.Value;
        session.Update(accepted, typedName: false, whileTyping: true, focused: true);

        Assert.Equal("x = logic(a, op: ", accepted.Text);
        Assert.True(session.IsOpen);
        Assert.Equal("and", session.Items[0].Label);
    }

    [Fact]
    public void AcceptingASettingBeforeItsColon_StaysClosed()
    {
        var session = new CompletionSession();
        session.Request();
        session.Update(new TextSnapshot("x = logic(a, o: and)", 14), typedName: false, whileTyping: true, focused: true);

        var accepted = session.Accept(0)!.Value;
        session.Update(accepted, typedName: false, whileTyping: true, focused: true);

        Assert.Equal("x = logic(a, op: and)", accepted.Text);
        Assert.False(session.IsOpen);
    }

    [Fact]
    public void AcceptingAnInput_StaysClosed()
    {
        var session = Open("x = logic(a, b");

        var accepted = session.Accept(0)!.Value;
        session.Update(accepted, typedName: false, whileTyping: true, focused: true);

        Assert.Equal("x = logic(a, b: ", accepted.Text);
        Assert.False(session.IsOpen);
    }

    [Fact]
    public void AcceptingAName_StaysClosed()
    {
        var session = Open("abc = 1\nx = logic(1, ab");

        var accepted = session.Accept(session.Selected)!.Value;
        session.Update(accepted, typedName: false, whileTyping: true, focused: true);

        Assert.False(session.IsOpen);
    }

    [Fact]
    public void Closes_WhenNothingMatches()
    {
        var session = Open("x = l");

        session.Update(new TextSnapshot("x = lzz", 7), typedName: true, whileTyping: true, focused: true);

        Assert.False(session.IsOpen);
    }

    [Fact]
    public void Closes_WhenTheCaretMovesToAnotherWord()
    {
        var session = Open("x = lo & lo");

        session.Update(new TextSnapshot("x = lo & lo", 6), typedName: false, whileTyping: true, focused: true);

        Assert.False(session.IsOpen);
    }

    [Fact]
    public void Closes_WhenUnfocusedOrTextIsSelected()
    {
        var unfocused = Open("x = lo");
        var selecting = Open("x = lo");

        unfocused.Update(new TextSnapshot("x = lo", 6), typedName: false, whileTyping: true, focused: false);
        selecting.Update(new TextSnapshot("x = lo", 6, -2), typedName: false, whileTyping: true, focused: true);

        Assert.False(unfocused.IsOpen);
        Assert.False(selecting.IsOpen);
    }

    [Fact]
    public void KeepsTheSelection_WhileTheTextIsUnchanged()
    {
        var session = Open("x = ");
        session.Select(2, wrap: false);

        session.Update(new TextSnapshot("x = ", 4), typedName: false, whileTyping: true, focused: true);

        Assert.Equal(2, session.Selected);
    }

    [Fact]
    public void Select_WrapsAroundTheEnds_OrStopsAtThem()
    {
        var session = Open("x = ");
        var last = session.Items.Count - 1;

        session.Select(-1, wrap: true);
        Assert.Equal(last, session.Selected);

        session.Select(last + 1, wrap: true);
        Assert.Equal(0, session.Selected);

        session.Select(-1, wrap: false);
        Assert.Equal(0, session.Selected);

        session.Select(last + 1, wrap: false);
        Assert.Equal(last, session.Selected);
    }

    [Fact]
    public void Select_ScrollsTheSelectionIntoView()
    {
        var session = Open("x = ");

        session.Select(CompletionSession.MaxRows, wrap: false);
        Assert.Equal(1, session.FirstRow);

        session.Select(0, wrap: false);
        Assert.Equal(0, session.FirstRow);
    }

    [Fact]
    public void Accept_WritesTheItem_AndCloses()
    {
        var session = Open("x = lo");

        var accepted = session.Accept(session.Selected);

        Assert.Equal("x = logic(", accepted?.Text);
        Assert.False(session.IsOpen);
    }

    [Fact]
    public void Items_DefinedNames_WithThePlacedBlocksTypeFromTheLevel()
    {
        var session = new CompletionSession(new EditorLookups(uid => uid == "L" ? new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId) : null));
        const string Text = "lamp_1 = placed(\"L\")\nlamp_1(intensity: la";

        session.Update(new TextSnapshot(Text, Text.Length), typedName: true, whileTyping: true, focused: true);

        var item = Assert.Single(session.Items, i => i.Label == "lamp_1");
        Assert.Equal("lamp", item.Detail);
    }

    [Fact]
    public void Opening_ReadsThePlacedBlocksTypeAgain()
    {
        PlacedBlock lamp = null;
        var session = new CompletionSession(new EditorLookups(_ => lamp));
        const string Text = "lamp_1 = placed(\"L\")\nx = la";
        session.Request();
        session.Update(new TextSnapshot(Text, Text.Length), typedName: false, whileTyping: true, focused: true);
        session.Close();

        lamp = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId);
        session.Request();
        session.Update(new TextSnapshot(Text, Text.Length), typedName: false, whileTyping: true, focused: true);

        Assert.Equal("lamp", Assert.Single(session.Items, i => i.Label == "lamp_1").Detail);
    }

    [Fact]
    public void Accept_NothingWhenClosed()
    {
        var session = new CompletionSession();

        Assert.Null(session.Accept(0));
    }

    /// <summary>
    /// Opened with Ctrl+Space, with the caret at the end.
    /// </summary>
    private static CompletionSession Open(string text)
    {
        var session = new CompletionSession();
        session.Request();
        session.Update(new TextSnapshot(text, text.Length), typedName: false, whileTyping: true, focused: true);
        Assert.True(session.IsOpen);
        return session;
    }
}
