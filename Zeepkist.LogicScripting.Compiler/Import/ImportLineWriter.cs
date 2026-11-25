using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// Writes one line of an import's text, with the blocks nested into it.
/// </summary>
internal sealed class ImportLineWriter
{
    private static readonly int _unaryPrecedence = BinaryOperator.All.Max(o => o.Precedence) + 1;

    private readonly ImportGraph _graph;
    private readonly ImportShapes _shapes;
    private readonly ICollection<string> _nested;
    private readonly Func<string, string> _getName;
    private readonly int _line;
    private readonly StringBuilder _text = new();
    private readonly Dictionary<(Position Position, BlockType Type), string> _places = [];
    private int _depth;

    /// <param name="graph">What to write.</param>
    /// <param name="shapes">How to write each block.</param>
    /// <param name="nested">The blocks written where they're used, instead of on a line of their own.</param>
    /// <param name="getName">The name of a block that isn't nested.</param>
    /// <param name="line">The line's number.</param>
    public ImportLineWriter(
        ImportGraph graph,
        ImportShapes shapes,
        ICollection<string> nested,
        Func<string, string> getName,
        int line = 1)
    {
        _graph = graph;
        _shapes = shapes;
        _nested = nested;
        _getName = getName;
        _line = line;
    }

    public string Text => _text.ToString();

    /// <summary>
    /// Where each block in the line is written, as <see cref="ImportedText.Places"/>.
    /// </summary>
    public IReadOnlyDictionary<(Position Position, BlockType Type), string> Places => _places;

    /// <summary>
    /// How deep the line nests, at least as deep as <see cref="Parser.MaxDepth"/> counts it.
    /// </summary>
    public int Depth { get; private set; }

    /// <summary>
    /// The definition of a block to import, or only its value when <paramref name="isNamed"/> is false.
    /// </summary>
    public void WriteStatement(string uid, bool isNamed)
    {
        if (!isNamed)
        {
            WriteBlock(uid, 0);
            return;
        }

        _text.Append(_getName(uid)).Append(" = ");
        if (!_shapes.TryGetPickedOutput(uid, out var output))
        {
            WriteBlock(uid, 0);
            return;
        }

        Enter();
        WriteBlock(uid, 0);
        _text.Append('.').Append(output.Name);
        Leave();
    }

    /// <summary>
    /// The call that wires into an outside block.
    /// </summary>
    public void WriteOutsideCall(string uid)
    {
        Enter();
        _text.Append(_getName(uid)).Append('(');
        WriteArguments(GetInputs(uid));
        _text.Append(')');
        Leave();
    }

    private void WriteBlock(string uid, int minPrecedence)
    {
        Enter();
        if (_shapes.TryGetUnary(uid, out var unary))
            WriteUnary(uid, unary);
        else if (_shapes.TryGetBinary(uid, out var binary))
            WriteBinary(uid, binary, minPrecedence);
        else
            WriteCall(uid);

        Leave();
    }

    private void WriteUnary(string uid, UnaryOperator op)
    {
        var inputs = op.Block.Type.Inputs;
        foreach (var literal in op.LeadingLiterals)
            AddPlace(BlockManifest.Generator, _shapes.GetLiteralUid(literal));

        AddPlace(op.Block.Type, uid);
        _text.Append(op.Symbol);
        WriteFeed(_graph.FindWire(uid, inputs[op.LeadingLiterals.Count]), _unaryPrecedence);
    }

    private void WriteBinary(string uid, BinaryOperator op, int minPrecedence)
    {
        var inputs = op.Block.Type.Inputs;
        var needsParentheses = op.Precedence < minPrecedence;
        if (needsParentheses)
        {
            Enter();
            _text.Append('(');
        }

        // Every operator is left-associative
        WriteFeed(_graph.FindWire(uid, inputs[0]), op.Precedence);
        _text.Append(' ');
        AddPlace(op.Block.Type, uid);
        _text.Append(op.Symbol).Append(' ');
        WriteFeed(_graph.FindWire(uid, inputs[1]), op.Precedence + 1);

        if (needsParentheses)
        {
            _text.Append(')');
            Leave();
        }
    }

    private void WriteCall(string uid)
    {
        var block = _graph.GetImported(uid);
        BlockManifest.TryGetBlock(block.BlockId, out var type, out var style);
        AddPlace(type, uid);
        _text.Append(type.Function).Append('(');

        List<Action> arguments = [.. GetInputs(uid), .. GetSettings(block, type.Settings)];
        if (!_shapes.HidesAll)
            arguments.AddRange(GetSettings(block, [BlockSetting.Hide]));

        if (style != type.DefaultStyle)
            arguments.Add(() => WriteSetting(BlockType.StyleName, style.Name));

        WriteArguments(arguments);
        _text.Append(')');
    }

    private void WriteArguments(IEnumerable<Action> arguments)
    {
        var isFirst = true;
        foreach (var write in arguments)
        {
            if (!isFirst)
                _text.Append(", ");

            isFirst = false;
            write();
        }
    }

    /// <summary>
    /// By position up to the first input without a Wire, and by name after it.
    /// </summary>
    private IEnumerable<Action> GetInputs(string uid)
    {
        var skipped = false;
        foreach (var input in _graph.GetType(uid).Inputs)
        {
            var wire = _graph.FindWire(uid, input);
            if (wire == null)
            {
                skipped = true;
                continue;
            }

            var prefix = skipped ? $"{input.Name}: " : "";
            yield return () =>
            {
                _text.Append(prefix);
                WriteFeed(wire, 0);
            };
        }
    }

    /// <summary>
    /// Leaves out what a Script can't write, which <see cref="ImportCompare"/> then finds missing.
    /// </summary>
    private IEnumerable<Action> GetSettings(ImportedBlock block, IReadOnlyList<BlockSetting> settings)
    {
        foreach (var setting in settings)
        {
            if (setting.Type == SettingType.Targets)
                yield return () => WriteSetting(setting.Name, $"[{string.Join(", ", block.FindTargets(setting).Select(_getName))}]");
            else if (block.FindValues(setting).FirstOrDefault() is { } stored && setting.ToText(stored) is { } text)
                yield return () => WriteSetting(setting.Name, text);
        }
    }

    /// <summary>
    /// Two levels, as many as a list of names takes.
    /// </summary>
    private void WriteSetting(string name, string value)
    {
        Enter();
        Enter();
        _text.Append(name).Append(": ").Append(value);
        Leave();
        Leave();
    }

    private void WriteFeed(ImportedWire wire, int minPrecedence)
    {
        var uid = wire.FromUid;
        if (_nested.Contains(uid))
        {
            WriteBlock(uid, minPrecedence);
            return;
        }

        Enter();
        if (_shapes.TryGetLiteral(uid, out var literal))
        {
            AddPlace(BlockManifest.Generator, _shapes.GetLiteralUid(literal));
            _text.Append(literal.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            _text.Append(_getName(uid));
            var outputs = _graph.GetOutputs(uid);
            if (outputs.Count > 1 && !_shapes.TryGetPickedOutput(uid, out _))
            {
                Enter();
                _text.Append('.').Append(outputs.First(o => o.Index == wire.FromConnector).Name);
                Leave();
            }
        }

        Leave();
    }

    private void AddPlace(BlockType type, string uid)
    {
        _places.Add((new Position(_line, _text.Length + 1), type), uid);
    }

    private void Enter()
    {
        _depth++;
        Depth = Math.Max(Depth, _depth);
    }

    private void Leave()
    {
        _depth--;
    }
}
