using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class CompletionSessionTests
{
    [Fact]
    public void TypingAName_Opens_WhenCompletingWhileTyping()
    {
        var session = new CompletionSession();

        session.Update(new TextSnapshot("x = lo", 6), typedName: true, whileTyping: true, focused: true);

        Assert.True(session.IsOpen);
        Assert.Equal("logic", session.Items[0].Label);
        Assert.Equal(4, session.WordStart);
    }

    [Fact]
    public void TypingAName_StaysClosed_WhenNotCompletingWhileTyping()
    {
        var session = new CompletionSession();

        session.Update(new TextSnapshot("x = lo", 6), typedName: true, whileTyping: false, focused: true);

        Assert.False(session.IsOpen);
        Assert.Null(session.WordStart);
    }

    [Fact]
    public void Request_OpensWithoutAPrefix()
    {
        var session = new CompletionSession();

        session.Request();
        session.Update(new TextSnapshot("x = ", 4), typedName: false, whileTyping: false, focused: true);

        Assert.True(session.IsOpen);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Request_IsPending_UntilTheNextUpdate(bool focused)
    {
        var session = new CompletionSession();

        session.Request();
        Assert.True(session.IsRequestPending);

        session.Update(new TextSnapshot("x = ", 4), typedName: false, whileTyping: false, focused: focused);
        Assert.False(session.IsRequestPending);
    }

    [Fact]
    public void AnEmptyPrefix_Closes_UnlessRequested()
    {
        var session = new CompletionSession();
        session.Update(new TextSnapshot("x = l", 5), typedName: true, whileTyping: true, focused: true);
        Assert.True(session.IsOpen);

        session.Update(new TextSnapshot("x = ", 4), typedName: false, whileTyping: true, focused: true);

        Assert.False(session.IsOpen);
    }

    [Theory]
    [InlineData("x = logic(")]
    [InlineData("x = logic(1,")]
    [InlineData("x = logic(1, ")]
    [InlineData("x = logic(1, delay(")]
    public void TypingACallsParenthesis_OpensWithItsNamedArguments(string text)
    {
        var session = new CompletionSession();

        session.Update(new TextSnapshot(text, text.Length), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        Assert.True(session.IsOpen);
        Assert.Equal(CompletionKind.Argument, session.Items[0].Kind);
    }

    [Fact]
    public void TypingAPlacedBlockReferencesParenthesis_OpensWithItsInputs()
    {
        var session = new CompletionSession(new EditorLookups(uid => uid == "L" ? new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId) : null));
        const string Text = "lamp_1 = placed(\"L\")\nlamp_1(";

        session.Update(new TextSnapshot(Text, Text.Length), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        Assert.True(session.IsOpen);
        Assert.Equal("angle:", session.Items[0].Label);
    }

    [Fact]
    public void TypingADotAfterABlocksName_OpensWithItsOutputs()
    {
        var session = new CompletionSession();
        const string Text = "t = simple_telemetry(mode: global_velocity)\nx = t.";

        session.Update(new TextSnapshot(Text, Text.Length), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        Assert.True(session.IsOpen);
        Assert.Equal(["x", "y", "z", "heading"], session.Items.Select(i => i.Label));
    }

    [Fact]
    public void TypingADotAfterAPlacedCall_OpensWithTheOutputsOfTheBlockInTheLevel()
    {
        var session = new CompletionSession(new EditorLookups(uid => uid == "D" ? new PlacedBlock(BlockManifest.DistanceSensor.DefaultStyle.BlockId) : null));
        const string Text = "x = placed(\"D\").";

        session.Update(new TextSnapshot(Text, Text.Length), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        Assert.True(session.IsOpen);
        Assert.Equal(["distance", "x", "y", "z"], session.Items.Select(i => i.Label));
    }

    [Fact]
    public void TypingADotAfterALibraryFunctionsCall_OpensWithItsOutputs()
    {
        var session = new CompletionSession(new EditorLookups(findLibrary: name => name == "timing" ? "function edge(a) -> lo, hi\n  lo = a\n  hi = !a\nend" : null));
        const string Text = "x = timing.edge(1).";

        session.Update(new TextSnapshot(Text, Text.Length), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        Assert.True(session.IsOpen);
        Assert.Equal(["lo", "hi"], session.Items.Select(i => i.Label));
    }

    [Fact]
    public void TypingADotAfterALibrarysName_OpensWithItsFunctions()
    {
        var session = new CompletionSession(new EditorLookups(findLibrary: name => name == "timing" ? "function edge(a) -> lo, hi\n  lo = a\n  hi = !a\nend" : null));
        const string Text = "x = timing.";

        session.Update(new TextSnapshot(Text, Text.Length), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        Assert.True(session.IsOpen);
        Assert.Equal(["edge"], session.Items.Select(i => i.Label));
    }

    [Fact]
    public void TypingADotAfterACall_OpensWithItsOutputs()
    {
        var session = new CompletionSession();
        const string Text = "x = simple_telemetry(mode: global_velocity).";

        session.Update(new TextSnapshot(Text, Text.Length), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        Assert.True(session.IsOpen);
        Assert.Equal(["x", "y", "z", "heading"], session.Items.Select(i => i.Label));
    }

    [Theory]
    [InlineData("x = 1.")]
    [InlineData("x = a & b\ny = x.")]
    [InlineData("x = f().")]
    public void TypingADot_StaysClosed_WithoutOutputs(string text)
    {
        var session = new CompletionSession();

        session.Update(new TextSnapshot(text, text.Length), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        Assert.False(session.IsOpen);
    }

    [Theory]
    [InlineData("x = (")]
    [InlineData("x = ")]
    [InlineData("x = logic(a ")]
    [InlineData("x = logic(b: ")]
    [InlineData("x = delay(1, ticks: ")]
    [InlineData("x = logic(1) ")]
    [InlineData("x = placed(")]
    [InlineData("a = logic(1, 2)\nx = a(")]
    public void TypingAParenthesis_StaysClosed_WithoutNamedArguments(string text)
    {
        var session = new CompletionSession();

        session.Update(new TextSnapshot(text, text.Length), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        Assert.False(session.IsOpen);
    }

    [Fact]
    public void TypingACallsParenthesis_StaysClosed_WhenNotCompletingWhileTyping()
    {
        var session = new CompletionSession();

        session.Update(new TextSnapshot("x = logic(", 10), typedName: false, whileTyping: false, focused: true, typedSeparator: true);

        Assert.False(session.IsOpen);
    }

    [Fact]
    public void TypingACallsParenthesis_WhileTheListIsOpen_ListsItsNamedArguments()
    {
        var session = Open("x = logic");

        session.Update(new TextSnapshot("x = logic(", 10), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        Assert.True(session.IsOpen);
        Assert.Equal(10, session.WordStart);
        Assert.Equal(CompletionKind.Argument, session.Items[0].Kind);
    }

    [Fact]
    public void AfterACallsParenthesis_TypingFiltersTheList_AndTheNextArgumentOpensItAgain()
    {
        var session = new CompletionSession();
        session.Update(new TextSnapshot("x = logic(", 10), typedName: false, whileTyping: true, focused: true, typedSeparator: true);

        session.Update(new TextSnapshot("x = logic(o", 11), typedName: true, whileTyping: true, focused: true);
        Assert.Equal("op:", session.Items[0].Label);

        session.Update(new TextSnapshot("x = logic(1", 11), typedName: false, whileTyping: true, focused: true);
        Assert.False(session.IsOpen);

        session.Update(new TextSnapshot("x = logic(1,", 12), typedName: false, whileTyping: true, focused: true, typedSeparator: true);
        session.Update(new TextSnapshot("x = logic(1, ", 13), typedName: false, whileTyping: true, focused: true, typedSeparator: true);
        Assert.True(session.IsOpen);
        Assert.Equal("b:", session.Items[0].Label);
    }
}
