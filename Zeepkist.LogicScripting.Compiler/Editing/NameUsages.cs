using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// Every place a Script without errors defines or uses one of its own names, read from the parsed Script. The top
/// level and each Function's body are separate scopes: a body sees only its inputs and its own names (ADR 0008).
/// </summary>
/// <remarks>
/// Not uses: a Library's name and its Functions' names in <c>timing.edge(...)</c>, Block Functions, the labels of a
/// call's named arguments (except a Script Function's inputs), outputs picked from blocks, and a word a named argument
/// gives that the scope doesn't define, such as <c>op: or</c>.
/// </remarks>
internal sealed class NameUsages
{
    // Deeper than any alias chain a Script writes, and it stops a definition that refers to itself
    private const int MaxAliasDepth = 32;

    private readonly Dictionary<Position, Token> _names = [];
    private readonly Dictionary<string, FunctionDefinition> _functions = [];
    private readonly Dictionary<string, Definition> _topDefinitions = [];
    private readonly Dictionary<FunctionDefinition, Dictionary<string, Definition>> _bodyDefinitions = [];
    private readonly List<NameUsage> _usages = [];
    private readonly List<MemberExpression> _unclearOutputs = [];

    private NameUsages(CompileResult compiled)
    {
        Compiled = compiled;
    }

    public IReadOnlyList<NameUsage> All => _usages;

    /// <summary>
    /// Every name the Script defines, in any scope: names, Functions, inputs and outputs.
    /// </summary>
    public HashSet<string> DefinedNames { get; } = [];

    public CompileResult Compiled { get; }

    public static NameUsages Read(List<Token> tokens, List<Statement> statements, CompileResult compiled)
    {
        var usages = new NameUsages(compiled);
        usages.Collect(tokens, statements);
        return usages;
    }

    /// <summary>
    /// The use the character at <paramref name="index"/> is in, or else the one it's right after; null when there's
    /// none.
    /// </summary>
    public NameUsage Find(int index)
    {
        return _usages.Find(u => u.Span.Start <= index && index < u.Span.End)
            ?? _usages.Find(u => u.Span.End == index);
    }

    /// <summary>
    /// A <c>.name</c> picking an output by the <paramref name="symbol"/>'s name from a value this can't follow to a
    /// call, such as an input, so it can't tell whether it's the symbol's; null when there's none or the symbol isn't
    /// an output.
    /// </summary>
    public MemberExpression FindUnclearOutput(NameSymbol symbol)
    {
        return symbol.Kind == NameSymbolKind.Output ? _unclearOutputs.Find(m => m.Output == symbol.Name) : null;
    }

    private void Collect(List<Token> tokens, List<Statement> statements)
    {
        foreach (var token in tokens.Where(t => t.Kind == TokenKind.Name))
        {
            _names[token.Position] = token;
        }

        foreach (var function in statements.OfType<FunctionDefinition>())
        {
            _functions[function.Name.Name] = function;
            _bodyDefinitions[function] = CollectDefinitions(function.Body);
            DefinedNames.Add(function.Name.Name);
            DefinedNames.UnionWith(function.Inputs.Select(i => i.Name));
            DefinedNames.UnionWith(function.Outputs.Select(o => o.Name));
            DefinedNames.UnionWith(_bodyDefinitions[function].Keys);
        }

        foreach (var pair in CollectDefinitions(statements))
        {
            _topDefinitions.Add(pair.Key, pair.Value);
        }

        DefinedNames.UnionWith(_topDefinitions.Keys);

        foreach (var statement in statements)
        {
            if (statement is FunctionDefinition function)
                VisitFunction(function);
            else
                VisitStatement(statement, null);
        }
    }

    private static Dictionary<string, Definition> CollectDefinitions(List<Statement> statements)
    {
        Dictionary<string, Definition> definitions = [];
        foreach (var definition in statements.OfType<Definition>())
        {
            if (!definitions.ContainsKey(definition.Name))
                definitions.Add(definition.Name, definition);
        }

        return definitions;
    }

    private void VisitFunction(FunctionDefinition function)
    {
        Add(new NameSymbol(NameSymbolKind.Function, function, function.Name.Name), function.Name.Position);
        foreach (var input in function.Inputs)
        {
            Add(new NameSymbol(NameSymbolKind.Input, function, input.Name), input.Position);
        }

        foreach (var output in function.Outputs)
        {
            Add(new NameSymbol(NameSymbolKind.Output, function, output.Name), output.Position);
        }

        foreach (var statement in function.Body)
        {
            VisitStatement(statement, function);
        }
    }

    /// <summary>
    /// <paramref name="scope"/> is the Function whose body it's in, or null at the top level.
    /// </summary>
    private void VisitStatement(Statement statement, FunctionDefinition scope)
    {
        switch (statement)
        {
            case Definition definition:
                Add(Resolve(definition.Name, scope), definition.Position);
                VisitExpression(definition.Value, scope);
                break;
            case ExpressionStatement expression:
                VisitExpression(expression.Expression, scope);
                break;
            case DefaultStatement defaultStatement:
                VisitConstant(defaultStatement.Setting.Value, scope);
                break;
        }
    }

