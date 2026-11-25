using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

public sealed class CompiledBlock
{
    public CompiledBlock(BlockType type, BlockStyle style, IReadOnlyList<Setting> settings, GridCell cell, int yaw, Position position = default)
    {
        Type = type;
        Style = style;
        Settings = settings;
        Cell = cell;
        Yaw = yaw;
        Position = position;
    }

    public BlockType Type { get; }

    public BlockStyle Style { get; }

    public IReadOnlyList<Setting> Settings { get; }

    /// <summary>
    /// The block's pivot cell, in world axes, relative to the group's center.
    /// </summary>
    public GridCell Cell { get; }

    /// <summary>
    /// Rotation about Y in degrees.
    /// </summary>
    public int Yaw { get; }

    /// <summary>
    /// The call, operator or literal the block is written as. Not unique: each call to a Function places its body's
    /// blocks, and the two blocks of <c>-x</c> are both at the sign. Inside a Library's Function it's a place in the
    /// Library's text.
    /// </summary>
    internal Position Position { get; }
}
