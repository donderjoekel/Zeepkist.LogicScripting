using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class ReferenceEntryLayoutTests
{
    [Fact]
    public void BlockEntry_HasItsInputsAndOutputsAsRows_AndItsSettingsUnderAHeading()
    {
        var layout = TestReferences.Find("logic").Layout;

        Assert.Equal("logic", layout.Function);
        Assert.Equal(string.Empty, layout.Note);
        Assert.Equal([("inputs", "a, b, op_in"), ("outputs", "out")], TestReferences.Rows(layout.Rows));
        var group = Assert.Single(layout.Groups);
        Assert.Equal("settings", group.Heading);
        Assert.Equal([("op", "and, or, nand, nor, xor, xnor"), ("hide", "true, false")], TestReferences.Rows(group.Rows));
    }

    [Fact]
    public void BlockEntryWithModes_HasItsOutputsPerModeUnderAHeading_BeforeItsSettings()
    {
        var layout = TestReferences.Find("simple_telemetry").Layout;

        Assert.Equal([("inputs", "–")], TestReferences.Rows(layout.Rows));
        Assert.Equal(["outputs", "settings"], layout.Groups.Select(g => g.Heading));
        Assert.Equal(("basic", "speed, yaw, up, state"), TestReferences.Rows(layout.Groups[0].Rows)[0]);
    }

    [Fact]
    public void BlockEntryWithoutSettings_HasNoSettingsHeading()
    {
        Assert.Empty(TestReferences.Find("trigger").Layout.Groups);
    }

    [Fact]
    public void BlockEntryWithAliases_NotesThem()
    {
        Assert.Equal("alias mux", TestReferences.Find("multiplexer").Layout.Note);
    }

    [Fact]
    public void PlacedOnlyBlockEntry_NotesHowToReachIt()
    {
        Assert.Equal("placed(\"UID\") only", TestReferences.Find("lamp").Layout.Note);
    }

    [Fact]
    public void LibraryEntry_HasItsInputsAndOutputsAsRows()
    {
        var layout = Assert.Single(LibraryReference.Describe(TestLibraries.WithLibrary("timing", "function edge(x) -> rise\n    rise = x\nend\n"))).Layout;

        Assert.Equal("timing.edge", layout.Function);
        Assert.Equal(string.Empty, layout.Note);
        Assert.Equal([("inputs", "x"), ("outputs", "rise")], TestReferences.Rows(layout.Rows));
        Assert.Empty(layout.Groups);
    }

    [Fact]
    public void LibraryEntryWhoseLibraryHasErrors_NotesItCantBeCalled()
    {
        var layout = Assert.Single(LibraryReference.Describe(TestLibraries.WithLibrary("broken", "x = 1\nfunction a() -> y\n    y = 1\nend\n"))).Layout;

        Assert.Equal("its Library has errors, so it can't be called", layout.Note);
    }
}