    private void VisitExpression(Expression expression, FunctionDefinition scope)
    {
        switch (expression)
        {
            case NameExpression name:
                Add(Resolve(name.Name, scope), name.Position);
                break;
            case MemberExpression member:
                VisitExpression(member.Target, scope);
                VisitOutput(member, scope);
                break;
            case CallExpression call:
                VisitCall(call, scope);
                break;
            default:
                foreach (var child in expression.Children)
                {
                    VisitExpression(child, scope);
                }

                break;
        }
    }

    private void VisitCall(CallExpression call, FunctionDefinition scope)
    {
        FunctionDefinition function = null;
        BlockType blockType = null;
        if (call.Library == null && _functions.TryGetValue(call.Function, out function))
            Add(new NameSymbol(NameSymbolKind.Function, function, function.Name.Name), call.Position);
        else if (call.Library == null && !BlockManifest.TryGetFunction(call.Function, out blockType))
            Add(Resolve(call.Function, scope), call.Position); // a placed block to wire into: lamp_1(intensity: x)

        foreach (var argument in call.Arguments)
        {
            VisitExpression(argument, scope);
        }

        foreach (var argument in call.NamedArguments)
        {
            if (function != null && function.Inputs.Exists(i => i.Name == argument.Name))
                Add(new NameSymbol(NameSymbolKind.Input, function, argument.Name), argument.Position);

            if (blockType != null && GraphBuilder.GivesConstant(blockType, argument.Name))
                VisitConstant(argument.Value, scope);
            else
                VisitExpression(argument.Value, scope);
        }
    }

    /// <summary>
    /// A Setting's or style's value: a word such as <c>or</c>, even one the scope also defines as a name, unless it
    /// names an input of the Function it's in, which passes on the constant its call gives.
    /// </summary>
    private void VisitConstant(Expression value, FunctionDefinition scope)
    {
        if (value is NameExpression name && Resolve(name.Name, scope) is { Kind: NameSymbolKind.Input } input)
            Add(input, name.Position);
    }

    private void VisitOutput(MemberExpression member, FunctionDefinition scope)
    {
        var function = FindOutputs(member.Target, scope, 0, out var unclear);
        if (unclear)
            _unclearOutputs.Add(member);
        else if (function != null && function.Outputs.Exists(o => o.Name == member.Output))
            Add(new NameSymbol(NameSymbolKind.Output, function, member.Output), member.Position);
    }

    /// <summary>
    /// The Script Function with several outputs whose call <paramref name="target"/> is, directly, through names
    /// defined as it, or through the outputs of other Script Functions that give it; null when it's something else,
    /// such as a block. <paramref name="unclear"/> when that can't be told from the text: an input, or a call into a
    /// Library, could be any value.
    /// </summary>
    private FunctionDefinition FindOutputs(Expression target, FunctionDefinition scope, int depth, out bool unclear)
    {
        unclear = false;
        if (depth >= MaxAliasDepth)
        {
            unclear = true;
            return null;
        }

        switch (target)
        {
            case CallExpression { Library: null } call when _functions.TryGetValue(call.Function, out var function):
                // A Function with one output gives that output's value
                return function.Outputs.Count > 1
                    ? function
                    : FindOutputDefinition(function, function.Outputs[0].Name, depth, out unclear);
            case CallExpression { Library: not null }:
                unclear = true;
                return null;
            case MemberExpression member:
                var picked = FindOutputs(member.Target, scope, depth + 1, out unclear);
                return picked != null && picked.Outputs.Exists(o => o.Name == member.Output)
                    ? FindOutputDefinition(picked, member.Output, depth, out unclear)
                    : null;
            case NameExpression name:
                var symbol = Resolve(name.Name, scope);
                if (symbol == null)
                    return null;

                if (symbol.Kind == NameSymbolKind.Input)
                {
                    unclear = true;
                    return null;
                }

                var definitions = scope == null ? _topDefinitions : _bodyDefinitions[scope];
                return definitions.TryGetValue(name.Name, out var definition)
                    ? FindOutputs(definition.Value, scope, depth + 1, out unclear)
                    : null;
            default:
                return null;
        }
    }

    /// <summary>
    /// <see cref="FindOutputs"/> for the value <paramref name="function"/>'s body gives its output
    /// <paramref name="output"/>.
    /// </summary>
    private FunctionDefinition FindOutputDefinition(FunctionDefinition function, string output, int depth, out bool unclear)
    {
        unclear = false;
        return _bodyDefinitions[function].TryGetValue(output, out var definition)
            ? FindOutputs(definition.Value, function, depth + 1, out unclear)
            : null;
    }

    private NameSymbol Resolve(string name, FunctionDefinition scope)
    {
        if (scope == null)
            return _topDefinitions.ContainsKey(name) ? new NameSymbol(NameSymbolKind.TopLevel, null, name) : null;

        if (scope.Inputs.Exists(i => i.Name == name))
            return new NameSymbol(NameSymbolKind.Input, scope, name);

        if (scope.Outputs.Exists(o => o.Name == name))
            return new NameSymbol(NameSymbolKind.Output, scope, name);

        return _bodyDefinitions[scope].ContainsKey(name) ? new NameSymbol(NameSymbolKind.Local, scope, name) : null;
    }

    private void Add(NameSymbol symbol, Position position)
    {
        if (symbol != null && _names.TryGetValue(position, out var token) && token.Text == symbol.Name)
            _usages.Add(new NameUsage(symbol, token.Span));
    }
}
