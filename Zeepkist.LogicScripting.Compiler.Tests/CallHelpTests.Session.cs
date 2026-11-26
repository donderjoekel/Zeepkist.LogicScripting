using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public partial class CallHelpTests
{
    [Fact]
    public void Session_ForALibrarysFunction_ItsInputs()
    {
        var session = new CallHelpSession(new EditorLookups(findLibrary: Library(TwoInputLibrary)));

        session.Update(Snapshot("x = timing.edge(1, |"), focused: true);

        Assert.Equal("edge", session.Help.Callee);
    }

    [Fact]
    public void Session_Dismissed_InACallToALibrarysFunction_StaysHiddenWhileTheCaretIsInIt()
    {
        var session = new CallHelpSession(new EditorLookups(findLibrary: Library(TwoInputLibrary)));
        session.Update(Snapshot("x = timing.edge(|"), focused: true);
        session.Dismiss();

        session.Update(Snapshot("x = timing.edge(1, |"), focused: true);

        Assert.Null(session.Help);
    }

    [Fact]
    public void Session_ShowsTheHelpWhileFocused()
    {
        var session = new CallHelpSession();

        session.Update(Snapshot("x = logic(a, |"), focused: true);
        Assert.Equal("logic", session.Help.Callee);

        session.Update(Snapshot("x = logic(a, |"), focused: false);
        Assert.Null(session.Help);
    }

    [Fact]
    public void Session_Dismiss_HidesUntilTheCaretLeavesTheCall()
    {
        var session = new CallHelpSession();
        session.Update(Snapshot("x = logic(a, |"), focused: true);

        session.Dismiss();
        Assert.Null(session.Help);

        session.Update(Snapshot("x = logic(a, b|"), focused: true);
        Assert.Null(session.Help);

        session.Update(Snapshot("x = logic(a, b)|"), focused: true);
        Assert.Null(session.Help);

        session.Update(Snapshot("x = logic(a, b|)"), focused: true);
        Assert.Equal("logic", session.Help.Callee);
    }

    [Fact]
    public void Session_Dismiss_StaysHiddenInACallInsideTheDismissedOne()
    {
        var session = new CallHelpSession();
        session.Update(Snapshot("x = logic(a, |"), focused: true);
        session.Dismiss();

        session.Update(Snapshot("x = logic(a, delay(|"), focused: true);
        Assert.Null(session.Help);

        session.Update(Snapshot("x = logic(a, delay(b)|"), focused: true);
        Assert.Null(session.Help);
    }

    [Fact]
    public void Session_Dismiss_ShowsAnotherCallsHelp()
    {
        var session = new CallHelpSession();
        session.Update(Snapshot("x = logic(1, 2)\ny = logic(a, delay(|"), focused: true);
        session.Dismiss();

        session.Update(Snapshot("x = logic(1, |2)\ny = logic(a, delay("), focused: true);

        Assert.Equal("logic", session.Help.Callee);
    }

    [Fact]
    public void Session_Dismiss_ShowsANewCallWithItsParenAtTheSamePlace()
    {
        var session = new CallHelpSession();
        session.Update(Snapshot("v = logic(|"), focused: true);
        session.Dismiss();

        session.Update(Snapshot("w = logic(|"), focused: true);

        Assert.Equal("logic", session.Help.Callee);
    }

    [Fact]
    public void Session_Dismiss_ShowsANewCalleeWithItsParenAtTheSamePlace()
    {
        var session = new CallHelpSession();
        session.Update(Snapshot("v = logic(|"), focused: true);
        session.Dismiss();

        session.Update(Snapshot("v = delay(|"), focused: true);

        Assert.Equal("delay", session.Help.Callee);
    }

    [Fact]
    public void Session_Dismiss_ForgetsTheCallWhenALineAboveChanges()
    {
        var session = new CallHelpSession();
        session.Update(Snapshot("a = 1\nv = logic(|"), focused: true);
        session.Dismiss();

        session.Update(Snapshot("b = 1\nv = logic(|"), focused: true);

        Assert.Equal("logic", session.Help.Callee);
    }

    [Fact]
    public void Session_Dismiss_WithoutHelpHidesNothing()
    {
        var session = new CallHelpSession();
        session.Update(Snapshot("x = 1|"), focused: true);
        session.Dismiss();

        session.Update(Snapshot("x = logic(|"), focused: true);

        Assert.Equal("logic", session.Help.Callee);
    }
}
