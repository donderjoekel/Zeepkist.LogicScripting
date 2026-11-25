using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// How each block to import is written: as a literal, as an operator or, when it's neither, as a call.
/// </summary>
internal sealed class ImportShapes
{
    private readonly ImportGraph _graph;
    private readonly ILookup<string, ImportedWire> _wiresFrom;
    private readonly Dictionary<string, long> _literals = [];
    private readonly Dictionary<long, string> _literalUids = [];
    private readonly Dictionary<string, BinaryOperator> _binaries = [];
    private readonly Dictionary<string, UnaryOperator> _unaries = [];
    private readonly Dictionary<string, BlockConnector> _pickedOutputs = [];

    public ImportShapes(ImportGraph graph, IReadOnlyList<ImportedBlock> ordered)
    {
        _graph = graph;
        _wiresFrom = graph.Wires.ToLookup(w => w.FromUid);
        HidesAll = ordered.All(b => b.FindValues(BlockSetting.Hide).Count > 0);

        foreach (var block in ordered.Where(IsLiteral))
        {
            var value = GetLiteralValue(block);
            _literals.Add(block.Uid, value);
            if (!_literalUids.ContainsKey(value))
                _literalUids.Add(value, block.Uid);
        }

        foreach (var block in ordered)
        {
            if (UnaryOperator.All.FirstOrDefault(op => IsUnary(block, op)) is { } unary)
                _unaries.Add(block.Uid, unary);
            else if (BinaryOperator.All.FirstOrDefault(op => IsBinary(block, op)) is { } binary)
                _binaries.Add(block.Uid, binary);
        }

        foreach (var block in ordered)
        {
            var outputs = graph.GetOutputs(block.Uid);
            var used = _wiresFrom[block.Uid].Select(w => w.FromConnector).Distinct().ToList();
            if (outputs.Count > 1 && used.Count == 1)
                _pickedOutputs.Add(block.Uid, outputs.First(o => o.Index == used[0]));
        }
    }

    /// <summary>
    /// Whether <c>default hide: true</c> hides every block, so no call has to.
    /// </summary>
    public bool HidesAll { get; }

    /// <summary>
    /// The Generators that are literals of a value another one already stands for, each with that one's UID.
    /// </summary>
    public IReadOnlyDictionary<string, string> Shared => _literals
        .Where(l => _literalUids[l.Value] != l.Key)
        .ToDictionary(l => l.Key, l => _literalUids[l.Value]);

    public bool IsUsed(string uid)
    {
        return _wiresFrom.Contains(uid);
    }

    /// <summary>
    /// Whether the block has one output, and that feeds one input.
    /// </summary>
    public bool IsUsedOnce(string uid)
    {
        return _graph.GetOutputs(uid).Count == 1 && _wiresFrom[uid].Count() == 1;
    }

    public bool TryGetLiteral(string uid, out long value)
    {
        return _literals.TryGetValue(uid, out value);
    }

    /// <summary>
    /// The Generator a literal of the value stands for.
    /// </summary>
    public string GetLiteralUid(long value)
    {
        return _literalUids[value];
    }

    public bool TryGetBinary(string uid, out BinaryOperator op)
    {
        return _binaries.TryGetValue(uid, out op);
    }

    public bool TryGetUnary(string uid, out UnaryOperator op)
    {
        return _unaries.TryGetValue(uid, out op);
    }

    /// <summary>
    /// The only output used of a block with several, which its definition picks, so its name stands for that output.
    /// </summary>
    public bool TryGetPickedOutput(string uid, out BlockConnector output)
    {
        return _pickedOutputs.TryGetValue(uid, out output);
    }

    private bool IsLiteral(ImportedBlock block)
    {
        return BlockManifest.GetPlacedType(block.BlockId) == BlockManifest.Generator
            && IsUsed(block.Uid)
            && HasNoFloats(block)
            && GetSettings(block).Keys.All(k => k == (SettingKind.Text, BlockManifest.GeneratorPowerKey));
    }

    private long GetLiteralValue(ImportedBlock block)
    {
        return GetSettings(block).TryGetValue((SettingKind.Text, BlockManifest.GeneratorPowerKey), out var power)
            ? long.Parse(power, CultureInfo.InvariantCulture)
            : 0;
    }

    private bool IsBinary(ImportedBlock block, BinaryOperator op)
    {
        return IsOperator(block, op.Block, 2);
    }

    private bool IsUnary(ImportedBlock block, UnaryOperator op)
    {
        var operand = op.LeadingLiterals.Count;
        if (!IsOperator(block, op.Block, operand + 1))
            return false;

        var inputs = op.Block.Type.Inputs;
        for (var i = 0; i < operand; i++)
        {
            if (!TryGetLiteral(_graph.FindWire(block.Uid, inputs[i]).FromUid, out var value) || value != op.LeadingLiterals[i])
                return false;
        }

        // The symbol would become the literal's own sign
        return !op.SignsInteger || !TryGetLiteral(_graph.FindWire(block.Uid, inputs[operand]).FromUid, out _);
    }

    /// <summary>
    /// Whether the block has Wires into its first <paramref name="operands"/> inputs and no others, and no Setting but
    /// the operator's.
    /// </summary>
    private bool IsOperator(ImportedBlock block, OperatorBlock op, int operands)
    {
        if (block.BlockId != op.Type.DefaultStyle.BlockId || !HasNoFloats(block))
            return false;

        for (var i = 0; i < op.Type.Inputs.Count; i++)
        {
            var isWired = _graph.FindWire(block.Uid, op.Type.Inputs[i]) != null;
            if (isWired != (i < operands))
                return false;
        }

        var settings = GetSettings(block);
        var value = ImportCompare.GetValue(op.Setting);
        return value == null
            ? settings.Count == 0
            : settings.Count == 1 && settings.TryGetValue((op.Setting.Kind, op.Setting.Key), out var stored) && stored == value;
    }

    private static bool HasNoFloats(ImportedBlock block)
    {
        return block.Floats.Values.All(f => f == 0);
    }

    /// <summary>
    /// What the block's own text has to give, so without the hiding that the default line does.
    /// </summary>
    private Dictionary<(SettingKind Kind, string Key), string> GetSettings(ImportedBlock block)
    {
        var settings = ImportCompare.GetValues(block.Settings, BlockManifest.GetPlacedType(block.BlockId));
        if (HidesAll)
            settings.Remove((SettingKind.Int, BlockSetting.Hide.Key));

        return settings;
    }
}
