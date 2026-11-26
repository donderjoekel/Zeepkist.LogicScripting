using System.Linq;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using TNRD.Zeepkist.LogicScripting.UI;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed partial class StepContext
{
    /// <summary>
    /// The Orphaned Groups the window lists, for a step's result.
    /// </summary>
    public string DescribeOrphans()
    {
        var orphans = Window.Orphans.Select(o => $"\"{OrphanedGroupsPanel.GetName(o)}\" with {o.BlockCount} blocks and {(o.Draft == null ? "no Draft" : "a Draft")}");
        return Window.Orphans.Count == 0 ? "no Orphaned Groups" : string.Join(", ", orphans);
    }

    /// <summary>
    /// The Orphaned Group whose row has the name, or null after failing the step when there's none.
    /// </summary>
    public OrphanedGroup RequireOrphan(string name)
    {
        var orphan = Window.FindOrphan(name);
        if (orphan == null)
            Fail($"no Orphaned Group is called \"{name}\"", name, DescribeOrphans());

        return orphan;
    }

    /// <summary>
    /// For a step that remembers a block by a name another step already placed or named a block as.
    /// </summary>
    public void FailNameTaken(string name)
    {
        Fail($"a block was already placed or named as \"{name}\"", "a new name", PlacedBlocks.Listed);
    }

    /// <summary>
    /// For a step whose text has <c>{uid:&lt;name&gt;}</c> with a name no block was placed or named as.
    /// </summary>
    public void FailUnknownUid(string name)
    {
        Fail($"no block placed or named as \"{name}\": add place-block <function> as {name} or name-group-block first", name, PlacedBlocks.Listed);
    }

    /// <summary>
    /// The Logic Block type with the Block Function's name, or null after failing the step when there's none.
    /// </summary>
    public BlockType RequireBlockType(string function)
    {
        var type = BlockManifest.All.FirstOrDefault(t => t.Function == function);
        if (type == null)
            Fail($"no Logic Block type is called '{function}'");

        return type;
    }

    /// <summary>
    /// The block the Scenario gave the name, or null after failing the step when there's none in the level.
    /// </summary>
    public NamedBlock RequireNamedBlock(LEV_LevelEditorCentral central, string name)
    {
        var uid = RequireUid(name);
        if (uid == null)
            return null;

        var block = NamedBlock.Find(central, name, uid, out var error);
        if (block == null)
            Fail(error);

        return block;
    }

    /// <summary>
    /// The block the Scenario gave the name, as it runs in test mode, or null after failing the step when the game isn't
    /// in test mode or the running level has no such block.
    /// </summary>
    public RunningBlock RequireRunningBlock(string name)
    {
        if (!TestModeControls.IsActive)
        {
            Fail("not in test mode: add test-mode first");
            return null;
        }

        var uid = RequireUid(name);
        if (uid == null)
            return null;

        var block = RunningBlock.Find(name, uid, out var error);
        if (block == null)
            Fail(error);

        return block;
    }

    /// <summary>
    /// The Wire between the named blocks' Connectors, whether the level has it or not, or null after failing the step
    /// when one of the blocks or Connectors doesn't exist.
    /// </summary>
    public BlockWire RequireWire(LEV_LevelEditorCentral central, WireArgument wire, out NamedBlock from, out NamedBlock to)
    {
        to = null;
        from = RequireNamedBlock(central, wire.From);
        if (from == null)
            return null;

        to = RequireNamedBlock(central, wire.To);
        if (to == null)
            return null;

        var output = NamedBlock.FindConnector(wire.From, "output", from.Outputs, wire.Output, out var error);
        if (output == null)
        {
            Fail(error);
            return null;
        }

        var input = NamedBlock.FindConnector(wire.To, "input", to.Type.Inputs, wire.Input, out error);
        if (input == null)
        {
            Fail(error);
            return null;
        }

        return new BlockWire(from.Block.UID, output.Index, to.Block.UID, input.Index);
    }

    /// <summary>
    /// The level editor, or null after failing the step when the game isn't in it.
    /// </summary>
    public LEV_LevelEditorCentral RequireEditor()
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
            Fail("not in the level editor");

        return central;
    }

    /// <summary>
    /// Like <see cref="RequireEditor"/>, with blocks selected and none grabbed too.
    /// </summary>
    public LEV_LevelEditorCentral RequireSelectedBlocks()
    {
        var central = RequireEditor();
        if (central == null)
            return null;

        if (central.gizmos.isGrabbing)
            Fail("blocks are grabbed: add drop-blocks first");
        else if (central.selection.list.Count == 0)
            Fail("no blocks are selected: add select-blocks first");

        return Failed ? null : central;
    }

    /// <summary>
    /// Fails the step when the window isn't open, as a player can't use its buttons then. Returns whether it is.
    /// </summary>
    public bool RequireOpenWindow()
    {
        if (!Window.IsOpen)
            Fail("the window isn't open: add open-window first");

        return Window.IsOpen;
    }

    /// <summary>
    /// Fails the step when no field in the window has focus, as keys typed then go to the level editor. Returns whether
    /// one has.
    /// </summary>
    public bool RequireFocusedField()
    {
        if (!RequireOpenWindow())
            return false;

        if (!Window.IsFieldFocused)
            Fail("no field is focused: add caret-at-start or caret-at-end first");

        return Window.IsFieldFocused;
    }

    /// <summary>
    /// Like <see cref="RequireOpenWindow"/>, with a Script selected too.
    /// </summary>
    public bool RequireScript()
    {
        if (!RequireOpenWindow())
            return false;

        if (!Window.HasScript)
            Fail("no Script selected: add new-script or select-script first");

        return Window.HasScript;
    }

    /// <summary>
    /// Like <see cref="RequireOpenWindow"/>, with a Script or a Library selected too, for a step on the editor's text.
    /// </summary>
    public bool RequireText()
    {
        if (!RequireOpenWindow())
            return false;

        if (!Window.HasText)
            Fail("no Script or Library selected: add new-script, select-script, new-library or select-library first");

        return Window.HasText;
    }

    /// <summary>
    /// The UID of the block the Scenario gave the name, or null after failing the step when it gave none.
    /// </summary>
    private string RequireUid(string name)
    {
        if (PlacedBlocks.TryGetUid(name, out var uid))
            return uid;

        Fail($"no block was placed or named as \"{name}\": add place-block ... as {name} or name-group-block first", "a placed block's name", PlacedBlocks.Listed);
        return null;
    }
}
